using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using GPU_T.Models;
using GPU_T.Services.Utilities;
using GPU_T.Services.Advanced.LinuxNvidia;

namespace GPU_T.Services.Probes.LinuxNvidia;

public partial class LinuxNvidiaGpuProbe
{
    public GpuStaticData LoadStaticData()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "").ToUpper();

        // Clean and pad the Sub IDs to ensure they are exactly 4 characters each
        string subVendor = ids.SubVendor.Replace("0x", "").PadLeft(4, '0').ToUpper();
        string subDevice = ids.SubDevice.Replace("0x", "").PadLeft(4, '0').ToUpper();

        // NVIDIA JSON structure matches [SubDevice][SubVendor]
        string subSysId = $"{subDevice}{subVendor}";

        var spec = PciIdLookup.GetSpecs(ids.Vendor, ids.Device, revId, subSysId);

        // MAX-Q HEURISTIC ALGORITHM
        bool maxqReplaced = false;
        if (spec != null && spec.Name.Contains("Mobile"))
        {
            string maxqName = spec.Name.Replace("Mobile", "Max-Q");

            if (DatabaseManager.MaxqGpus.TryGetValue(maxqName, out var maxqDto))
            {
                if (maxqDto.CodeName == spec.CodeName)
                {
                    double defaultPower = GetDefaultPowerLimit(_busId);
                    double threshold = CommonGpuHelpers.ExtractNumber(maxqDto.MaxqThreshold);

                    // Compare valid power numbers
                    if (defaultPower > 0 && threshold > 0 && defaultPower <= threshold)
                    {
                        // We found a Max-Q, adopt the new specs and mark as non-exact
                        spec = maxqDto.ToGpuSpec(isExactMatch: false);
                        maxqReplaced = true;
                    }
                }
            }
        }

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

        // Prefer DB name when we have an exact revision match or Max-Q variant probably present
        if ((spec != null && spec.IsExactMatch) || (spec != null && maxqReplaced))
            deviceName = spec.Name;

        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion(ids.Device);
        string driverDate = GpuFeatureDetection.GetNvidiaDriverDate();

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        bool isPhysXEnabled = false;
        //If CUDA environment is detected, we assume hardware accelerated PhysX is most probably available as well,
        //since it's been true for many years that NVIDIA includes PhysX support in all their consumer GPUs with driver support.
        //we don't check the PhysX libraries directly as they don't have to be present unless a PhysX-using game is installed;
        //Games provide their own PhysX runtimes, and the GPU's ability to run PhysX is more about driver support and CUDA capability anyway.
        bool isCudaAvailable = isPhysXEnabled = IsNvidiaSmiAvailable() ||
                               GpuFeatureDetection.IsNativeLibraryAvailable("libcuda.so.1") ||
                               GpuFeatureDetection.CheckEglVendorInstalled("10_nvidia.json");

        bool isOneApiAvailable = GpuFeatureDetection.IsNativeLibraryAvailable("libze_intel_gpu.so.1");

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
            IsOneApiAvailable = isOneApiAvailable,
            IsPhysXEnabled = isPhysXEnabled,
            IsVulkanAvailable = GpuFeatureDetection.CheckVulkanSupport(ids.Device, "nvidia_icd.json", "nvidia_icd.x86_64.json"),
            IsOpenClAvailable = isOpenClAvailable,
            IsOpenglAvailable = isOpenglAvailable,
            IsHsaAvailable = false,     //we treat HIP as AMD-specific (user-perspective!)
            IsRocmAvailable = false,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsUefiAvailable = CommonGpuHelpers.CheckGpuUefiSupport(spec?.ReleaseDate),
        };
    }

    /// <summary>
    /// Queries the NVML Sidecar (or nvidia-smi fallback) for the GPU's default power limit in Watts.
    /// Returns -1 if unavailable or invalid.
    /// </summary>
    private double GetDefaultPowerLimit(string pciBusId)
    {
        try
        {
            // 1. Try our sidecar app
            string pciArg = !string.IsNullOrEmpty(pciBusId) && pciBusId != "Unknown" ? $" --pci {pciBusId}" : "";
            string rawData = LinuxNvidiaSidecarHelper.Run("--limits" + pciArg, 1000);
            
            if (!string.IsNullOrWhiteSpace(rawData))
            {
                var lines = rawData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Default Power Limit="))
                    {
                        string valStr = line.Split('=')[1].Replace("W", "").Trim();
                        if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double power))
                            return power;
                    }
                }
            }

            // 2. Fallback to nvidia-smi if sidecar failed or returned nothing
            string targetArg = !string.IsNullOrEmpty(pciBusId) && pciBusId != "Unknown" ? $"-i {pciBusId} " : "";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = $"{targetArg}--query-gpu=power.default_limit --format=csv,noheader",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            string output = process?.StandardOutput.ReadToEnd().Trim() ?? "";
            process?.WaitForExit();
            
            if (!string.IsNullOrEmpty(output) && output != "[Not Supported]" && output != "[N/A]")
            {
                string cleanOut = output.Replace("W", "").Trim();
                if (double.TryParse(cleanOut, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double smiPower))
                    return smiPower;
            }
        }
        catch { }
        
        return -1; // Invalid/Not Found
    }
}