using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GPU_T.ViewModels;

namespace GPU_T.Services;

/// <summary>
/// Utility service that builds CSV-style headers and data rows for sensor logging.
/// </summary>
public static class SensorLogService
{
    private const int DateColumnWidth = 20;

    /// <summary>
    /// Builds the CSV header line for the provided sensors.
    /// </summary>
    /// <param name="sensors">Collection of sensors to include as columns.</param>
    /// <returns>A single header line string with columns separated by commas.</returns>
    public static string BuildHeader(IEnumerable<SensorItemViewModel> sensors)
    {
        var sb = new StringBuilder();
        
        sb.Append(PadCenter("Date", DateColumnWidth) + ",");

        foreach (var sensor in sensors)
        {
            string label = $"{sensor.Name} [{sensor.Unit}]";
            sb.Append(" " + label + " ,");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a CSV data row representing current sensor values along with the current timestamp.
    /// </summary>
    /// <param name="sensors">Collection of sensors to render values for.</param>
    /// <returns>A single data line string with values formatted and padded to align with header columns.</returns>
    public static string BuildDataRow(IEnumerable<SensorItemViewModel> sensors)
    {
        var sb = new StringBuilder();

        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        sb.Append(dateStr + " ,"); 

        foreach (var sensor in sensors)
        {
            string label = $"{sensor.Name} [{sensor.Unit}]";
            int headerWidth = 1 + label.Length + 1; // left space + label + right space

            string valStr = sensor.CurrentValue.ToString(GetFormat(sensor.Unit), CultureInfo.InvariantCulture);

            // Compute left padding so that the value plus three trailing spaces fits the header width.
            int paddingCount = headerWidth - valStr.Length - 3;
            if (paddingCount < 0) paddingCount = 0;

            string leftPadding = new string(' ', paddingCount);
            string rightPadding = "   ";

            sb.Append(leftPadding + valStr + rightPadding + ",");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Centers the provided text within a field of the specified width.
    /// </summary>
    /// <param name="text">Text to center.</param>
    /// <param name="width">Field width in characters.</param>
    /// <returns>The centered text padded with spaces to the given width.</returns>
    private static string PadCenter(string text, int width)
    {
        if (text.Length >= width) return text;
        int leftPadding = (width - text.Length) / 2;
        int rightPadding = width - text.Length - leftPadding;
        // Centering algorithm: distribute remaining spaces between left and right.
        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }

    /// <summary>
    /// Determines numeric format string for a given sensor unit to match UI formatting.
    /// </summary>
    /// <param name="unit">Sensor unit (e.g., "RPM", "%", "V").</param>
    /// <returns>A format string suitable for ToString with InvariantCulture.</returns>
    private static string GetFormat(string unit)
    {
        if (unit == "RPM" || unit == "%" || unit == "MB") return "0";
        if (unit == "V") return "0.000";
        return "0.0";
    }
}