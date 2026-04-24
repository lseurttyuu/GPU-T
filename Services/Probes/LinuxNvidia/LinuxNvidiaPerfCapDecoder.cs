using System;
using System.Collections.Generic;
using System.Globalization;

namespace GPU_T.Services.Probes.LinuxNvidia;

/// <summary>
/// Bitmask flags representing NVIDIA clock throttle reasons.
/// </summary>
[Flags]
public enum NvidiaThrottleReason : long
{
    None = 0x0000000000000000,
    GpuIdle = 0x0000000000000001,
    ApplicationsClocksSetting = 0x0000000000000002,
    SwPowerCap = 0x0000000000000004,
    HwSlowdown = 0x0000000000000008,
    SyncBoost = 0x0000000000000010,
    SwThermalSlowdown = 0x0000000000000020,
    HwThermalSlowdown = 0x0000000000000040,
    HwPowerBrakeSlowdown = 0x0000000000000080,
    DisplayClockSetting = 0x0000000000000100
}

/// <summary>
/// Decodes raw nvidia-smi hex values into human-readable PerfCap Reason strings.
/// </summary>
public static class LinuxNvidiaPerfCapDecoder
{
    // Maps the bitmask flags to clean, GPU-Z style strings
    private static readonly Dictionary<NvidiaThrottleReason, string> ReasonNames = new()
    {
        { NvidiaThrottleReason.ApplicationsClocksSetting, "Util" },   // Closest Linux equivalent to utilization/app limit
        { NvidiaThrottleReason.SwPowerCap, "Pwr" },                   // GPU-Z: Pwr
        { NvidiaThrottleReason.HwSlowdown, "HW" },                    // Generic hardware slowdown
        { NvidiaThrottleReason.SyncBoost, "SLI" },                    // GPU-Z: SLI (Sync Boost)
        { NvidiaThrottleReason.SwThermalSlowdown, "Thrm" },           // GPU-Z: Thrm
        { NvidiaThrottleReason.HwThermalSlowdown, "Thrm" },           // GPU-Z: Thrm
        { NvidiaThrottleReason.HwPowerBrakeSlowdown, "Pwr (Ext)" },   // External power brake (e.g. PSU limit)
        { NvidiaThrottleReason.DisplayClockSetting, "Disp" }
    };

    /// <summary>
    /// Parses a hex string (e.g., "0x0000000000000001") into a human-readable reason.
    /// Handles combined bitmask limits (e.g., Power + Thermal).
    /// </summary>
    public static string Decode(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "None" || rawValue.Contains("Not Active"))
            return "None";

        // Clean the string for hex parsing
        string cleanHex = rawValue.Replace("0x", "").Trim();
        
        if (!long.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long numericValue))
        {
            // If it completely fails to parse (e.g. driver returned raw text instead of hex), just return it as-is
            return rawValue;
        }

        if (numericValue == 0) return "None";

        NvidiaThrottleReason currentReason = (NvidiaThrottleReason)numericValue;

        // If the GPU is purely idle, skip checking the bad limits
        if (currentReason == NvidiaThrottleReason.GpuIdle) return "Idle";

        // Collect all active limitation flags
        var activeReasons = new List<string>();
        foreach (var kvp in ReasonNames)
        {
            if (currentReason.HasFlag(kvp.Key))
            {
                activeReasons.Add(kvp.Value);
            }
        }

        if (activeReasons.Count == 0) return $"Unknown (0x{cleanHex})";

        // If multiple limits are hit simultaneously, join them with a plus (e.g., "SW Power Cap + HW Thermal Slowdown")
        return string.Join(" + ", activeReasons);
    }

    /// <summary>
    /// Determines the binary graph value: 0.0 for Idle/None, 1.0 if any actual throttling is occurring.
    /// </summary>
    public static double GetGraphValue(string decodedText)
    {
        if (string.IsNullOrEmpty(decodedText) || decodedText == "Idle" || decodedText == "None")
            return 0.0;
        
        return 1.0;
    }
}