using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Models;
using GPU_T.Services.Advanced;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxIntel;

/// <summary>
/// Probe implementation for Intel GPUs on Linux. Reads i915/xe driver sysfs paths
/// for frequency data and uses shared helpers for feature detection.
/// Integrated GPUs have limited sensor availability (no fan, power, VRAM, or load metrics).
/// </summary>
public class LinuxIntelGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _drmPath;
    private readonly string _gpuId;

    public LinuxIntelGpuProbe(string gpuId)
    {
        _gpuId = gpuId;
        _basePath = $"/sys/class/drm/{gpuId}/device";
        _drmPath = $"/sys/class/drm/{gpuId}";
    }

    #region Static Data

    public GpuStaticData LoadStaticData()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "").ToUpper();

        string busId = GpuFeatureDetection.GetBusId(_basePath);

        // Try database lookup first, then lspci fallback
        string deviceName = "Unknown Intel GPU";
        string codeName = "N/A";
        string technology = "N/A";
        string dieSize = "N/A";
        string releaseDate = "N/A";
        string transistors = "N/A";
        string ropsTmus = "N/A";
        string shaders = "N/A";
        string computeUnits = "N/A";
        string memoryType = "N/A";
        string busWidth = "N/A";
        string defaultGpuClock = "N/A";
        string defaultBoostClock = "N/A";
        string defaultMemClock = "N/A";
        string lookupUrl = "";
        bool isExactMatch = true;
        string pixelFillrate = "N/A";
        string textureFillrate = "N/A";
        string bandwidth = "N/A";

        var spec = PciIdLookup.GetSpecs(ids.Device, revId);
        if (spec != null)
        {
            deviceName = spec.Name;
            if (!deviceName.StartsWith("Intel", StringComparison.OrdinalIgnoreCase))
                deviceName = $"Intel {deviceName}";
            isExactMatch = spec.IsExactMatch;
            codeName = spec.CodeName;
            technology = spec.Technology;
            dieSize = spec.DieSize;
            releaseDate = spec.ReleaseDate;
            transistors = spec.Transistors;
            shaders = spec.Shaders;
            computeUnits = spec.ComputeUnits;
            memoryType = spec.MemoryType;
            busWidth = spec.BusWidth;
            defaultGpuClock = spec.DefGpuClock;
            defaultBoostClock = spec.DefBoostClock;
            defaultMemClock = spec.DefMemClock;
            lookupUrl = spec.LookupUrl;

            if (spec.Rops != "N/A" || spec.Tmus != "N/A")
                ropsTmus = $"{spec.Rops} / {spec.Tmus}";
        }
        else
        {
            // Fallback: try to identify from lspci
            deviceName = GetDeviceNameFromLspci(busId);
            if (string.IsNullOrEmpty(deviceName) || deviceName == "Unknown")
                deviceName = "Unknown Intel GPU";
            else if (!deviceName.StartsWith("Intel", StringComparison.OrdinalIgnoreCase))
                deviceName = $"Intel {deviceName}";
            isExactMatch = false;
        }

        // Read clocks from i915 DRM sysfs (gt_*_freq_mhz files are at the DRM card level)
        string maxGpuClockStr = ReadDrmFile("gt_max_freq_mhz");
        string boostClockStr = ReadDrmFile("gt_boost_freq_mhz");
        string curGpuClockStr = ReadDrmFile("gt_cur_freq_mhz");

        // Use sysfs values if database didn't provide defaults
        if (defaultGpuClock == "N/A" && !string.IsNullOrEmpty(maxGpuClockStr))
            defaultGpuClock = $"{maxGpuClockStr} MHz";
        if (defaultBoostClock == "N/A" && !string.IsNullOrEmpty(boostClockStr))
            defaultBoostClock = $"{boostClockStr} MHz";

        string currentGpuClock = !string.IsNullOrEmpty(curGpuClockStr) ? $"{curGpuClockStr} MHz" : "---";
        string currentBoostClock = currentGpuClock;

        // Memory: integrated GPUs use shared system RAM
        string memorySize = GetSharedMemorySize();

        string driverVersion = GpuFeatureDetection.GetRealDriverVersion();
        string driverDate = GpuFeatureDetection.GetDriverDate();
        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion();

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        // OpenCL: check for Intel ICD
        bool isOpenClAvailable = File.Exists("/etc/OpenCL/vendors/intel.icd") ||
                                 File.Exists("/etc/OpenCL/vendors/intel_icd.x86_64.icd");

        return new GpuStaticData
        {
            DeviceName = deviceName,
            IsExactMatch = isExactMatch,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = busId,
            BiosVersion = "N/A",
            DriverVersion = driverVersion,
            DriverDate = driverDate,
            VulkanApi = vulkanApi,
            BusInterface = busInterface,
            ResizableBarState = "N/A",

            Revision = revId,

            GpuCodeName = codeName,
            Technology = technology,
            DieSize = dieSize,
            ReleaseDate = releaseDate,
            Transistors = transistors,

            RopsTmus = ropsTmus,
            Shaders = shaders,
            ComputeUnits = computeUnits,
            PixelFillrate = pixelFillrate,
            TextureFillrate = textureFillrate,

            MemoryType = memoryType,
            BusWidth = busWidth,
            MemorySize = memorySize,
            Bandwidth = bandwidth,

            DefaultGpuClock = defaultGpuClock,
            DefaultBoostClock = defaultBoostClock,
            DefaultMemoryClock = defaultMemClock,
            CurrentGpuClock = currentGpuClock,
            BoostClock = currentBoostClock,
            CurrentMemClock = "N/A",

            IsOpenClAvailable = isOpenClAvailable,
            IsVulkanAvailable = vulkanApi != "N/A",
            IsOpenglAvailable = isOpenglAvailable,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),

            LookupUrl = lookupUrl,
        };
    }

    #endregion

    #region Sensor Data

    public GpuSensorData LoadSensorData()
    {
        // GPU clock from i915 DRM sysfs
        double gpuClock = 0;
        string curFreq = ReadDrmFile("gt_cur_freq_mhz");
        if (!string.IsNullOrEmpty(curFreq))
            double.TryParse(curFreq, NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);

        // GPU temperature: try i915 hwmon first, then thermal zones
        double gpuTemp = GetGpuTemperature();

        double cpuTemp = GetCpuTemperature();
        double sysRam = GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = gpuClock,
            MemoryClock = 0,
            GpuTemp = gpuTemp,
            FanPercent = 0,
            BoardPower = 0,
            GpuLoad = 0,
            MemControllerLoad = 0,
            MemoryUsed = 0,
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam
        };
    }

    #endregion

    #region Sensor Availability

    public SensorAvailability GetSensorAvailability()
    {
        // Integrated Intel GPUs have very limited sensor availability
        return new SensorAvailability();
    }

    #endregion

    #region Advanced Data

    public AdvancedDataProvider? GetAdvancedDataProvider(string category)
    {
        return category switch
        {
            "General" => new GeneralProvider(),
            "Vulkan" => new VulkanProvider(),
            "OpenCL" => new OpenClProvider(),
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return new[] { "General", "Vulkan", "OpenCL" };
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Reads a file from the DRM card directory (e.g., gt_cur_freq_mhz).
    /// </summary>
    private string ReadDrmFile(string filename)
    {
        try
        {
            string path = Path.Combine(_drmPath, filename);
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
        }
        catch { }
        return "";
    }

    /// <summary>
    /// Gets total system RAM as "Shared (X MB)" for integrated GPUs.
    /// </summary>
    private static string GetSharedMemorySize()
    {
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                string[] lines = File.ReadAllLines("/proc/meminfo");
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        double kb = ExtractKb(line);
                        if (kb > 0)
                        {
                            int mb = (int)(kb / 1024.0);
                            return $"{mb} MB (Shared)";
                        }
                    }
                }
            }
        }
        catch { }
        return "N/A";
    }

    /// <summary>
    /// Tries to get the GPU temperature from i915 hwmon or thermal zones.
    /// </summary>
    private double GetGpuTemperature()
    {
        // Try hwmon directories for an i915-named sensor
        try
        {
            string baseDir = "/sys/class/hwmon/";
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath))
                    {
                        string name = File.ReadAllText(namePath).Trim();
                        if (name == "i915")
                        {
                            string tempPath = Path.Combine(dir, "temp1_input");
                            if (File.Exists(tempPath))
                            {
                                if (double.TryParse(File.ReadAllText(tempPath).Trim(), out double val))
                                    return val / 1000.0;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // Fallback: scan thermal zones for an Intel GPU zone
        try
        {
            string thermalBase = "/sys/class/thermal/";
            if (Directory.Exists(thermalBase))
            {
                foreach (var dir in Directory.GetDirectories(thermalBase, "thermal_zone*"))
                {
                    string typePath = Path.Combine(dir, "type");
                    if (File.Exists(typePath))
                    {
                        string type = File.ReadAllText(typePath).Trim();
                        if (type.Contains("gpu", StringComparison.OrdinalIgnoreCase) ||
                            type.Contains("i915", StringComparison.OrdinalIgnoreCase))
                        {
                            string tempPath = Path.Combine(dir, "temp");
                            if (File.Exists(tempPath))
                            {
                                if (double.TryParse(File.ReadAllText(tempPath).Trim(), out double val))
                                    return val / 1000.0;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        return 0;
    }

    /// <summary>
    /// Tries to get the device name from lspci output.
    /// </summary>
    private static string GetDeviceNameFromLspci(string busId)
    {
        try
        {
            string output = ShellHelper.RunCommand("lspci", $"-s {busId}");
            if (!string.IsNullOrEmpty(output))
            {
                int colonIdx = output.IndexOf(": ", StringComparison.Ordinal);
                if (colonIdx > 0)
                {
                    string name = output.Substring(colonIdx + 2).Trim();
                    var revMatch = Regex.Match(name, @"\s*\(rev\s+\w+\)\s*$");
                    if (revMatch.Success)
                        name = name.Substring(0, revMatch.Index).Trim();
                    return name;
                }
            }
        }
        catch { }
        return "Unknown";
    }

    private double GetCpuTemperature()
    {
        try
        {
            var baseDir = "/sys/class/hwmon/";
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath))
                    {
                        string name = File.ReadAllText(namePath).Trim();
                        if (name == "k10temp" || name == "coretemp")
                        {
                            string tempPath = Path.Combine(dir, "temp1_input");
                            if (File.Exists(tempPath))
                            {
                                if (double.TryParse(File.ReadAllText(tempPath), out double val))
                                    return val / 1000.0;
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    private double GetSystemRamUsage()
    {
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                string[] lines = File.ReadAllLines("/proc/meminfo");
                double total = 0, avail = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:")) total = ExtractKb(line);
                    else if (line.StartsWith("MemAvailable:")) avail = ExtractKb(line);
                    if (total > 0 && avail > 0) break;
                }
                return (total - avail) / 1024.0;
            }
        }
        catch { }
        return 0;
    }

    private static double ExtractKb(string line)
    {
        var match = Regex.Match(line, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }

    #endregion
}
