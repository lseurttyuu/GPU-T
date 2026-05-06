using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxNvidia;

public partial class LinuxNvidiaGpuProbe
{
    /// <summary>
    /// Checks if nvidia-smi is available on the system.
    /// </summary>
    private static bool IsNvidiaSmiAvailable()
    {
        if (_nvidiaSmiAvailable.HasValue) return _nvidiaSmiAvailable.Value;

        try
        {
            string output = ShellHelper.RunCommand("nvidia-smi", "--query-gpu=name --format=csv,noheader,nounits");
            _nvidiaSmiAvailable = !string.IsNullOrEmpty(output);
        }
        catch
        {
            _nvidiaSmiAvailable = false;
        }
        return _nvidiaSmiAvailable.Value;
    }

    /// <summary>
    /// Queries nvidia-smi for the specified fields and returns parsed CSV values.
    /// Returns null if nvidia-smi is unavailable or the query fails.
    /// </summary>
    private List<string>? QueryNvidiaSmi(string queryFields)
    {
        if (!IsNvidiaSmiAvailable()) return null;

        try
        {
            string idArg = !string.IsNullOrEmpty(_busId) && _busId != "Unknown"
                ? $"-i {_busId} " : "";
            string output = ShellHelper.RunCommand("nvidia-smi",
                $"{idArg}--query-gpu={queryFields} --format=csv,noheader,nounits");

            if (string.IsNullOrEmpty(output)) return null;

            string firstLine = output.Split('\n')[0];
            var values = new List<string>();
            foreach (var val in firstLine.Split(','))
            {
                values.Add(val.Trim());
            }
            return values;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cleans an nvidia-smi value by removing "[Not Supported]", "[N/A]", and unit suffixes.
    /// Returns the fallback if the value is not usable.
    /// </summary>
    private static string CleanSmiValue(string raw, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        string trimmed = raw.Trim();
        if (trimmed.Contains("[Not Supported]") || trimmed.Contains("[N/A]") ||
            trimmed == "N/A" || trimmed == "[Insufficient Permissions]")
            return fallback;
        
        // Added " KB/s" and " MB/s" to the replace chain
        trimmed = trimmed.Replace(" MiB", "").Replace(" MHz", "").Replace(" W", "")
                         .Replace(" %", "").Replace(" KB/s", "").Replace(" MB/s", "").Trim();

        return trimmed;
    }

    /// <summary>
    /// Converts nvidia-smi's data-rate clock into a true GPU-Z style base clock.
    /// </summary>
    private double NormalizeMemoryClock(double smiClock, string memoryType)
    {
        if (smiClock <= 0) return 0;
        if(memoryType.ToUpperInvariant().Contains("N/A")) return smiClock; // If memory type is unknown, return as-is to avoid incorrect normalization
        
        // Use the common helper to get the divisor (8 for GDDR6, 4 for GDDR5, etc.)
        double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
        
        // nvidia-smi always reports (Effective / 2). 
        // Therefore, Base = (smiClock * 2) / Multiplier.
        return (smiClock * 2.0) / multiplier;
    }

    /// <summary>
    /// Calculates the actual current GPU clock, boost clock, memory clock, pixel fillrate, texture fillrate, and bandwidth
    /// </summary>
    public static (string GpuClock, string BoostClock, string MemClock, string PixelFill, string TexFill, string Bandwidth) 
        CalculateDynamicSpecs(
            double defGpuClock, double defBoostClock, double defMemClock,
            double rops, double tmus, double busWidth, string memoryType,
            int coreOffset, int memOffset)
    {
        int actualMemOffsetBase = 0;
        if (memOffset != 0)
        {
            actualMemOffsetBase = (int)(memOffset / CommonGpuHelpers.GetMemoryMultiplier(memoryType));
        }

        double currentGpuClock_nvapi = defGpuClock > 0 ? defGpuClock + coreOffset : 0;
        double currentBoostClock_nvapi = defBoostClock > 0 ? defBoostClock + coreOffset : 0;
        double currentMemClock_nvapi = defMemClock > 0 ? defMemClock + actualMemOffsetBase : 0;

        string gpuClockStr = currentGpuClock_nvapi > 0 ? $"{currentGpuClock_nvapi} MHz" : "---";
        string boostClockStr = currentBoostClock_nvapi > 0 ? $"{currentBoostClock_nvapi} MHz" : "---";
        string memClockStr = currentMemClock_nvapi > 0 ? $"{currentMemClock_nvapi} MHz" : "---";

        string pixelFill = "---";
        string texFill = "---";
        string bandwidth = "---";

        if (currentBoostClock_nvapi > 0 && rops > 0 && tmus > 0)
        {
            pixelFill = $"{(currentBoostClock_nvapi * rops / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GPixel/s";
            texFill = $"{(currentBoostClock_nvapi * tmus / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GTexel/s";
        }

        if (currentMemClock_nvapi > 0 && busWidth > 0)
        {
            double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
            double bandwidthValue = (currentMemClock_nvapi * multiplier * busWidth) / 8000.0;
            bandwidth = $"{bandwidthValue.ToString("0.0", CultureInfo.InvariantCulture)} GB/s";
        }

        return (gpuClockStr, boostClockStr, memClockStr, pixelFill, texFill, bandwidth);
    }

    private double ReadHwmonDouble(string filename)
    {
        if (string.IsNullOrEmpty(_hwmonPath)) return 0;
        try
        {
            string path = Path.Combine(_hwmonPath, filename);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    return val;
            }
        }
        catch { }
        return 0;
    }
}