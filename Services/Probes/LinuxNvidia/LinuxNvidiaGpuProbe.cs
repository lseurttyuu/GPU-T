using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Models;
using GPU_T.Services.Advanced;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxNvidia;

/// <summary>
/// Probe implementation for NVIDIA GPUs on Linux. Uses nvidia-smi as primary data source
/// with sysfs/hwmon fallback for sensor and static data collection.
/// </summary>
public class LinuxNvidiaGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _hwmonPath;
    private readonly string _gpuId;

    /// <summary>
    /// Cached result of nvidia-smi availability check. Null means unchecked.
    /// </summary>
    private static bool? _nvidiaSmiAvailable;

    public LinuxNvidiaGpuProbe(string gpuId)
    {
        _gpuId = gpuId;
        _basePath = $"/sys/class/drm/{gpuId}/device";

        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }
    }

    #region Static Data

    public GpuStaticData LoadStaticData()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "").ToUpper();

        // Try nvidia-smi first for rich data, fall back to sysfs
        var smiData = QueryNvidiaSmi(
            "name,driver_version,vbios_version,pci.bus_id,memory.total," +
            "pci.device_id,pci.sub_device_id,clocks.max.graphics,clocks.max.memory," +
            "clocks.current.graphics,clocks.current.memory");

        string deviceName = "Unknown NVIDIA GPU";
        string driverVersion = "Unknown";
        string biosVersion = "Unknown";
        string busId = GpuFeatureDetection.GetBusId(_basePath);
        string memorySize = "N/A";
        string currentGpuClock = "---";
        string currentMemClock = "---";
        string maxGpuClock = "N/A";
        string maxMemClock = "N/A";

        if (smiData != null && smiData.Count >= 11)
        {
            deviceName = CleanSmiValue(smiData[0], "Unknown NVIDIA GPU");
            // Prefix "NVIDIA" if not already present
            if (!deviceName.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase))
                deviceName = $"NVIDIA {deviceName}";

            driverVersion = CleanSmiValue(smiData[1], "Unknown");
            biosVersion = CleanSmiValue(smiData[2], "Unknown");

            string smiBusId = CleanSmiValue(smiData[3]);
            if (!string.IsNullOrEmpty(smiBusId)) busId = smiBusId;

            string memTotalStr = CleanSmiValue(smiData[4]);
            if (!string.IsNullOrEmpty(memTotalStr))
            {
                // nvidia-smi reports memory in MiB
                if (double.TryParse(memTotalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double memMb))
                    memorySize = $"{(int)memMb} MB";
            }

            string maxGpuClockStr = CleanSmiValue(smiData[7]);
            if (!string.IsNullOrEmpty(maxGpuClockStr))
                maxGpuClock = $"{maxGpuClockStr} MHz";

            string maxMemClockStr = CleanSmiValue(smiData[8]);
            if (!string.IsNullOrEmpty(maxMemClockStr))
                maxMemClock = $"{maxMemClockStr} MHz";

            string curGpuClockStr = CleanSmiValue(smiData[9]);
            if (!string.IsNullOrEmpty(curGpuClockStr))
                currentGpuClock = $"{curGpuClockStr} MHz";

            string curMemClockStr = CleanSmiValue(smiData[10]);
            if (!string.IsNullOrEmpty(curMemClockStr))
                currentMemClock = $"{curMemClockStr} MHz";
        }
        else
        {
            // Fallback: try to identify from lspci
            deviceName = GetDeviceNameFromLspci(busId);
            if (string.IsNullOrEmpty(deviceName) || deviceName == "Unknown")
                deviceName = "Unknown NVIDIA GPU";

            driverVersion = GpuFeatureDetection.GetRealDriverVersion();
        }

        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion();
        string driverDate = GpuFeatureDetection.GetDriverDate();

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        bool isCudaAvailable = IsNvidiaSmiAvailable() ||
                               GpuFeatureDetection.IsNativeLibraryAvailable("libcuda.so.1") ||
                               GpuFeatureDetection.CheckEglVendorInstalled("10_nvidia.json");

        bool isPhysXEnabled = GpuFeatureDetection.IsNativeLibraryAvailable("libPhysXCommon.so");

        // Resizable BAR: nvidia-smi doesn't expose this directly, use PCI resource heuristic
        long totalVramBytes = 0;
        if (memorySize != "N/A")
        {
            var memMatch = Regex.Match(memorySize, @"(\d+)");
            if (memMatch.Success && long.TryParse(memMatch.Value, out long memMb))
                totalVramBytes = memMb * 1024 * 1024;
        }
        string reBarState = GpuFeatureDetection.CheckResizableBar(_basePath, totalVramBytes);

        bool isOpenClAvailable = GpuFeatureDetection.CheckOpenClIcdInstalled("nvidia.icd");

        return new GpuStaticData
        {
            DeviceName = deviceName,
            IsExactMatch = true,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = busId,
            BiosVersion = biosVersion,
            DriverVersion = driverVersion,
            DriverDate = driverDate,
            VulkanApi = vulkanApi,
            BusInterface = busInterface,
            ResizableBarState = reBarState,

            Revision = revId,

            DefaultGpuClock = maxGpuClock,
            DefaultBoostClock = maxGpuClock,
            DefaultMemoryClock = maxMemClock,
            CurrentGpuClock = currentGpuClock,
            BoostClock = currentGpuClock,
            CurrentMemClock = currentMemClock,

            MemorySize = memorySize,

            IsCudaAvailable = isCudaAvailable,
            IsPhysXEnabled = isPhysXEnabled,
            IsVulkanAvailable = vulkanApi != "N/A" || GpuFeatureDetection.CheckVulkanIcdInstalled("nvidia_icd.json", "nvidia_icd.x86_64.json"),
            IsOpenClAvailable = isOpenClAvailable,
            IsOpenglAvailable = isOpenglAvailable,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
        };
    }

    #endregion

    #region Sensor Data

    public GpuSensorData LoadSensorData()
    {
        // Primary: nvidia-smi query
        var smiData = QueryNvidiaSmi(
            "temperature.gpu,fan.speed,power.draw,clocks.current.graphics," +
            "clocks.current.memory,utilization.gpu,utilization.memory,memory.used");

        double gpuTemp = 0, fanPercent = 0, powerW = 0;
        double gpuClock = 0, memClock = 0;
        int gpuLoad = 0, memLoad = 0;
        double memUsedMb = 0;

        if (smiData != null && smiData.Count >= 8)
        {
            double.TryParse(CleanSmiValue(smiData[0]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuTemp);
            double.TryParse(CleanSmiValue(smiData[1]), NumberStyles.Any, CultureInfo.InvariantCulture, out fanPercent);
            double.TryParse(CleanSmiValue(smiData[2]), NumberStyles.Any, CultureInfo.InvariantCulture, out powerW);
            double.TryParse(CleanSmiValue(smiData[3]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
            double.TryParse(CleanSmiValue(smiData[4]), NumberStyles.Any, CultureInfo.InvariantCulture, out memClock);

            string gpuLoadStr = CleanSmiValue(smiData[5]);
            int.TryParse(gpuLoadStr, out gpuLoad);

            string memLoadStr = CleanSmiValue(smiData[6]);
            int.TryParse(memLoadStr, out memLoad);

            double.TryParse(CleanSmiValue(smiData[7]), NumberStyles.Any, CultureInfo.InvariantCulture, out memUsedMb);
        }
        else
        {
            // Fallback: read from hwmon if available
            gpuTemp = ReadHwmonDouble("temp1_input") / 1000.0;
            gpuClock = ReadHwmonDouble("freq1_input") / 1000000.0;
        }

        double cpuTemp = GetCpuTemperature();
        double sysRam = GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = gpuClock,
            MemoryClock = memClock,
            GpuTemp = gpuTemp,
            FanPercent = (int)fanPercent,
            BoardPower = powerW,
            GpuLoad = gpuLoad,
            MemControllerLoad = memLoad,
            MemoryUsed = memUsedMb,
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam
        };
    }

    #endregion

    #region Sensor Availability

    public SensorAvailability GetSensorAvailability()
    {
        var avail = new SensorAvailability();

        if (IsNvidiaSmiAvailable())
        {
            // nvidia-smi provides most sensors; check which are actually reporting
            var smiData = QueryNvidiaSmi(
                "temperature.gpu,fan.speed,power.draw,utilization.gpu,utilization.memory,memory.used");

            if (smiData != null && smiData.Count >= 6)
            {
                // Fan available if not "[Not Supported]" or "[N/A]"
                string fanVal = CleanSmiValue(smiData[1]);
                avail.HasFan = !string.IsNullOrEmpty(fanVal);

                string powerVal = CleanSmiValue(smiData[2]);
                avail.HasPower = !string.IsNullOrEmpty(powerVal);

                string gpuLoadVal = CleanSmiValue(smiData[3]);
                avail.HasGpuLoad = !string.IsNullOrEmpty(gpuLoadVal);

                string memLoadVal = CleanSmiValue(smiData[4]);
                avail.HasMemControllerLoad = !string.IsNullOrEmpty(memLoadVal);

                string memUsedVal = CleanSmiValue(smiData[5]);
                avail.HasMemUsed = !string.IsNullOrEmpty(memUsedVal);
            }
        }
        else
        {
            // Fallback: check hwmon files
            if (!string.IsNullOrEmpty(_hwmonPath))
            {
                if (File.Exists(Path.Combine(_hwmonPath, "fan1_input"))) avail.HasFan = true;
                if (File.Exists(Path.Combine(_hwmonPath, "power1_average")) ||
                    File.Exists(Path.Combine(_hwmonPath, "power1_input"))) avail.HasPower = true;
            }
        }

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
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return new[] { "General", "Vulkan", "OpenCL" };
    }

    #endregion

    #region nvidia-smi Helpers

    /// <summary>
    /// Checks if nvidia-smi is available on the system.
    /// </summary>
    private static bool IsNvidiaSmiAvailable()
    {
        if (_nvidiaSmiAvailable.HasValue) return _nvidiaSmiAvailable.Value;

        try
        {
            string output = ShellHelper.RunCommand("nvidia-smi", "--query-gpu=name --format=csv,noheader,nounits");
            _nvidiaSmiAvailable = !string.IsNullOrEmpty(output);
        }
        catch
        {
            _nvidiaSmiAvailable = false;
        }
        return _nvidiaSmiAvailable.Value;
    }

    /// <summary>
    /// Queries nvidia-smi for the specified fields and returns parsed CSV values.
    /// Returns null if nvidia-smi is unavailable or the query fails.
    /// </summary>
    private static List<string>? QueryNvidiaSmi(string queryFields)
    {
        if (!IsNvidiaSmiAvailable()) return null;

        try
        {
            string output = ShellHelper.RunCommand("nvidia-smi",
                $"--query-gpu={queryFields} --format=csv,noheader,nounits");

            if (string.IsNullOrEmpty(output)) return null;

            // Take only the first line (first GPU) in case of multi-GPU
            string firstLine = output.Split('\n')[0];
            var values = new List<string>();
            foreach (var val in firstLine.Split(','))
            {
                values.Add(val.Trim());
            }
            return values;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cleans an nvidia-smi value by removing "[Not Supported]", "[N/A]", and unit suffixes.
    /// Returns the fallback if the value is not usable.
    /// </summary>
    private static string CleanSmiValue(string raw, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        string trimmed = raw.Trim();
        if (trimmed.Contains("[Not Supported]") || trimmed.Contains("[N/A]") ||
            trimmed == "N/A" || trimmed == "[Insufficient Permissions]")
            return fallback;
        // Remove trailing unit strings that nvidia-smi sometimes includes even with nounits
        trimmed = trimmed.Replace(" MiB", "").Replace(" MHz", "").Replace(" W", "").Replace(" %", "").Trim();
        return trimmed;
    }

    #endregion

    #region Fallback Helpers

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
                // Format: "01:00.0 VGA compatible controller: NVIDIA Corporation GeForce RTX 3080 (rev a1)"
                int colonIdx = output.IndexOf(": ", StringComparison.Ordinal);
                if (colonIdx > 0)
                {
                    string name = output.Substring(colonIdx + 2).Trim();
                    // Remove "(rev xx)" suffix
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

    private double ReadHwmonDouble(string filename)
    {
        if (string.IsNullOrEmpty(_hwmonPath)) return 0;
        try
        {
            string path = Path.Combine(_hwmonPath, filename);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    return val;
            }
        }
        catch { }
        return 0;
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
