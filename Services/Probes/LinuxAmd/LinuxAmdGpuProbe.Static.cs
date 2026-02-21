using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides static hardware and capability discovery for AMD GPUs on Linux.
/// Contains logic to read sysfs, invoke platform utilities, and compute derived specifications.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    /// <summary>
    /// Loads static data describing the GPU, driver, and capabilities.
    /// </summary>
    public GpuStaticData LoadStaticData()
    {
        if (!Directory.Exists(_basePath))
        {
            return new GpuStaticData { DeviceName = "No AMD GPU found at " + _basePath };
        }

        var ids = GetRawIds();
        string revId = ReadFile("revision").Replace("0x", "").ToUpper();

        var spec = PciIdLookup.GetSpecs(ids.Device, revId);

        double dpmMemMultiplier = 1.0;

        // Apply multiplier when detected memory type implies effective DPM frequency doubling (e.g., GDDR6).
        if (spec != null && !string.IsNullOrEmpty(spec.MemoryType) &&
            spec.MemoryType.Contains("GDDR6", StringComparison.OrdinalIgnoreCase))
        {
            dpmMemMultiplier = 2.0;
        }

        string vramVendor = ReadFile("mem_info_vram_vendor");
        string memTypeDb = spec?.MemoryType ?? "Unknown";
        string finalMemType = !string.IsNullOrEmpty(vramVendor) && vramVendor != "N/A"
            ? $"{memTypeDb} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vramVendor)})"
            : memTypeDb;

        string finalMemorySize = GetVramSize();
        if (finalMemorySize == "0 MB") finalMemorySize = "N/A";

        string busInterface = GpuFeatureDetection.GetPcieInfo(_basePath);
        string reBarState = CheckResizableBar();

        string driverVer = GpuFeatureDetection.GetRealDriverVersion();
        string driverDate = GpuFeatureDetection.GetKernelDriverDate();
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion();

        double maxCoreDpm = GetMaxClockFromDpm("pp_dpm_sclk");
        double maxMemDpm = GetMaxClockFromDpm("pp_dpm_mclk") * dpmMemMultiplier;

        // Detect presence of optional runtime libraries or capabilities; these checks reflect user-facing feature toggles.
        bool isHipAvailable = GpuFeatureDetection.IsNativeLibraryAvailable("libamdhip64.so");

        bool isOpenglAvailable = CheckOpenglSupport();
        bool isRayTracingAvailable = CheckRayTracingSupportVulkan(ids.Device);

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
            DeviceName = spec?.Name ?? "Unknown AMD GPU",
            IsExactMatch = spec?.IsExactMatch ?? true,
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = GpuFeatureDetection.GetBusId(_basePath),
            BiosVersion = ReadFile("vbios_version", "Unknown"),
            DriverVersion = driverVer,
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
            IsVulkanAvailable = vulkanApi != "N/A" || GpuFeatureDetection.CheckVulkanIcdInstalled("radeon_icd.x86_64.json", "radeon_icd.i686.json"),
            IsOpenClAvailable = GpuFeatureDetection.CheckOpenClIcdInstalled("amdocl64.icd", "mesa.icd"),
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),

            IsCudaAvailable = false,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsPhysXEnabled = false,
            IsOpenglAvailable = isOpenglAvailable
        };
    }

    /// <summary>
    /// Represents raw PCI IDs for device and subsystem.
    /// </summary>
    private record RawIds(string Vendor, string Device, string SubVendor, string SubDevice);

    /// <summary>
    /// Reads PCI IDs from sysfs files and returns a RawIds record.
    /// </summary>
    private RawIds GetRawIds()
    {
        var ids = GpuFeatureDetection.GetRawPciIds(_basePath);
        return new RawIds(ids.Vendor, ids.Device, ids.SubVendor, ids.SubDevice);
    }

    /// <summary>
    /// Reads VRAM size from sysfs and returns it as a formatted string.
    /// </summary>
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

    /// <summary>
    /// Determines if Resizable BAR (ReBAR) is enabled using a heuristic based on BAR size and VRAM.
    /// </summary>
    private string CheckResizableBar()
    {
        try
        {
            long totalVram = long.Parse(ReadFile("mem_info_vram_total", "0"));
            return GpuFeatureDetection.CheckResizableBar(_basePath, totalVram);
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Reads the maximum clock value from a DPM file.
    /// </summary>
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
