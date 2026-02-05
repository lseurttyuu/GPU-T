using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GPU_T.Services.Probes.LinuxAmd;

public partial class LinuxAmdGpuProbe
{
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

                bool isNewGpuSection = trimmed.StartsWith("GPU id :") || 
                                      (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("="));

                if (isNewGpuSection)
                {
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

                if (trimmed.StartsWith("deviceID"))
                {
                    var parts = trimmed.Split('=');
                    if (parts.Length > 1) currentDevice = parts[1].Trim().Replace("0x", "").ToUpper();
                }

                if (trimmed.Contains("VK_KHR_ray_tracing_pipeline") || trimmed.Contains("VK_KHR_ray_query"))
                {
                    currentHasRt = true;
                }
            }

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