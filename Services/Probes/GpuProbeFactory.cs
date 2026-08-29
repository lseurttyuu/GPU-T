using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Probes.LinuxAmd;
using GPU_T.Services.Probes.LinuxIntel;
using GPU_T.Services.Probes.LinuxNvidia;
using GPU_T.Services.Probes.LinuxGeneric;

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
        string vendorId = GetVendorId(gpuId).Replace("0x", "", StringComparison.OrdinalIgnoreCase);

        if (vendorId == "8086") // Intel
        {
            return new LinuxIntelGpuProbe(gpuId);
        }

        if (vendorId == "10DE") // NVIDIA
        {
            return new LinuxNvidiaGpuProbe(gpuId, memoryType);
        }

        if(vendorId == "1002" || vendorId == "1022") // AMD
        {
            return new LinuxAmdGpuProbe(gpuId, memoryType);
        }

        // Default to a Generic/unknown provider;
        return new LinuxGenericGpuProbe(gpuId);
    }

    /// <summary>
    /// Reads the Vendor ID from the sysfs device directory for the specified GPU.
    /// </summary>
    /// <param name="gpuId">The GPU identifier.</param>
    /// <returns>The Vendor ID string, or empty if unavailable.</returns>
    public static string GetVendorId(string gpuId)
    {
        try
        {
            string basePath = $"/sys/class/drm/{gpuId}/device";
            string vendorPath = Path.Combine(basePath, "vendor");

            // 1. Standard fast-path (works for 99% of modern drivers)
            if (File.Exists(vendorPath))
            {
                return File.ReadAllText(vendorPath).Trim().ToUpper();
            }

            // 2. Smart path-resolving fallback for simple-framebuffer or legacy drivers
            DirectoryInfo currentDir = new DirectoryInfo($"/sys/class/drm/{gpuId}");
            if (currentDir.Exists)
            {
                // Resolve the symlink to the physical PCI hierarchy
                FileSystemInfo? resolvedTarget = currentDir.ResolveLinkTarget(true);
                DirectoryInfo? searchDir = resolvedTarget as DirectoryInfo ?? currentDir;

                // Walk up the directory tree, look for the "vendor" file in parent directories
                while (searchDir != null)
                {
                    vendorPath = Path.Combine(searchDir.FullName, "vendor");
                    if (File.Exists(vendorPath))
                    {
                        return File.ReadAllText(vendorPath).Trim().ToUpper();
                    }
                    
                    searchDir = searchDir.Parent;
                }
            }
        }
        catch 
        { }
        
        return "";
    }
}
