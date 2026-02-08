using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides helper methods for Linux AMD GPU probe operations, including file reading and value extraction.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    /// <summary>
    /// Reads the contents of a file in the GPU sysfs base path, returning a fallback value if unavailable.
    /// </summary>
    /// <param name="filename">The file name to read.</param>
    /// <param name="fallback">The fallback value if the file is not found or unreadable.</param>
    /// <returns>The trimmed file content or the fallback value.</returns>
    private string ReadFile(string filename, string fallback = "N/A")
    {
        try
        {
            string path = Path.Combine(_basePath, filename);
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
        }
        catch { }
        return fallback;
    }

    /// <summary>
    /// Extracts the first numeric value from the input string.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <returns>The extracted number as a double, or 0 if not found.</returns>
    private double ExtractNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var match = Regex.Match(input, @"[\d]+(\.[\d]+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }

    /// <summary>
    /// Determines the memory clock multiplier based on the memory type string.
    /// </summary>
    /// <param name="memoryType">The memory type descriptor.</param>
    /// <returns>The multiplier as a double.</returns>
    private double GetMemoryMultiplier(string memoryType)
    {
        if (string.IsNullOrEmpty(memoryType)) return 1;
        string type = memoryType.ToUpperInvariant();
        if (type.Contains("GDDR6") || type.Contains("GDDR6X")) return 8.0; 
        if (type.Contains("GDDR5") || type.Contains("GDDR5X")) return 4.0; 
        if (type.Contains("HBM") || type.Contains("HBM2")) return 2.0;     
        if (type.Contains("DDR")) return 2.0;                              
        return 1.0; 
    }

    /// <summary>
    /// Parses the first integer value from a clock string.
    /// </summary>
    /// <param name="clockString">The clock string to parse.</param>
    /// <returns>The parsed clock value as a double, or 0 if not found.</returns>
    private double ParseClock(string clockString)
    {
        var match = Regex.Match(clockString, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }
}