using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GPU_T.Services;

public static class ExecChecker
{
    // Dictionary mapping the actual command (Key) to the user-friendly UI string (Value)
    private static readonly Dictionary<string, string> RequiredTools = new()
    {
        { "vulkaninfo", "vulkaninfo (vulkan-tools)" },
        { "clinfo", "clinfo" },
        { "glxinfo", "glxinfo (mesa-utils)" },
        { "vainfo", "vainfo" },
        { "lspci", "lspci (pciutils)" }
    };

    /// <summary>
    /// Checks for the presence of required system tools and returns a list of missing ones in a user-friendly format.
    /// </summary>
    /// <returns>A list of user-friendly names of missing tools.</returns>
    public static List<string> GetMissingTools()
    {
        var missing = new List<string>();

        foreach (var tool in RequiredTools)
        {
            // tool.Key is the command to check (e.g., "vulkaninfo")
            // tool.Value is the display name (e.g., "vulkaninfo (vulkan-tools)")
            if (!IsCommandAvailable(tool.Key)) 
            {
                missing.Add(tool.Value);
            }
        }

        return missing;
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