using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GPU_T.Models;
using GPU_T.Services.Advanced;
using GPU_T.Services.Advanced.LinuxIntel;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxIntel;

/// <summary>
/// Probe implementation for Intel GPUs on Linux. Reads i915/xe driver sysfs paths
/// for frequency data and uses shared helpers for feature detection.
/// </summary>
public class LinuxIntelGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _drmPath;
    private readonly string _gpuId;
    private readonly string _driverName;

    private class ProbeStateCache
    {
        public bool HasInitialData = false;
        public GpuSensorData LastData = new();
        public bool IsUpdating = false;
        public readonly object LockObj = new object();
    }

    private static readonly Dictionary<string, ProbeStateCache> _stateCache = new();

    public LinuxIntelGpuProbe(string gpuId)
    {
        _gpuId = gpuId;
        _basePath = $"/sys/class/drm/{gpuId}/device";
        _drmPath = $"/sys/class/drm/{gpuId}";
        _driverName = GetDriverName();
    }

    private string GetDriverName()
    {
        try
        {
            string driverLink = Path.Combine(_basePath, "driver");
            if (Directory.Exists(driverLink) || File.Exists(driverLink))
            {
                var target = File.ResolveLinkTarget(driverLink, true);
                if (target != null) return target.Name;

                // Fallback if ResolveLinkTarget fails
                return new DirectoryInfo(driverLink).Name;
            }
        }
        catch { }
        return "Unknown";
    }

    #region Static Data

    public GpuStaticData LoadStaticData()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "", StringComparison.OrdinalIgnoreCase).ToUpper();

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

        // Ensure revision ID is padded for consistent database lookup
        string paddedRev = revId.PadLeft(2, '0').ToUpper();
        var spec = PciIdLookup.GetSpecs(ids.Vendor, ids.Device, paddedRev);
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
            deviceName = CommonGpuHelpers.GetDeviceNameFromLspci(busId);
            if (string.IsNullOrEmpty(deviceName) || deviceName == "Unknown")
                deviceName = "Unknown Intel GPU";
            else if (!deviceName.StartsWith("Intel", StringComparison.OrdinalIgnoreCase))
                deviceName = $"Intel {deviceName}";
            isExactMatch = false;
        }

        // Read clocks
        string currentGpuClock = "---";
        string currentBoostClock = "---";
        double activeGpuMHz = 0;

        if (_driverName == "xe")
        {
            string curFreq = ReadDeviceFile("tile0/gt0/freq0/act_freq");
            if (string.IsNullOrEmpty(curFreq)) curFreq = ReadDeviceFile("tile0/gt0/freq0/cur_freq");
            if (!string.IsNullOrEmpty(curFreq))
            {
                currentGpuClock = $"{curFreq} MHz";
                double.TryParse(curFreq, out activeGpuMHz);
            }

            string maxFreq = ReadDeviceFile("tile0/gt0/freq0/rp0_freq");
            if (!string.IsNullOrEmpty(maxFreq)) currentBoostClock = $"{maxFreq} MHz";
        }
        else // i915 or fallback
        {
            string curGpuClockStr = ReadDrmFile("gt_cur_freq_mhz");
            if (!string.IsNullOrEmpty(curGpuClockStr))
            {
                currentGpuClock = $"{curGpuClockStr} MHz";
                double.TryParse(curGpuClockStr, out activeGpuMHz);
            }

            string boostClockStr = ReadDrmFile("gt_boost_freq_mhz");
            if (!string.IsNullOrEmpty(boostClockStr)) currentBoostClock = $"{boostClockStr} MHz";
        }

        // Memory size detection
        long totalVramBytes = GetTotalVramBytes();
        string memorySize = totalVramBytes > 0 ? $"{totalVramBytes / (1024 * 1024)} MB" : "System Shared";
        // Attempt database fallback for memory size if sysfs returns 0
        if (totalVramBytes == 0 && spec != null && spec.MemoryType != "N/A" && spec.MemoryType != "System Shared")
        {
             // Extract memory size from spec.MemoryType if it contains "GB" or "MB"
             var match = Regex.Match(spec.MemoryType, @"(\d+\s*(GB|MB))", RegexOptions.IgnoreCase);
             if (match.Success)
             {
                 memorySize = match.Groups[1].Value;
             }
        }

        // Dynamic Spec Calculations (Fillrates & Bandwidth)
        if (spec != null)
        {
            double bWidth = CommonGpuHelpers.ExtractNumber(spec.BusWidth);
            double mClock = CommonGpuHelpers.ExtractNumber(spec.DefMemClock);
            double ropCount = CommonGpuHelpers.ExtractNumber(spec.Rops);
            double tmuCount = CommonGpuHelpers.ExtractNumber(spec.Tmus);
            double multiplier = CommonGpuHelpers.GetMemoryMultiplier(spec.MemoryType);

            if (bWidth > 0 && mClock > 0)
            {
                double bw = (bWidth * mClock * multiplier) / 8000.0;
                bandwidth = bw.ToString("F1", CultureInfo.InvariantCulture) + " GB/s";
            }

            double fillClock = activeGpuMHz > 0 ? activeGpuMHz : CommonGpuHelpers.ExtractNumber(spec.DefGpuClock);
            if (fillClock > 0)
            {
                if (ropCount > 0) pixelFillrate = ((ropCount * fillClock) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " GPixel/s";
                if (tmuCount > 0) textureFillrate = ((tmuCount * fillClock) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " GTexel/s";
            }
        }

        string driverVersion = GpuFeatureDetection.GetRealDriverVersion(ids.Device);
        string driverDate = GpuFeatureDetection.GetKernelDriverDate();
        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion(ids.Device);

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        // Intel-specific computing technologies
        bool isOpenClAvailable = GpuFeatureDetection.CheckOpenClIcdInstalled("intel.icd", "intel_icd.x86_64.icd");
        bool isOneApiAvailable = GpuFeatureDetection.IsNativeLibraryAvailable("libze_intel_gpu.so.1");
        // SYCL: On Linux, Intel SYCL is typically implemented over Level Zero (oneAPI) or standalone libsycl
        bool isSyclAvailable = isOneApiAvailable || GpuFeatureDetection.IsNativeLibraryAvailable("libsycl.so.7") || GpuFeatureDetection.IsNativeLibraryAvailable("libsycl.so");

        string biosVersion = "N/A";
        if (_driverName == "xe")
        {
            string gmd = ReadDeviceFile("gmd_id");
            if (!string.IsNullOrEmpty(gmd)) biosVersion = $"GMD {gmd}";
        }

        return new GpuStaticData
        {
            DeviceName = deviceName,
            IsExactMatch = isExactMatch,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            VendorId = ids.Vendor,
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = busId,
            BiosVersion = biosVersion,
            DriverVersion = driverVersion,
            DriverDate = driverDate,
            VulkanApi = vulkanApi,
            BusInterface = busInterface,
            ResizableBarState = GpuFeatureDetection.CheckResizableBar(_basePath, totalVramBytes),

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
            IsOneApiAvailable = isOneApiAvailable,
            IsSyclAvailable = isSyclAvailable,
            IsVulkanAvailable = GpuFeatureDetection.CheckVulkanSupport(ids.Device, "intel_icd.x86_64.json", "intel_icd.i686.json", "intel_hasvk.json", "intel_xe_icd.x86_64.json", "intel_xe_icd.i686.json"),
            IsOpenglAvailable = isOpenglAvailable,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsUefiAvailable = CommonGpuHelpers.CheckGpuUefiSupport(spec?.ReleaseDate),

            LookupUrl = lookupUrl,
        };
    }

    #endregion

    #region Sensor Data

    public GpuSensorData LoadSensorData()
    {
        var cache = GetState();

        bool needsInitialFetch = false;
        bool needsAsyncFetch = false;

        lock (cache.LockObj)
        {
            if (!cache.HasInitialData)
            {
                needsInitialFetch = true;
                cache.IsUpdating = true;
            }
        }

        if (needsInitialFetch)
        {
            BackgroundFetchSensors(cache);
            lock (cache.LockObj)
            {
                cache.HasInitialData = true;
            }
        }

        lock (cache.LockObj)
        {
            if (!cache.IsUpdating)
            {
                needsAsyncFetch = true;
                cache.IsUpdating = true;
            }
        }

        if (needsAsyncFetch)
        {
            var probeInstance = this;
            Task.Run(() => probeInstance.BackgroundFetchSensors(cache));
        }

        lock (cache.LockObj)
        {
            return cache.LastData;
        }
    }

    private void BackgroundFetchSensors(ProbeStateCache cache)
    {
        try
        {
            double gpuClock = 0;
            if (_driverName == "xe")
            {
                string curFreq = ReadDeviceFile("tile0/gt0/freq0/act_freq");
                if (string.IsNullOrEmpty(curFreq)) curFreq = ReadDeviceFile("tile0/gt0/freq0/cur_freq");
                if (!string.IsNullOrEmpty(curFreq)) double.TryParse(curFreq, NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
            }
            else
            {
                string curFreq = ReadDrmFile("gt_cur_freq_mhz");
                if (!string.IsNullOrEmpty(curFreq)) double.TryParse(curFreq, NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
            }

            double gpuTemp = GetGpuTemperature();
            double cpuTemp = CommonGpuHelpers.GetCpuTemperature();
            double sysRam = CommonGpuHelpers.GetSystemRamUsage();

            double vramUsedMb = GetUsedVramBytes() / (1024.0 * 1024.0);
            double powerW = GetPowerUsageW();
            int load = GetGpuLoad();

            double memClock = 0;
            if (_driverName == "xe")
            {
                string curMemFreq = ReadDeviceFile("tile0/vram0/freq0/act_freq");
                if (string.IsNullOrEmpty(curMemFreq)) curMemFreq = ReadDeviceFile("tile0/vram0/freq0/cur_freq");
                if (!string.IsNullOrEmpty(curMemFreq)) double.TryParse(curMemFreq, NumberStyles.Any, CultureInfo.InvariantCulture, out memClock);
            }

            var newData = new GpuSensorData
            {
                GpuClock = gpuClock,
                MemoryClock = memClock,
                GpuTemp = gpuTemp,
                FanPercent = GetFanPercent(),
                BoardPower = powerW,
                GpuLoad = load,
                MemControllerLoad = 0,
                MemoryUsed = vramUsedMb,
                CpuTemperature = cpuTemp,
                SystemRamUsed = sysRam
            };

            lock (cache.LockObj)
            {
                cache.LastData = newData;
            }
        }
        finally
        {
            lock (cache.LockObj)
            {
                cache.IsUpdating = false;
            }
        }
    }

    private ProbeStateCache GetState()
    {
        if (!_stateCache.TryGetValue(_gpuId, out var state))
        {
            state = new ProbeStateCache();
            _stateCache[_gpuId] = state;
        }
        return state;
    }

    #endregion

    #region Sensor Availability

    public SensorAvailability GetSensorAvailability()
    {
        var avail = new SensorAvailability();
        avail.HasGpuLoad = GetGpuLoad() >= 0;
        avail.HasPower = GetPowerUsageW() > 0;
        avail.HasMemUsed = GetTotalVramBytes() > 0;
        avail.HasFan = GetFanPercent() > 0;

        if (_driverName == "xe")
        {
            // Discrete Arc GPUs on 'xe' often expose memory frequency
            string memFreq = ReadDeviceFile("tile0/vram0/freq0/act_freq");
            if (!string.IsNullOrEmpty(memFreq))
            {
                // We'll treat this as a signal that Memory Clock sensor is available
            }
        }

        // Temperature is usually available via hwmon
        if (GetGpuTemperature() > 0) avail.HasHotSpot = false; // We use GpuTemp

        return avail;
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
            "Multimedia" => new LinuxIntelMultimediaProvider(),
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return new[] { "General", "Vulkan", "OpenCL", "Multimedia" };
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
    /// Reads a file from the device sysfs directory.
    /// </summary>
    private string ReadDeviceFile(string filename)
    {
        try
        {
            string path = Path.Combine(_basePath, filename);
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
        }
        catch { }
        return "";
    }

    private long GetTotalVramBytes()
    {
        if (_driverName == "xe")
        {
            string val = ReadDeviceFile("tile0/vram0/capacity");
            if (long.TryParse(val, out long bytes)) return bytes;
        }
        return 0;
    }

    private long GetUsedVramBytes()
    {
        if (_driverName == "xe")
        {
            string val = ReadDeviceFile("tile0/vram0/usage");
            if (long.TryParse(val, out long bytes)) return bytes;
        }
        return 0;
    }

    private double GetPowerUsageW()
    {
        string hwmonPath = GetSpecificHwmonPath();
        if (string.IsNullOrEmpty(hwmonPath)) return 0;

        try
        {
            string pwrPath = Path.Combine(hwmonPath, "power1_average");
            if (!File.Exists(pwrPath)) pwrPath = Path.Combine(hwmonPath, "power1_input");

            if (File.Exists(pwrPath))
            {
                if (double.TryParse(File.ReadAllText(pwrPath).Trim(), out double val))
                    return val / 1000000.0;
            }
        }
        catch { }
        return 0;
    }

    private int GetFanPercent()
    {
        string hwmonPath = GetSpecificHwmonPath();
        if (string.IsNullOrEmpty(hwmonPath)) return 0;

        try
        {
            string pwmPath = Path.Combine(hwmonPath, "pwm1");
            string pwmMaxPath = Path.Combine(hwmonPath, "pwm1_max");
            if (File.Exists(pwmPath) && File.Exists(pwmMaxPath))
            {
                if (double.TryParse(File.ReadAllText(pwmPath).Trim(), out double cur) &&
                    double.TryParse(File.ReadAllText(pwmMaxPath).Trim(), out double max) && max > 0)
                {
                    return (int)((cur / max) * 100.0);
                }
            }
        }
        catch { }
        return 0;
    }

    private int GetGpuLoad()
    {
        // For 'xe' driver, we can try to read engine utilization from sysfs
        if (_driverName == "xe")
        {
            try
            {
                string enginesDir = Path.Combine(_basePath, "tile0/gt0/engines");
                if (Directory.Exists(enginesDir))
                {
                    double maxUtilization = 0;
                    foreach (var dir in Directory.GetDirectories(enginesDir))
                    {
                        string utilPath = Path.Combine(dir, "utilization");
                        if (File.Exists(utilPath))
                        {
                            if (double.TryParse(File.ReadAllText(utilPath).Trim(), out double util))
                            {
                                if (util > maxUtilization) maxUtilization = util;
                            }
                        }
                    }
                    if (maxUtilization > 0) return (int)Math.Clamp(maxUtilization, 0, 100);
                }
            }
            catch { }
        }

        // For i915, global load is not easily available in sysfs without debugfs/root.
        return -1;
    }

    /// <summary>
    /// Tries to get the GPU temperature from i915/xe hwmon or thermal zones.
    /// </summary>
    private double GetGpuTemperature()
    {
        // Try specific hwmon linked to this device
        string hwmonPath = GetSpecificHwmonPath();
        if (!string.IsNullOrEmpty(hwmonPath))
        {
            try
            {
                string tempPath = Path.Combine(hwmonPath, "temp1_input");
                if (File.Exists(tempPath))
                {
                    if (double.TryParse(File.ReadAllText(tempPath).Trim(), out double val))
                        return val / 1000.0;
                }
            }
            catch { }
        }

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
                            type.Contains("i915", StringComparison.OrdinalIgnoreCase) ||
                            type.Contains("xe", StringComparison.OrdinalIgnoreCase))
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
    /// Attempts to find the hwmon directory specifically linked to this GPU.
    /// </summary>
    private string GetSpecificHwmonPath()
    {
        try
        {
            // Path 1: Check device-internal hwmon directory (modern kernels)
            string internalHwmon = Path.Combine(_basePath, "hwmon");
            if (Directory.Exists(internalHwmon))
            {
                var dirs = Directory.GetDirectories(internalHwmon, "hwmon*");
                if (dirs.Length > 0) return dirs[0];
            }

            // Path 2: Global scan with device link verification
            string baseDir = "/sys/class/hwmon/";
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath))
                    {
                        string name = File.ReadAllText(namePath).Trim();
                        if (name == "i915" || name == "xe")
                        {
                            // Verify this hwmon actually belongs to our PCI device
                            string deviceLink = Path.Combine(dir, "device");
                            if (Directory.Exists(deviceLink) || File.Exists(deviceLink))
                            {
                                string realPath = Path.GetFullPath(deviceLink);
                                if (realPath.Contains(_basePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    return dir;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return "";
    }

    public static (string GpuClock, string BoostClock, string MemClock, string PixelFill, string TexFill, string Bandwidth)
        CalculateDynamicSpecs(
            double activeGpuMHz,
            double defGpuClock, double defBoostClock, double defMemClock,
            double rops, double tmus, double busWidth, string memoryType)
    {
        string gpuClock = "---";
        string boostClockDisplay = "---";
        string memClockDisplay = "---";
        string pixelFill = "N/A";
        string texFill = "N/A";
        string bandwidth = "N/A";

        if (activeGpuMHz > 0)
        {
            gpuClock = $"{activeGpuMHz.ToString(CultureInfo.InvariantCulture)} MHz";
        }

        if (defBoostClock > 0)
        {
            boostClockDisplay = $"{defBoostClock.ToString(CultureInfo.InvariantCulture)} MHz";
        }

        if (defMemClock > 0)
        {
            memClockDisplay = $"{defMemClock.ToString(CultureInfo.InvariantCulture)} MHz";
        }

        double fillClock = activeGpuMHz > 0 ? activeGpuMHz : defGpuClock;
        if (fillClock > 0)
        {
            if (rops > 0) pixelFill = ((rops * fillClock) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " GPixel/s";
            if (tmus > 0) texFill = ((tmus * fillClock) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " GTexel/s";
        }

        if (defMemClock > 0 && busWidth > 0)
        {
            double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
            double bw = (busWidth * defMemClock * multiplier) / 8000.0;
            bandwidth = bw.ToString("F1", CultureInfo.InvariantCulture) + " GB/s";
        }

        return (gpuClock, boostClockDisplay, memClockDisplay, pixelFill, texFill, bandwidth);
    }

    #endregion
}
