using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GPU_T.ViewModels;


namespace GPU_T.Services.Advanced;

public class OpenClProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "clinfo", Arguments = "--json",
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) { AddRow(list, "Error", "Could not start clinfo"); return; }

            string rawOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(rawOutput)) { AddRow(list, "Error", "clinfo empty output"); return; }
            int jsonStartIndex = rawOutput.IndexOf('{');
            if (jsonStartIndex == -1) { AddRow(list, "Error", "No valid JSON"); return; }
            string jsonOutput = rawOutput.Substring(jsonStartIndex);

            var root = JsonNode.Parse(jsonOutput);
            var platforms = root?["platforms"] as JsonArray;
            var devicesGroups = root?["devices"] as JsonArray;

            if (platforms == null || devicesGroups == null) { AddRow(list, "Error", "Invalid JSON"); return; }

            JsonNode? refClover = null, refRusticl = null, refAmdApp = null;
            foreach (var p in platforms)
            {
                string pName = p?["CL_PLATFORM_NAME"]?.ToString() ?? "";
                if (pName.Contains("Clover", StringComparison.OrdinalIgnoreCase)) refClover = p;
                else if (pName.Contains("rusticl", StringComparison.OrdinalIgnoreCase)) refRusticl = p;
                else if (refAmdApp == null && !pName.Contains("Clover") && !pName.Contains("rusticl")) refAmdApp = p;
            }

            // Pobierz nazwę karty z ViewModela (tutaj musimy ją mieć, więc trick z Probe)
            string appGpuName = "Unknown";
            if (selectedGpu != null)
            {
                var probe = GpuProbeFactory.Create(selectedGpu.Id);
                appGpuName = probe.LoadStaticData().DeviceName;
            }

            bool foundAny = false;
            foreach (var group in devicesGroups)
            {
                var online = group?["online"] as JsonArray;
                if (online == null) continue;

                foreach (var device in online)
                {
                    string devName = device["CL_DEVICE_NAME"]?.ToString() ?? "";
                    string verStr = device["CL_DEVICE_VERSION"]?.ToString() ?? "";
                    float verNum = ParseVer(verStr);
                    
                    JsonNode? platform = null;
                    string impl = "Unknown";

                    if (devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase))
                    {
                        if (verNum >= 3.0f) { impl = "Rusticl"; platform = refRusticl; }
                        else { impl = "Clover"; platform = refClover; }
                    }
                    else
                    {
                        impl = "AMD APP"; platform = refAmdApp;
                    }

                    if (platform != null)
                    {
                        // Matchowanie nazwy
                        string dispName = devName;
                        var board = device["CL_DEVICE_BOARD_NAME_AMD"]?.ToString();
                        if (!string.IsNullOrEmpty(board)) dispName = board;

                        string clean = dispName.Split('(')[0].Trim();
                        if (!string.IsNullOrEmpty(clean) && appGpuName.Contains(clean, StringComparison.OrdinalIgnoreCase))
                        {
                            foundAny = true;
                            RenderDevice(list, device, platform, impl);
                        }
                    }
                }
            }

            if (!foundAny) AddRow(list, "Info", "No matching OpenCL device found.");
        }
        catch (Exception ex) { AddRow(list, "Error", $"OpenCL logic failed: {ex.Message}"); }
    }

    private void RenderDevice(ObservableCollection<AdvancedItemViewModel> list, JsonNode device, JsonNode? platform, string implType)
    {
        AddRow(list, $"Implementation: {implType}", "", true); 
        AddRow(list, "General Information", "", true);
        AddRow(list, "Platform Name", platform?["CL_PLATFORM_NAME"]?.ToString());
        AddRow(list, "Device Name", device["CL_DEVICE_NAME"]?.ToString());
        
        var boardName = device["CL_DEVICE_BOARD_NAME_AMD"]?.ToString();
        if (!string.IsNullOrEmpty(boardName)) AddRow(list, "Board Name", boardName);
        
        AddRow(list, "Vendor", device["CL_DEVICE_VENDOR"]?.ToString());
        AddRow(list, "Driver Version", device["CL_DRIVER_VERSION"]?.ToString());
        AddRow(list, "OpenCL C Version", device["CL_DEVICE_OPENCL_C_VERSION"]?.ToString());
        AddRow(list, "Compute Units", device["CL_DEVICE_MAX_COMPUTE_UNITS"]?.ToString());
        AddRow(list, "Max Clock", $"{device["CL_DEVICE_MAX_CLOCK_FREQUENCY"]} MHz");

        AddRow(list, "Memory", "", true);
        if (long.TryParse(device["CL_DEVICE_GLOBAL_MEM_SIZE"]?.ToString(), out long globalMem))
            AddRow(list, "Global Memory Size", FormatSizeMb(globalMem));
        
        AddRow(list, "Extensions", "", true);
        string exts = device["CL_DEVICE_EXTENSIONS"]?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(exts))
        {
            var extList = exts.Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x).ToList();
            AddRow(list, "Extensions Count", extList.Count.ToString());
            foreach(var ext in extList) AddRow(list, ext, "Supported");
        }
        AddRow(list, " ", " "); 
    }

    private float ParseVer(string s)
    {
        var m = Regex.Match(s ?? "", @"OpenCL\s+(\d+\.\d+)");
        if (m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float v)) return v;
        return 0f;
    }
}