using System;
using System.Diagnostics;

namespace GPU_T.Services;

/// <summary>
/// Helper utilities for executing shell commands and opening URLs on the host system.
/// </summary>
public static class ShellHelper
{
    /// <summary>
    /// Executes an external command with the provided arguments and returns trimmed standard output.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <returns>Trimmed standard output from the process, or an empty string on error.</returns>
    public static string RunCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            // Use a bounded wait to avoid indefinitely blocking the caller if the command hangs.
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000); 
            
            return output.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Opens the specified URL using the platform's default mechanism (xdg-open on Linux).
    /// </summary>
    /// <param name="url">The URL to open.</param>
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
    }
}