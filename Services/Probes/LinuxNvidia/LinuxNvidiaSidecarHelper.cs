using System;
using System.Diagnostics;
using System.IO;

namespace GPU_T.Services.Probes.LinuxNvidia;

/// <summary>
/// A centralized helper for executing and formatting arguments for the GPU-T.Nvapi sidecar.
/// </summary>
public static class LinuxNvidiaSidecarHelper
{
    /// <summary>
    /// Executes the sidecar with the given arguments and returns the standard output.
    /// </summary>
    public static string Run(string arguments, int timeoutMs = 500)
    {
        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string sidecarPath = Path.Combine(appDir, "GPU-T.Nvapi");

            // Handle running either the compiled binary or the cross-platform DLL via dotnet
            if (!File.Exists(sidecarPath))
                sidecarPath += ".dll";

            var psi = new ProcessStartInfo
            {
                FileName = sidecarPath.EndsWith(".dll") ? "dotnet" : sidecarPath,
                Arguments = sidecarPath.EndsWith(".dll") ? $"\"{sidecarPath}\" {arguments}" : arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(timeoutMs); 
                if (process.ExitCode == 0) return output;
            }
        }
        catch { }
        
        return "";
    }

    /// <summary>
    /// Parses the Linux PCI string into the hex bus ID needed by NVAPI, 
    /// and returns the fully formatted telemetry argument string.
    /// </summary>
    public static string BuildTelemetryArgs(string actionArg, string busId)
    {
        string busArg = "";
        if (!string.IsNullOrEmpty(busId) && busId != "Unknown")
        {
            var parts = busId.Split(':');
            if (parts.Length >= 2)
            {
                // Parse the Hex string ("0A") into an integer (10) for NVAPI
                if (uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out uint busInt))
                {
                    busArg = $" --bus {busInt}";
                }
            }
        }

        string pciArg = !string.IsNullOrEmpty(busId) && busId != "Unknown" ? $" --pci {busId}" : "";

        // Returns e.g., "--read --bus 10 --pci 0000:0A:00.0"
        return $"{actionArg}{busArg}{pciArg}";
    }
}