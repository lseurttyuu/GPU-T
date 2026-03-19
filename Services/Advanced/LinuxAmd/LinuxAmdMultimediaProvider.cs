using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced.LinuxAmd;

/// <summary>
/// Provides advanced multimedia codec and VA-API support information for AMD GPUs on Linux.
/// </summary>
public class LinuxAmdMultimediaProvider : AdvancedDataProvider
{
    /// <summary>
    /// Loads VA-API device and codec support information for the selected AMD GPU.
    /// </summary>
    /// <param name="list">The collection to populate with advanced item view models.</param>
    /// <param name="selectedGpu">The currently selected GPU item, or null if not specified.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();

        if (selectedGpu == null) return;

        try
        {
            var renderNodes = Directory.GetFiles("/dev/dri", "renderD*");
            if (renderNodes.Length == 0)
            {
                AddRow(list, "Error", "No /dev/dri/renderD* devices found.");
                return;
            }

            // 1. Fetch the target hardware IDs from our static probe data
            var probe = GpuProbeFactory.Create(selectedGpu.Id);
            var staticData = probe.LoadStaticData();
            
            // Extract the core device ID from "Vendor Device - SubVendor SubDevice"
            var parts = staticData.DeviceId.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string targetDevice = parts.Length >= 2 ? parts[1].ToUpper() : "";
            
            string targetRevision = staticData.Revision.ToUpper(); 
            string targetUniqueId = staticData.UniqueId?.Replace("0x", "").ToUpper() ?? "";

            bool foundDevice = false;

            // 2. Iterate through DRM render nodes to find the exact hardware match
            foreach (var node in renderNodes)
            {
                string nodeName = Path.GetFileName(node); // e.g., "renderD128"
                string deviceDir = $"/sys/class/drm/{nodeName}/device";

                if (!Directory.Exists(deviceDir)) continue;

                // Read sysfs values and normalize them immediately (strip "0x" and uppercase)
                string nodeDevice = ReadSysfsValue($"{deviceDir}/device").Replace("0x", "").ToUpper();
                string nodeRevision = ReadSysfsValue($"{deviceDir}/revision").Replace("0x", "").ToUpper();
                string nodeUniqueId = ReadSysfsValue($"{deviceDir}/unique_id").Replace("0x", "").ToUpper();

                // Check for a match
                bool isDeviceMatch = targetDevice == nodeDevice;
                bool isRevisionMatch = targetRevision == nodeRevision;
                
                // If the target has a valid unique_id, enforce a strict match.
                bool isUniqueIdMatch = true; 
                if (!string.IsNullOrEmpty(targetUniqueId) && targetUniqueId != "UNKNOWN" && !string.IsNullOrEmpty(nodeUniqueId))
                {
                    isUniqueIdMatch = targetUniqueId == nodeUniqueId;
                }

                // If we found our exact GPU, execute vainfo ONLY for this node
                if (isDeviceMatch && isRevisionMatch && isUniqueIdMatch)
                {
                    foundDevice = true;
                    ExecuteAndParseVaInfo(list, node);
                    break; // Stop iterating, we found and processed our target
                }
            }

            if (!foundDevice)
            {
                AddRow(list, "Info", "No matching VA-API device found.");
                AddRow(list, "Note", "Ensure 'vainfo' is installed.");
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"VA-API check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes vainfo on the matched node and parses the codec results.
    /// </summary>
    private void ExecuteAndParseVaInfo(ObservableCollection<AdvancedItemViewModel> list, string node)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "vainfo",
            Arguments = $"--display drm --device {node}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string fullOutput = output + "\n" + error;

        // Parse the Driver Info Line
        string driverInfo = "Unknown";
        using (var reader = new StringReader(fullOutput))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("Driver version:"))
                {
                    driverInfo = line.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "Unknown";
                    break;
                }
            }
        }

        AddRow(list, "General", "", true);
        AddRow(list, "Device Node", node);
        AddRow(list, "Driver Info", driverInfo);
        AddRow(list, "Supported Codecs", "", true);

        // Parse Codec Profiles
        using (var reader = new StringReader(fullOutput))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("VAProfile") && line.Contains(":"))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string profileNice = CleanVaProfile(parts[0].Trim());
                        string entryNice = CleanVaEntrypoint(parts[1].Trim());
                        if (profileNice != "None") AddRow(list, profileNice, entryNice);
                    }
                }
            }
        }
    }

    private string ReadSysfsValue(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Cleans and formats VA-API profile strings for display.
    /// </summary>
    /// <param name="profile">The raw VA-API profile string.</param>
    /// <returns>A formatted profile name.</returns>
    private string CleanVaProfile(string profile)
    {
        string p = profile.Replace("VAProfile", "").Trim();
        p = p.Replace("MPEG2", "MPEG-2 ");
        p = p.Replace("MPEG4", "MPEG-4 ");
        p = p.Replace("H264", "H.264 ");
        p = p.Replace("HEVC", "H.265 (HEVC) ");
        p = p.Replace("VC1", "VC-1 ");
        p = p.Replace("VP8", "VP8 ");
        p = p.Replace("VP9", "VP9 ");
        p = p.Replace("AV1", "AV1 ");
        p = p.Replace("JPEGBaseline", "JPEG Baseline");
        p = p.Replace("None", "None");
        return p.Trim();
    }

    /// <summary>
    /// Cleans and formats VA-API entrypoint strings for display.
    /// </summary>
    /// <param name="entrypoint">The raw VA-API entrypoint string.</param>
    /// <returns>A formatted entrypoint name.</returns>
    private string CleanVaEntrypoint(string entrypoint)
    {
        if (entrypoint.Contains("VLD")) return "Decode";
        if (entrypoint.Contains("EncSlice")) return "Encode";
        if (entrypoint.Contains("EncPicture")) return "Encode (Picture)";
        if (entrypoint.Contains("VideoProc")) return "Video Processing";
        return entrypoint.Replace("VAEntrypoint", "");
    }
}