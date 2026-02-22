using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

/// <summary>
/// Provides advanced Vulkan GPU property and memory information by parsing vulkaninfo output.
/// </summary>
public class VulkanProvider : AdvancedDataProvider
{
    private string _targetIdHex = "";
    
    private string _pendingHeapName = "";
    private string _pendingHeapSize = "";
    private string _pendingHeapBudget = "";
    private string _pendingHeapUsage = "";
    private List<string> _pendingHeapFlags = new();
    private bool _parsingHeapFlags = false;

    private string _pendingTypeName = ""; 
    private string _pendingTypeHeapIndex = ""; 
    private List<string> _pendingTypeFlags = new();
    private bool _parsingTypeFlags = false;

    private string _currentSection = "";

    /// <summary>
    /// Loads advanced GPU data for the selected GPU and populates the provided list.
    /// </summary>
    /// <param name="list">Collection to populate with advanced GPU items.</param>
    /// <param name="selectedGpu">Selected GPU descriptor.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        _targetIdHex = "";
        
        // Extracts the device ID from static probe data for target GPU matching.
        if (selectedGpu != null)
        {
            var probe = GpuProbeFactory.Create(selectedGpu.Id);
            var data = probe.LoadStaticData();
            if (!string.IsNullOrEmpty(data.DeviceId))
            {
                var parts = data.DeviceId.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) _targetIdHex = parts[1].Trim().ToUpper();
            }
        }

        RunVulkanInfo(list);
    }

    /// <summary>
    /// Runs vulkaninfo and parses its output to extract advanced GPU properties.
    /// </summary>
    /// <param name="list">Collection to populate with parsed items.</param>
    private void RunVulkanInfo(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "vulkaninfo",
                Arguments = "",
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) { AddRow(list, "Error", "Could not start vulkaninfo"); return; }

            using StreamReader reader = process.StandardOutput;
            string? line;
            bool isTargetGpu = false;
            bool foundAnyMatch = false; 
            _currentSection = "";   
            var propsBuffer = new List<(string Key, string Value)>();

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // GPU change detection
                if (trimmed.StartsWith("GPU id :") || (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("=")))
                {
                    if (isTargetGpu) 
                    {
                        // Kill vulkaninfo - we already got the data we wanted
                        try { process.Kill(); } catch {} 
                        break; 
                    }
                    isTargetGpu = false;
                    propsBuffer.Clear();
                    _currentSection = "";
                    continue;
                }

                // Section change detection
                bool sectionChanged = CheckSectionChange(trimmed, list, isTargetGpu);
                if (sectionChanged) continue;

                // Properties parsing (only if we're in the target GPU block or buffering for potential match)
                if (_currentSection == "PROPERTIES")
                {
                    if (trimmed.StartsWith("deviceID"))
                    {
                        string val = GetValue(trimmed);
                        string currentIdHex = val.Replace("0x", "").Trim().ToUpper();

                        if (!string.IsNullOrEmpty(_targetIdHex) && currentIdHex == _targetIdHex)
                        {
                            isTargetGpu = true; 
                            foundAnyMatch = true;
                            AddRow(list, "General", "", true);
                            foreach (var prop in propsBuffer) AddRow(list, prop.Key, prop.Value);
                            propsBuffer.Clear();
                            AddRow(list, "Device ID", $"0x{currentIdHex}");
                        }
                        else
                        {
                            isTargetGpu = false;
                            propsBuffer.Clear();
                        }
                    }
                    else
                    {
                        ParseProperties(trimmed, list, isTargetGpu, propsBuffer);
                    }
                }

                if (!isTargetGpu) continue; 

                // Parsing memory, extensions, and features only if we're in the target GPU block
                if (_currentSection == "MEMORY") ParseMemory(trimmed, line, list);
                else if (_currentSection == "EXTENSIONS") ParseExtensions(trimmed, list);
                else if (_currentSection == "FEATURES") ParseFeatures(trimmed, list);
            }
            
            CloseMemoryBlocks(list); 
            process.WaitForExit();

            if (!foundAnyMatch)
            {
                AddRow(list, "Info", "GPU Match Failed");
                AddRow(list, "Target ID", _targetIdHex);
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"Parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks for section changes in vulkaninfo output and updates parsing state.
    /// </summary>
    /// <param name="trimmed">Current line trimmed.</param>
    /// <param name="list">Collection to populate.</param>
    /// <param name="isTargetGpu">Indicates if parsing target GPU.</param>
    /// <returns>True if section changed, otherwise false.</returns>
    private bool CheckSectionChange(string trimmed, ObservableCollection<AdvancedItemViewModel> list, bool isTargetGpu)
    {
        if (trimmed.StartsWith("VkPhysicalDeviceMemoryProperties:")) { CloseMemoryBlocks(list); _currentSection = "MEMORY"; if (isTargetGpu) AddRow(list, "Memory Heaps", "", true); return true; }
        else if (trimmed.StartsWith("VkPhysicalDeviceFeatures:")) { CloseMemoryBlocks(list); _currentSection = "FEATURES"; if (isTargetGpu) AddRow(list, "Device Features", "", true); return true; }
        else if (trimmed.StartsWith("VkPhysicalDeviceProperties:") || trimmed.Contains("Device Properties and Extensions:")) { CloseMemoryBlocks(list); _currentSection = "PROPERTIES"; return true; }
        else if (trimmed.StartsWith("Device Extensions:")) { CloseMemoryBlocks(list); _currentSection = "EXTENSIONS"; if (isTargetGpu) AddRow(list, "Extensions", "", true); return true; }
        else if (trimmed.EndsWith("Features:") || trimmed.EndsWith("FeaturesEXT:") || trimmed.EndsWith("FeaturesKHR:")) { CloseMemoryBlocks(list); _currentSection = "FEATURES"; return true; }
        else if (trimmed.EndsWith("Properties:") || trimmed.EndsWith("PropertiesEXT:") || trimmed.EndsWith("PropertiesKHR:")) { CloseMemoryBlocks(list); _currentSection = "OTHER_PROPS"; return true; }
        return false;
    }

    /// <summary>
    /// Parses GPU properties from vulkaninfo output.
    /// </summary>
    /// <param name="trimmed">Current line trimmed.</param>
    /// <param name="list">Collection to populate.</param>
    /// <param name="isTargetGpu">Indicates if parsing target GPU.</param>
    /// <param name="buffer">Buffer for properties.</param>
    private void ParseProperties(string trimmed, ObservableCollection<AdvancedItemViewModel> list, bool isTargetGpu, List<(string, string)> buffer)
    {
        string k = "", v = "";
        if (trimmed.StartsWith("deviceName"))      { k = "Device Name"; v = GetValue(trimmed); }
        else if (trimmed.StartsWith("apiVersion")) { k = "API Version"; v = GetValue(trimmed); }
        else if (trimmed.StartsWith("driverVersion")) { k = "Driver Version"; v = GetValue(trimmed); }
        else if (trimmed.StartsWith("deviceType")) { k = "Device Type"; v = GetValue(trimmed); }
        else if (trimmed.StartsWith("vendorID"))   { k = "Vendor ID"; v = GetValue(trimmed); }

        if (!string.IsNullOrEmpty(k))
        {
            if (isTargetGpu) AddRow(list, k, v);
            else buffer.Add((k, v));
        }
    }

    /// <summary>
    /// Parses memory heap and type information from vulkaninfo output.
    /// </summary>
    /// <param name="trimmed">Current line trimmed.</param>
    /// <param name="line">Original line.</param>
    /// <param name="list">Collection to populate.</param>
    private void ParseMemory(string trimmed, string line, ObservableCollection<AdvancedItemViewModel> list)
    {
        if (trimmed.StartsWith("memoryHeaps[")) { CommitHeap(list); CommitType(list); _pendingHeapName = trimmed.Replace(":", ""); }
        else if (trimmed.StartsWith("size") && !string.IsNullOrEmpty(_pendingHeapName)) _pendingHeapSize = ExtractParen(line);
        else if (trimmed.StartsWith("budget") && !string.IsNullOrEmpty(_pendingHeapName)) _pendingHeapBudget = ExtractParen(line);
        else if (trimmed.StartsWith("usage") && !string.IsNullOrEmpty(_pendingHeapName)) _pendingHeapUsage = ExtractParen(line);
        else if (trimmed.StartsWith("memoryTypes")) 
        { 
            if (trimmed.Contains("count =")) { CommitHeap(list); AddRow(list, "Memory Types", "", true); } 
            else if (trimmed.EndsWith("]:")) { CommitType(list); _pendingTypeName = trimmed.Replace(":", ""); _parsingTypeFlags = false; } 
        }
        else if (trimmed.StartsWith("heapIndex")) _pendingTypeHeapIndex = GetValue(trimmed);
        else if (trimmed.StartsWith("propertyFlags")) _parsingTypeFlags = true;
        else if (trimmed.StartsWith("usable for:")) { _parsingTypeFlags = false; CommitType(list); }
        else
        {
            if (trimmed.StartsWith("flags:") && !string.IsNullOrEmpty(_pendingHeapName)) _parsingHeapFlags = true;
            else if (_parsingHeapFlags && !string.IsNullOrEmpty(_pendingHeapName)) { if (!line.StartsWith("\t") && !line.StartsWith("    ")) _parsingHeapFlags = false; else if (!trimmed.Contains("count =")) _pendingHeapFlags.Add(trimmed); }
            else if (_parsingTypeFlags && !string.IsNullOrEmpty(_pendingTypeName)) { if (trimmed.StartsWith("MEMORY_PROPERTY_")) _pendingTypeFlags.Add(trimmed); }
        }
    }

    /// <summary>
    /// Parses extension information from vulkaninfo output.
    /// </summary>
    /// <param name="trimmed">Current line trimmed.</param>
    /// <param name="list">Collection to populate.</param>
    private void ParseExtensions(string trimmed, ObservableCollection<AdvancedItemViewModel> list)
    {
        if (trimmed.StartsWith("VK_")) { var parts = trimmed.Split(':'); AddRow(list, parts[0].Trim(), "Supported"); }
    }

    /// <summary>
    /// Parses feature information from vulkaninfo output.
    /// </summary>
    /// <param name="trimmed">Current line trimmed.</param>
    /// <param name="list">Collection to populate.</param>
    private void ParseFeatures(string trimmed, ObservableCollection<AdvancedItemViewModel> list)
    {
        if (trimmed.Contains("=") && !trimmed.StartsWith("Userspace"))
        {
            var kv = trimmed.Split(new[]{'='}, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                string k = kv[0].Trim();
                string val = kv[1].Trim();
                if (val == "true") val = "Yes"; else if (val == "false") val = "No";
                AddRow(list, k, val);
            }
        }
    }

    /// <summary>
    /// Closes memory parsing blocks and commits pending heap/type data.
    /// </summary>
    /// <param name="list">Collection to populate.</param>
    private void CloseMemoryBlocks(ObservableCollection<AdvancedItemViewModel> list)
    {
        if (_currentSection == "MEMORY") { CommitHeap(list); CommitType(list); }
    }

    /// <summary>
    /// Commits pending heap information to the list.
    /// </summary>
    /// <param name="list">Collection to populate.</param>
    private void CommitHeap(ObservableCollection<AdvancedItemViewModel> list)
    {
        if (string.IsNullOrEmpty(_pendingHeapName)) return;
        string flagsStr = "None";
        if (_pendingHeapFlags.Count > 0)
        {
            var cleanFlags = _pendingHeapFlags.Select(f => f.Replace("MEMORY_HEAP_", "").Replace("_BIT", "").Trim()).Where(f => f != "None");
            if (cleanFlags.Any()) flagsStr = string.Join(", ", cleanFlags);
        }
        string details = "";
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(_pendingHeapBudget)) parts.Add($"Budget: {_pendingHeapBudget}");
        if (!string.IsNullOrEmpty(_pendingHeapUsage)) parts.Add($"Usage: {_pendingHeapUsage}");
        if (parts.Count > 0) details = $"({string.Join(", ", parts)})";

        string val = $"{_pendingHeapSize} {details} ({flagsStr})".Trim();
        val = Regex.Replace(val, @"\s+", " ");
        AddRow(list, _pendingHeapName, val);

        _pendingHeapName = ""; _pendingHeapSize = ""; _pendingHeapBudget = ""; _pendingHeapUsage = "";
        _pendingHeapFlags.Clear(); _parsingHeapFlags = false;
    }

    /// <summary>
    /// Commits pending memory type information to the list.
    /// </summary>
    /// <param name="list">Collection to populate.</param>
    private void CommitType(ObservableCollection<AdvancedItemViewModel> list)
    {
        if (string.IsNullOrEmpty(_pendingTypeName)) return;
        string displayName = _pendingTypeName.Replace("memoryTypes", "Type ").Replace("[", "").Replace("]", "").Trim();
        AddRow(list, displayName, $"Heap Index {_pendingTypeHeapIndex}");

        if (_pendingTypeFlags.Count > 0)
        {
            var cleanFlags = _pendingTypeFlags.Select(f => f.Replace("MEMORY_PROPERTY_", "").Replace("_BIT", "").Trim()).Where(f => f != "None");
            foreach (var flag in cleanFlags) AddRow(list, displayName, flag);
        }
        _pendingTypeName = ""; _pendingTypeHeapIndex = ""; _pendingTypeFlags.Clear(); _parsingTypeFlags = false;
    }

    /// <summary>
    /// Extracts value from a line with '=' separator.
    /// </summary>
    /// <param name="line">Line to parse.</param>
    /// <returns>Extracted value.</returns>
    private string GetValue(string line) { var p = line.Split('='); return p.Length > 1 ? p[1].Trim() : ""; }
    /// <summary>
    /// Extracts value inside parentheses from a line.
    /// </summary>
    /// <param name="l">Line to parse.</param>
    /// <returns>Extracted value.</returns>
    private string ExtractParen(string l) { int s = l.LastIndexOf('('), e = l.LastIndexOf(')'); return s != -1 && e > s ? l.Substring(s + 1, e - s - 1) : ""; }
}