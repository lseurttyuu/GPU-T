using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GPU_T.Services.Utilities;

/// <summary>
/// Shared feature detection and sysfs helper methods used by all GPU probe implementations.
/// </summary>
public static class GpuFeatureDetection
{
    private static Dictionary<string, bool>? _rtSupportCache;

    /// <summary>
    /// Checks whether OpenGL direct rendering is supported and not using software rendering.
    /// </summary>
    public static bool CheckOpenglSupport()
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
    public static bool CheckRayTracingSupportVulkan(string currentDeviceIdHex)
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
    /// Populates the Ray Tracing support cache by parsing vulkaninfo output.
    /// </summary>
    private static void PopulateRtCache()
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

    /// <summary>
    /// Retrieves the Vulkan API version using vulkaninfo --summary.
    /// </summary>
    public static string GetVulkanApiVersion()
    {
        try
        {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary")
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var match = Regex.Match(output, @"apiVersion\s*=\s*.*?(\d+\.\d+(?:\.\d+)?)");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }
        return "N/A";
    }

    /// <summary>
    /// Retrieves the real driver version using vulkaninfo or kernel version as fallback.
    /// </summary>
    public static string GetRealDriverVersion()
    {
        try
        {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary")
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string vInfo = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var match = Regex.Match(vInfo, @"driverInfo\s*=\s*(.*)");
                if (match.Success) return match.Groups[1].Value.Trim();
            }
        }
        catch { }

        string kernel = GetKernelVersion();
        if (!string.IsNullOrEmpty(kernel)) return $"{kernel} (Kernel)";
        return "Unknown";
    }

    /// <summary>
    /// Retrieves the kernel version using uname.
    /// </summary>
    public static string GetKernelVersion()
    {
        try
        {
            var psi = new ProcessStartInfo("uname", "-r")
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
        }
        catch { }
        return "";
    }

    /// <summary>
    /// Retrieves the driver date from /proc/version.
    /// </summary>
    public static string GetDriverDate()
    {
        try
        {
            if (File.Exists("/proc/version"))
            {
                string content = File.ReadAllText("/proc/version").Trim();
                var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 6)
                {
                    string day = parts[^4];
                    string month = parts[^5];
                    string year = parts[^1];
                    return $"{day} {month} {year}";
                }
            }
        }
        catch { }
        return "N/A";
    }

    /// <summary>
    /// Retrieves the PCI bus ID from sysfs link or directory name.
    /// </summary>
    public static string GetBusId(string basePath)
    {
        try
        {
            var targetInfo = File.ResolveLinkTarget(basePath, true);
            if (targetInfo != null) return targetInfo.Name;
            return new DirectoryInfo(basePath).Name;
        }
        catch { return "Unknown"; }
    }

    /// <summary>
    /// Retrieves PCIe interface information including capability and current link state.
    /// </summary>
    /// <param name="basePath">The sysfs base path for the GPU device.</param>
    public static string GetPcieInfo(string basePath)
    {
        string maxSpeedStr = ReadSysfsFile(basePath, "max_link_speed");
        string maxWidthStr = ReadSysfsFile(basePath, "max_link_width");
        string maxGen = "Unknown";
        if (double.TryParse(Regex.Match(maxSpeedStr, @"(\d+\.?\d*)").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double maxSpeedVal))
        {
            maxGen = SpeedToGen(maxSpeedVal);
        }
        string capability = $"PCIe x{maxWidthStr} {maxGen}";

        string currentGen = "?";
        string currentWidth = "?";
        bool foundInDpm = false;

        try
        {
            string dpmPath = Path.Combine(basePath, "pp_dpm_pcie");
            if (File.Exists(dpmPath))
            {
                string[] lines = File.ReadAllLines(dpmPath);
                foreach (var line in lines)
                {
                    if (line.Contains("*"))
                    {
                        var match = Regex.Match(line, @"(\d+\.?\d*)GT/s,\s*x(\d+)");
                        if (match.Success)
                        {
                            double speedGt = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                            currentWidth = match.Groups[2].Value;
                            currentGen = SpeedToGen(speedGt);
                            foundInDpm = true;
                        }
                        break;
                    }
                }
            }
        }
        catch { }

        if (!foundInDpm)
        {
            try
            {
                string speedStr = ReadSysfsFile(basePath, "current_link_speed");
                string widthStr = ReadSysfsFile(basePath, "current_link_width");
                currentWidth = widthStr;
                var match = Regex.Match(speedStr, @"(\d+\.?\d*)");
                if (match.Success)
                {
                    double speedGt = double.Parse(match.Value, CultureInfo.InvariantCulture);
                    currentGen = SpeedToGen(speedGt);
                }
            }
            catch
            {
                currentWidth = "x16";
                currentGen = "Unknown";
            }
        }
        return $"{capability} @ x{currentWidth} {currentGen}";
    }

    /// <summary>
    /// Maps PCIe GT/s speed to PCIe generation string.
    /// </summary>
    public static string SpeedToGen(double gtPerSecond)
    {
        if (gtPerSecond > 30.0) return "5.0";
        if (gtPerSecond > 15.0) return "4.0";
        if (gtPerSecond > 7.0)  return "3.0";
        if (gtPerSecond > 4.0)  return "2.0";
        return "1.1";
    }

    /// <summary>
    /// Determines if Resizable BAR (ReBAR) is enabled using a heuristic based on BAR size and VRAM.
    /// </summary>
    /// <param name="basePath">The sysfs base path for the GPU device.</param>
    /// <param name="totalVramBytes">Total VRAM in bytes. Pass 0 to read from sysfs (AMD-specific).</param>
    public static string CheckResizableBar(string basePath, long totalVramBytes = 0)
    {
        try
        {
            if (totalVramBytes == 0) return "N/A";

            string resourceContent = ReadSysfsFile(basePath, "resource");

            if (string.IsNullOrWhiteSpace(resourceContent)) return "Unknown";

            long maxBarSize = 0;
            var lines = resourceContent.Split('\n');

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    try
                    {
                        long start = Convert.ToInt64(parts[0], 16);
                        long end = Convert.ToInt64(parts[1], 16);
                        long size = (end - start) + 1;
                        if (size > maxBarSize) maxBarSize = size;
                    }
                    catch { continue; }
                }
            }

            if (maxBarSize > (totalVramBytes * 0.9))
            {
                return "Enabled";
            }

            return "Disabled";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Reads the contents of a file in a sysfs base path, returning a fallback value if unavailable.
    /// </summary>
    public static string ReadSysfsFile(string basePath, string filename, string fallback = "N/A")
    {
        try
        {
            string path = Path.Combine(basePath, filename);
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
        }
        catch { }
        return fallback;
    }

    /// <summary>
    /// Reads PCI IDs from sysfs and returns them as a tuple.
    /// </summary>
    public static (string Vendor, string Device, string SubVendor, string SubDevice) GetRawPciIds(string basePath)
    {
        try
        {
            string v = ReadSysfsFile(basePath, "vendor").Replace("0x", "").ToUpper();
            string d = ReadSysfsFile(basePath, "device").Replace("0x", "").ToUpper();
            string sv = ReadSysfsFile(basePath, "subsystem_vendor").Replace("0x", "").ToUpper();
            string sd = ReadSysfsFile(basePath, "subsystem_device").Replace("0x", "").ToUpper();
            return (v, d, sv, sd);
        }
        catch
        {
            return ("0000", "0000", "0000", "0000");
        }
    }
}
