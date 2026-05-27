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
        string uniqueId = ReadFile("unique_id", "Unknown").Trim();

        var spec = PciIdLookup.GetSpecs(ids.Vendor, ids.Device, revId);

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

        string driverVer = GpuFeatureDetection.GetRealDriverVersion(ids.Device);
        string driverDate = GpuFeatureDetection.GetKernelDriverDate();
        string vulkanApi = GpuFeatureDetection.GetVulkanApiVersion(ids.Device);

        var odClocks = GetMaxClocksFromOd("pp_od_clk_voltage");

        double maxCoreDpm = GetMaxClockFromDpm("pp_dpm_sclk");

        double maxMemDpm = odClocks.Mclk * dpmMemMultiplier;
        if (maxMemDpm <= 0)
            maxMemDpm = GetMaxClockFromDpm("pp_dpm_mclk") * dpmMemMultiplier;

        bool isRocmAvailable = GpuFeatureDetection.IsNativeLibraryAvailable("libhsa-runtime64.so.1") && 
                                                    (Directory.Exists("/opt/rocm") || Directory.Exists("/usr/lib/x86_64-linux-gnu/rocm"));

        // Detect presence of optional runtime libraries or capabilities; these checks reflect user-facing feature toggles.
        bool isHipAvailable = GpuFeatureDetection.IsNativeLibraryAvailable("libamdhip64.so") && isRocmAvailable;

        bool isOpenglAvailable = GpuFeatureDetection.CheckOpenglSupport();
        bool isRayTracingAvailable = GpuFeatureDetection.CheckRayTracingSupportVulkan(ids.Device);

        bool isOpenClAvailable = GpuFeatureDetection.CheckOpenClIcdInstalled("amdocl64.icd", "mesa.icd", "rusticl.icd");

        //string pixelFill = "N/A";
        //string texFill = "N/A";
        //string bandwidth = "N/A";
        string ropsTmus = "N/A";
        string lookupUrl = "";

        //string gpuClock = "---";
        //string boostClockDisplay = "---";
        //string memClockDisplay = "---";

        //string defaultGpuClockDb = "N/A";

        double actualBoost = 0;

        (string GpuClock, string BoostClock, string MemClock, 
         string PixelFill, string TexFill, string Bandwidth) dynamicSpecs = ("---", "---", "---", "N/A", "N/A", "N/A");

        string defaultGpuClockDb = "N/A";

        if (spec != null)
        {
            lookupUrl = spec.LookupUrl;
            ropsTmus = $"{spec.Rops} / {spec.Tmus}";
            double boostClock = CommonGpuHelpers.ExtractNumber(spec.DefBoostClock);
            double memClock = CommonGpuHelpers.ExtractNumber(spec.DefMemClock);
            double busWidth = CommonGpuHelpers.ExtractNumber(spec.BusWidth);
            double rops = CommonGpuHelpers.ExtractNumber(spec.Rops);
            double tmus = CommonGpuHelpers.ExtractNumber(spec.Tmus);

            defaultGpuClockDb = spec?.DefGpuClock ?? "N/A";
        
            // Use GameClock if it exists and is valid, otherwise fallback to standard DefGpuClock
            if (spec != null && !string.IsNullOrWhiteSpace(spec.GameClock) && spec.GameClock != "N/A")
            {
                defaultGpuClockDb = spec.GameClock;
            }

            double baseClock = CommonGpuHelpers.ExtractNumber(defaultGpuClockDb);

            // Trigger our new helper to calculate everything dynamically
            dynamicSpecs = CalculateDynamicSpecs(
                maxCoreDpm, maxMemDpm, odClocks.Sclk,
                baseClock, boostClock, memClock,
                rops, tmus, busWidth, spec.MemoryType);
        }
        else
        {
            // Even if the DB spec is missing, pass 0s to format the sysfs DPM clock reads correctly!
            dynamicSpecs = CalculateDynamicSpecs(
                maxCoreDpm, maxMemDpm, odClocks.Sclk,
                0, 0, 0, 0, 0, 0, "");
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
            UniqueId = uniqueId,
            Technology = spec?.Technology ?? "N/A",
            DieSize = spec?.DieSize ?? "N/A",
            ReleaseDate = spec?.ReleaseDate ?? "N/A",
            Transistors = spec?.Transistors ?? "N/A",
            RopsTmus = ropsTmus,
            Shaders = spec?.Shaders ?? "N/A",
            ComputeUnits = spec?.ComputeUnits ?? "N/A",
            PixelFillrate = dynamicSpecs.PixelFill,
            TextureFillrate = dynamicSpecs.TexFill,
            MemoryType = finalMemType,
            BusWidth = spec?.BusWidth ?? "N/A",
            MemorySize = finalMemorySize,
            Bandwidth = dynamicSpecs.Bandwidth,
            DefaultGpuClock = defaultGpuClockDb,
            DefaultBoostClock = spec?.DefBoostClock ?? "N/A",
            DefaultMemoryClock = spec?.DefMemClock ?? "N/A",

            CurrentGpuClock = dynamicSpecs.GpuClock,
            BoostClock = dynamicSpecs.BoostClock,
            CurrentMemClock = dynamicSpecs.MemClock,

            IsHsaAvailable = isHipAvailable,
            IsRocmAvailable = isRocmAvailable,
            IsVulkanAvailable = GpuFeatureDetection.CheckVulkanSupport(ids.Device, "radeon_icd.x86_64.json", "radeon_icd.i686.json"),
            IsOpenClAvailable = isOpenClAvailable,
            IsUefiAvailable = CommonGpuHelpers.CheckGpuUefiSupport(spec?.ReleaseDate),

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

    /// <summary>
    /// Reads the maximum core and memory clocks from the AMD OD conf file.
    /// Returns 0 for values it cannot find.
    /// </summary>
    private (double Sclk, double Mclk) GetMaxClocksFromOd(string fileName)
    {
        try
        {
            string path = Path.Combine(_basePath, fileName);
            if (!File.Exists(path)) return (0, 0);

            string[] lines = File.ReadAllLines(path);
            double maxSclk = 0;
            double maxMclk = 0;
            string currentSection = "";

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                
                // Track which section of the file we are currently reading
                if (trimmed.StartsWith("OD_SCLK:")) { currentSection = "SCLK"; continue; }
                if (trimmed.StartsWith("OD_MCLK:")) { currentSection = "MCLK"; continue; }
                if (trimmed.StartsWith("OD_")) { currentSection = "OTHER"; continue; }

                // If we are in the Core or Memory section, parse the MHz value
                if (currentSection == "SCLK" || currentSection == "MCLK")
                {
                    var match = Regex.Match(trimmed, @"(\d+)\s*Mhz", RegexOptions.IgnoreCase);
                    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        if (currentSection == "SCLK" && val > maxSclk) maxSclk = val;
                        if (currentSection == "MCLK" && val > maxMclk) maxMclk = val;
                    }
                }
            }
            return (maxSclk, maxMclk);
        }
        catch { return (0, 0); }
    }

}
