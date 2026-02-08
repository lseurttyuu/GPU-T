using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides static hardware and capability discovery for AMD GPUs on Linux.
/// Contains logic to read sysfs, invoke platform utilities, and compute derived specifications.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    /// <summary>
    /// Loads static data describing the GPU, driver, and capabilities.
    /// </summary>
    /// <returns>
    /// A <see cref="GpuStaticData"/> instance populated from sysfs, vendor databases, and utility probes.
    /// </returns>
    public GpuStaticData LoadStaticData()
    {
        if (!Directory.Exists(_basePath))
        {
            return new GpuStaticData { DeviceName = "No AMD GPU found at " + _basePath };
        }

        var ids = GetRawIds();
        string revId = ReadFile("revision").Replace("0x", "").ToUpper();

        var spec = PciIdLookup.GetSpecs(ids.Device, revId);

        double dpmMemMultiplier = 1.0;
        
        // Apply multiplier when detected memory type implies effective DPM frequency doubling (e.g., GDDR6).
        if (spec != null && !string.IsNullOrEmpty(spec.MemoryType) && 
            spec.MemoryType.Contains("GDDR6", StringComparison.OrdinalIgnoreCase))
        {
            dpmMemMultiplier = 2.0;
        }

        string vramVendor = ReadFile("mem_info_vram_vendor"); 
        string memTypeDb = spec?.MemoryType ?? "Unknown";
        string finalMemType = !string.IsNullOrEmpty(vramVendor) && vramVendor != "N/A"
            ? $"{memTypeDb} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vramVendor)})"
            : memTypeDb;

        string finalMemorySize = GetVramSize(); 
        if (finalMemorySize == "0 MB") finalMemorySize = "N/A";

        string busInterface = GetPcieInfo();
        string reBarState = CheckResizableBar();

        string driverVer = GetRealDriverVersion();
        string driverDate = GetDriverDate();
        string vulkanApi = GetVulkanApiVersion();

        double maxCoreDpm = GetMaxClockFromDpm("pp_dpm_sclk");
        double maxMemDpm = GetMaxClockFromDpm("pp_dpm_mclk") * dpmMemMultiplier;

        // Detect presence of optional runtime libraries or capabilities; these checks reflect user-facing feature toggles.
        bool isHipAvailable = File.Exists("/opt/rocm/lib/libamdhip64.so") || 
                              File.Exists("/usr/lib/libamdhip64.so") ||
                              File.Exists("/usr/lib/x86_64-linux-gnu/libamdhip64.so");

        bool isOpenglAvailable = CheckOpenglSupport();
        bool isRayTracingAvailable = CheckRayTracingSupportVulkan(ids.Device);

        string pixelFill = "N/A";
        string texFill = "N/A";
        string bandwidth = "N/A";
        string ropsTmus = "N/A";
        string lookupUrl = "";

        string gpuClock = "---";
        string boostClockDisplay = "---";
        string memClockDisplay = "---";

        if (maxCoreDpm > 0)
        {
            string coreStr = $"{maxCoreDpm.ToString(CultureInfo.InvariantCulture)} MHz";
            gpuClock = coreStr;
            boostClockDisplay = coreStr;
        }

        if (maxMemDpm > 0)
        {
            string memStr = $"{maxMemDpm.ToString(CultureInfo.InvariantCulture)} MHz";
            memClockDisplay = memStr;
        }

        if (spec != null)
        {
            lookupUrl = spec.LookupUrl;
            ropsTmus = $"{spec.Rops} / {spec.Tmus}";
            double boostClock = ExtractNumber(spec.DefBoostClock);
            double memClock = ExtractNumber(spec.DefMemClock);
            double busWidth = ExtractNumber(spec.BusWidth);
            double rops = ExtractNumber(spec.Rops);
            double tmus = ExtractNumber(spec.Tmus);

            if (boostClock > 0 && rops > 0 && tmus > 0)
            {
                pixelFill = $"{(boostClock * rops / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GPixel/s";
                texFill = $"{(boostClock * tmus / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GTexel/s";
            }

            if (memClock > 0 && busWidth > 0)
            {
                double multiplier = GetMemoryMultiplier(spec.MemoryType);
                double bandwidthValue = (memClock * multiplier * busWidth) / 8000.0;
                bandwidth = $"{bandwidthValue.ToString("0.0", CultureInfo.InvariantCulture)} GB/s";
            }
        }

        return new GpuStaticData
        {
            DeviceName = spec?.Name ?? "Unknown AMD GPU",
            IsExactMatch = spec?.IsExactMatch ?? true,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = GetBusId(),
            BiosVersion = ReadFile("vbios_version", "Unknown"),
            DriverVersion = driverVer,
            DriverDate = driverDate,
            VulkanApi = vulkanApi,
            BusInterface = busInterface, 
            ResizableBarState = reBarState,
            LookupUrl = lookupUrl,

            GpuCodeName = spec?.CodeName ?? "N/A",
            Revision = revId,
            Technology = spec?.Technology ?? "N/A",
            DieSize = spec?.DieSize ?? "N/A",
            ReleaseDate = spec?.ReleaseDate ?? "N/A",
            Transistors = spec?.Transistors ?? "N/A",
            RopsTmus = ropsTmus,
            Shaders = spec?.Shaders ?? "N/A",
            ComputeUnits = spec?.ComputeUnits ?? "N/A",
            PixelFillrate = pixelFill,
            TextureFillrate = texFill,
            MemoryType = finalMemType,
            BusWidth = spec?.BusWidth ?? "N/A",
            MemorySize = finalMemorySize,
            Bandwidth = bandwidth,
            DefaultGpuClock = spec?.DefGpuClock ?? "N/A",
            DefaultBoostClock = spec?.DefBoostClock ?? "N/A",
            DefaultMemoryClock = spec?.DefMemClock ?? "N/A",
            
            CurrentGpuClock = gpuClock,
            BoostClock = boostClockDisplay,
            CurrentMemClock = memClockDisplay,

            IsHsaAvailable = isHipAvailable, 
            IsRocmAvailable = Directory.Exists("/opt/rocm") || Directory.Exists("/usr/lib/x86_64-linux-gnu/rocm"),
            IsVulkanAvailable = vulkanApi != "N/A",
            IsOpenClAvailable = File.Exists("/etc/OpenCL/vendors/amdocl64.icd") || File.Exists("/etc/OpenCL/vendors/mesa.icd"),
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
            
            IsCudaAvailable = false,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsPhysXEnabled = false,
            IsOpenglAvailable = isOpenglAvailable
        };
    }

    /// <summary>
    /// Represents raw PCI IDs for device and subsystem.
    /// </summary>
    private record RawIds(string Vendor, string Device, string SubVendor, string SubDevice);

    /// <summary>
    /// Reads PCI IDs from sysfs files and returns a RawIds record.
    /// </summary>
    /// <returns>RawIds containing vendor, device, subvendor, and subdevice IDs.</returns>
    private RawIds GetRawIds()
    {
        try 
        {
            string v = ReadFile("vendor").Replace("0x", "").ToUpper();
            string d = ReadFile("device").Replace("0x", "").ToUpper();
            string sv = ReadFile("subsystem_vendor").Replace("0x", "").ToUpper();
            string sd = ReadFile("subsystem_device").Replace("0x", "").ToUpper();
            return new RawIds(v, d, sv, sd);
        }
        catch
        {
            return new RawIds("0000", "0000", "0000", "0000");
        }
    }

    /// <summary>
    /// Reads VRAM size from sysfs and returns it as a formatted string.
    /// </summary>
    /// <returns>VRAM size in MB, or "0 MB" if unavailable.</returns>
    private string GetVramSize()
    {
        try 
        {
            string content = ReadFile("mem_info_vram_total");
            if (long.TryParse(content, out long bytes))
            {
                long mb = bytes / (1024 * 1024);
                return $"{mb} MB";
            }
        }
        catch {}
        return "0 MB";
    }

    /// <summary>
    /// Retrieves PCIe interface information including capability and current link state.
    /// </summary>
    /// <returns>Formatted PCIe capability and current link information.</returns>
    private string GetPcieInfo()
    {
        string maxSpeedStr = ReadFile("max_link_speed");
        string maxWidthStr = ReadFile("max_link_width");
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
            string dpmPath = Path.Combine(_basePath, "pp_dpm_pcie");
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
                string speedStr = ReadFile("current_link_speed");
                string widthStr = ReadFile("current_link_width");
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
    /// <param name="gtPerSecond">GT/s speed value.</param>
    /// <returns>PCIe generation as string.</returns>
    private string SpeedToGen(double gtPerSecond)
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
    /// <returns>"Enabled", "Disabled", "N/A", or "Unknown".</returns>
    private string CheckResizableBar()
    {
        try
        {
            // Determine total VRAM in bytes; used to estimate whether a BAR maps most of VRAM (heuristic for ReBAR).
            long totalVram = long.Parse(ReadFile("mem_info_vram_total", "0"));
            if (totalVram == 0) return "N/A";

            string resourceContent = ReadFile("resource");

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
                    catch
                    {
                        // Ignore parsing errors for individual resource lines.
                        continue;
                    }
                }
            }

            // Heuristic: if the largest BAR maps more than 90% of VRAM, consider ReBAR enabled.
            if (maxBarSize > (totalVram * 0.9))
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
    /// Retrieves the real driver version using vulkaninfo or kernel version as fallback.
    /// </summary>
    /// <returns>Driver version string.</returns>
    private string GetRealDriverVersion()
    {
        try {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary") {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string vInfo = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var match = Regex.Match(vInfo, @"driverInfo\s*=\s*(.*)");
                if (match.Success) return match.Groups[1].Value.Trim();
            }
        } catch {}

        string kernel = GetKernelVersion();
        if (!string.IsNullOrEmpty(kernel)) return $"{kernel} (Kernel)";
        return "Unknown";
    }

    /// <summary>
    /// Retrieves the kernel version using uname.
    /// </summary>
    /// <returns>Kernel version string.</returns>
    private string GetKernelVersion()
    {
        try {
            var psi = new ProcessStartInfo("uname", "-r") {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
        } catch {}
        return "";
    }

    /// <summary>
    /// Retrieves the Vulkan API version using vulkaninfo.
    /// </summary>
    /// <returns>Vulkan API version string or "N/A".</returns>
    private string GetVulkanApiVersion()
    {
        try 
        {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary") {
                RedirectStandardOutput = true, 
                UseShellExecute = false, 
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                // Use a non-greedy search to extract a semantic apiVersion string if present.
                var match = Regex.Match(output, @"apiVersion\s*=\s*.*?(\d+\.\d+(?:\.\d+)?)");
                
                if (match.Success) return match.Groups[1].Value;
            }
        } 
        catch {}
        return "N/A";
    }

    /// <summary>
    /// Retrieves the driver date from /proc/version.
    /// </summary>
    /// <returns>Driver date string or "N/A".</returns>
    private string GetDriverDate()
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
        catch {}
        return "N/A";
    }

    /// <summary>
    /// Retrieves the PCI bus ID from sysfs link or directory name.
    /// </summary>
    /// <returns>Bus ID string or "Unknown".</returns>
    private string GetBusId()
    {
        try
        {
            var targetInfo = File.ResolveLinkTarget(_basePath, true);
            if (targetInfo != null) return targetInfo.Name;
            return new DirectoryInfo(_basePath).Name; 
        }
        catch { return "Unknown"; }
    }

    /// <summary>
    /// Reads the maximum clock value from a DPM file.
    /// </summary>
    /// <param name="fileName">DPM file name.</param>
    /// <returns>Maximum clock value in MHz.</returns>
    private double GetMaxClockFromDpm(string fileName)
    {
        try
        {
            string path = Path.Combine(_basePath, fileName);
            if (!File.Exists(path)) return 0;

            string[] lines = File.ReadAllLines(path);
            double maxClock = 0;

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"(\d+)\s*Mhz", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        if (val > maxClock) maxClock = val;
                    }
                }
            }
            return maxClock;
        }
        catch { return 0; }
    }
}