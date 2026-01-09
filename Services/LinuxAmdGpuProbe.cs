using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Do skanowania katalogów
using System.Text.RegularExpressions;

namespace GPU_T.Services;

public class LinuxAmdGpuProbe : IGpuProbe
{
    private readonly string _basePath;

    // Konstruktor przyjmuje teraz ID karty (domyślnie "card0")
    public LinuxAmdGpuProbe(string cardId = "card0")
    {
        _basePath = $"/sys/class/drm/{cardId}/device";
    }

    // Metoda statyczna do wykrywania dostępnych GPU
    public static List<string> GetAvailableCards()
    {
        var cards = new List<string>();
        try
        {
            var drmDir = "/sys/class/drm/";
            if (Directory.Exists(drmDir))
            {
                // Szukamy folderów "card0", "card1" itd.
                var dirs = Directory.GetDirectories(drmDir, "card*");
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    // Filtrujemy, żeby nie brać np. "card0-HDMI..." tylko czyste "cardX"
                    if (Regex.IsMatch(name, @"^card\d+$"))
                    {
                        cards.Add(name);
                    }
                }
            }
        }
        catch { }
        
        // Sortujemy (card0, card1...)
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

        // --- 1. DANE SYSTEMOWE ---
        string vramVendor = ReadFile("mem_info_vram_vendor"); 
        string memTypeDb = spec?.MemoryType ?? "Unknown";
        string finalMemType = !string.IsNullOrEmpty(vramVendor) && vramVendor != "N/A"
            ? $"{memTypeDb} ({System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vramVendor)})"
            : memTypeDb;

        string finalMemorySize = GetVramSize(); 
        if (finalMemorySize == "0 MB") finalMemorySize = "N/A";

        string busInterface = GetPcieInfo();
        string reBarState = CheckResizableBar();

        // --- 2. DANE Z KOMEND ---
        string driverVer = GetRealDriverVersion();
        string driverDate = GetDriverDate();
        string vulkanApi = GetVulkanApiVersion();

        // --- 3. CLOCKS SNAPSHOT (Jednorazowy odczyt na start) ---
        // Żeby nie było zer, czytamy aktualny stan. W Sensorach będziemy to robić w pętli.
        string currentGpuClock = GetCurrentClock("pp_dpm_sclk");
        string currentMemClock = GetCurrentClock("pp_dpm_mclk");

        // --- 4. HIP Detection (Zamiast HSA) ---
        // Sprawdzamy czy istnieje biblioteka HIP w systemie
        bool isHipAvailable = File.Exists("/opt/rocm/lib/libamdhip64.so") || 
                              File.Exists("/usr/lib/libamdhip64.so") ||
                              File.Exists("/usr/lib/x86_64-linux-gnu/libamdhip64.so");

        // --- 5. OBLICZENIA I BAZA ---
        string pixelFill = "N/A";
        string texFill = "N/A";
        string bandwidth = "N/A";
        string ropsTmus = "N/A";
        string lookupUrl = "";

        if (spec != null)
        {
            lookupUrl = spec.LookupUrl; // Pobieramy URL z bazy
            ropsTmus = $"{spec.Rops} / {spec.Tmus}";
            double boostClock = ExtractNumber(spec.DefBoostClock);
            double memClock = ExtractNumber(spec.DefMemClock);
            double busWidth = ExtractNumber(spec.BusWidth);
            double rops = ExtractNumber(spec.Rops);
            double tmus = ExtractNumber(spec.Tmus);

            if (boostClock > 0 && rops > 0 && tmus > 0)
            {
                pixelFill = $"{(boostClock * rops / 1000.0):0.0} GPixel/s";
                texFill = $"{(boostClock * tmus / 1000.0):0.0} GTexel/s";
            }

            if (memClock > 0 && busWidth > 0)
            {
                double multiplier = GetMemoryMultiplier(spec.MemoryType);
                double bandwidthValue = (memClock * multiplier * busWidth) / 8000.0;
                bandwidth = $"{bandwidthValue:0.0} GB/s";
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
            
            // Przekazujemy URL
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
            
            // Przekazujemy bieżące zegary (snapshot)
            CurrentGpuClock = currentGpuClock,
            CurrentMemClock = currentMemClock,

            // Zmieniono HSA na HIP
            IsHsaAvailable = isHipAvailable, 
            IsRocmAvailable = Directory.Exists("/opt/rocm") || Directory.Exists("/usr/lib/x86_64-linux-gnu/rocm"),
            IsVulkanAvailable = vulkanApi != "N/A",
            IsOpenClAvailable = File.Exists("/etc/OpenCL/vendors/amdocl64.icd") || File.Exists("/etc/OpenCL/vendors/mesa.icd"),
            IsUefiAvailable = Directory.Exists("/sys/firmware/efi"),
            IsCudaAvailable = false,
            IsRayTracingAvailable = true,
            IsPhysXEnabled = false,
            IsOpenglAvailable = true
        };
    }
    
    // --- NOWE METODY ODCZYTU Z PLIKÓW ---


    // --- Helper Clock ---
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
                    if (line.Contains("*")) // Szukamy aktywnego stanu
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
        // 1. Próba pobrania wersji Mesa z vulkaninfo (to jest najbardziej "pro")
        // vulkaninfo zwraca np: "driverInfo = Mesa 23.1.5"
        string vInfo = ShellHelper.RunCommand("vulkaninfo", "--summary");
        if (!string.IsNullOrEmpty(vInfo))
        {
            var match = Regex.Match(vInfo, @"driverInfo\s*=\s*(.*)");
            if (match.Success) return match.Groups[1].Value.Trim();
        }

        // 2. Fallback: Wersja kernela (jeśli nie mamy vulkaninfo)
        string kernel = ShellHelper.RunCommand("uname", "-r");
        if (!string.IsNullOrEmpty(kernel)) return $"{kernel} (Kernel)";

        return "Unknown";
    }

    private string GetVulkanApiVersion()
    {
        // Próba pobrania z vulkaninfo
        // output: "apiVersion = 1.3.255"
        string output = ShellHelper.RunCommand("vulkaninfo", "--summary");
        if (string.IsNullOrEmpty(output)) return "N/A";

        var match = Regex.Match(output, @"apiVersion\s*=\s*([\d\.]+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return "N/A";
    }

    private string GetDriverDate()
    {
        try
        {
            if (File.Exists("/proc/version"))
            {
                string content = File.ReadAllText("/proc/version").Trim();
                
                // Dzielimy po spacjach i usuwamy puste wpisy (żeby nie było problemu z podwójną spacją)
                var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 6)
                {
                    // Celujemy w format: "Sun Sep 21 22:36:29 CEST 2025" (na końcu pliku)
                    string day = parts[^4];   // 21
                    string month = parts[^5]; // Sep
                    string year = parts[^1];  // 2025

                    // Składamy: "21 Sep 2025"
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
            // mem_info_vram_total = Całkowita pamięć
            // mem_info_vis_vram_total = Pamięć widoczna dla CPU (przez BAR)
            long total = long.Parse(ReadFile("mem_info_vram_total", "0"));
            long visible = long.Parse(ReadFile("mem_info_vis_vram_total", "0"));

            if (total == 0) return "N/A";

            // Jeśli widoczna pamięć to więcej niż 90% całkowitej, to ReBAR działa.
            // (Dajemy margines błędu, bo czasem system rezerwuje drobne kawałki)
            if (visible > (total * 0.9))
            {
                return "Enabled";
            }
            return "Disabled";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Zmodernizowana metoda pobierania informacji o PCIe
    private string GetPcieInfo()
    {
        // 1. CAPABILITY (Lewa strona) - Bierzemy z max_link_speed/width
        // Dzięki temu nie musimy nic hardcodować w C# ani w JSON.
        string maxSpeedStr = ReadFile("max_link_speed"); // np. "16.0 GT/s PCIe"
        string maxWidthStr = ReadFile("max_link_width"); // np. "16"
        
        string maxGen = "Unknown";
        if (double.TryParse(Regex.Match(maxSpeedStr, @"(\d+\.?\d*)").Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double maxSpeedVal))
        {
            maxGen = SpeedToGen(maxSpeedVal);
        }
        string capability = $"PCIe x{maxWidthStr} {maxGen}"; // np. "PCIe x16 4.0"

        // 2. CURRENT STATE (Prawa strona)
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

        // Wynik: "PCIe x16 4.0 @ x16 3.0" - w pełni z systemu!
        return $"{capability} @ x{currentWidth} {currentGen}";
    }

    // Helper: Konwersja prędkości GT/s na Generację PCIe
    private string SpeedToGen(double gtPerSecond)
    {
        // Tolerancja na drobne odchylenia float
        if (gtPerSecond > 30.0) return "5.0"; // 32.0 GT/s = Gen 5
        if (gtPerSecond > 15.0) return "4.0"; // 16.0 GT/s = Gen 4
        if (gtPerSecond > 7.0)  return "3.0"; // 8.0 GT/s  = Gen 3
        if (gtPerSecond > 4.0)  return "2.0"; // 5.0 GT/s  = Gen 2
        return "1.1";                         // 2.5 GT/s  = Gen 1.1
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

    // ... [Reszta metod ExtractNumber, GetMemoryMultiplier, GetRawIds, ReadFile, etc. bez zmian] ...
    
// --- Metody Pomocnicze ---

    // Wyciąga pierwszą liczbę ze stringa (np. "384 Bit" -> 384.0, "2250 MHz" -> 2250.0)
    private double ExtractNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        
        // Regex szuka ciągu cyfr (ewentualnie z kropką)
        var match = Regex.Match(input, @"[\d]+(\.[\d]+)?");
        if (match.Success && double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }
        return 0;
    }

    // Zwraca mnożnik efektywnego taktowani w zależności od typu pamięci
    private double GetMemoryMultiplier(string memoryType)
    {
        if (string.IsNullOrEmpty(memoryType)) return 1;
        
        string type = memoryType.ToUpperInvariant();

        if (type.Contains("GDDR6") || type.Contains("GDDR6X")) return 8.0; // 2500 MHz * 8 = 20000 Mbps
        if (type.Contains("GDDR5") || type.Contains("GDDR5X")) return 4.0; // 2000 MHz * 4 = 8000 Mbps
        if (type.Contains("HBM") || type.Contains("HBM2")) return 2.0;     // High Bandwidth Memory zazwyczaj x2
        if (type.Contains("DDR")) return 2.0;                              // Standardowe DDR to x2

        return 1.0; // Domyślnie brak mnożnika
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