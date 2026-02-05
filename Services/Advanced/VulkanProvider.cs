using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

public class VulkanProvider : AdvancedDataProvider
{
    private string _targetIdHex = "";
    
    // Pola pomocnicze parsera
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

    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        _targetIdHex = "";
        
        // Wyciągnij ID z DeviceId (z ViewModela, tu musimy je pobrać ponownie lub przekazać)
        // Dla uproszczenia pobierzemy je ponownie z LinuxAmdGpuProbe, bo GpuListItem ma tylko "card0"
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

                // Detekcja zmiany GPU
                if (trimmed.StartsWith("GPU id :") || (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("=")))
                {
                    if (isTargetGpu) break; 
                    isTargetGpu = false;
                    propsBuffer.Clear();
                    _currentSection = "";
                    continue;
                }

                // Detekcja sekcji
                bool sectionChanged = CheckSectionChange(trimmed, list, isTargetGpu);
                if (sectionChanged) continue;

                // Parsowanie Properties
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

                // Parsowanie Memory, Extensions, Features
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

    private void ParseExtensions(string trimmed, ObservableCollection<AdvancedItemViewModel> list)
    {
        if (trimmed.StartsWith("VK_")) { var parts = trimmed.Split(':'); AddRow(list, parts[0].Trim(), "Supported"); }
    }

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

    private void CloseMemoryBlocks(ObservableCollection<AdvancedItemViewModel> list) { if (_currentSection == "MEMORY") { CommitHeap(list); CommitType(list); } }

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

    private string GetValue(string line) { var p = line.Split('='); return p.Length > 1 ? p[1].Trim() : ""; }
    private string ExtractParen(string l) { int s = l.LastIndexOf('('), e = l.LastIndexOf(')'); return s != -1 && e > s ? l.Substring(s + 1, e - s - 1) : ""; }
}