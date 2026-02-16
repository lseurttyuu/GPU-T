using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Probes.LinuxAmd;
using GPU_T.Services.Probes.LinuxIntel;
using GPU_T.Services.Probes.LinuxNvidia;

namespace GPU_T.Services;

/// <summary>
/// Factory for creating GPU probe instances and enumerating available GPU cards.
/// </summary>
public static class GpuProbeFactory
{
    /// <summary>
    /// Retrieves a combined list of available GPU cards by scanning /sys/class/drm.
    /// </summary>
    /// <returns>A sorted list of GPU card identifiers.</returns>
    public static List<string> GetAvailableCards()
    {
        var cards = new List<string>();
        try
        {
            var drmDir = "/sys/class/drm/";
            if (Directory.Exists(drmDir))
            {
                var dirs = Directory.GetDirectories(drmDir, "card*");
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    if (Regex.IsMatch(name, @"^card\d+$"))
                    {
                        cards.Add(name);
                    }
                }
            }
        }
        catch { }
        cards.Sort();
        return cards;
    }

    /// <summary>
    /// Creates a GPU probe instance for the specified GPU ID and optional memory type.
    /// </summary>
    /// <param name="gpuId">The GPU identifier (e.g., "card0").</param>
    /// <param name="memoryType">Optional memory type string for provider initialization.</param>
    /// <returns>An <see cref="IGpuProbe"/> instance for the detected vendor.</returns>
    public static IGpuProbe Create(string gpuId, string memoryType = "")
    {
        string vendorId = GetVendorId(gpuId);

        if (vendorId == "0X10DE") // NVIDIA
        {
            return new LinuxNvidiaGpuProbe(gpuId);
        }

        if (vendorId == "0X8086") // Intel
        {
            return new LinuxIntelGpuProbe(gpuId);
        }

        // Default to AMD provider; memoryType is passed for clock multiplier logic.
        return new LinuxAmdGpuProbe(gpuId, memoryType);
    }

    /// <summary>
    /// Reads the Vendor ID from the sysfs device directory for the specified GPU.
    /// </summary>
    /// <param name="gpuId">The GPU identifier.</param>
    /// <returns>The Vendor ID string, or empty if unavailable.</returns>
    private static string GetVendorId(string gpuId)
    {
        try
        {
            string path = $"/sys/class/drm/{gpuId}/device/vendor";
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim().ToUpper();
            }
        }
        catch { }
        return "";
    }
}
