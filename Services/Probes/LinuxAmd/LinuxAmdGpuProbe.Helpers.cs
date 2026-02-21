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