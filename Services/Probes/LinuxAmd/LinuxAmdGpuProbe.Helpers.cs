using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GPU_T.Services.Probes.LinuxAmd;

public partial class LinuxAmdGpuProbe
{
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

    private double ExtractNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var match = Regex.Match(input, @"[\d]+(\.[\d]+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }

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

    private double ParseClock(string clockString)
    {
        var match = Regex.Match(clockString, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }
}