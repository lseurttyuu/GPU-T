using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GPU_T.Services.Probes.LinuxAmd;

public partial class LinuxAmdGpuProbe
{
    public GpuStaticData LoadStaticData()
    {
        if (!Directory.Exists(_basePath))
        {
            return new GpuStaticData { DeviceName = "No AMD GPU found at " + _basePath };
        }

        var ids = GetRawIds();
        var spec = PciIdLookup.GetSpecs(ids.Device);

        double dpmMemMultiplier = 1.0;
        
        if (spec != null && !string.IsNullOrEmpty(spec.MemoryType) && 
            spec.MemoryType.Contains("GDDR6", StringComparison.OrdinalIgnoreCase))
        {
            dpmMemMultiplier = 2.0;
        }

        // --- DANE SYSTEMOWE ---
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

        // --- ZEGARY DPM ---
        double maxCoreDpm = GetMaxClockFromDpm("pp_dpm_sclk");
        double maxMemDpm = GetMaxClockFromDpm("pp_dpm_mclk") * dpmMemMultiplier;

        // --- DETEKCJA FUNKCJI ---
        bool isHipAvailable = File.Exists("/opt/rocm/lib/libamdhip64.so") || 
                              File.Exists("/usr/lib/libamdhip64.so") ||
                              File.Exists("/usr/lib/x86_64-linux-gnu/libamdhip64.so");

        bool isOpenglAvailable = CheckOpenglSupport();
        bool isRayTracingAvailable = CheckRayTracingSupportVulkan(ids.Device);

        // --- OBLICZENIA ---
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
            Revision = ReadFile("revision", "N/A").Replace("0x", "").ToUpper(),
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

    private record RawIds(string Vendor, string Device, string SubVendor, string SubDevice);

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

    private string SpeedToGen(double gtPerSecond)
    {
        if (gtPerSecond > 30.0) return "5.0";
        if (gtPerSecond > 15.0) return "4.0";
        if (gtPerSecond > 7.0)  return "3.0";
        if (gtPerSecond > 4.0)  return "2.0";
        return "1.1";
    }

    private string CheckResizableBar()
    {
        try
        {
            long total = long.Parse(ReadFile("mem_info_vram_total", "0"));
            long visible = long.Parse(ReadFile("mem_info_vis_vram_total", "0"));
            if (total == 0) return "N/A";
            if (visible > (total * 0.9)) return "Enabled";
            return "Disabled";
        }
        catch { return "Unknown"; }
    }

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

    private string GetVulkanApiVersion()
    {
        try {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary") {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var match = Regex.Match(output, @"apiVersion\s*=\s*([\d\.]+)");
                if (match.Success) return match.Groups[1].Value;
            }
        } catch {}
        return "N/A";
    }

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