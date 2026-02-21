using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Advanced;
using GPU_T.Services.Advanced.LinuxAmd;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Probe implementation for AMD GPUs on Linux, providing advanced data providers and device enumeration.
/// </summary>
public partial class LinuxAmdGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _hwmonPath;
    private readonly double _memClockMultiplier = 1.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinuxAmdGpuProbe"/> class for the specified GPU.
    /// </summary>
    /// <param name="gpuId">The GPU identifier (e.g., "card0").</param>
    /// <param name="memoryType">Optional memory type string for clock multiplier adjustment.</param>
    public LinuxAmdGpuProbe(string gpuId, string memoryType = "")
    {
        _basePath = $"/sys/class/drm/{gpuId}/device";
        
        // Selects the first hwmon directory for the GPU if available.
        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }

        // Adjusts memory clock multiplier for GDDR6 memory type.
        if (!string.IsNullOrEmpty(memoryType) && 
            memoryType.Contains("GDDR6", System.StringComparison.OrdinalIgnoreCase))
        {
            _memClockMultiplier = 2.0;
        }
    }

    /// <summary>
    /// Returns an advanced data provider instance for the specified category.
    /// </summary>
    /// <param name="category">The advanced category name.</param>
    /// <returns>An <see cref="AdvancedDataProvider"/> instance or null if unsupported.</returns>
    public AdvancedDataProvider? GetAdvancedDataProvider(string category)
    {
        return category switch
        {
            "General" => new GeneralProvider(),
            "Vulkan" => new VulkanProvider(),
            "OpenCL" => new OpenClProvider(),
            // AMD-specific categories; Nvidia will return null or other classes in future.
            "Multimedia (VA-API)" => new LinuxAmdMultimediaProvider(),
            "Power & Limits" => new LinuxAmdPowerProvider(),
            "PCIe Resizable BAR" => new LinuxAmdResizableBarProvider(),
            _ => null
        };
    }

    /// <summary>
    /// Returns the list of advanced categories supported by AMD GPUs.
    /// </summary>
    public string[] GetAdvancedCategories()
    {
        return new[]
        {
            "General",
            "Vulkan",
            "OpenCL",
            "Multimedia (VA-API)",
            "Power & Limits",
            "PCIe Resizable BAR"
        };
    }

    /// <summary>
    /// Enumerates available GPU cards by scanning /sys/class/drm for card directories.
    /// </summary>
    /// <returns>A sorted list of card identifiers.</returns>
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
                    // Filters only valid card directories matching "cardN" pattern.
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
}