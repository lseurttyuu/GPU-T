using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GPU_T.ViewModels;
using GPU_T.Services.Probes;
using GPU_T.Services.Probes.LinuxNvidia;

namespace GPU_T.Services.Advanced.LinuxNvidia;

/// <summary>
/// Provides advanced power limits, thermal thresholds, and cooling capabilities for NVIDIA GPUs on Linux by invoking the native libcuda sidecar.
/// Implements a Hybrid Approach: Prefers native NVML API data, but falls back to nvidia-smi parsing if the native library is missing or fails.
/// </summary>
public class LinuxNvidiaPowerProvider : AdvancedDataProvider
{
    /// <summary>
    /// Loads comprehensive power and thermal limit properties for the selected NVIDIA GPU.
    /// </summary>
    /// <param name="list">The collection to which the data will be added.</param>
    /// <param name="selectedGpu">The selected GPU for which to load data.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        try
        {
            string pciString = "";
            if (selectedGpu != null)
            {
                var probe = GpuProbeFactory.Create(selectedGpu.Id);
                pciString = probe.LoadStaticData().BusId;
            }

            string pciArg = !string.IsNullOrEmpty(pciString) && pciString != "Unknown" ? $" --pci {pciString}" : "";
            
            // 1. Primary Attempt: Native Sidecar via NVML
            string rawData = LinuxNvidiaSidecarHelper.Run("--limits" + pciArg, 1000);

            // 2. Fallback Attempt: If the sidecar fails, use nvidia-smi
            if (string.IsNullOrWhiteSpace(rawData))
            {
                RenderFallback(list, pciString);
                return;
            }

            // Parse Sidecar INI output
            var lines = rawData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    AddRow(list, t.Trim('[', ']'), "", true);
                }
                else if (t.Contains('='))
                {
                    var parts = t.Split('=', 2);
                    AddRow(list, parts[0], parts[1]);
                }
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"Power limits check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes if the native sidecar fails. Scrapes available limits directly from nvidia-smi.
    /// </summary>
    private void RenderFallback(ObservableCollection<AdvancedItemViewModel> list, string pciString)
    {
        AddRow(list, "API Status", "", true);
        AddRow(list, "Note", "NVML sidecar failed to load or returned no data. Displaying limited information from nvidia-smi.");

        // Helper to filter out the various ways NVIDIA says "no"
        bool IsValid(string val) => !string.IsNullOrWhiteSpace(val) && val != "[Not Supported]" && val != "[N/A]" && val != "N/A";

        try
        {
            string targetArg = !string.IsNullOrEmpty(pciString) && pciString != "Unknown" ? $"-i {pciString} " : "";

            // Query 1: Extract Power, Clocks, Persistence, and Fan Speed
            var psiCsv = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = $"{targetArg}--query-gpu=power.limit,power.default_limit,power.min_limit,power.max_limit,persistence_mode,clocks.max.graphics,clocks.max.memory,fan.speed --format=csv,noheader",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processCsv = Process.Start(psiCsv);
            string csvOutput = processCsv?.StandardOutput.ReadToEnd().Trim() ?? "";
            processCsv?.WaitForExit();

            if (!string.IsNullOrEmpty(csvOutput) && !csvOutput.StartsWith("No devices"))
            {
                var parts = csvOutput.Split(new[] { ',' }, StringSplitOptions.TrimEntries);
                if (parts.Length >= 8)
                {
                    AddRow(list, "Power Limits", "", true);
                    if (IsValid(parts[0])) AddRow(list, "Target Power Limit (TDP)", parts[0]);
                    if (IsValid(parts[1])) AddRow(list, "Default Power Limit", parts[1]);
                    if (IsValid(parts[2])) AddRow(list, "Minimum Allowed Limit", parts[2]);
                    if (IsValid(parts[3])) AddRow(list, "Maximum Allowed Limit", parts[3]);

                    AddRow(list, "Current Clock Limits", "", true);
                    if (IsValid(parts[5])) AddRow(list, "Maximum Graphics Clock", parts[5]);
                    if (IsValid(parts[6])) AddRow(list, "Maximum Memory Clock", parts[6]);

                    AddRow(list, "Driver State", "", true);
                    if (IsValid(parts[4])) AddRow(list, "Persistence Mode", parts[4]);

                    // Determine Cooling Capabilities based on fan speed support
                    AddRow(list, "Cooling Capabilities", "", true);
                    if (IsValid(parts[7]))
                    {
                        AddRow(list, "Active Fan Control", "Supported");
                        AddRow(list, "Hardware Fan Range", "Requires NVML sidecar for full limits");
                    }
                    else
                    {
                        AddRow(list, "Active Fan Control", "No");
                        AddRow(list, "Cooling Type", "Passive / Liquid / Undetected");
                    }
                }
            }

            // Query 2: Extract Thermal Limits by parsing the detailed text readout
            var psiTemp = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = $"{targetArg}-q -d TEMPERATURE",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processTemp = Process.Start(psiTemp);
            string tempOutput = processTemp?.StandardOutput.ReadToEnd() ?? "";
            processTemp?.WaitForExit();

            if (!string.IsNullOrEmpty(tempOutput))
            {
                string slowdown = "";
                string shutdown = "";

                var lines = tempOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("Slowdown Temp") && line.Contains(':'))
                        slowdown = line.Split(':')[1].Trim();
                    else if (line.Contains("Shutdown Temp") && line.Contains(':'))
                        shutdown = line.Split(':')[1].Trim();
                }

                if (!string.IsNullOrEmpty(slowdown) || !string.IsNullOrEmpty(shutdown))
                {
                    AddRow(list, "Thermal Limits", "", true);
                    if (!string.IsNullOrEmpty(slowdown)) AddRow(list, "Thermal Throttle Point", slowdown);
                    if (!string.IsNullOrEmpty(shutdown)) AddRow(list, "Emergency Shutdown Point", shutdown);
                }
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"Fallback scrape also failed: {ex.Message}");
        }
    }
}