using System.Collections.Generic;
using System.IO;
using GPU_T.Services.Probes.LinuxAmd;

namespace GPU_T.Services;

/// <summary>
/// Factory for creating GPU probe instances and enumerating available GPU cards.
/// </summary>
public static class GpuProbeFactory
{
    /// <summary>
    /// Retrieves a combined list of available GPU cards from all known providers.
    /// </summary>
    /// <returns>A sorted list of GPU card identifiers.</returns>
    public static List<string> GetAvailableCards()
    {
        var allCards = new List<string>();

        // Adds AMD cards to the list; future providers will be appended here.
        allCards.AddRange(LinuxAmdGpuProbe.GetAvailableCards());

        // 2. Future impl: NVIDIA
        // allCards.AddRange(LinuxNvidiaGpuProbe.GetAvailableCards());

        // 3. Future impl: INTEL
        // allCards.AddRange(LinuxIntelGpuProbe.GetAvailableCards());

        // Sorting ensures card0 precedes card1 for UI consistency.
        allCards.Sort();
        
        return allCards;
    }

    /// <summary>
    /// Creates a GPU probe instance for the specified GPU ID and optional memory type.
    /// </summary>
    /// <param name="gpuId">The GPU identifier (e.g., "card0").</param>
    /// <param name="memoryType">Optional memory type string for provider initialization.</param>
    /// <returns>An <see cref="IGpuProbe"/> instance for the detected vendor.</returns>
    public static IGpuProbe Create(string gpuId, string memoryType = "")
    {
        // Vendor detection logic is prepared for future expansion.
        string vendorId = GetVendorId(gpuId);

        /*
        if (vendorId == "0x10DE") // NVIDIA
        {
             return new LinuxNvidiaGpuProbe(gpuId); 
        }
        else if (vendorId == "0x8086") // INTEL
        {
             return new LinuxIntelGpuProbe(gpuId);
        }
        */

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