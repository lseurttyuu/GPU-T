using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes;

/// <summary>
/// Shared helper methods used by all GPU vendor probes for system-level sensor readings,
/// PCI device identification, and spec computation utilities.
/// </summary>
internal static class CommonGpuHelpers
{
    /// <summary>
    /// Reads the CPU temperature from hwmon directories (k10temp for AMD, coretemp for Intel).
    /// </summary>
    /// <returns>CPU temperature in Celsius, or 0 if unavailable.</returns>
    internal static double GetCpuTemperature()
    {
        try
        {
            var baseDir = "/sys/class/hwmon/";
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath))
                    {
                        string name = File.ReadAllText(namePath).Trim();
                        if (name == "k10temp" || name == "coretemp")
                        {
                            string tempPath = Path.Combine(dir, "temp1_input");
                            if (File.Exists(tempPath))
                            {
                                if (double.TryParse(File.ReadAllText(tempPath), out double val))
                                    return val / 1000.0;
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Reads system RAM usage from /proc/meminfo.
    /// </summary>
    /// <returns>Used RAM in MB, or 0 if unavailable.</returns>
    internal static double GetSystemRamUsage()
    {
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                string[] lines = File.ReadAllLines("/proc/meminfo");
                double total = 0, avail = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:")) total = ExtractKb(line);
                    else if (line.StartsWith("MemAvailable:")) avail = ExtractKb(line);
                    if (total > 0 && avail > 0) break;
                }
                return (total - avail) / 1024.0;
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Extracts a numeric value in kilobytes from a /proc/meminfo line.
    /// </summary>
    /// <param name="line">A line from /proc/meminfo.</param>
    /// <returns>Value in kilobytes, or 0 if parsing fails.</returns>
    internal static double ExtractKb(string line)
    {
        var match = Regex.Match(line, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }

    /// <summary>
    /// Tries to get the device name from lspci output.
    /// </summary>
    /// <param name="busId">PCI bus ID string.</param>
    /// <returns>Device name from lspci, or "Unknown" if unavailable.</returns>
    internal static string GetDeviceNameFromLspci(string busId)
    {
        try
        {
            string output = ShellHelper.RunCommand("lspci", $"-s {busId}");
            if (!string.IsNullOrEmpty(output))
            {
                // Format: "01:00.0 VGA compatible controller: NVIDIA Corporation GeForce RTX 3080 (rev a1)"
                int colonIdx = output.IndexOf(": ", StringComparison.Ordinal);
                if (colonIdx > 0)
                {
                    string name = output.Substring(colonIdx + 2).Trim();
                    // Remove "(rev xx)" suffix
                    var revMatch = Regex.Match(name, @"\s*\(rev\s+\w+\)\s*$");
                    if (revMatch.Success)
                        name = name.Substring(0, revMatch.Index).Trim();
                    return name;
                }
            }
        }
        catch { }
        return "Unknown";
    }

    /// <summary>
    /// Extracts the first numeric value from the input string.
    /// </summary>
    /// <param name="input">The input string to parse (e.g. "1800 MHz").</param>
    /// <returns>The extracted number as a double, or 0 if not found.</returns>
    internal static double ExtractNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var match = Regex.Match(input, @"[\d]+(\.[\d]+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }

    /// <summary>
    /// Determines the effective data rate multiplier based on the memory type string.
    /// </summary>
    /// <param name="memoryType">The memory type descriptor (e.g. "GDDR6", "HBM2").</param>
    /// <returns>The multiplier as a double.</returns>
    internal static double GetMemoryMultiplier(string memoryType)
    {
        if (string.IsNullOrEmpty(memoryType)) return 1;
        string type = memoryType.ToUpperInvariant();
        if (type.Contains("GDDR6") || type.Contains("GDDR6X")) return 8.0;
        if (type.Contains("GDDR5") || type.Contains("GDDR5X")) return 4.0;
        if (type.Contains("HBM") || type.Contains("HBM2")) return 2.0;
        if (type.Contains("DDR")) return 2.0;
        return 1.0;
    }
}
