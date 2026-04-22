using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using GPU_T.ViewModels;
using GPU_T.Services.Probes; // Needed for GpuProbeFactory
using GPU_T.Services.Probes.LinuxNvidia; // Needed for NvidiaSidecarHelper

namespace GPU_T.Services.Advanced.LinuxNvidia;

/// <summary>
/// Provides advanced CUDA constraints, memory limits, and hardware capabilities 
/// for NVIDIA GPUs on Linux by invoking the native libcuda sidecar.
/// </summary>
public class LinuxNvidiaCudaProvider : AdvancedDataProvider
{
    /// <summary>
    /// Loads comprehensive CUDA properties for the selected NVIDIA GPU.
    /// </summary>
    /// <param name="list">The collection to populate with advanced item view models.</param>
    /// <param name="selectedGpu">The currently selected GPU item, or null if not specified.</param>
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();

        try
        {
            // 1. Get the PCI Bus ID for the selected GPU to ensure we query the right card
            string pciString = "";
            if (selectedGpu != null)
            {
                var probe = GpuProbeFactory.Create(selectedGpu.Id);
                var staticData = probe.LoadStaticData();
                pciString = staticData.BusId;
            }

            // 2. Build the argument and run the sidecar
            string arg = "--cuda";
            if (!string.IsNullOrEmpty(pciString) && pciString != "Unknown")
            {
                arg += $" --pci {pciString}";
            }

            string rawData = LinuxNvidiaSidecarHelper.Run(arg, 1000);

            if (string.IsNullOrWhiteSpace(rawData))
            {
                AddRow(list, "Error", "Failed to retrieve CUDA info (is the proprietary NVIDIA driver installed?)");
                return;
            }

            // 3. Parse the INI-style output from the sidecar
            var lines = rawData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string t = line.Trim();
                
                // Check if it's a category header (e.g., "[Memory]")
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    string headerName = t.Trim('[', ']');
                    AddRow(list, headerName, "", true); // Add as a category header
                }
                // Check if it's a data row (e.g., "Max Threads Per Block=1024")
                else if (t.Contains('='))
                {
                    var parts = t.Split('=', 2);
                    AddRow(list, parts[0].Trim(), parts[1].Trim());
                }
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"CUDA check failed: {ex.Message}");
        }
    }


}