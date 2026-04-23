using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using GPU_T.ViewModels;
using GPU_T.Services.Probes;
using GPU_T.Services.Probes.LinuxNvidia; // For NvidiaSidecarHelper

namespace GPU_T.Services.Advanced.LinuxNvidia;

/// <summary>
/// Provides advanced multimedia encoding (NVENC) and decoding (NVDEC) capabilities.
/// Implements a Hybrid Approach: Prefers native libnvidia-encode API data, but falls back 
/// to Compute Capability architecture estimation if the native libraries are missing or fail.
/// </summary>
public class LinuxNvidiaMultimediaProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();

        try
        {
            string pciString = "";
            string deviceName = "";

            if (selectedGpu != null)
            {
                var probe = GpuProbeFactory.Create(selectedGpu.Id);
                var staticData = probe.LoadStaticData();
                pciString = staticData.BusId;
                deviceName = staticData.DeviceName;
            }

            // Try the native NVENC sidecar first
            string pciArg = !string.IsNullOrEmpty(pciString) && pciString != "Unknown" ? $" --pci {pciString}" : "";
            string rawData = LinuxNvidiaSidecarHelper.Run("--nvenc" + pciArg, 1000);
            
            // If NVENC crashes/fails, fallback to the safe CUDA call
            if (string.IsNullOrWhiteSpace(rawData))
            {
                rawData = LinuxNvidiaSidecarHelper.Run("--cuda" + pciArg, 1000);
                
                // If CUDA also fails, the NVIDIA driver is completely broken or missing.
                if (string.IsNullOrWhiteSpace(rawData))
                {
                    AddRow(list, "Error", "Failed to communicate with NVIDIA sidecar (Driver error).");
                    return;
                }
            }

            // Parse the output into distinct buckets
            double cc = 0.0;
            var nvencLines = new List<string>();
            var nvdecLines = new List<string>();
            string currentSection = "";

            var lines = rawData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string t = line.Trim();
                
                if (t.StartsWith("Compute Capability="))
                {
                    string ccStr = t.Split('=')[1].Trim();
                    double.TryParse(ccStr, NumberStyles.Any, CultureInfo.InvariantCulture, out cc);
                }
                else if (t == "[NVENC]") currentSection = "NVENC";
                else if (t == "[NVDEC]") currentSection = "NVDEC";
                else if (t.Contains('='))
                {
                    if (currentSection == "NVENC") nvencLines.Add(t);
                    else if (currentSection == "NVDEC") nvdecLines.Add(t);
                }
            }

            if (cc < 5.0)
            {
                AddRow(list, "NVENC / NVDEC", "Hardware encoding/decoding not supported on this architecture (Pre-Maxwell).");
                return;
            }

            // HARDWARE ENCODING (NVENC)
            AddRow(list, "Video Encoding (NVENC)", "", true);
            if (nvencLines.Count > 0)
            {
                RenderRealData(list, nvencLines);
            }
            else
            {
                AddRow(list, "API Status", "Native library failed; using architecture estimation");
                RenderFallbackNvenc(list, cc);
            }

            // HARDWARE DECODING (NVDEC)
            AddRow(list, "Video Decoding (NVDEC)", "", true);
            if (nvdecLines.Count > 0)
            {
                RenderRealData(list, nvdecLines);
            }
            else
            {
                AddRow(list, "API Status", "Native library failed; using architecture estimation");
                RenderFallbackNvdec(list, cc);
            }

            // GENERATIONS & LIMITS
            AddRow(list, "Architecture Details", "", true);
            AddRow(list, "NVENC Generation", GetNvencGen(cc));
            AddRow(list, "NVDEC Generation", GetNvdecGen(cc));
            
            // Only render fallback session limits if the real NVENC API didn't already provide them
            if (nvencLines.Count == 0 || !rawData.Contains("Concurrent Sessions"))
            {
                bool isConsumerCard = deviceName.Contains("GeForce") || deviceName.Contains("GTX") || deviceName.Contains("RTX 2") || deviceName.Contains("RTX 3") || deviceName.Contains("RTX 4");
                AddRow(list, "Concurrent Encode Sessions", isConsumerCard ? "Max 8 (Driver Restricted Estimated)" : "Unlimited (Enterprise Estimated)");
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"Multimedia check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Renders exact data pulled directly from the NVIDIA native libraries.
    /// </summary>
    private void RenderRealData(ObservableCollection<AdvancedItemViewModel> list, List<string> lines)
    {
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                AddRow(list, parts[0].Trim(), parts[1].Trim());
            }
        }
    }

    /// <summary>
    /// Fallback logic for Encoding if libnvidia-encode.so.1 is missing or hardware is stripped.
    /// </summary>
    private void RenderFallbackNvenc(ObservableCollection<AdvancedItemViewModel> list, double cc)
    {
        AddRow(list, "H.264 (AVC) Encode", cc >= 5.0 ? "Supported" : "No");
        AddRow(list, "H.264 B-Frames", cc >= 5.0 ? "Supported" : "No");
        AddRow(list, "HEVC (H.265) Encode", cc >= 6.0 ? "Supported" : "No"); // Pascal
        AddRow(list, "HEVC 10-bit Encode", cc >= 6.1 ? "Supported" : "No");  // Pascal GTX 1080/1070+
        AddRow(list, "HEVC B-Frames", cc >= 7.5 ? "Supported" : "No");       // Turing (GTX 1660 / RTX 2000)
        AddRow(list, "HEVC 4:4:4 Chroma", cc >= 7.5 ? "Supported" : "No");
        AddRow(list, "AV1 Encode", cc >= 8.9 ? "Supported" : "No");          // Ada Lovelace (RTX 4000)
    }

    /// <summary>
    /// Fallback logic for Decoding if libnvcuvid.so.1 is missing.
    /// </summary>
    private void RenderFallbackNvdec(ObservableCollection<AdvancedItemViewModel> list, double cc)
    {
        AddRow(list, "H.264 Decode", cc >= 5.0 ? "Supported" : "No");
        AddRow(list, "HEVC (H.265) Decode", cc >= 5.2 ? "Supported" : "No"); // Maxwell Gen 2
        AddRow(list, "HEVC 10-bit Decode", cc >= 6.0 ? "Supported" : "No");  // Pascal
        AddRow(list, "HEVC 12-bit Decode", cc >= 7.0 ? "Supported" : "No");  // Volta/Turing
        AddRow(list, "VP9 Decode", cc >= 6.1 ? "Supported" : "No");          // Pascal Refresh
        AddRow(list, "VP9 10/12-bit Decode", cc >= 7.0 ? "Supported" : "No");
        AddRow(list, "AV1 Decode", cc >= 8.0 ? "Supported" : "No");          // Ampere (RTX 3000)
    }

    private string GetNvencGen(double cc)
    {
        if (cc >= 8.9) return "8th Gen (Ada Lovelace)";
        if (cc >= 7.5) return "7th Gen (Turing/Ampere)";
        if (cc >= 6.0) return "6th Gen (Pascal)";
        if (cc >= 5.2) return "5th Gen (Maxwell Gen 2)";
        if (cc >= 5.0) return "4th Gen (Maxwell Gen 1)";
        return "Unknown";
    }

    private string GetNvdecGen(double cc)
    {
        if (cc >= 8.9) return "5th Gen (Ada Lovelace)";
        if (cc >= 8.0) return "5th Gen (Ampere)";
        if (cc >= 7.0) return "4th Gen (Volta/Turing)";
        if (cc >= 6.1) return "3rd Gen (Pascal Refresh)";
        if (cc >= 6.0) return "2nd Gen (Pascal)";
        if (cc >= 5.2) return "1st Gen (Maxwell Gen 2)";
        return "Unknown";
    }
}