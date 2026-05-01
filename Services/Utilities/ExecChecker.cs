using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GPU_T.Models;

namespace GPU_T.Services;

public static class ExecChecker
{
    // Split dictionaries by vendor applicability
    private static readonly Dictionary<string, string> CommonTools = new()
    {
        { "vulkaninfo", "vulkaninfo (vulkan-tools)" },
        { "clinfo", "clinfo" },
        { "glxinfo", "glxinfo (mesa-utils)" },
        { "lspci", "lspci (pciutils)" }
    };

    private static readonly Dictionary<string, string> AmdTools = new()
    {
        { "vainfo", "vainfo" }
    };

    private static readonly Dictionary<string, string> NvidiaTools = new()
    {
        { "nvidia-smi", "nvidia-smi (nvidia-utils)" }
    };

    /// <summary>
    /// Checks for the presence of required system tools based on current hardware and user settings.
    /// </summary>
    public static List<string> GetMissingTools(UserSettings settings)
    {
        var missing = new List<string>();
        
        // Fetch present hardware vendors
        var vendors = DatabaseManager.ScanForPresentGpuVendors();
        bool hasAmd = vendors.Contains("0x1002") || vendors.Contains("1002");
        bool hasNvidia = (vendors.Contains("0x10de") || vendors.Contains("10de")) && AppConfig.EnableExperimentalGpuSupport;

        // 1. Check Common Tools
        if (!settings.IgnoreExecWarning)
            CheckDictionary(CommonTools, missing);

        // 2. Check AMD Tools
        if (hasAmd && !settings.IgnoreExecWarning_AMD)
            CheckDictionary(AmdTools, missing);

        // 3. Check NVIDIA Tools
        if (hasNvidia && !settings.IgnoreExecWarning_NVIDIA)
            CheckDictionary(NvidiaTools, missing);

        return missing;
    }

    private static void CheckDictionary(Dictionary<string, string> tools, List<string> missingList)
    {
        foreach (var tool in tools)
        {
            if (!IsCommandAvailable(tool.Key)) 
            {
                missingList.Add(tool.Value);
            }
        }
    }

    /// <summary>
    /// Flips the appropriate ignore flags in the settings object based on which tools were actually missing.
    /// </summary>
    public static void ApplyIgnoreFlags(List<string> missingToolsShown, UserSettings settings)
    {
        // If the shown list contained ANY common tools, flag common as ignored
        if (missingToolsShown.Any(t => CommonTools.Values.Contains(t)))
            settings.IgnoreExecWarning = true;

        // If the shown list contained ANY AMD tools, flag AMD as ignored
        if (missingToolsShown.Any(t => AmdTools.Values.Contains(t)))
            settings.IgnoreExecWarning_AMD = true;

        // If the shown list contained ANY NVIDIA tools, flag NVIDIA as ignored
        if (missingToolsShown.Any(t => NvidiaTools.Values.Contains(t)))
            settings.IgnoreExecWarning_NVIDIA = true;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = $"-c \"command -v {command} >/dev/null 2>&1\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
            
            // ExitCode 0 means the command was successfully found in the system PATH
            return process?.ExitCode == 0;
        }
        catch
        {
            // In case of any error, we safely assume the tool is missing
            return false; 
        }
    }
}