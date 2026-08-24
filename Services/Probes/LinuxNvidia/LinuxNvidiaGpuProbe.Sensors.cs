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
            string[] smiFields = new[]
            {
                "temperature.gpu", "fan.speed", "power.draw", "clocks.current.graphics",
                "clocks.current.memory", "utilization.gpu", "utilization.memory", "memory.used",
                "temperature.memory", "utilization.encoder", "utilization.decoder", "clocks_throttle_reasons.active"
            };

            Task<Dictionary<string, string>?> smiTask = Task.Run(() => SmartQueryNvidiaSmi(smiFields, cache));

            Task<string> nvapiTask = Task.FromResult("");
            if (cache.IsNvapiSupported == true)
            {
                nvapiTask = Task.Run(() => LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--read", _busId)));
            }

            // Wait for both to finish (takes only as long as the slowest process)
            Task.WaitAll(smiTask, nvapiTask);

            var smiData = smiTask.Result;
            string readData = nvapiTask.Result;

            // 2. Parse the Data
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

            // Parse nvidia-smi dictionary output if available
            if (smiData != null)
            {
                if (smiData.TryGetValue("temperature.gpu", out var valGpuTemp)) double.TryParse(CleanSmiValue(valGpuTemp), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuTemp);
                if (smiData.TryGetValue("fan.speed", out var valFan)) double.TryParse(CleanSmiValue(valFan), NumberStyles.Any, CultureInfo.InvariantCulture, out fanPercent);
                if (smiData.TryGetValue("power.draw", out var valPower)) double.TryParse(CleanSmiValue(valPower), NumberStyles.Any, CultureInfo.InvariantCulture, out powerW);
                if (smiData.TryGetValue("clocks.current.graphics", out var valGpuClk)) double.TryParse(CleanSmiValue(valGpuClk), NumberStyles.Any, CultureInfo.InvariantCulture, out gpuClock);
                
                if (smiData.TryGetValue("clocks.current.memory", out var valMemClk)) 
                {
                    double.TryParse(CleanSmiValue(valMemClk), NumberStyles.Any, CultureInfo.InvariantCulture, out memClock);
                    memClock = NormalizeMemoryClock(memClock, _memoryType);
                }

                if (smiData.TryGetValue("utilization.gpu", out var valGpuLoad)) int.TryParse(CleanSmiValue(valGpuLoad), out gpuLoad);
                if (smiData.TryGetValue("utilization.memory", out var valMemLoad)) int.TryParse(CleanSmiValue(valMemLoad), out memLoad);
                if (smiData.TryGetValue("memory.used", out var valMemUsed)) double.TryParse(CleanSmiValue(valMemUsed), NumberStyles.Any, CultureInfo.InvariantCulture, out memUsedMb);

                // If memory temperature not provided by NVAPI, fallback to nvidia-smi
                if (memTemp == 0 && smiData.TryGetValue("temperature.memory", out var valMemTemp)) double.TryParse(CleanSmiValue(valMemTemp), NumberStyles.Any, CultureInfo.InvariantCulture, out memTemp);

                if (smiData.TryGetValue("utilization.encoder", out var valEnc)) int.TryParse(CleanSmiValue(valEnc), out encLoad);
                if (smiData.TryGetValue("utilization.decoder", out var valDec)) int.TryParse(CleanSmiValue(valDec), out decLoad);
                
                if (smiData.TryGetValue("clocks_throttle_reasons.active", out var valPerf)) 
                {
                    perfCap = CleanSmiValue(valPerf, "None");
                    if (string.IsNullOrEmpty(perfCap)) perfCap = "None";
                }
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
            string[] smiFields = new[]
            {
                "temperature.gpu", "fan.speed", "power.draw", "utilization.gpu",
                "utilization.memory", "memory.used", "temperature.memory",
                "utilization.encoder", "utilization.decoder", "clocks_throttle_reasons.active"
            };

            var smiData = SmartQueryNvidiaSmi(smiFields, cache);
            if (smiData != null)
            {
                avail.HasFan = smiData.TryGetValue("fan.speed", out var valFan) && !string.IsNullOrEmpty(CleanSmiValue(valFan));
                avail.HasPower = smiData.TryGetValue("power.draw", out var valPow) && !string.IsNullOrEmpty(CleanSmiValue(valPow));
                avail.HasGpuLoad = smiData.TryGetValue("utilization.gpu", out var valGLoad) && !string.IsNullOrEmpty(CleanSmiValue(valGLoad));
                avail.HasMemControllerLoad = smiData.TryGetValue("utilization.memory", out var valMLoad) && !string.IsNullOrEmpty(CleanSmiValue(valMLoad));
                avail.HasMemUsed = smiData.TryGetValue("memory.used", out var valMUsed) && !string.IsNullOrEmpty(CleanSmiValue(valMUsed));
                avail.HasMemTemp = smiData.TryGetValue("temperature.memory", out var valMTemp) && !string.IsNullOrEmpty(CleanSmiValue(valMTemp));
                avail.HasEncoderLoad = smiData.TryGetValue("utilization.encoder", out var valEnc) && !string.IsNullOrEmpty(CleanSmiValue(valEnc));
                avail.HasDecoderLoad = smiData.TryGetValue("utilization.decoder", out var valDec) && !string.IsNullOrEmpty(CleanSmiValue(valDec));
                avail.HasPerfCapReason = smiData.TryGetValue("clocks_throttle_reasons.active", out var valPerf) && !string.IsNullOrEmpty(CleanSmiValue(valPerf));
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
            string checkResult = LinuxNvidiaSidecarHelper.Run(LinuxNvidiaSidecarHelper.BuildTelemetryArgs("--check", _busId));
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

    /// <summary>
    /// Builds the query, stripping known invalid fields. 
    /// Returns a Dictionary mapping the requested field name to its string value.
    /// </summary>
    private Dictionary<string, string>? SmartQueryNvidiaSmi(string[] requestedFields, ProbeStateCache cache)
    {
        List<string>? rawResult = null;
        List<string> activeQueryFields = new List<string>();
        
        int maxRetries = requestedFields.Length;
        int retries = 0;

        while (retries <= maxRetries)
        {
            retries++;
            activeQueryFields.Clear();

            lock (cache.LockObj)
            {
                foreach (var field in requestedFields)
                {
                    if (!cache.UnsupportedSmiFields.Contains(field))
                    {
                        activeQueryFields.Add(field);
                    }
                }
            }

            if (activeQueryFields.Count == 0) return null;

            string queryStr = string.Join(",", activeQueryFields);
            rawResult = QueryNvidiaSmi(queryStr);

            if (rawResult == null || rawResult.Count == 0) return null;

            string firstElement = rawResult[0];
            if (firstElement.StartsWith("Field \"") && firstElement.Contains("\" is not a valid field"))
            {
                int startQuote = firstElement.IndexOf('"') + 1;
                int endQuote = firstElement.IndexOf('"', startQuote);
                
                if (startQuote > 0 && endQuote > startQuote)
                {
                    string badField = firstElement.Substring(startQuote, endQuote - startQuote);
                    
                    lock (cache.LockObj)
                    {
                        cache.UnsupportedSmiFields.Add(badField);
                    }
                    continue; 
                }
            }
            break;
        }

        if (rawResult == null) return null;

        // Map the fields to their corresponding results
        var resultDict = new Dictionary<string, string>();
        for (int i = 0; i < activeQueryFields.Count; i++)
        {
            if (i < rawResult.Count)
            {
                resultDict[activeQueryFields[i]] = rawResult[i];
            }
        }

        return resultDict;
    }
}