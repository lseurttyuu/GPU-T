using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Advanced;
using GPU_T.Services.Advanced.LinuxAmd;

namespace GPU_T.Services.Probes.LinuxAmd;

public partial class LinuxAmdGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _hwmonPath;
    private readonly double _memClockMultiplier = 1.0;

    // Statyczny cache dla wsparcia Ray Tracingu (DeviceID -> Supported)
    private static Dictionary<string, bool>? _rtSupportCache;

    public LinuxAmdGpuProbe(string gpuId, string memoryType = "")
    {
        _basePath = $"/sys/class/drm/{gpuId}/device";
        
        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }

        if (!string.IsNullOrEmpty(memoryType) && 
            memoryType.Contains("GDDR6", System.StringComparison.OrdinalIgnoreCase))
        {
            _memClockMultiplier = 2.0;
        }
    }

    public AdvancedDataProvider? GetAdvancedDataProvider(string category)
    {
        return category switch
        {
            "General" => new GeneralProvider(),
            "Vulkan" => new VulkanProvider(),
            "OpenCL" => new OpenClProvider(),
            
            
            // Te są specyficzne dla AMD - w przyszłości Nvidia zwróci tu null lub inne klasy
            "Multimedia (VA-API)" => new LinuxAmdMultimediaProvider(),
            "Power & Limits" => new LinuxAmdPowerProvider(),
            "PCIe Resizable BAR" => new LinuxAmdResizableBarProvider(),
            
            _ => null
        };
    }

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
}