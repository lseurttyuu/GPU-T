using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GPU_T.Models;
using GPU_T.Services.Advanced;
using GPU_T.Services.Advanced.LinuxNvidia;
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
    private readonly string _busId;
    private readonly string _memoryType;

    private class ProbeStateCache
    {
        // Availability Cache
        public bool IsAvailabilityCached = false;
        public SensorAvailability Availability = new();
        public bool? IsNvapiSupported;

        // Smart Polling Cache
        public bool HasInitialData = false;
        public GpuSensorData LastData = new();
        public bool IsUpdating = false;
        public readonly object LockObj = new object();
    }

    private static readonly Dictionary<string, ProbeStateCache> _stateCache = new();

    private ProbeStateCache GetState()
    {
        if (!_stateCache.TryGetValue(_gpuId, out var state))
        {
            state = new ProbeStateCache();
            _stateCache[_gpuId] = state;
        }
        return state;
    }

    /// <summary>
    /// Cached result of nvidia-smi availability check. Null means unchecked.
    /// </summary>
    private static bool? _nvidiaSmiAvailable;

    public LinuxNvidiaGpuProbe(string gpuId, string memoryType = "")
    {
        _gpuId = gpuId;
        _memoryType = memoryType;
        _basePath = $"/sys/class/drm/{gpuId}/device";

        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }

        _busId = GpuFeatureDetection.GetBusId(_basePath);
    }

    #region Static Data

    public GpuStaticData LoadStaticData()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "").ToUpper();

        var spec = PciIdLookup.GetSpecs(ids.Device, revId);

        string resolvedMemType = spec?.MemoryType ?? _memoryType;

        // Try nvidia-smi first for rich data, fall back to sysfs
        var smiData = QueryNvidiaSmi(
            "name,driver_version,vbios_version,pci.bus_id,memory.total," +
            "pci.device_id,pci.sub_device_id");

        string deviceName = "Unknown NVIDIA GPU";
        string driverVersion = "Unknown";
        string biosVersion = "Unknown";
        string busId = _busId;
        string memorySize = "N/A";

        if (smiData != null && smiData.Count >= 7)
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

        }
        else
        {
            // Fallback: try to identify from lspci
            deviceName = CommonGpuHelpers.GetDeviceNameFromLspci(busId);
            if (string.IsNullOrEmpty(deviceName) || deviceName == "Unknown")
                deviceName = "Unknown NVIDIA GPU";

            driverVersion = GpuFeatureDetection.GetNvidiaDriverVersion();
        }

        // Prefer DB name when we have an exact revision match
        if (spec != null && spec.IsExactMatch)
            deviceName = spec.Name;

        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion();
        string driverDate = GpuFeatureDetection.GetNvidiaDriverDate();

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        bool isPhysXEnabled = false;
        //If CUDA environment is detected, we assume hardware accelerated PhysX is most probably available as well,
        //since it's been true for many years that NVIDIA includes PhysX support in all their consumer GPUs with driver support.
        //we don't check the PhysX libraries directly as they don't have to be present unless a PhysX-using game is installed.
        bool isCudaAvailable = isPhysXEnabled = IsNvidiaSmiAvailable() ||
                               GpuFeatureDetection.IsNativeLibraryAvailable("libcuda.so.1") ||
                               GpuFeatureDetection.CheckEglVendorInstalled("10_nvidia.json");

        //bool isPhysXEnabled = GpuFeatureDetection.IsNativeLibraryAvailable("libPhysXCommon.so");

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

        string ropsTmus = "N/A";
        string lookupUrl = "";

        (string GpuClock, string BoostClock, string MemClock, 
        string PixelFill, string TexFill, string Bandwidth) dynamicSpecs = ("---", "---", "---", "N/A", "N/A", "N/A");

        if (spec != null)
        {
            lookupUrl = spec.LookupUrl;
            ropsTmus = $"{spec.Rops} / {spec.Tmus}";
            double defGpuClock = CommonGpuHelpers.ExtractNumber(spec.DefGpuClock);
            double defBoostClock = CommonGpuHelpers.ExtractNumber(spec.DefBoostClock);
            double defMemClock = CommonGpuHelpers.ExtractNumber(spec.DefMemClock);

            double busWidth = CommonGpuHelpers.ExtractNumber(spec.BusWidth);
            double rops = CommonGpuHelpers.ExtractNumber(spec.Rops);
            double tmus = CommonGpuHelpers.ExtractNumber(spec.Tmus);

            int coreOffset = 0;
            int memOffset = 0;
            string sidecarOutput = LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--read", _busId));
            
            
            //calculate real current clocks by applying OC offsets from NVAPI sidecar to the default clocks from our DB.
            //This allows us to report actual current clocks even when user has an overclock applied.
            if (!string.IsNullOrEmpty(sidecarOutput) && sidecarOutput.Contains(','))
            {
                var parts = sidecarOutput.Split(',');
                if (parts.Length >= 7)
                {
                    int.TryParse(parts[5], out coreOffset);
                    int.TryParse(parts[6], out memOffset);
                }
            }

            dynamicSpecs = CalculateDynamicSpecs(
                defGpuClock, defBoostClock, defMemClock, 
                rops, tmus, busWidth, resolvedMemType, 
                coreOffset, memOffset);

        }

        return new GpuStaticData
        {
            DeviceName = deviceName,
            IsExactMatch = spec?.IsExactMatch ?? true,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = busId,
            BiosVersion = biosVersion,
            DriverVersion = driverVersion,
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
            PixelFillrate = dynamicSpecs.PixelFill,
            TextureFillrate = dynamicSpecs.TexFill,
            MemoryType = spec?.MemoryType ?? "N/A",
            BusWidth = spec?.BusWidth ?? "N/A",
            Bandwidth = dynamicSpecs.Bandwidth,

            DefaultGpuClock = spec?.DefGpuClock ?? "---",
            DefaultBoostClock = spec?.DefBoostClock ?? "---",
            DefaultMemoryClock = spec?.DefMemClock ?? "---",
            CurrentGpuClock = dynamicSpecs.GpuClock,
            BoostClock = dynamicSpecs.BoostClock,
            CurrentMemClock = dynamicSpecs.MemClock,

            MemorySize = memorySize,

            IsCudaAvailable = isCudaAvailable,
            IsPhysXEnabled = isPhysXEnabled,
            IsVulkanAvailable = vulkanApi != "N/A" || GpuFeatureDetection.CheckVulkanIcdInstalled("nvidia_icd.json", "nvidia_icd.x86_64.json"),
            IsOpenClAvailable = isOpenClAvailable,
            IsOpenglAvailable = isOpenglAvailable,
            IsHsaAvailable = false,     //we treat HIP as AMD-specific (user-perspective!)
            IsRocmAvailable = false,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
        };
    }

    #endregion

    #region Sensor Data

    /// <summary>
    /// Loads the latest GPU sensor data, ensuring the first call is synchronous to avoid returning zeroed values,
    /// and subsequent calls are handled asynchronously to keep UI responsive.
    /// </summary>
    public GpuSensorData LoadSensorData()
    {
        var cache = GetState();

        // Prevent returning 0s on the very first tick by blocking synchronously once.
        bool needsInitialFetch = false;
        lock (cache.LockObj)
        {
            if (!cache.HasInitialData)
            {
                needsInitialFetch = true;
                cache.IsUpdating = true; // Lock out other threads
            }
        }

        // Run synchronously so we have real numbers before returning to the UI
        if (needsInitialFetch)
        {
            BackgroundFetchSensors(cache);
            lock (cache.LockObj)
            {
                cache.HasInitialData = true;
            }
        }

        // Standard Async Polling for all subsequent ticks
        lock (cache.LockObj)
        {
            if (!cache.IsUpdating)
            {
                cache.IsUpdating = true;
                var probeInstance = this; 
                // Launch background sensor update to avoid blocking UI thread
                Task.Run(() => probeInstance.BackgroundFetchSensors(cache));
            }

            return cache.LastData;
        }
    }

    /// <summary>
    /// Executes parallel hardware queries for sensor data off the UI thread,
    /// parses the results, and updates the cache in a thread-safe manner.
    /// </summary>
    private void BackgroundFetchSensors(ProbeStateCache cache)
    {
        try
        {
            // 1. Parallel Execution: Launch smi and nvapi simultaneously
            Task<List<string>?> smiTask = Task.Run(() => QueryNvidiaSmi(
                "temperature.gpu,fan.speed,power.draw,clocks.current.graphics," +
                "clocks.current.memory,utilization.gpu,utilization.memory,memory.used," +
                "temperature.memory,utilization.encoder,utilization.decoder,clocks_throttle_reasons.active"));

            Task<string> nvapiTask = Task.FromResult("");
            if (cache.IsNvapiSupported == true)
            {
                nvapiTask = Task.Run(() => LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--read", _busId)));
            }

            // Wait for both to finish (takes only as long as the slowest process)
            Task.WaitAll(smiTask, nvapiTask);

            var smiData = smiTask.Result;
            string readData = nvapiTask.Result;

            // 2. Parse the Data (Exactly as before)
            double gpuTemp = 0, fanPercent = 0, powerW = 0, gpuClock = 0, memClock = 0;
            int gpuLoad = 0, memLoad = 0, encLoad = 0, decLoad = 0;
            double memUsedMb = 0, memTemp = 0, GpuVoltage = 0, hotSpotTemp = 0, pcieTxGb = 0, pcieRxGb = 0;
            int coreOcOffset = 0;
            int memOcOffset = 0;
            string perfCap = "None";

            // Parse NVAPI sidecar output if available
            if (!string.IsNullOrEmpty(readData) && readData.Contains(','))
            {
                var parts = readData.Split(',');
                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0], out int hs) && hs > 0) hotSpotTemp = hs;
                    if (int.TryParse(parts[1], out int vr) && vr > 0) memTemp = vr;
                }
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[2], out int mv) && mv > 0) GpuVoltage = mv / 1000.0;
                }
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[3], out int tx) && tx >= 0) pcieTxGb = tx / 1048576.0;
                    if (int.TryParse(parts[4], out int rx) && rx >= 0) pcieRxGb = rx / 1048576.0;
                }
                if (parts.Length >= 7)
                {
                    if (int.TryParse(parts[5], out int co)) coreOcOffset = co;
                    if (int.TryParse(parts[6], out int mo)) memOcOffset = mo;
                }
            }

            // Parse nvidia-smi output if available
            if (smiData != null && smiData.Count >= 12)
            {
                double.TryParse(CleanSmiValue(smiData[0]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuTemp);
                double.TryParse(CleanSmiValue(smiData[1]), NumberStyles.Any, CultureInfo.InvariantCulture, out fanPercent);
                double.TryParse(CleanSmiValue(smiData[2]), NumberStyles.Any, CultureInfo.InvariantCulture, out powerW);
                double.TryParse(CleanSmiValue(smiData[3]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
                double.TryParse(CleanSmiValue(smiData[4]), NumberStyles.Any, CultureInfo.InvariantCulture, out memClock);
                memClock = NormalizeMemoryClock(memClock, _memoryType);

                int.TryParse(CleanSmiValue(smiData[5]), out gpuLoad);
                int.TryParse(CleanSmiValue(smiData[6]), out memLoad);
                double.TryParse(CleanSmiValue(smiData[7]), NumberStyles.Any, CultureInfo.InvariantCulture, out memUsedMb);

                // If memory temperature not provided by NVAPI, fallback to nvidia-smi
                if (memTemp == 0) double.TryParse(CleanSmiValue(smiData[8]), NumberStyles.Any, CultureInfo.InvariantCulture, out memTemp);

                int.TryParse(CleanSmiValue(smiData[9]), out encLoad);
                int.TryParse(CleanSmiValue(smiData[10]), out decLoad);
                perfCap = CleanSmiValue(smiData[11], "None");
                if (string.IsNullOrEmpty(perfCap)) perfCap = "None";
            }
            else
            {
                // Fallback to hwmon sysfs if nvidia-smi is not available
                gpuTemp = ReadHwmonDouble("temp1_input") / 1000.0;
                gpuClock = ReadHwmonDouble("freq1_input") / 1000000.0;
            }

            var newData = new GpuSensorData
            {
                GpuClock = gpuClock, MemoryClock = memClock, GpuTemp = gpuTemp, GpuHotSpot = hotSpotTemp,
                FanPercent = (int)fanPercent, BoardPower = powerW, GpuLoad = gpuLoad, MemControllerLoad = memLoad,
                MemoryUsed = memUsedMb, GpuVoltage = GpuVoltage, MemoryTemp = memTemp, EncoderLoad = encLoad,
                DecoderLoad = decLoad, PerfCapReason = perfCap, PcieTx = pcieTxGb, PcieRx = pcieRxGb,
                CoreOcOffset = coreOcOffset,
                MemOcOffset = memOcOffset,
                // These read fast local files, so we keep them in the background thread too!
                CpuTemperature = CommonGpuHelpers.GetCpuTemperature(),
                SystemRamUsed = CommonGpuHelpers.GetSystemRamUsage()
            };

            // 3. Thread-safe push back to the cache
            lock (cache.LockObj)
            {
                cache.LastData = newData;
            }
        }
        catch { }
        finally
        {
            // Always unlock the state so the next UI tick can trigger a new poll
            lock (cache.LockObj)
            {
                cache.IsUpdating = false;
            }
        }
    }

    #endregion

    #region Sensor Availability

    /// <summary>
    /// Determines which sensors are available for the current GPU by probing nvidia-smi,
    /// hwmon, and NVAPI sidecar, and caches the result for future queries.
    /// </summary>
    public SensorAvailability GetSensorAvailability()
    {
        var cache = GetState();
        
        // Fast-path: If we already discovered the sensors, return instantly to avoid stutter
        lock (cache.LockObj)
        {
            if (cache.IsAvailabilityCached) return cache.Availability;
        }

        var avail = new SensorAvailability();

        // Probe nvidia-smi for sensor support
        if (IsNvidiaSmiAvailable())
        {
            var smiData = QueryNvidiaSmi("temperature.gpu,fan.speed,power.draw,utilization.gpu,utilization.memory,memory.used,temperature.memory,utilization.encoder,utilization.decoder,clocks_throttle_reasons.active");
            if (smiData != null && smiData.Count >= 10)
            {
                avail.HasFan = !string.IsNullOrEmpty(CleanSmiValue(smiData[1]));
                avail.HasPower = !string.IsNullOrEmpty(CleanSmiValue(smiData[2]));
                avail.HasGpuLoad = !string.IsNullOrEmpty(CleanSmiValue(smiData[3]));
                avail.HasMemControllerLoad = !string.IsNullOrEmpty(CleanSmiValue(smiData[4]));
                avail.HasMemUsed = !string.IsNullOrEmpty(CleanSmiValue(smiData[5]));
                avail.HasMemTemp = !string.IsNullOrEmpty(CleanSmiValue(smiData[6]));
                avail.HasEncoderLoad = !string.IsNullOrEmpty(CleanSmiValue(smiData[7]));
                avail.HasDecoderLoad = !string.IsNullOrEmpty(CleanSmiValue(smiData[8]));
                avail.HasPerfCapReason = !string.IsNullOrEmpty(CleanSmiValue(smiData[9]));
            }
        }
        // Probe hwmon sysfs as a fallback for basic sensors
        else if (!string.IsNullOrEmpty(_hwmonPath))
        {
            if (File.Exists(Path.Combine(_hwmonPath, "fan1_input"))) avail.HasFan = true;
            if (File.Exists(Path.Combine(_hwmonPath, "power1_average")) || File.Exists(Path.Combine(_hwmonPath, "power1_input"))) avail.HasPower = true;
        }

        // Probe NVAPI sidecar for advanced sensors if available
        if (!cache.IsNvapiSupported.HasValue)
        {
            string checkResult=LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--check", _busId));
            cache.IsNvapiSupported = (checkResult != null);
        }

        if (cache.IsNvapiSupported == true)
        {
            string readData = LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--read", _busId));
            if (!string.IsNullOrEmpty(readData) && readData.Contains(','))
            {
                var parts = readData.Split(',');
                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0], out int hs) && hs > 0) avail.HasHotSpot = true;
                    if (int.TryParse(parts[1], out int vr) && vr > 0) avail.HasMemTemp = true; 
                }
                if (parts.Length >= 3 && int.TryParse(parts[2], out int mv) && mv > 0) avail.HasVoltage = true;
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[3], out int tx) && tx >= 0) avail.HasPcieTx = true;
                    if (int.TryParse(parts[4], out int rx) && rx >= 0) avail.HasPcieRx = true;
                }
            }
        }

        // Lock and cache the result permanently
        lock (cache.LockObj)
        {
            cache.Availability = avail;
            cache.IsAvailabilityCached = true;
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
            "CUDA" => new LinuxNvidiaCudaProvider(),
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return new[] { "General", "Vulkan", "OpenCL", "CUDA" };
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
    private List<string>? QueryNvidiaSmi(string queryFields)
    {
        if (!IsNvidiaSmiAvailable()) return null;

        try
        {
            string idArg = !string.IsNullOrEmpty(_busId) && _busId != "Unknown"
                ? $"-i {_busId} " : "";
            string output = ShellHelper.RunCommand("nvidia-smi",
                $"{idArg}--query-gpu={queryFields} --format=csv,noheader,nounits");

            if (string.IsNullOrEmpty(output)) return null;

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
        
        // Added " KB/s" and " MB/s" to the replace chain
        trimmed = trimmed.Replace(" MiB", "").Replace(" MHz", "").Replace(" W", "")
                         .Replace(" %", "").Replace(" KB/s", "").Replace(" MB/s", "").Trim();

        return trimmed;
    }

    /// <summary>
    /// Converts nvidia-smi's data-rate clock into a true GPU-Z style base clock.
    /// </summary>
    private double NormalizeMemoryClock(double smiClock, string memoryType)
    {
        if (smiClock <= 0) return 0;
        if(memoryType.ToUpperInvariant().Contains("N/A")) return smiClock; // If memory type is unknown, return as-is to avoid incorrect normalization
        
        // Use the common helper to get the divisor (8 for GDDR6, 4 for GDDR5, etc.)
        double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
        
        // nvidia-smi always reports (Effective / 2). 
        // Therefore, Base = (smiClock * 2) / Multiplier.
        return (smiClock * 2.0) / multiplier;
    }

    /// <summary>
    /// Calculates the actual current GPU clock, boost clock, memory clock, pixel fillrate, texture fillrate, and bandwidth
    /// </summary>
    /// <param name="defGpuClock"></param>
    /// <param name="defBoostClock"></param>
    /// <param name="defMemClock"></param>
    /// <param name="rops"></param>
    /// <param name="tmus"></param>
    /// <param name="busWidth"></param>
    /// <param name="memoryType"></param>
    /// <param name="coreOffset"></param>
    /// <param name="memOffset"></param>
    /// <returns>The real specs of the GPU, calculated on demand.</returns>
    public static (string GpuClock, string BoostClock, string MemClock, string PixelFill, string TexFill, string Bandwidth) 
        CalculateDynamicSpecs(
            double defGpuClock, double defBoostClock, double defMemClock,
            double rops, double tmus, double busWidth, string memoryType,
            int coreOffset, int memOffset)
    {
        int actualMemOffsetBase = 0;
        if (memOffset != 0)
        {
            actualMemOffsetBase = (int)(memOffset / CommonGpuHelpers.GetMemoryMultiplier(memoryType));
        }

        double currentGpuClock_nvapi = defGpuClock > 0 ? defGpuClock + coreOffset : 0;
        double currentBoostClock_nvapi = defBoostClock > 0 ? defBoostClock + coreOffset : 0;
        double currentMemClock_nvapi = defMemClock > 0 ? defMemClock + actualMemOffsetBase : 0;

        string gpuClockStr = currentGpuClock_nvapi > 0 ? $"{currentGpuClock_nvapi} MHz" : "---";
        string boostClockStr = currentBoostClock_nvapi > 0 ? $"{currentBoostClock_nvapi} MHz" : "---";
        string memClockStr = currentMemClock_nvapi > 0 ? $"{currentMemClock_nvapi} MHz" : "---";

        string pixelFill = "---";
        string texFill = "---";
        string bandwidth = "---";

        if (currentBoostClock_nvapi > 0 && rops > 0 && tmus > 0)
        {
            pixelFill = $"{(currentBoostClock_nvapi * rops / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GPixel/s";
            texFill = $"{(currentBoostClock_nvapi * tmus / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GTexel/s";
        }

        if (currentMemClock_nvapi > 0 && busWidth > 0)
        {
            double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
            double bandwidthValue = (currentMemClock_nvapi * multiplier * busWidth) / 8000.0;
            bandwidth = $"{bandwidthValue.ToString("0.0", CultureInfo.InvariantCulture)} GB/s";
        }

        return (gpuClockStr, boostClockStr, memClockStr, pixelFill, texFill, bandwidth);
    }

    #endregion

    #region Fallback Helpers

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

    #endregion
}
