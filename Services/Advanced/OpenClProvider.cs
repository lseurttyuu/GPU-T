using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

/// <summary>
/// Provides advanced OpenCL GPU property and memory information by orchestrating and parsing 'clinfo' JSON output.
/// </summary>
public class OpenClProvider : AdvancedDataProvider
{
    /// <summary>
    /// Executes the 'clinfo' process, parses the JSON output, and populates the collection with OpenCL details for the selected GPU.
    /// </summary>
    /// <param name="list">The target collection for the UI data rows.</param>
    /// <param name="selectedGpu">The descriptor of the currently selected GPU.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        try
        {

            string[] potentialClinfoPaths = {
                "/usr/bin/clinfo",
                "/bin/clinfo",
                "/usr/local/bin/clinfo"
            };

            string clinfoExecutable = "clinfo";

            foreach (var path in potentialClinfoPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    clinfoExecutable = path;
                    break;
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = clinfoExecutable, Arguments = "--json",
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            startInfo.Environment["RUSTICL_ENABLE"] = "radeonsi";

            using var process = Process.Start(startInfo);
            if (process == null) { AddRow(list, "Error", "Could not start clinfo"); return; }

            string rawOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(rawOutput)) { AddRow(list, "Error", "clinfo returned empty output"); return; }
            
            // clinfo may output warning text before the actual JSON payload; locate the JSON start.
            int jsonStartIndex = rawOutput.IndexOf('{');
            
            if (jsonStartIndex == -1) { AddRow(list, "Error", "No valid JSON found"); return; }
            
            string jsonOutput = rawOutput.Substring(jsonStartIndex);

            var root = JsonNode.Parse(jsonOutput);
            var platforms = root?["platforms"] as JsonArray;
            var devicesGroups = root?["devices"] as JsonArray;

            if (platforms == null || devicesGroups == null) { AddRow(list, "Error", "Invalid clinfo JSON structure"); return; }

            JsonNode? refClover = null, refRusticl = null, refAmdApp = null;
            
            // Categorize available OpenCL platforms to correlate devices with their drivers later.
            foreach (var p in platforms)
            {
                string pName = p?["CL_PLATFORM_NAME"]?.ToString() ?? "";
                
                if (pName.Contains("Clover", StringComparison.OrdinalIgnoreCase)) refClover = p;
                else if (pName.Contains("rusticl", StringComparison.OrdinalIgnoreCase)) refRusticl = p;
                else if (refAmdApp == null && !pName.Contains("Clover") && !pName.Contains("rusticl")) refAmdApp = p;
            }

            // Retrieve the authoritative device name for the selected GPU ID to filter the OpenCL device list.
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

                    // Determine the OpenCL implementation based on device name heuristics and versioning.
                    // 1. Rusticl: Identified by 'radeonsi' and OpenCL version >= 3.0.
                    if (devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase) && verNum >= 3.0f)
                    {
                        impl = "Rusticl";
                        platform = refRusticl;
                    }
                    // 2. Clover: Identified by 'radeonsi' and OpenCL version < 2.0.
                    else if (devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase) && verNum < 2.0f)
                    {
                        impl = "Clover";
                        platform = refClover;
                    }
                    // 3. AMD APP: Default proprietary driver when 'radeonsi' is absent.
                    else if (!devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase))
                    {
                        impl = "AMD APP";
                        platform = refAmdApp;
                    }

                    if (platform != null)
                    {
                        // Match the OpenCL device against the selected GPU name to ensure we only display relevant data.
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

    /// <summary>
    /// Populates the ViewModel collection with detailed OpenCL properties, memory stats, and capabilities.
    /// </summary>
    /// <param name="list">The target collection.</param>
    /// <param name="device">The JSON node representing the OpenCL device.</param>
    /// <param name="platform">The JSON node representing the OpenCL platform.</param>
    /// <param name="implType">The determined implementation type (e.g., Rusticl, Clover).</param>
    private void RenderDevice(ObservableCollection<AdvancedItemViewModel> list, JsonNode device, JsonNode? platform, string implType)
    {
        string platformName = platform?["CL_PLATFORM_NAME"]?.ToString() ?? "Unknown";
        
        // --- HEADER ---
        AddRow(list, $"Implementation: {implType}", "", true); 

        // --- GENERAL ---
        AddRow(list, "General Information", "", true);
        AddRow(list, "Platform Name", platformName);
        AddRow(list, "Device Name", device["CL_DEVICE_NAME"]?.ToString());
        
        var boardName = device["CL_DEVICE_BOARD_NAME_AMD"]?.ToString();
        if (!string.IsNullOrEmpty(boardName)) AddRow(list, "Board Name", boardName);
        
        AddRow(list, "Vendor", device["CL_DEVICE_VENDOR"]?.ToString());
        AddRow(list, "Device Version", device["CL_DEVICE_VERSION"]?.ToString());
        AddRow(list, "Driver Version", device["CL_DRIVER_VERSION"]?.ToString());
        AddRow(list, "OpenCL C Version", device["CL_DEVICE_OPENCL_C_VERSION"]?.ToString());
        AddRow(list, "Device Profile", device["CL_DEVICE_PROFILE"]?.ToString());
        AddRow(list, "Device Available", device["CL_DEVICE_AVAILABLE"]?.ToString() == "true" ? "Yes" : "No");
        AddRow(list, "Compiler Available", device["CL_DEVICE_COMPILER_AVAILABLE"]?.ToString() == "true" ? "Yes" : "No");

        // --- COMPUTE ---
        AddRow(list, "Compute Capabilities", "", true);
        AddRow(list, "Compute Units", device["CL_DEVICE_MAX_COMPUTE_UNITS"]?.ToString());
        AddRow(list, "Max Clock", $"{device["CL_DEVICE_MAX_CLOCK_FREQUENCY"]} MHz");
        
        // AMD Specific properties
        var simdPerCu = device["CL_DEVICE_SIMD_PER_COMPUTE_UNIT_AMD"];
        if (simdPerCu != null) AddRow(list, "SIMD per CU", simdPerCu.ToString());
        
        var simdWidth = device["CL_DEVICE_SIMD_WIDTH_AMD"];
        if (simdWidth != null) AddRow(list, "SIMD Width", simdWidth.ToString());
        
        var wavefront = device["CL_DEVICE_WAVEFRONT_WIDTH_AMD"];
        if (wavefront != null) AddRow(list, "Wavefront Width", wavefront.ToString());

        var gfxIp = device["CL_DEVICE_GFXIP_MAJOR_AMD"];
        if (gfxIp != null)
        {
            string ipVer = $"{gfxIp}.{device["CL_DEVICE_GFXIP_MINOR_AMD"]}.{device["CL_DEVICE_GFXIP_STEPPING_AMD"]}";
            AddRow(list, "GFX IP (AMD)", ipVer);
        }

        // --- FLOATING POINT CONFIG ---
        AddRow(list, "Floating Point Capabilities", "", true);
        RenderFlagsList(list, "Single Precision (FP32)", device["CL_DEVICE_SINGLE_FP_CONFIG"], "CL_FP_");
        RenderFlagsList(list, "Double Precision (FP64)", device["CL_DEVICE_DOUBLE_FP_CONFIG"], "CL_FP_");
        RenderFlagsList(list, "Half Precision (FP16)", device["CL_DEVICE_HALF_FP_CONFIG"], "CL_FP_");

        // --- MEMORY ---
        AddRow(list, "Memory", "", true);
        if (long.TryParse(device["CL_DEVICE_GLOBAL_MEM_SIZE"]?.ToString(), out long globalMem))
            AddRow(list, "Global Memory Size", FormatSizeMb(globalMem));
        
        var memChannels = device["CL_DEVICE_GLOBAL_MEM_CHANNELS_AMD"];
        if (memChannels != null) AddRow(list, "Memory Channels", memChannels.ToString());
        
        string cacheSize = device["CL_DEVICE_GLOBAL_MEM_CACHE_SIZE"]?.ToString() ?? "0";
        string cacheType = device["CL_DEVICE_GLOBAL_MEM_CACHE_TYPE"]?.ToString()?.Replace("CL_", "") ?? "None";
        AddRow(list, "Global Cache", $"{FormatSizeBytes(cacheSize)} ({cacheType})");
        
        if (long.TryParse(device["CL_DEVICE_LOCAL_MEM_SIZE"]?.ToString(), out long localMem))
            AddRow(list, "Local Memory", $"{FormatSizeKb(localMem)} ({device["CL_DEVICE_LOCAL_MEM_TYPE"]?.ToString()?.Replace("CL_", "")})"); 

        // --- SVM & QUEUES ---
        AddRow(list, "Queue & SVM", "", true);
        RenderFlagsList(list, "SVM Support", device["CL_DEVICE_SVM_CAPABILITIES"], "CL_DEVICE_SVM_");
        RenderFlagsList(list, "Queue On Host", device["CL_DEVICE_QUEUE_ON_HOST_PROPERTIES"], "CL_QUEUE_");
        RenderFlagsList(list, "Queue On Device", device["CL_DEVICE_QUEUE_ON_DEVICE_PROPERTIES"], "CL_QUEUE_");

        // --- LIMITS ---
        AddRow(list, "Limits", "", true);
        AddRow(list, "Max Memory Allocation", FormatSizeMb(long.Parse(device["CL_DEVICE_MAX_MEM_ALLOC_SIZE"]?.ToString() ?? "0")));
        AddRow(list, "Max Constant Buffer", FormatSizeMb(long.Parse(device["CL_DEVICE_MAX_CONSTANT_BUFFER_SIZE"]?.ToString() ?? "0")));
        AddRow(list, "Max Constant Args", device["CL_DEVICE_MAX_CONSTANT_ARGS"]?.ToString());
        AddRow(list, "Max Parameter Size", $"{device["CL_DEVICE_MAX_PARAMETER_SIZE"]} Bytes");
        
        AddRow(list, "Max Work Group Size", device["CL_DEVICE_MAX_WORK_GROUP_SIZE"]?.ToString());
        AddRow(list, "Max Work Item Dims", device["CL_DEVICE_MAX_WORK_ITEM_DIMENSIONS"]?.ToString());
        
        var workItemSizes = device["CL_DEVICE_MAX_WORK_ITEM_SIZES"] as JsonArray;
        if (workItemSizes != null)
            AddRow(list, "Max Work Item Sizes", string.Join(" x ", workItemSizes));

        // Conditionally render image support details; older drivers like Clover might lack support.
        string imgSupport = device["CL_DEVICE_IMAGE_SUPPORT"]?.ToString()?.ToLower() ?? "false";
        if (imgSupport == "true")
        {
            AddRow(list, "Max Samplers", device["CL_DEVICE_MAX_SAMPLERS"]?.ToString());
            AddRow(list, "Max Read Image Args", device["CL_DEVICE_MAX_READ_IMAGE_ARGS"]?.ToString());
            AddRow(list, "Max Write Image Args", device["CL_DEVICE_MAX_WRITE_IMAGE_ARGS"]?.ToString());
            
            string w2d = device["CL_DEVICE_IMAGE2D_MAX_WIDTH"]?.ToString() ?? "?";
            string h2d = device["CL_DEVICE_IMAGE2D_MAX_HEIGHT"]?.ToString() ?? "?";
            AddRow(list, "Max 2D Image", $"{w2d} x {h2d}");

            string w3d = device["CL_DEVICE_IMAGE3D_MAX_WIDTH"]?.ToString() ?? "?";
            string h3d = device["CL_DEVICE_IMAGE3D_MAX_HEIGHT"]?.ToString() ?? "?";
            string d3d = device["CL_DEVICE_IMAGE3D_MAX_DEPTH"]?.ToString() ?? "?";
            AddRow(list, "Max 3D Image", $"{w3d} x {h3d} x {d3d}");
        }
        else
        {
            AddRow(list, "Image Support", "No");
        }

        AddRow(list, "Max Pipe Args", device["CL_DEVICE_MAX_PIPE_ARGS"]?.ToString());
        
        // --- NATIVE VECTORS ---
        AddRow(list, "Native Vector Widths", "", true);
        string[] vecTypes = { "CHAR", "SHORT", "INT", "LONG", "FLOAT", "DOUBLE", "HALF" };
        foreach (var t in vecTypes)
        {
            var val = device[$"CL_DEVICE_NATIVE_VECTOR_WIDTH_{t}"];
            if (val != null) AddRow(list, t, val.ToString());
        }

        // --- EXTENSIONS ---
        AddRow(list, "Extensions", "", true);
        string exts = device["CL_DEVICE_EXTENSIONS"]?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(exts))
        {
            var extList = exts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x).ToList();
            AddRow(list, "Extensions Count", extList.Count.ToString());
            
            foreach(var ext in extList) AddRow(list, ext, "Supported");
        }

        AddRow(list, " ", " "); 
    }

    /// <summary>
    /// Renders capability flags as individual rows, mirroring the style of tools like GPU-Z.
    /// Handles JSON polymorphism where flags may appear as objects or arrays.
    /// </summary>
    /// <param name="list">The target collection.</param>
    /// <param name="headerName">The category header name.</param>
    /// <param name="node">The JSON node containing the flags.</param>
    /// <param name="prefixToRemove">The API prefix to strip for readability (e.g., "CL_FP_").</param>
    private void RenderFlagsList(ObservableCollection<AdvancedItemViewModel> list, string headerName, JsonNode? node, string prefixToRemove)
    {
        if (node == null)
        {
            AddRow(list, headerName, "N/A");
            return;
        }

        JsonArray? items = null;

        // Case 1: Node is an object containing specific keys (config, capabilities, queue_prop).
        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("config")) items = obj["config"] as JsonArray;
            else if (obj.ContainsKey("capabilities")) items = obj["capabilities"] as JsonArray;
            else if (obj.ContainsKey("queue_prop")) items = obj["queue_prop"] as JsonArray;
        }
        // Case 2: Node is a direct array (less common in current clinfo versions but possible).
        else if (node is JsonArray arr)
        {
            items = arr;
        }

        if (items != null && items.Count > 0)
        {
            // Iterate and display each flag with the header repeated for clarity.
            foreach (var item in items)
            {
                string flagName = item?.ToString().Replace(prefixToRemove, "").Trim() ?? "";
                if (!string.IsNullOrEmpty(flagName))
                {
                    AddRow(list, headerName, flagName);
                }
            }
        }
        else
        {
            // Handle empty lists or '0' raw values indicating no support.
            string val = node["raw"]?.ToString();
            if (val == "0") AddRow(list, headerName, "None");
            else AddRow(list, headerName, "No capabilities reported");
        }
    }

    /// <summary>
    /// Extracts the numeric version from an OpenCL version string (e.g., "OpenCL 3.0").
    /// </summary>
    private float ParseVer(string s)
    {
        var m = Regex.Match(s ?? "", @"OpenCL\s+(\d+\.\d+)");
        if (m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float v)) return v;
        return 0f;
    }
}