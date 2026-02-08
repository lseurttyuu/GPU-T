using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides feature detection methods for Linux AMD GPU probes, including OpenGL and Ray Tracing support.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    /// <summary>
    /// Checks whether OpenGL direct rendering is supported and not using software rendering.
    /// </summary>
    /// <returns>True if direct rendering is available and not using llvmpipe; otherwise, false.</returns>
    private bool CheckOpenglSupport()
    {
        try 
        {
            var psi = new ProcessStartInfo("glxinfo", "-B")
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            
            using var p = Process.Start(psi);
            if (p == null) return false;
            
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if (string.IsNullOrEmpty(output)) return false;

            bool direct = output.Contains("direct rendering: Yes");
            bool isSoftware = output.Contains("llvmpipe");

            return direct && !isSoftware;
        }
        catch { return false; }
    }

    /// <summary>
    /// Checks Ray Tracing support for the specified Vulkan device ID using cached results.
    /// </summary>
    /// <param name="currentDeviceIdHex">The hexadecimal device ID string.</param>
    /// <returns>True if Ray Tracing is supported; otherwise, false.</returns>
    private bool CheckRayTracingSupportVulkan(string currentDeviceIdHex)
    {
        if (_rtSupportCache == null)
        {
            _rtSupportCache = new Dictionary<string, bool>();
            PopulateRtCache();
        }

        string key = currentDeviceIdHex.ToUpper().Trim();
        if (_rtSupportCache.ContainsKey(key))
        {
            return _rtSupportCache[key];
        }
        return false;
    }

    /// <summary>
    /// Populates the Ray Tracing support cache by parsing vulkaninfo output for device IDs and Ray Tracing extensions.
    /// </summary>
    private void PopulateRtCache()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "vulkaninfo",
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) return;

            using StreamReader reader = process.StandardOutput;
            string? line;
            
            string currentDevice = "";
            bool currentHasRt = false;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                // Detects the start of a new GPU section in vulkaninfo output.
                bool isNewGpuSection = trimmed.StartsWith("GPU id :") || 
                                      (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("="));

                if (isNewGpuSection)
                {
                    // Commits Ray Tracing support status for the previous device.
                    if (!string.IsNullOrEmpty(currentDevice))
                    {
                        if (!_rtSupportCache!.ContainsKey(currentDevice))
                            _rtSupportCache[currentDevice] = currentHasRt;
                        else if (currentHasRt) 
                            _rtSupportCache[currentDevice] = true;
                    }
                    currentDevice = "";
                    currentHasRt = false;
                }

                // Extracts device ID from the section.
                if (trimmed.StartsWith("deviceID"))
                {
                    var parts = trimmed.Split('=');
                    if (parts.Length > 1) currentDevice = parts[1].Trim().Replace("0x", "").ToUpper();
                }

                // Detects Ray Tracing extension presence for the current device.
                if (trimmed.Contains("VK_KHR_ray_tracing_pipeline") || trimmed.Contains("VK_KHR_ray_query"))
                {
                    currentHasRt = true;
                }
            }

            // Commits Ray Tracing support status for the last device after parsing.
            if (!string.IsNullOrEmpty(currentDevice))
            {
                if (!_rtSupportCache!.ContainsKey(currentDevice))
                    _rtSupportCache[currentDevice] = currentHasRt;
            }

            process.WaitForExit();
        }
        catch { }
    }
}