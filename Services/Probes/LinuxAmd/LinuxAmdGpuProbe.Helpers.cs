using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides helper methods for Linux AMD GPU probe operations, including file reading and value extraction.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    
    /// <summary>
    /// Calculates the actual current GPU clock, boost clock, memory clock, pixel fillrate, texture fillrate, and bandwidth
    /// using AMD's DPM states and OverDrive limits.
    /// </summary>
    public static (string GpuClock, string BoostClock, string MemClock, string PixelFill, string TexFill, string Bandwidth) 
        CalculateDynamicSpecs(
            double maxCoreDpm, double maxMemDpm, double odSclk,
            double baseClock, double boostClock, double memClock,
            double rops, double tmus, double busWidth, string memoryType)
    {
        string gpuClock = "---";
        string boostClockDisplay = "---";
        string memClockDisplay = "---";
        string pixelFill = "N/A";
        string texFill = "N/A";
        string bandwidth = "N/A";

        double actualBoost = 0;

        if (maxCoreDpm > 0)
        {
            string coreStr = $"{maxCoreDpm.ToString(CultureInfo.InvariantCulture)} MHz";
            gpuClock = coreStr;
            boostClockDisplay = coreStr;
            actualBoost = maxCoreDpm;
        }

        if (maxMemDpm > 0)
        {
            string memStr = $"{maxMemDpm.ToString(CultureInfo.InvariantCulture)} MHz";
            memClockDisplay = memStr;
        }

        if (odSclk > 0)
        {
            actualBoost = odSclk;
            boostClockDisplay = $"{odSclk.ToString(CultureInfo.InvariantCulture)} MHz";
        }
        else if (maxCoreDpm > 0 && boostClock > 0 && baseClock > 0)
        {
            double diff = boostClock - baseClock;
            
            // prevent unrealistically high boost clocks for iGPUs - ignore the logic if max DPM clock is more than double the base (DB) clock
            if (maxCoreDpm / baseClock < 2)
            {
                actualBoost = maxCoreDpm + diff;
                boostClockDisplay = $"{actualBoost.ToString(CultureInfo.InvariantCulture)} MHz";
            } 
            else if (boostClock > maxCoreDpm)
            {
                boostClockDisplay = $"{boostClock.ToString(CultureInfo.InvariantCulture)} MHz";
                actualBoost = boostClock;
            }
        } //fallback to static boost clock
        else if (boostClock > 0 && maxCoreDpm <= 0)
        {
            boostClockDisplay = $"{boostClock.ToString(CultureInfo.InvariantCulture)} MHz";
            actualBoost = boostClock;
        }

        if (actualBoost > 0 && rops > 0 && tmus > 0)
        {
            pixelFill = $"{(actualBoost * rops / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GPixel/s";
            texFill = $"{(actualBoost * tmus / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GTexel/s";
        }

        double currentMemForBandwidth = maxMemDpm > 0 ? maxMemDpm : memClock;
        if (currentMemForBandwidth > 0 && busWidth > 0)
        {
            double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
            double bandwidthValue = (currentMemForBandwidth * multiplier * busWidth) / 8000.0;
            bandwidth = $"{bandwidthValue.ToString("0.0", CultureInfo.InvariantCulture)} GB/s";
        }

        return (gpuClock, boostClockDisplay, memClockDisplay, pixelFill, texFill, bandwidth);
    }
    
    
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