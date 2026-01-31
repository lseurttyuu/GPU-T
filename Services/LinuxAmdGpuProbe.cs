using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq; 
using System.Text.RegularExpressions;
using System.Diagnostics; 

namespace GPU_T.Services;

public class LinuxAmdGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _hwmonPath;
    private readonly double _memClockMultiplier = 1.0;

    // Statyczny cache dla wsparcia Ray Tracingu (DeviceID -> Supported)
    private static Dictionary<string, bool>? _rtSupportCache;

    public LinuxAmdGpuProbe(string gpuId, string memoryType = "")
    {
        _basePath = $"/sys/class/drm/{gpuId}/device";
        
        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }

        if (!string.IsNullOrEmpty(memoryType) && 
            memoryType.Contains("GDDR6", StringComparison.OrdinalIgnoreCase))
        {
            _memClockMultiplier = 2.0;
        }
    }

    public SensorAvailability GetSensorAvailability()
    {
        var avail = new SensorAvailability();

        if (Directory.Exists(_hwmonPath))
        {
            for (int i = 1; i <= 4; i++)
            {
                string labelPath = Path.Combine(_hwmonPath, $"temp{i}_label");
                if (File.Exists(labelPath))
                {
                    string label = File.ReadAllText(labelPath).Trim().ToLower();
                    if (label.Contains("junction") || label.Contains("hotspot")) avail.HasHotSpot = true;
                    if (label.Contains("mem")) avail.HasMemTemp = true;
                }
            }

            if (File.Exists(Path.Combine(_hwmonPath, "fan1_input"))) avail.HasFan = true;
            if (File.Exists(Path.Combine(_hwmonPath, "power1_average")) || 
                File.Exists(Path.Combine(_hwmonPath, "power1_input"))) avail.HasPower = true;
            if (File.Exists(Path.Combine(_hwmonPath, "in0_input"))) avail.HasVoltage = true;
        }

        if (File.Exists(Path.Combine(_basePath, "gpu_busy_percent"))) avail.HasGpuLoad = true;
        if (File.Exists(Path.Combine(_basePath, "mem_busy_percent"))) avail.HasMemControllerLoad = true;
        if (File.Exists(Path.Combine(_basePath, "mem_info_vram_used"))) avail.HasMemUsed = true;

        return avail;
    }

    public static List<string> GetAvailableCards()
    {
        var cards = new List<string>();
        try
        {
            var drmDir = "/sys/class/drm/";
            if (Directory.Exists(drmDir))
            {
                var dirs = Directory.GetDirectories(drmDir, "card*");
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    if (Regex.IsMatch(name, @"^card\d+$"))
                    {
                        cards.Add(name);
                    }
                }
            }
        }
        catch { }
        cards.Sort();
        return cards;
    }

    public GpuStaticData LoadStaticData()
    {
        if (!Directory.Exists(_basePath))
        {
            return new GpuStaticData { DeviceName = "No AMD GPU found at " + _basePath };
        }

        var ids = GetRawIds();
        var spec = PciIdLookup.GetSpecs(ids.Device);

        double dpmMemMultiplier = 1.0;
        
        if (spec != null && !string.IsNullOrEmpty(spec.MemoryType) && 
            spec.MemoryType.Contains("GDDR6", StringComparison.OrdinalIgnoreCase))
        {
            dpmMemMultiplier = 2.0;
        }

        // --- DANE SYSTEMOWE ---
        string vramVendor = ReadFile("mem_info_vram_vendor"); 
        string memTypeDb = spec?.MemoryType ?? "Unknown";
        string finalMemType = !string.IsNullOrEmpty(vramVendor) && vramVendor != "N/A"
            ? $"{memTypeDb} ({System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vramVendor)})"
            : memTypeDb;

        string finalMemorySize = GetVramSize(); 
        if (finalMemorySize == "0 MB") finalMemorySize = "N/A";

        string busInterface = GetPcieInfo();
        string reBarState = CheckResizableBar();

        string driverVer = GetRealDriverVersion();
        string driverDate = GetDriverDate();
        string vulkanApi = GetVulkanApiVersion();

        // --- ZEGARY DPM ---
        double maxCoreDpm = GetMaxClockFromDpm("pp_dpm_sclk");
        double maxMemDpm = GetMaxClockFromDpm("pp_dpm_mclk") * dpmMemMultiplier;

        // --- DETEKCJA FUNKCJI ---
        bool isHipAvailable = File.Exists("/opt/rocm/lib/libamdhip64.so") || 
                              File.Exists("/usr/lib/libamdhip64.so") ||
                              File.Exists("/usr/lib/x86_64-linux-gnu/libamdhip64.so");

        bool isOpenglAvailable = CheckOpenglSupport();

        // Ray Tracing sprawdzany przez vulkaninfo (z poprawionym cachem)
        bool isRayTracingAvailable = CheckRayTracingSupportVulkan(ids.Device);

        // --- OBLICZENIA ---
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
            double boostClock = ExtractNumber(spec.DefBoostClock);
            double memClock = ExtractNumber(spec.DefMemClock);
            double busWidth = ExtractNumber(spec.BusWidth);
            double rops = ExtractNumber(spec.Rops);
            double tmus = ExtractNumber(spec.Tmus);

            if (boostClock > 0 && rops > 0 && tmus > 0)
            {
                pixelFill = $"{(boostClock * rops / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} GPixel/s";
                texFill = $"{(boostClock * tmus / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} GTexel/s";
            }

            if (memClock > 0 && busWidth > 0)
            {
                double multiplier = GetMemoryMultiplier(spec.MemoryType);
                double bandwidthValue = (memClock * multiplier * busWidth) / 8000.0;
                bandwidth = $"{bandwidthValue.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} GB/s";
            }
        }

        return new GpuStaticData
        {
            DeviceName = spec?.Name ?? "Unknown AMD GPU",
            DeviceId = $"{ids.Vendor} {ids.Device} - {ids.SubVendor} {ids.SubDevice}",
            Subvendor = PciIdLookup.LookupVendorName(ids.SubVendor),
            BusId = GetBusId(),
            BiosVersion = ReadFile("vbios_version", "Unknown"),
            DriverVersion = driverVer,
            DriverDate = driverDate,
            VulkanApi = vulkanApi,
            BusInterface = busInterface, 
            ResizableBarState = reBarState,
            LookupUrl = lookupUrl,

            GpuCodeName = spec?.CodeName ?? "N/A",
            Revision = ReadFile("revision", "N/A").Replace("0x", "").ToUpper(),
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
            IsVulkanAvailable = vulkanApi != "N/A",
            IsOpenClAvailable = File.Exists("/etc/OpenCL/vendors/amdocl64.icd") || File.Exists("/etc/OpenCL/vendors/mesa.icd"),
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
            
            IsCudaAvailable = false,
            IsRayTracingAvailable = isRayTracingAvailable,
            IsPhysXEnabled = false,
            IsOpenglAvailable = isOpenglAvailable
        };
    }

    // --- POPRAWIONA WERYFIKACJA OPENGL ---
    private bool CheckOpenglSupport()
    {
        try 
        {
            var psi = new ProcessStartInfo("glxinfo", "-B")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var p = Process.Start(psi);
            if (p == null) return false;
            
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if (string.IsNullOrEmpty(output)) return false;

            // Sprawdzamy czy akceleracja działa (Direct rendering)
            bool direct = output.Contains("direct rendering: Yes");
            
            // Sprawdzamy czy nie używamy software rasterizera (llvmpipe)
            bool isSoftware = output.Contains("llvmpipe");

            return direct && !isSoftware;
        }
        catch 
        {
            return false; 
        }
    }

    // --- POPRAWIONA WERYFIKACJA RAY TRACING (VULKANINFO) ---
    private bool CheckRayTracingSupportVulkan(string currentDeviceIdHex)
    {
        // 1. Sprawdź czy mamy to już w pamięci
        if (_rtSupportCache == null)
        {
            _rtSupportCache = new Dictionary<string, bool>();
            PopulateRtCache();
        }

        // 2. Pobierz wartość z cache (znormalizowany ID np. "744C")
        string key = currentDeviceIdHex.ToUpper().Trim();
        if (_rtSupportCache.ContainsKey(key))
        {
            return _rtSupportCache[key];
        }

        // Jeśli nie znaleziono w cache, zwracamy false
        return false;
    }

    // Metoda uruchamiana tylko RAZ na sesję aplikacji
    private void PopulateRtCache()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "vulkaninfo",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) return;

            using StreamReader reader = process.StandardOutput;
            string? line;
            
            // Stan parsera
            string currentDevice = "";
            bool currentHasRt = false;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                // 1. DETEKCJA NOWEJ SEKCJI GPU
                // Obsługuje formaty: "GPU id : 0...", "GPU0:", "GPU 0:"
                // Kluczowe dla vlkinfo.txt użytkownika (tam jest "GPU0:")
                bool isNewGpuSection = trimmed.StartsWith("GPU id :") || 
                                      (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("="));

                if (isNewGpuSection)
                {
                    // Zapisz poprzednią kartę jeśli istniała
                    if (!string.IsNullOrEmpty(currentDevice))
                    {
                        if (!_rtSupportCache!.ContainsKey(currentDevice))
                        {
                            _rtSupportCache[currentDevice] = currentHasRt;
                        }
                        else if (currentHasRt) 
                        {
                            _rtSupportCache[currentDevice] = true;
                        }
                    }

                    // Reset
                    currentDevice = "";
                    currentHasRt = false;
                }

                // 2. SZUKANIE ID URZĄDZENIA
                if (trimmed.StartsWith("deviceID"))
                {
                    // Format: deviceID = 0x744c
                    var parts = trimmed.Split('=');
                    if (parts.Length > 1)
                    {
                        // Wyciągamy "744C" z "0x744c"
                        currentDevice = parts[1].Trim().Replace("0x", "").ToUpper();
                    }
                }

                // 3. SZUKANIE ROZSZERZENIA RAY TRACING
                if (trimmed.Contains("VK_KHR_ray_tracing_pipeline") || trimmed.Contains("VK_KHR_ray_query"))
                {
                    currentHasRt = true;
                }
            }

            // Zapisz ostatnią kartę po wyjściu z pętli
            if (!string.IsNullOrEmpty(currentDevice))
            {
                if (!_rtSupportCache!.ContainsKey(currentDevice))
                    _rtSupportCache[currentDevice] = currentHasRt;
            }

            process.WaitForExit();
        }
        catch
        {
            // Fail silent
        }
    }

    // --- HELPERY DPM ---
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
        catch
        {
            return 0;
        }
    }

    // --- HELPERY STANDARDOWE ---
    public GpuSensorData LoadSensorData()
    {
        double coreClk = ReadFreq("freq1", "sclk"); 
        double memClk  = ReadFreq("freq2", "mclk")*_memClockMultiplier; 

        if (coreClk == 0) coreClk = ParseClock(GetCurrentClock("pp_dpm_sclk"));
        if (memClk == 0)  memClk  = ParseClock(GetCurrentClock("pp_dpm_mclk"))*_memClockMultiplier; 

        double tEdge = 0, tSpot = 0, tMem = 0;
        for (int i = 1; i <= 3; i++)
        {
            string label = ReadFileFromHwmon($"temp{i}_label", "").ToLower();
            double val = ReadHwmonDouble($"temp{i}_input") / 1000.0;

            if (label.Contains("edge") || label == "") tEdge = val;
            else if (label.Contains("junction") || label.Contains("hotspot")) tSpot = val;
            else if (label.Contains("mem")) tMem = val;
        }
        if (tEdge == 0) tEdge = ReadHwmonDouble("temp1_input") / 1000.0;

        int fanRpm = (int)ReadHwmonDouble("fan1_input");
        int fanPct = 0;
        double pwmNow = ReadHwmonDouble("pwm1");
        double pwmMax = ReadHwmonDouble("pwm1_max");
        if (pwmMax > 0) fanPct = (int)((pwmNow / pwmMax) * 100.0);

        double powerW = ReadHwmonDouble("power1_average");
        if (powerW == 0) powerW = ReadHwmonDouble("power1_input");
        powerW /= 1000000.0;

        double voltage = ReadHwmonDouble("in0_input") / 1000.0;

        int load = 0;
        int.TryParse(ReadFile("gpu_busy_percent", "0"), out load);

        double memUsedMb = 0;
        if (long.TryParse(ReadFile("mem_info_vram_used", "0"), out long memBytes))
            memUsedMb = memBytes / (1024.0 * 1024.0);

        int memLoad = 0;
        int.TryParse(ReadFile("mem_busy_percent", "0"), out memLoad);

        double memGttMb = 0;
        if (long.TryParse(ReadFile("mem_info_gtt_used", "0"), out long gttBytes))
            memGttMb = gttBytes / (1024.0 * 1024.0);

        double cpuTemp = GetCpuTemperature();
        double sysRam = GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = coreClk,
            MemoryClock = memClk,
            GpuTemp = tEdge,
            GpuHotSpot = tSpot,
            MemoryTemp = tMem,
            FanRpm = fanRpm,
            FanPercent = fanPct,
            BoardPower = powerW,
            GpuLoad = load,
            MemoryUsed = memUsedMb,
            GpuVoltage = voltage,
            MemControllerLoad = memLoad,
            MemoryUsedDynamic = memGttMb,
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam
        };
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
                double total = 0;
                double avail = 0;

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

    private double ExtractKb(string line)
    {
        var match = Regex.Match(line, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }

    private double ReadFreq(string prefix, string expectedLabelContent)
    {
        string label = ReadFileFromHwmon($"{prefix}_label", "").ToLower();
        if (string.IsNullOrEmpty(label) || label.Contains(expectedLabelContent))
        {
            return ReadHwmonDouble($"{prefix}_input") / 1000000.0;
        }
        return 0;
    }

    private string ReadFileFromHwmon(string filename, string fallback)
    {
        if (string.IsNullOrEmpty(_hwmonPath)) return fallback;
        try {
            string p = Path.Combine(_hwmonPath, filename);
            return File.Exists(p) ? File.ReadAllText(p).Trim() : fallback;
        } catch { return fallback; }
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
                if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                    return val;
            }
        }
        catch { }
        return 0;
    }

    private double ParseClock(string clockString)
    {
        var match = Regex.Match(clockString, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }

    private string GetCurrentClock(string fileName)
    {
        try
        {
            string path = Path.Combine(_basePath, fileName);
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (line.Contains("*"))
                    {
                        var match = Regex.Match(line, @"(\d+)Mhz");
                        if (match.Success) return $"{match.Groups[1].Value} MHz";
                    }
                }
            }
        }
        catch {}
        return "0 MHz";
    }

    private string GetRealDriverVersion()
    {
        try {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary") {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string vInfo = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var match = Regex.Match(vInfo, @"driverInfo\s*=\s*(.*)");
                if (match.Success) return match.Groups[1].Value.Trim();
            }
        } catch {}

        string kernel = GetKernelVersion();
        if (!string.IsNullOrEmpty(kernel)) return $"{kernel} (Kernel)";
        return "Unknown";
    }

    private string GetKernelVersion()
    {
        try {
            var psi = new ProcessStartInfo("uname", "-r") {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
        } catch {}
        return "";
    }

    private string GetVulkanApiVersion()
    {
        try {
            var psi = new ProcessStartInfo("vulkaninfo", "--summary") {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null) {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var match = Regex.Match(output, @"apiVersion\s*=\s*([\d\.]+)");
                if (match.Success) return match.Groups[1].Value;
            }
        } catch {}
        return "N/A";
    }

    private string GetDriverDate()
    {
        try
        {
            if (File.Exists("/proc/version"))
            {
                string content = File.ReadAllText("/proc/version").Trim();
                var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 6)
                {
                    string day = parts[^4];
                    string month = parts[^5];
                    string year = parts[^1];
                    return $"{day} {month} {year}";
                }
            }
        }
        catch {}
        return "N/A";
    }

    private string CheckResizableBar()
    {
        try
        {
            long total = long.Parse(ReadFile("mem_info_vram_total", "0"));
            long visible = long.Parse(ReadFile("mem_info_vis_vram_total", "0"));
            if (total == 0) return "N/A";
            if (visible > (total * 0.9)) return "Enabled";
            return "Disabled";
        }
        catch { return "Unknown"; }
    }

    private string GetPcieInfo()
    {
        string maxSpeedStr = ReadFile("max_link_speed");
        string maxWidthStr = ReadFile("max_link_width");
        string maxGen = "Unknown";
        if (double.TryParse(Regex.Match(maxSpeedStr, @"(\d+\.?\d*)").Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double maxSpeedVal))
        {
            maxGen = SpeedToGen(maxSpeedVal);
        }
        string capability = $"PCIe x{maxWidthStr} {maxGen}";

        string currentGen = "?";
        string currentWidth = "?";
        bool foundInDpm = false;

        try
        {
            string dpmPath = Path.Combine(_basePath, "pp_dpm_pcie");
            if (File.Exists(dpmPath))
            {
                string[] lines = File.ReadAllLines(dpmPath);
                foreach (var line in lines)
                {
                    if (line.Contains("*"))
                    {
                        var match = Regex.Match(line, @"(\d+\.?\d*)GT/s,\s*x(\d+)");
                        if (match.Success)
                        {
                            double speedGt = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                            currentWidth = match.Groups[2].Value;
                            currentGen = SpeedToGen(speedGt);
                            foundInDpm = true;
                        }
                        break;
                    }
                }
            }
        }
        catch { }

        if (!foundInDpm)
        {
            try
            {
                string speedStr = ReadFile("current_link_speed");
                string widthStr = ReadFile("current_link_width");
                currentWidth = widthStr;
                var match = Regex.Match(speedStr, @"(\d+\.?\d*)");
                if (match.Success)
                {
                    double speedGt = double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
                    currentGen = SpeedToGen(speedGt);
                }
            }
            catch 
            {
                currentWidth = "x16";
                currentGen = "Unknown";
            }
        }
        return $"{capability} @ x{currentWidth} {currentGen}";
    }

    private string SpeedToGen(double gtPerSecond)
    {
        if (gtPerSecond > 30.0) return "5.0";
        if (gtPerSecond > 15.0) return "4.0";
        if (gtPerSecond > 7.0)  return "3.0";
        if (gtPerSecond > 4.0)  return "2.0";
        return "1.1";
    }

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

    private double ExtractNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var match = Regex.Match(input, @"[\d]+(\.[\d]+)?");
        if (match.Success && double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }

    private double GetMemoryMultiplier(string memoryType)
    {
        if (string.IsNullOrEmpty(memoryType)) return 1;
        string type = memoryType.ToUpperInvariant();
        if (type.Contains("GDDR6") || type.Contains("GDDR6X")) return 8.0; 
        if (type.Contains("GDDR5") || type.Contains("GDDR5X")) return 4.0; 
        if (type.Contains("HBM") || type.Contains("HBM2")) return 2.0;     
        if (type.Contains("DDR")) return 2.0;                              
        return 1.0; 
    }

    private record RawIds(string Vendor, string Device, string SubVendor, string SubDevice);

    private RawIds GetRawIds()
    {
        try 
        {
            string v = ReadFile("vendor").Replace("0x", "").ToUpper();
            string d = ReadFile("device").Replace("0x", "").ToUpper();
            string sv = ReadFile("subsystem_vendor").Replace("0x", "").ToUpper();
            string sd = ReadFile("subsystem_device").Replace("0x", "").ToUpper();
            return new RawIds(v, d, sv, sd);
        }
        catch
        {
            return new RawIds("0000", "0000", "0000", "0000");
        }
    }

    private string ReadFile(string filename, string fallback = "N/A")
    {
        try
        {
            string path = Path.Combine(_basePath, filename);
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
        }
        catch { }
        return fallback;
    }

    private string GetBusId()
    {
        try
        {
            var targetInfo = File.ResolveLinkTarget(_basePath, true);
            if (targetInfo != null) return targetInfo.Name;
            return new DirectoryInfo(_basePath).Name; 
        }
        catch { return "Unknown"; }
    }
    
    private string GetDriverVersion()
    {
        try
        {
            string path = "/sys/module/amdgpu/version";
            if (File.Exists(path)) return $"{File.ReadAllText(path).Trim()} (amdgpu)";
        }
        catch {}
        return "Unknown";
    }
}

public class SensorAvailability
{
    public bool HasHotSpot { get; set; }
    public bool HasMemTemp { get; set; }
    public bool HasFan { get; set; }
    public bool HasGpuLoad { get; set; }
    public bool HasMemControllerLoad { get; set; }
    public bool HasPower { get; set; }
    public bool HasVoltage { get; set; }
    public bool HasMemUsed { get; set; } 
}