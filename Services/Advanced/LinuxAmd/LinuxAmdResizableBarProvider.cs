using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced.LinuxAmd;

/// <summary>
/// Provides advanced diagnostic information about PCI-Express Resizable BAR support for AMD GPUs on Linux.
/// </summary>
public class LinuxAmdResizableBarProvider : AdvancedDataProvider
{
    /// <summary>
    /// Loads Resizable BAR and related PCI/firmware information into the provided collection for the selected AMD GPU.
    /// </summary>
    /// <param name="list">The collection to populate with advanced item view models.</param>
    /// <param name="selectedGpu">The currently selected GPU item, or null if not specified.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        try
        {
            string busId = "";
            string deviceName = "";

            if (selectedGpu != null)
            {
                var probe = GpuProbeFactory.Create(selectedGpu.Id);
                var staticData = probe.LoadStaticData();
                
                busId = staticData.BusId;
                deviceName = staticData.DeviceName;
            }

            if (string.IsNullOrEmpty(busId)) { AddRow(list, "Error", "Could not determine PCI Bus ID"); return; }

            var startInfo = new ProcessStartInfo
            {
                FileName = "lspci",
                Arguments = $"-vv -s {busId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) { AddRow(list, "Error", "Could not start lspci (pciutils required)"); return; }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var barSizes = new List<(string Name, string SizeText, long SizeBytes)>();
            bool has64BitBar = false;
            long maxBarSize = 0;
            int barCounter = 0; 

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (t.Contains("Memory at") && t.Contains("[size="))
                {
                    int sizeStart = t.LastIndexOf("[size=");
                    if (sizeStart != -1)
                    {
                        int sizeEnd = t.IndexOf(']', sizeStart);
                        if (sizeEnd != -1)
                        {
                            string sizeStr = t.Substring(sizeStart + 6, sizeEnd - sizeStart - 6); 
                            long bytes = ParseLspciSize(sizeStr);
                            
                            // Tracks the largest BAR size and presence of 64-bit BAR for Resizable BAR eligibility.
                            if (bytes > maxBarSize) maxBarSize = bytes;
                            if (t.Contains("64-bit")) has64BitBar = true;

                            string barName;
                            if (t.StartsWith("Region")) { var parts = t.Split(':'); barName = parts[0].Replace("Region", "BAR"); }
                            else { barName = $"BAR {barCounter}"; barCounter++; }

                            barSizes.Add((barName, sizeStr, bytes));
                        }
                    }
                }
            }

            // Determines Resizable BAR status based on maximum BAR size threshold (256MB).
            bool isReBarEnabled = maxBarSize > 268435456; 

            AddRow(list, "PCI-Express Resizable BAR", "", true);
            AddRow(list, "Resizable BAR", isReBarEnabled ? "Enabled" : "Disabled");

            AddRow(list, "Resizable BAR Requirements", "", true);
            bool isRdna = deviceName.Contains("7900") || deviceName.Contains("Navi") || deviceName.Contains("RX 6") || deviceName.Contains("RX 7") || deviceName.Contains("RX 8") || deviceName.Contains("RX 9");
            AddRow(list, "GPU Hardware Support", isRdna ? "Yes" : "Unknown");
            AddRow(list, "Above 4G Decode enabled", has64BitBar ? "Yes" : "No/Unknown");
            AddRow(list, "Resizable BAR enabled in BIOS", isReBarEnabled ? "Yes" : "Disabled or Unsupported");

            bool isUefi = Directory.Exists("/sys/firmware/efi");
            AddRow(list, "CSM disabled", isUefi ? "Yes" : "No (Legacy Mode)");
            AddRow(list, "Linux running in UEFI Mode", isUefi ? "Yes" : "No"); 
            AddRow(list, "64-Bit Operating System", Environment.Is64BitOperatingSystem ? "Yes" : "No");

            string kernelDriver = "";
            foreach (var l in lines) { string trimL = l.Trim(); if (trimL.StartsWith("Kernel driver in use:")) kernelDriver = trimL.Split(':')[1].Trim(); }
            bool driverOk = kernelDriver.Contains("amdgpu");
            AddRow(list, "Graphics Driver Support", driverOk ? "Yes" : $"Unknown ({kernelDriver})");

            AddRow(list, "PCI-Express BAR Sizes", "", true);
            if (barSizes.Count > 0)
            {
                foreach (var bar in barSizes.OrderBy(b => b.Name)) AddRow(list, bar.Name, bar.SizeText); 
            }
            else { AddRow(list, "Info", "No memory regions found"); }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"ReBAR check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses a size string from lspci output and returns its value in bytes.
    /// </summary>
    /// <param name="sizeStr">The size string to parse (e.g., "256M", "1G").</param>
    /// <returns>The size in bytes as a long integer.</returns>
    private long ParseLspciSize(string sizeStr)
    {
        if (string.IsNullOrEmpty(sizeStr)) return 0;
        char suffix = sizeStr.Last();
        if (char.IsDigit(suffix)) return long.Parse(sizeStr);
        string numberPart = sizeStr.Substring(0, sizeStr.Length - 1);
        if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num)) return 0;
        return suffix switch
        {
            'K' => (long)(num * 1024),
            'M' => (long)(num * 1024 * 1024),
            'G' => (long)(num * 1024 * 1024 * 1024),
            'T' => (long)(num * 1024 * 1024 * 1024 * 1024),
            _ => (long)num
        };
    }
    
}