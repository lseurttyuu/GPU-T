using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using GPU_T.Models;
using GPU_T.Services.Advanced.LinuxNvidia;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxNvidia;

public partial class LinuxNvidiaGpuProbe
{
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
            int gpuLoad = 0, memLoad = 0, encLoad = 0, decLoad = 0, fanRpm = 0;
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
                if(parts.Length >= 8)
                {
                    if (int.TryParse(parts[7], out int rpm) && rpm >= 0) fanRpm = rpm;
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
                NVIDIA_CoreOcOffset = coreOcOffset,
                NVIDIA_MemOcOffset = memOcOffset,
                FanRpm = fanRpm,
                // These read fast local files, so we keep them in the background thread too!
                CpuTemperature = CommonGpuHelpers.GetCpuTemperature(),
                SystemRamUsed = CommonGpuHelpers.GetSystemRamUsage(),

                BusInterface = GpuFeatureDetection.GetPcieInfo(_basePath)
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
                if(parts.Length >= 8)
                {
                    if (int.TryParse(parts[7], out int rpm) && rpm >= 0) avail.HasFanRpm = true;
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
}