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
    private readonly string _busId;

    private readonly string _memoryType;

    private class NvapiState
    {
        public bool? IsSupported;
        public int HotspotTemp = 0;
        public int VramTemp = 0;
        public double GPUVoltage = 0.0;
        public double PcieTxGb = 0.0; 
        public double PcieRxGb = 0.0;
    }

    private static readonly Dictionary<string, NvapiState> _stateCache = new();

    private NvapiState GetState()
    {
        if (!_stateCache.TryGetValue(_gpuId, out var state))
        {
            state = new NvapiState();
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
            "pci.device_id,pci.sub_device_id,clocks.max.graphics,clocks.max.memory," +
            "clocks.current.graphics,clocks.current.memory");

        string deviceName = "Unknown NVIDIA GPU";
        string driverVersion = "Unknown";
        string biosVersion = "Unknown";
        string busId = _busId;
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
            {
                if (double.TryParse(maxMemClockStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double rawClock))
                {
                    maxMemClock = $"{NormalizeMemoryClock(rawClock, resolvedMemType):0} MHz";
                }
            }

            string curGpuClockStr = CleanSmiValue(smiData[9]);
            if (!string.IsNullOrEmpty(curGpuClockStr))
                currentGpuClock = $"{curGpuClockStr} MHz";

            string curMemClockStr = CleanSmiValue(smiData[10]);
            if (!string.IsNullOrEmpty(curMemClockStr))
            {
                if (double.TryParse(curMemClockStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double rawClock))
                {
                    currentMemClock = $"{NormalizeMemoryClock(rawClock, resolvedMemType):0} MHz";
                }
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

        string pixelFill = "N/A";
        string texFill = "N/A";
        string bandwidth = "N/A";
        string ropsTmus = "N/A";
        string lookupUrl = "";

        if (spec != null)
        {
            lookupUrl = spec.LookupUrl;
            ropsTmus = $"{spec.Rops} / {spec.Tmus}";
            double boostClock = CommonGpuHelpers.ExtractNumber(spec.DefBoostClock);
            double memClock = CommonGpuHelpers.ExtractNumber(spec.DefMemClock);
            double busWidth = CommonGpuHelpers.ExtractNumber(spec.BusWidth);
            double rops = CommonGpuHelpers.ExtractNumber(spec.Rops);
            double tmus = CommonGpuHelpers.ExtractNumber(spec.Tmus);

            if (boostClock > 0 && rops > 0 && tmus > 0)
            {
                pixelFill = $"{(boostClock * rops / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GPixel/s";
                texFill = $"{(boostClock * tmus / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} GTexel/s";
            }

            if (memClock > 0 && busWidth > 0)
            {
                double multiplier = CommonGpuHelpers.GetMemoryMultiplier(spec.MemoryType);
                double bandwidthValue = (memClock * multiplier * busWidth) / 8000.0;
                bandwidth = $"{bandwidthValue.ToString("0.0", CultureInfo.InvariantCulture)} GB/s";
            }
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
            PixelFillrate = pixelFill,
            TextureFillrate = texFill,
            MemoryType = spec?.MemoryType ?? "N/A",
            BusWidth = spec?.BusWidth ?? "N/A",
            Bandwidth = bandwidth,

            DefaultGpuClock = spec?.DefGpuClock ?? "---",
            DefaultBoostClock = spec?.DefBoostClock ?? "---",
            DefaultMemoryClock = spec?.DefMemClock ?? "---",
            CurrentGpuClock = maxGpuClock,
            BoostClock = maxGpuClock,
            CurrentMemClock = maxMemClock,

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
            "clocks.current.memory,utilization.gpu,utilization.memory,memory.used," +
            "temperature.memory,utilization.encoder,utilization.decoder,clocks_throttle_reasons.active");

        double gpuTemp = 0, fanPercent = 0, powerW = 0;
        double gpuClock = 0, memClock = 0;
        int gpuLoad = 0, memLoad = 0;
        double memUsedMb = 0;
        double memTemp = 0;
        double GpuVoltage = 0;
        double hotSpotTemp = 0;
        double pcieTxGb = 0, pcieRxGb = 0;
        int encLoad = 0, decLoad = 0;
        string perfCap = "None";


        var nvapiState = GetState();

        // Try NVAPI sidecar for Hotspot, VRAM temperatures and GPU Voltage
        if (nvapiState.IsSupported == true)
        {
            string readData = RunNvapiSidecar("--read");

            if (!string.IsNullOrEmpty(readData) && readData.Contains(','))
            {
                var parts = readData.Split(',');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0], out int hs) && hs > 0) nvapiState.HotspotTemp = hs;
                        if (int.TryParse(parts[1], out int vr) && vr > 0) nvapiState.VramTemp = vr;
                    }
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[2], out int mv) && mv > 0) nvapiState.GPUVoltage = mv / 1000.0;
                    }
                    
                    if (parts.Length >= 5)
                    {
                        if (int.TryParse(parts[3], out int tx) && tx >= 0) 
                            nvapiState.PcieTxGb = tx / 1048576.0;
                            
                        if (int.TryParse(parts[4], out int rx) && rx >= 0) 
                            nvapiState.PcieRxGb = rx / 1048576.0;
                    }
            }

            hotSpotTemp = nvapiState.HotspotTemp;
            memTemp = nvapiState.VramTemp;
            GpuVoltage = nvapiState.GPUVoltage;
            pcieTxGb = nvapiState.PcieTxGb;
            pcieRxGb = nvapiState.PcieRxGb;

        }


        if (smiData != null && smiData.Count >= 12)
        {
            double.TryParse(CleanSmiValue(smiData[0]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuTemp);
            double.TryParse(CleanSmiValue(smiData[1]), NumberStyles.Any, CultureInfo.InvariantCulture, out fanPercent);
            double.TryParse(CleanSmiValue(smiData[2]), NumberStyles.Any, CultureInfo.InvariantCulture, out powerW);
            double.TryParse(CleanSmiValue(smiData[3]), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
            
            double.TryParse(CleanSmiValue(smiData[4]), NumberStyles.Any, CultureInfo.InvariantCulture, out memClock);
            memClock = NormalizeMemoryClock(memClock, _memoryType);

            string gpuLoadStr = CleanSmiValue(smiData[5]);
            int.TryParse(gpuLoadStr, out gpuLoad);

            string memLoadStr = CleanSmiValue(smiData[6]);
            int.TryParse(memLoadStr, out memLoad);

            double.TryParse(CleanSmiValue(smiData[7]), NumberStyles.Any, CultureInfo.InvariantCulture, out memUsedMb);

            if(memTemp == 0) // If NVAPI didn't provide a value, try nvidia-smi's memory temp
            {
                double.TryParse(CleanSmiValue(smiData[8]), NumberStyles.Any, CultureInfo.InvariantCulture, out memTemp);
            }

            int.TryParse(CleanSmiValue(smiData[9]), out encLoad);
            int.TryParse(CleanSmiValue(smiData[10]), out decLoad);
            
            perfCap = CleanSmiValue(smiData[11], "None");
            if (string.IsNullOrEmpty(perfCap)) perfCap = "None";
            
        }
        else
        {
            // Fallback: read from hwmon if available
            gpuTemp = ReadHwmonDouble("temp1_input") / 1000.0;
            gpuClock = ReadHwmonDouble("freq1_input") / 1000000.0;
        }

        double cpuTemp = CommonGpuHelpers.GetCpuTemperature();
        double sysRam = CommonGpuHelpers.GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = gpuClock,
            MemoryClock = memClock,
            GpuTemp = gpuTemp,
            GpuHotSpot = hotSpotTemp,
            FanPercent = (int)fanPercent,
            BoardPower = powerW,
            GpuLoad = gpuLoad,
            MemControllerLoad = memLoad,
            MemoryUsed = memUsedMb,
            GpuVoltage=GpuVoltage,
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam,
            MemoryTemp = memTemp,
            EncoderLoad = encLoad,
            DecoderLoad = decLoad,
            PerfCapReason = perfCap,
            PcieTx = pcieTxGb,
            PcieRx = pcieRxGb
        };
    }

    #endregion

    #region Sensor Availability

    public SensorAvailability GetSensorAvailability()
    {
        var avail = new SensorAvailability();

        if (IsNvidiaSmiAvailable())
        {
            var smiData = QueryNvidiaSmi(
                "temperature.gpu,fan.speed,power.draw,utilization.gpu,utilization.memory,memory.used," +
                "temperature.memory,utilization.encoder,utilization.decoder,clocks_throttle_reasons.active");

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

        var nvapiState = GetState();

        // NVAPI Sidecar Check
        if (!nvapiState.IsSupported.HasValue)
        {
            string checkResult = RunNvapiSidecar("--check");
            nvapiState.IsSupported = (checkResult != null); 
        }

        if (nvapiState.IsSupported == true)
        {
            string? readData = RunNvapiSidecar("--read");
            if (!string.IsNullOrEmpty(readData) && readData.Contains(','))
            {
                var parts = readData.Split(',');
                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0], out int hs) && hs > 0) avail.HasHotSpot = true;
                    if (int.TryParse(parts[1], out int vr) && vr > 0) avail.HasMemTemp = true; 
                }
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[2], out int mv) && mv > 0) avail.HasVoltage = true;
                }
                // NEW: Flag PCIe throughput as available
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[3], out int tx) && tx >= 0) avail.HasPcieTx = true;
                    if (int.TryParse(parts[4], out int rx) && rx >= 0) avail.HasPcieRx = true;
                }
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
        
        // Use the common helper to get the divisor (8 for GDDR6, 4 for GDDR5, etc.)
        double multiplier = CommonGpuHelpers.GetMemoryMultiplier(memoryType);
        
        // nvidia-smi always reports (Effective / 2). 
        // Therefore, Base = (smiClock * 2) / Multiplier.
        return (smiClock * 2.0) / multiplier;
    }

    /// <summary>
    /// Calls the isolated NVAPI sidecar executable. 
    /// </summary>
    private string RunNvapiSidecar(string arg)
    {
        try
        {
            // Extract the Hex Bus ID from Linux format (e.g. "0000:0A:00.0" -> "0A")
            string busArg = "";
            if (!string.IsNullOrEmpty(_busId) && _busId != "Unknown")
            {
                var parts = _busId.Split(':');
                if (parts.Length >= 2)
                {
                    // Parse the Hex string ("0A") into an integer (10) for NVAPI
                    if (uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out uint busInt))
                    {
                        busArg = $" --bus {busInt}";
                    }
                }
            }

            string pciArg = !string.IsNullOrEmpty(_busId) && _busId != "Unknown" ? $" --pci {_busId}" : "";

            // Append the target bus to the command (E.g., "--read --bus 10 --pci 0000:0A:00.0")
            string fullArg = $"{arg}{busArg}{pciArg}";

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string sidecarPath = System.IO.Path.Combine(appDir, "GPU-T.Nvapi");

            if (!System.IO.File.Exists(sidecarPath))
                sidecarPath += ".dll"; 

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sidecarPath.EndsWith(".dll") ? "dotnet" : sidecarPath,
                Arguments = sidecarPath.EndsWith(".dll") ? $"\"{sidecarPath}\" {fullArg}" : fullArg,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(500); 
                if (process.ExitCode == 0) return output;
            }
        }
        catch { }
        return "";
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
