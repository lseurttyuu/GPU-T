using System;
using System.Globalization;
using System.IO;
using GPU_T.Models;
using GPU_T.Services.Advanced;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxGeneric;

/// <summary>
/// A generic fallback probe for unsupported or unidentified GPUs on Linux.
/// Relies purely on standard Linux sysfs paths, lspci, and the local GPU database.
/// Does not attempt to read vendor-specific sensors.
/// </summary>
public class LinuxGenericGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _gpuId;
    private readonly string _busId;

    public LinuxGenericGpuProbe(string gpuId)
    {
        _gpuId = gpuId;
        _basePath = $"/sys/class/drm/{gpuId}/device";
        _busId = GpuFeatureDetection.GetBusId(_basePath);
    }

    #region Static Data

    public GpuStaticData LoadStaticData()
    {
        // 1. Hardware Identification
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        string revId = GpuFeatureDetection.ReadSysfsFile(_basePath, "revision", "N/A").Replace("0x", "").ToUpper();

        // 2. Database Lookup
        var spec = PciIdLookup.GetSpecs(ids.Device, revId);

        // 3. Fallback Naming
        string deviceName = "Unknown GPU";
        if (spec != null && spec.IsExactMatch)
        {
            deviceName = spec.Name;
        }
        else
        {
            string lspciName = CommonGpuHelpers.GetDeviceNameFromLspci(_busId);
            if (!string.IsNullOrEmpty(lspciName) && lspciName != "Unknown")
            {
                deviceName = lspciName;
            }
        }

        // 4. Standard Linux Subsystem Information
        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion();
        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        
        // We pass 0 for VRAM bytes since we can't reliably read dedicated VRAM generically
        string reBarState = GpuFeatureDetection.CheckResizableBar(_basePath, 0);

        // 5. Database Specification Extraction
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
            IsExactMatch = spec?.IsExactMatch ?? false,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = _busId,
            
            // Generic fallbacks for proprietary data
            BiosVersion = "N/A",
            DriverVersion = "Unknown",
            DriverDate = "N/A",
            
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

            DefaultGpuClock = spec?.DefGpuClock ?? "N/A",
            DefaultBoostClock = spec?.DefBoostClock ?? "N/A",
            DefaultMemoryClock = spec?.DefMemClock ?? "N/A",
            
            // Real-time clocks are not generic, default to N/A
            CurrentGpuClock = "---",
            BoostClock = "---",
            CurrentMemClock = "---",
            MemorySize = "N/A",

            IsVulkanAvailable = vulkanApi != "N/A",
            IsOpenglAvailable = isOpenglAvailable,
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
            
            // Assume vendor-specific tech is unavailable
            IsCudaAvailable = false,
            IsPhysXEnabled = false,
            IsOpenClAvailable = false,
            IsRayTracingAvailable = false
        };
    }

    #endregion

    #region Sensor Data

    public GpuSensorData LoadSensorData()
    {
        // For a generic GPU, we do not attempt to read hwmon files as we don't know the vendor labels.
        // We only return the system-wide metrics.
        
        double cpuTemp = CommonGpuHelpers.GetCpuTemperature();
        double sysRam = CommonGpuHelpers.GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = 0,
            MemoryClock = 0,
            GpuTemp = 0,
            FanPercent = 0,
            BoardPower = 0,
            GpuLoad = 0,
            MemControllerLoad = 0,
            MemoryUsed = 0,
            
            // Supply system stats so the UI isn't completely empty
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam
        };
    }

    #endregion

    #region Sensor Availability

    public SensorAvailability GetSensorAvailability()
    {
        // Tell the UI that this GPU provides absolutely no sensor data
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
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return ["General", "Vulkan"];
    }

    #endregion
}