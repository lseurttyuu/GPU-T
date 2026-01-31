using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Potrzebne do RelayCommand
using GPU_T.Services;
using Avalonia.Threading; // Do DispatcherTimer
using System.Linq;
using System.Diagnostics;
using System.Text.Json.Nodes; // Do parsowania JSON
using Avalonia.Controls; // Potrzebne do enum WindowStartupLocation, SizeToContent itp.

namespace GPU_T.ViewModels;


public class GpuListItem
{
    public string Id { get; set; }          // Np. "card0"
    public string DisplayName { get; set; } // Np. "AMD Radeon RX 7900 XTX (card0)"

    // Nadpisujemy ToString, żeby ComboBox wiedział co wyświetlić (najprostsza metoda)
    public override string ToString() => DisplayName; 
}


public partial class MainWindowViewModel : ViewModelBase
{
    // ==========================================================
    // DOMYŚLNE WARTOŚCI: "N/A" lub "Unknown"
    // ==========================================================

    [ObservableProperty] private string _deviceName = "Detecting...";
    
    // Dane Architektury (Jeszcze nie zaimplementowane)
    [ObservableProperty] private string _gpuCodeName = "N/A";
    [ObservableProperty] private string _revision = "N/A";
    [ObservableProperty] private string _technology = "N/A";
    [ObservableProperty] private string _dieSize = "N/A";
    [ObservableProperty] private string _releaseDate = "N/A";
    [ObservableProperty] private string _transistors = "N/A";
    
    // Dane Systemowe (Częściowo zaimplementowane)
    [ObservableProperty] private string _biosVersion = "Unknown";
    [ObservableProperty] private bool _isUefiEnabled;
    [ObservableProperty] private string _subvendor = "Unknown";
    [ObservableProperty] private string _deviceId = "Unknown";
    [ObservableProperty] private string _busInterface = "N/A"; // Np. PCIe x16
    
    // Dane Jednostek (Jeszcze nie zaimplementowane)
    [ObservableProperty] private string _ropsTmus = "N/A";
    [ObservableProperty] private string _shaders = "N/A";
    [ObservableProperty] private string _computeUnits = "N/A"; 
    [ObservableProperty] private string _pixelFillrate = "N/A";
    [ObservableProperty] private string _textureFillrate = "N/A";
    
    // Pamięć (Jeszcze nie zaimplementowane)
    [ObservableProperty] private string _memoryType = "N/A";
    [ObservableProperty] private string _busWidth = "N/A";
    [ObservableProperty] private string _memorySize = "0 MB";
    [ObservableProperty] private string _bandwidth = "N/A";
    
    // Sterowniki (Częściowo zaimplementowane)
    [ObservableProperty] private string _driverVersion = "Unknown";
    [ObservableProperty] private string _driverDate = "N/A";     // Trudne do wyciągnięcia bez zewnętrznej bazy
    [ObservableProperty] private string _vulkanApi = "N/A";      // Wymaga 'vulkaninfo'
    [ObservableProperty] private string _busId = "0000:00:00.0";
    
    // Zegary (Jeszcze nie zaimplementowane - dynamiczne)
    [ObservableProperty] private string _gpuClock = "0 MHz";
    [ObservableProperty] private string _memoryClock = "0 MHz";
    [ObservableProperty] private string _boostClock = "0 MHz";

    [ObservableProperty] private string _defaultGpuClock = "0 MHz";
    [ObservableProperty] private string _defaultMemoryClock = "0 MHz";
    [ObservableProperty] private string _defaultBoostClock = "0 MHz";

    [ObservableProperty] private string _resizableBar = "N/A"; // Nowe pole

    // COMPUTING (Checkboxy)
    [ObservableProperty] private bool _isOpenClEnabled;
    [ObservableProperty] private bool _isCudaEnabled;
    [ObservableProperty] private bool _isRocmEnabled;
    [ObservableProperty] private bool _isHsaEnabled;

    // TECHNOLOGIES (Checkboxy)
    [ObservableProperty] private bool _isVulkanEnabled;
    [ObservableProperty] private bool _isRayTracingEnabled;
    [ObservableProperty] private bool _isPhysXEnabled;
    [ObservableProperty] private bool _isOpenglEnabled;

    [ObservableProperty] private ObservableCollection<GpuListItem> _availableGpus;
    [ObservableProperty] private GpuListItem? _selectedGpu; // Znak zapytania, bo może być null na starcie
    private string _currentLookupUrl = "";

    private void AddAdvancedRow(ObservableCollection<AdvancedItemViewModel> list, string name, string value = "", bool isHeader = false)
    {
        // Logika zebry: Parzyste = Biały, Nieparzyste = Szary (#F4F4F4)
        string color = (_advancedRowCounter % 2 == 0) ? "#FFFFFF" : "#F4F4F4";
        
        list.Add(new AdvancedItemViewModel(name, value, isHeader, color));
        
        _advancedRowCounter++;
    }

    public class RefreshRateItem
{
    public string Label { get; set; } = "";
    public double Seconds { get; set; }
    public override string ToString() => Label; // To wyświetli ComboBox
}


[ObservableProperty]
    private ObservableCollection<RefreshRateItem> _refreshRates = new()
    {
        new RefreshRateItem { Label = "0.1 s", Seconds = 0.1 },
        new RefreshRateItem { Label = "0.2 s", Seconds = 0.2 },
        new RefreshRateItem { Label = "0.5 s", Seconds = 0.5 },
        new RefreshRateItem { Label = "1.0 s", Seconds = 1.0 },
        new RefreshRateItem { Label = "2.0 s", Seconds = 2.0 },
        new RefreshRateItem { Label = "5.0 s", Seconds = 5.0 },
        new RefreshRateItem { Label = "10.0 s", Seconds = 10.0 },
    };

    [ObservableProperty]
    private RefreshRateItem _selectedRefreshRate;

    // Metoda wywoływana automatycznie przy zmianie wyboru w ComboBox
    partial void OnSelectedRefreshRateChanged(RefreshRateItem value)
    {
        if (_sensorTimer != null && value != null)
        {
            // Zmieniamy interwał "w locie"
            _sensorTimer.Interval = TimeSpan.FromSeconds(value.Seconds);
        }
    }


    [ObservableProperty] private int _selectedTabIndex;

    private int _advancedRowCounter = 0;

    [ObservableProperty] private ObservableCollection<SensorItemViewModel> _sensors;


    [ObservableProperty] 
    private ObservableCollection<string> _advancedCategories = new()
    {
        "General",
        "Vulkan",
        "OpenCL",
        "Multimedia (VA-API)",
        "Power & Limits",
        "PCIe Resizable BAR"
    };

    [ObservableProperty] 
    private string _selectedAdvancedCategory = "General";

    [ObservableProperty] 
    private ObservableCollection<AdvancedItemViewModel> _advancedItems = new();

    // Metoda wywoływana automatycznie przy zmianie wyboru w ComboBoxie
    partial void OnSelectedAdvancedCategoryChanged(string value)
    {
        LoadAdvancedData(value);
    }

    // Wywołaj to też w OnSelectedTabIndexChanged, żeby odświeżyć dane przy wejściu w zakładkę
    // Dodaj to do istniejącej metody OnSelectedTabIndexChanged w bloku 'else':
    /*
       if (value == 2) // Zakładka Advanced
       {
           LoadAdvancedData(SelectedAdvancedCategory);
       }
    */




    // Helper: VAProfileH264ConstrainedBaseline -> H264 Constrained Baseline
    private string CleanVaProfile(string profile)
    {
        string p = profile.Replace("VAProfile", "").Trim();
        
        // Wstawiamy spacje przed wielkimi literami (CamelCase -> Spaced), ale inteligentnie
        // np. H264Main -> H264 Main
        
        // Prosta podmiana znanych kodeków dla czytelności
        p = p.Replace("MPEG2", "MPEG-2 ");
        p = p.Replace("MPEG4", "MPEG-4 ");
        p = p.Replace("H264", "H.264 ");
        p = p.Replace("HEVC", "H.265 (HEVC) ");
        p = p.Replace("VC1", "VC-1 ");
        p = p.Replace("VP8", "VP8 ");
        p = p.Replace("VP9", "VP9 ");
        p = p.Replace("AV1", "AV1 ");
        p = p.Replace("JPEGBaseline", "JPEG Baseline");
        p = p.Replace("None", "None");

        return p.Trim();
    }

    // Helper: VAEntrypointVLD -> Decode
    private string CleanVaEntrypoint(string entrypoint)
    {
        if (entrypoint.Contains("VLD")) return "Decode";
        if (entrypoint.Contains("EncSlice")) return "Encode";
        if (entrypoint.Contains("EncPicture")) return "Encode (Picture)";
        if (entrypoint.Contains("VideoProc")) return "Video Processing";
        
        return entrypoint.Replace("VAEntrypoint", "");
    }



    private void PopulateMultimediaAdvanced(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            // 1. Znajdź dostępne render nodes (np. /dev/dri/renderD128, renderD129)
            var renderNodes = Directory.GetFiles("/dev/dri", "renderD*");
            if (renderNodes.Length == 0)
            {
                AddAdvancedRow(list, "Error", "No /dev/dri/renderD* devices found.");
                return;
            }

            // Przygotuj słowa kluczowe do szukania (np. "Navi31", "Radeon", "7900")
            // Używamy tej samej logiki co przy OpenCL
            var gpuNameParts = _selectedGpu?.DisplayName?
                .Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 2 && !p.Equals("AMD", StringComparison.OrdinalIgnoreCase) && !p.Equals("Radeon", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<string>();
            
            // Dodajemy fallbacki techniczne
            //gpuNameParts.Add("Navi"); 
            //gpuNameParts.Add("Radeon");

            bool foundDevice = false;

            // 2. Iterujemy po render nodes, aż trafimy na właściwy
            foreach (var node in renderNodes)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "vainfo",
                    // Ważne: Wymuszamy tryb DRM i konkretne urządzenie
                    Arguments = $"--display drm --device {node}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                // vainfo często zwraca błąd 1 lub pisze na stderr jeśli driver nie pasuje,
                // więc musimy to obsłużyć miękko.
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd(); 
                process.WaitForExit();

                // Połącz stdout i stderr, bo driver info czasem leci na stderr
                string fullOutput = output + "\n" + error;

                // 3. Sprawdzamy czy to nasza karta (szukamy Driver version i nazwy)
                // Przykład linii: "vainfo: Driver version: Mesa Gallium driver ... for AMD Radeon RX 7900 XTX (navi31...)"
                bool isMatch = false;
                
                // Szukamy linii z wersją sterownika
                string driverLine = "";
                using (var reader = new StringReader(fullOutput))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("Driver version:"))
                        {
                            driverLine = line;
                            break;
                        }
                    }
                }

                // Weryfikacja
                if (!string.IsNullOrEmpty(driverLine))
                {
                    foreach (var part in gpuNameParts)
                    {
                        if (driverLine.Contains(part, StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    foundDevice = true;
                    
                    // --- NAGŁÓWEK ---
                    AddAdvancedRow(list, "General", "", true);
                    AddAdvancedRow(list, "Device Node", node);
                    
                    // Wyciągamy czystą wersję sterownika z długiej linii
                    // np. "Mesa Gallium driver 23.1.0 for AMD Radeon..."
                    var driverInfo = driverLine.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "Unknown";
                    AddAdvancedRow(list, "Driver Info", driverInfo);

                    AddAdvancedRow(list, "Supported Codecs", "", true);

                    // --- PARSOWANIE PROFILI ---
                    using (var reader = new StringReader(fullOutput))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            // Szukamy linii: VAProfileH264Main : VAEntrypointVLD
                            if (line.StartsWith("VAProfile") && line.Contains(":"))
                            {
                                var parts = line.Split(':');
                                if (parts.Length == 2)
                                {
                                    string profileRaw = parts[0].Trim();
                                    string entryRaw = parts[1].Trim();

                                    string profileNice = CleanVaProfile(profileRaw);
                                    string entryNice = CleanVaEntrypoint(entryRaw);

                                    // Filtrujemy "None" (czasem się zdarza)
                                    if (profileNice != "None")
                                    {
                                        AddAdvancedRow(list, profileNice, entryNice);
                                    }
                                }
                            }
                        }
                    }
                    
                    // Znaleźliśmy i wypisaliśmy - kończymy pętlę (nie szukamy dalej)
                    break;
                }
            }

            if (!foundDevice)
            {
                AddAdvancedRow(list, "Info", "No matching VA-API device found.");
                AddAdvancedRow(list, "Note", "Ensure 'vainfo' is installed (package: libva-utils).");
            }

        }
        catch (Exception ex)
        {
            AddAdvancedRow(list, "Error", $"VA-API check failed: {ex.Message}");
        }
    }





private void PopulatePowerLimitsAdvanced(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            string cardPath = $"/sys/class/drm/{_selectedGpu?.Id ?? "card0"}/device";
            string hwmonPath = "";

            // 1. Znajdź właściwy folder hwmon
            try 
            {
                var hwmonDirs = Directory.GetDirectories($"{cardPath}/hwmon");
                foreach (var dir in hwmonDirs)
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath) && File.ReadAllText(namePath).Trim() == "amdgpu")
                    {
                        hwmonPath = dir;
                        break;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(hwmonPath))
            {
                AddAdvancedRow(list, "Error", "Could not find AMDGPU hwmon directory");
                return;
            }

            // --- POWER LIMITS (Tylko konfiguracja, bez bieżącego zużycia) ---
            AddAdvancedRow(list, "Power Configuration", "", true);
            
            string powerCap = ReadSysFs(Path.Combine(hwmonPath, "power1_cap"));
            string powerCapDefault = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_default"));
            string powerCapMin = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_min"));
            string powerCapMax = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_max"));

            if (double.TryParse(powerCap, out double pCap))
                AddAdvancedRow(list, "Current Limit (TDP)", $"{pCap / 1000000.0:0.0} W");
            
            if (double.TryParse(powerCapDefault, out double pDef))
                AddAdvancedRow(list, "Default Limit", $"{pDef / 1000000.0:0.0} W");

            if (double.TryParse(powerCapMin, out double pMin) && double.TryParse(powerCapMax, out double pMax))
                AddAdvancedRow(list, "Allowed Range", $"{pMin / 1000000.0:0.0} W - {pMax / 1000000.0:0.0} W");

            // --- FANS CONFIG (Tylko sterowanie, bez bieżących RPM) ---
            AddAdvancedRow(list, "Fan Control", "", true);
            string fanMode = ReadSysFs(Path.Combine(hwmonPath, "pwm1_enable"));
            // 1 = Manual, 2 = Auto
            AddAdvancedRow(list, "Control Mode", fanMode == "1" ? "Manual" : (fanMode == "2" ? "Auto" : "Unknown"));

            string pwm = ReadSysFs(Path.Combine(hwmonPath, "pwm1"));
            string pwmMax = ReadSysFs(Path.Combine(hwmonPath, "pwm1_max"));
            
            if (double.TryParse(pwm, out double pwmVal) && double.TryParse(pwmMax, out double pwmMaxVal))
            {
                double percent = (pwmVal / pwmMaxVal) * 100.0;
                AddAdvancedRow(list, "Current Signal (PWM)", $"{percent:0}% ({pwmVal}/{pwmMaxVal})");
            }
            
            string fanTarget = ReadSysFs(Path.Combine(hwmonPath, "fan1_target"));
            if (!string.IsNullOrEmpty(fanTarget))
            {
                AddAdvancedRow(list, "Target RPM", $"{fanTarget} RPM");
            }

            // --- POWER PROFILES ---
            AddAdvancedRow(list, "Power Profile", "", true);
            string profilePath = Path.Combine(cardPath, "pp_power_profile_mode");
            if (File.Exists(profilePath))
            {
                try
                {
                    var lines = File.ReadAllLines(profilePath);
                    foreach (var line in lines)
                    {
                        if (line.Contains("*"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string activeProfile = parts[1].Replace("*", "").Replace(":", "");
                                AddAdvancedRow(list, "Active Profile", activeProfile);
                            }
                        }
                    }
                }
                catch { AddAdvancedRow(list, "Active Profile", "Error reading profiles"); }
            }
            else
            {
                AddAdvancedRow(list, "Active Profile", "Not supported / file missing");
            }

            // --- OVERDRIVE ---
            string odPath = Path.Combine(cardPath, "pp_od_clk_voltage");
            if (File.Exists(odPath))
            {
                AddAdvancedRow(list, "Overdrive Limits", "", true);
                try 
                {
                    var odLines = File.ReadAllLines(odPath);
                    foreach (var l in odLines) AddAdvancedRow(list, "OD Info", l);
                }
                catch {}
            }
            // Jeśli pliku nie ma, po prostu pomijamy sekcję Overdrive (zamiast pisać błąd), 
            // bo to standardowe zachowanie na nowych kernelach.

            // --- DRIVER FEATURES (PEŁNA LISTA) ---
            AddAdvancedRow(list, "Driver Features (pp_features)", "", true);
            string featPath = Path.Combine(cardPath, "pp_features");
            if (File.Exists(featPath))
            {
                 try
                 {
                     var lines = File.ReadAllLines(featPath);
                     // Format linii: "00. FW_DATA_READ         ( 0) : enabled"
                     
                     foreach (var line in lines)
                     {
                         string l = line.Trim();
                         if (string.IsNullOrWhiteSpace(l)) continue;
                         // Pomijamy nagłówki
                         if (l.StartsWith("features high") || l.StartsWith("No. Feature")) continue;

                         var parts = l.Split(':');
                         if (parts.Length == 2)
                         {
                             string state = parts[1].Trim(); // "enabled"
                             
                             // Parsowanie nazwy z lewej strony: "00. FW_DATA_READ         ( 0) "
                             string leftSide = parts[0];
                             int dotIndex = leftSide.IndexOf('.');
                             int parenIndex = leftSide.IndexOf('(');
                             
                             if (dotIndex != -1 && parenIndex > dotIndex)
                             {
                                 string featureName = leftSide.Substring(dotIndex + 1, parenIndex - dotIndex - 1).Trim();
                                 AddAdvancedRow(list, featureName, state);
                             }
                             else
                             {
                                 // Fallback gdyby format był inny
                                 AddAdvancedRow(list, leftSide.Trim(), state);
                             }
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     AddAdvancedRow(list, "Error", $"Features parsing error: {ex.Message}");
                 }
            }
            else
            {
                AddAdvancedRow(list, "Features", "Not accessible (pp_features missing)");
            }

        }
        catch (Exception ex)
        {
            AddAdvancedRow(list, "Error", $"Power/Limits check failed: {ex.Message}");
        }
    }

    // Helper do bezpiecznego czytania jednej linii z pliku sysfs
    private string ReadSysFs(string path)
    {
        try
        {
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
        }
        catch {}
        return "";
    }






private void PopulateResizableBarAdvanced(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            string targetBusId = BusId; 
            
            if (string.IsNullOrEmpty(targetBusId))
            {
                AddAdvancedRow(list, "Error", "Could not determine PCI Bus ID");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "lspci",
                Arguments = $"-vv -s {targetBusId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                AddAdvancedRow(list, "Error", "Could not start lspci (pciutils required)");
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // --- ANALIZA DANYCH ---

            var barSizes = new List<(string Name, string SizeText, long SizeBytes)>();
            bool has64BitBar = false;
            long maxBarSize = 0;
            int barCounter = 0; 

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                string t = line.Trim();
                
                // Szukamy linii definiujących pamięć
                if (t.Contains("Memory at") && t.Contains("[size="))
                {
                    int sizeStart = t.LastIndexOf("[size=");
                    if (sizeStart != -1)
                    {
                        int sizeEnd = t.IndexOf(']', sizeStart);
                        if (sizeEnd != -1)
                        {
                            // Wyciągamy SAM rozmiar, np. "32G"
                            string sizeStr = t.Substring(sizeStart + 6, sizeEnd - sizeStart - 6); 
                            long bytes = ParseLspciSize(sizeStr);
                            
                            if (bytes > maxBarSize) maxBarSize = bytes;
                            if (t.Contains("64-bit")) has64BitBar = true;

                            // Nazwa BAR
                            string barName;
                            if (t.StartsWith("Region"))
                            {
                                var parts = t.Split(':');
                                barName = parts[0].Replace("Region", "BAR");
                            }
                            else
                            {
                                barName = $"BAR {barCounter}";
                                barCounter++;
                            }

                            // ZMIANA: Zapisujemy tylko sizeStr zamiast całego wiersza details
                            barSizes.Add((barName, sizeStr, bytes));
                        }
                    }
                }
            }

            // --- STATUS ---
            // ReBAR > 256MB
            bool isReBarEnabled = maxBarSize > 268435456; 

            // --- SEKCJA 1: STATUS GŁÓWNY ---
            AddAdvancedRow(list, "PCI-Express Resizable BAR", "", true);
            AddAdvancedRow(list, "Resizable BAR", isReBarEnabled ? "Enabled" : "Disabled");

            // --- SEKCJA 2: WYMAGANIA ---
            AddAdvancedRow(list, "Resizable BAR Requirements", "", true);
            
            bool isRdna = DeviceName.Contains("7900") || DeviceName.Contains("Navi") || DeviceName.Contains("RX 6") || DeviceName.Contains("RX 7");
            AddAdvancedRow(list, "GPU Hardware Support", isRdna ? "Yes" : "Unknown");

            AddAdvancedRow(list, "Above 4G Decode enabled", has64BitBar ? "Yes" : "No/Unknown");
            AddAdvancedRow(list, "Resizable BAR enabled in BIOS", isReBarEnabled ? "Yes" : "Disabled or Unsupported");

            bool isUefi = Directory.Exists("/sys/firmware/efi");
            AddAdvancedRow(list, "CSM disabled", isUefi ? "Yes" : "No (Legacy Mode)");
            
            // ZMIANA: Poprawna nazwa dla Linuxa :)
            AddAdvancedRow(list, "Linux running in UEFI Mode", isUefi ? "Yes" : "No"); 

            AddAdvancedRow(list, "64-Bit Operating System", Environment.Is64BitOperatingSystem ? "Yes" : "No");

            string kernelDriver = "";
            foreach (var l in lines) 
            {
                string trimL = l.Trim();
                if (trimL.StartsWith("Kernel driver in use:")) 
                    kernelDriver = trimL.Split(':')[1].Trim();
            }
            bool driverOk = kernelDriver.Contains("amdgpu");
            AddAdvancedRow(list, "Graphics Driver Support", driverOk ? "Yes" : $"Unknown ({kernelDriver})");


            // --- SEKCJA 3: BAR SIZES ---
            AddAdvancedRow(list, "PCI-Express BAR Sizes", "", true);
            if (barSizes.Count > 0)
            {
                foreach (var bar in barSizes.OrderBy(b => b.Name))
                {
                    // Tutaj teraz trafia np. "32G" zamiast długiego stringa
                    AddAdvancedRow(list, bar.Name, bar.SizeText); 
                }
            }
            else
            {
                AddAdvancedRow(list, "Info", "No memory regions found");
            }

        }
        catch (Exception ex)
        {
            AddAdvancedRow(list, "Error", $"ReBAR check failed: {ex.Message}");
        }
    }

    // Helper do parsowania rozmiarów z lspci (np. "24G", "2M", "16K")
    private long ParseLspciSize(string sizeStr)
    {
        if (string.IsNullOrEmpty(sizeStr)) return 0;
        
        char suffix = sizeStr.Last();
        if (char.IsDigit(suffix)) return long.Parse(sizeStr);

        string numberPart = sizeStr.Substring(0, sizeStr.Length - 1);
        if (!double.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num))
            return 0;

        return suffix switch
        {
            'K' => (long)(num * 1024),
            'M' => (long)(num * 1024 * 1024),
            'G' => (long)(num * 1024 * 1024 * 1024),
            'T' => (long)(num * 1024 * 1024 * 1024 * 1024),
            _ => (long)num
        };
    }


    private void LoadAdvancedData(string category)
    {
        var list = new ObservableCollection<AdvancedItemViewModel>();
        _advancedRowCounter = 0; // Reset licznika kolorów na start

        switch (category)
        {
            case "General":
                PopulateGeneralAdvanced(list);
                break;
            case "Vulkan":
                PopulateVulkanAdvanced(list);
                break;
            case "OpenCL":
                PopulateOpenClAdvanced(list);
                break;
            case "Multimedia (VA-API)":
                PopulateMultimediaAdvanced(list);
                break;
            case "Power & Limits":
                PopulatePowerLimitsAdvanced(list);
                break;
            case "PCIe Resizable BAR":
                PopulateResizableBarAdvanced(list);
                break;
            default:
                AddAdvancedRow(list, "Info", "", true);
                AddAdvancedRow(list, "Status", "Not implemented");
                break;
        }

        AdvancedItems = list;
    }

    private void PopulateGeneralAdvanced(ObservableCollection<AdvancedItemViewModel> list)
    {
        // 1. Sekcja Systemowa (Nagłówek)
        AddAdvancedRow(list, "System", "", true);
        
        // Kernel
        string kernel = "Unknown";
        try { kernel = File.ReadAllText("/proc/sys/kernel/osrelease").Trim(); } catch {}
        AddAdvancedRow(list, "Kernel Version", kernel);

        // Display Server
        string session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "Unknown";
        AddAdvancedRow(list, "Display Server", session.ToUpper());

        // Desktop
        string desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "Unknown";
        AddAdvancedRow(list, "Desktop Environment", desktop);

        // 2. Sekcja Sterowników (Nagłówek)
        AddAdvancedRow(list, "Graphics Drivers", "", true);
        
        // Kernel Driver
        string driverModule = "Unknown";
        try 
        {
            var driverPath = $"/sys/class/drm/{_selectedGpu?.Id ?? "card0"}/device/driver";
            var dirInfo = new DirectoryInfo(driverPath);
            if (dirInfo.Exists)
            {
                var target = dirInfo.ResolveLinkTarget(true); 
                driverModule = target != null ? target.Name : dirInfo.Name;
            }
        } 
        catch {}
        AddAdvancedRow(list, "Kernel Driver", driverModule);

        // --- OPENGL / MESA (IMPLEMENTACJA) ---
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "glxinfo",
                Arguments = "-B", // -B (Brief) daje mniej śmieci, ale zawiera wszystko co ważne
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string glVersion = "";
                string mesaVersion = "";
                string renderer = "";
                string directRendering = "No";

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    string t = line.Trim();
                    
                    if (t.StartsWith("direct rendering:"))
                    {
                        directRendering = t.Contains("Yes") ? "Yes" : "No";
                    }
                    else if (t.StartsWith("OpenGL core profile version string:"))
                    {
                        // Format: "4.6 (Core Profile) Mesa 23.2.1-1ubuntu3.1"
                        // Wyciągamy "4.6" oraz "23.2.1..."
                        glVersion = t.Replace("OpenGL core profile version string:", "").Trim().Split(' ')[0];
                        
                        // Szukamy słowa Mesa
                        int mesaIndex = t.IndexOf("Mesa");
                        if (mesaIndex != -1)
                        {
                            mesaVersion = t.Substring(mesaIndex).Replace("Mesa", "").Trim();
                        }
                    }
                    else if (t.StartsWith("OpenGL renderer string:"))
                    {
                        // Format: "AMD Radeon RX 7900 XTX (radeonsi, navi31, LLVM 15.0.7, DRM 3.54, ...)"
                        renderer = t.Replace("OpenGL renderer string:", "").Trim();
                    }
                }

                // Wyświetlanie wyników
                AddAdvancedRow(list, "OpenGL Version", glVersion);
                AddAdvancedRow(list, "Mesa Version", mesaVersion);
                
                // Direct Rendering to ważny sanity check na Linuxie
                AddAdvancedRow(list, "Direct Rendering", directRendering);

                // Z Renderera często da się wyciągnąć LLVM (ważne dla AMD)
                if (renderer.Contains("LLVM"))
                {
                    // Próba wycięcia samej wersji LLVM
                    var match = System.Text.RegularExpressions.Regex.Match(renderer, @"LLVM\s+([\d\.]+)");
                    if (match.Success)
                    {
                        AddAdvancedRow(list, "LLVM Version", match.Groups[1].Value);
                    }
                }
                
                // Jeśli renderer zawiera "llvmpipe", to znaczy że akceleracja nie działa!
                if (renderer.Contains("llvmpipe"))
                {
                    AddAdvancedRow(list, "Warning", "Software Rendering (llvmpipe) detected!");
                }
            }
            else
            {
                AddAdvancedRow(list, "OpenGL Info", "Failed to start glxinfo");
            }
        }
        catch
        {
            AddAdvancedRow(list, "OpenGL Info", "Not available ('glxinfo' missing?)");
        }

        // 3. Firmware
        AddAdvancedRow(list, "Firmware", "", true);
        
        string fwDirPath = $"/sys/class/drm/{_selectedGpu?.Id ?? "card0"}/device/fw_version";
        
        if (Directory.Exists(fwDirPath))
        {
            try
            {
                var files = Directory.GetFiles(fwDirPath, "*_fw_version");
                Array.Sort(files);

                foreach (var filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string shortName = fileName.Replace("_fw_version", "").ToUpper();
                    string version = File.ReadAllText(filePath).Trim();

                    AddAdvancedRow(list, shortName, version);
                }

                if (files.Length == 0)
                    AddAdvancedRow(list, "Info", "No firmware files found");
            }
            catch (Exception ex)
            {
                AddAdvancedRow(list, "Error", ex.Message);
            }
        }
        else
        {
             if (File.Exists(fwDirPath))
                 AddAdvancedRow(list, "Legacy Info", "Old kernel format detected");
             else
                 AddAdvancedRow(list, "Firmware Info", "Not available");
        }
    }


// Klasa pomocnicza do zbierania danych o GPU z rozsypanych sekcji
    private class TempGpuData
    {
        public string IdHex { get; set; } = "";
        public List<AdvancedItemViewModel> GeneralRows { get; } = new();
        public List<AdvancedItemViewModel> MemoryRows { get; } = new();
        public List<AdvancedItemViewModel> ExtensionRows { get; } = new();
        public List<AdvancedItemViewModel> FeatureRows { get; } = new();
    }



    private double _lastUserHeight = 525-1;

    [ObservableProperty] 
    private double _windowHeight = 525-1;
    
    private DispatcherTimer _sensorTimer;


  // Właściwości sterujące oknem (read-only, zależą od SelectedTabIndex)
    public bool ShowResizeGrip => (SelectedTabIndex == 1 || SelectedTabIndex == 2); // 1 to Sensors, 2 to Settings
    public SizeToContent WindowSizeMode => SelectedTabIndex == 0 ? SizeToContent.Height : SizeToContent.Manual;



    [ObservableProperty] private bool _isLogEnabled; // Spięte z Checkboxem
    private string _logFilePath = "";
    
    // Metoda wywoływana z Code-behind po wybraniu pliku
    public void StartLogging(string filePath)
    {
        _logFilePath = filePath;
        IsLogEnabled = true;
        
        // Zapisz nagłówek na start
        WriteLogHeader();
    }

    partial void OnWindowHeightChanged(double value)
    {
        // Jeśli jesteśmy na zakładce Sensors (1) lub Advanced (2), 
        // to zapamiętujemy każdą zmianę wysokości jako "preferencję użytkownika".
        // Ignorujemy zmiany na zakładce 0, bo tam wysokość wymusza "SizeToContent".
        if (_selectedTabIndex != 0 && value > 100)
        {
            _lastUserHeight = value;
        }
    }

    public void StopLogging()
    {
        IsLogEnabled = false;
        _logFilePath = "";
    }

    private void WriteLogHeader()
    {
        if (!IsLogEnabled || string.IsNullOrEmpty(_logFilePath) || Sensors == null) return;

        try
        {
            string header = SensorLogService.BuildHeader(Sensors);
            File.AppendAllText(_logFilePath, header + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Opcjonalnie: obsługa błędu zapisu (np. brak uprawnień)
            Console.WriteLine($"Log write error: {ex.Message}");
            StopLogging(); // Bezpiecznik
        }
    }




    // Metoda wywoływana automatycznie przy zmianie tabu (dzięki CommunityToolkit)
    partial void OnSelectedTabIndexChanged(int value)
    {
        // 1. Najpierw powiadamiamy widok, że tryb rozmiaru się zmieni
        // (Ważne, żeby Avalonia wiedziała, czy ma blokować okno czy nie)
        OnPropertyChanged(nameof(ShowResizeGrip));
        OnPropertyChanged(nameof(WindowSizeMode));

        // 2. Logika przywracania wysokości
        if (value == 0)
        {
            // IDZIEMY NA GŁÓWNĄ (Graphics Card):
            // Tutaj nic nie musimy robić z Height, bo WindowSizeMode = SizeToContent.Height
            // automatycznie "zgniecie" okno do potrzebnej wielkości.

            WindowHeight = double.NaN;
        }
        else
        {
            // IDZIEMY NA SENSORS lub ADVANCED:
            // WindowSizeMode zmienia się na Manual (dzięki OnPropertyChanged wyżej).
            // Teraz musimy ręcznie przywrócić ostatnią zapamiętaną wysokość.
            
            // Używamy Dispatchera, aby upewnić się, że zmiana trybu na Manual 
            // przetworzyła się przed ustawieniem wysokości (dla bezpieczeństwa UI).
            Dispatcher.UIThread.Post(() =>
            {
                WindowHeight = _lastUserHeight;
            });
        }

        if(value == 2)
        {
            LoadAdvancedData(SelectedAdvancedCategory);
        }


    }



    [RelayCommand]
    private void ResetSensors()
    {
        if (Sensors != null)
        {
            foreach (var sensor in Sensors)
            {
                sensor.Reset();
            }
        }
    }



    private void SensorTimer_Tick(object? sender, EventArgs e)
    {
        if (_selectedGpu == null) return;
        
        var probe = new LinuxAmdGpuProbe(_selectedGpu.Id, MemoryType); 
        var data = probe.LoadSensorData();

        // 1. Aktualizacja Głównej Zakładki
        //GpuClock = $"{data.GpuClock.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} MHz";
        //MemoryClock = $"{data.MemoryClock.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} MHz";

        // 2. Aktualizacja Sensorów
        UpdateSensor("GPU Clock", data.GpuClock);
        UpdateSensor("Memory Clock", data.MemoryClock);
        UpdateSensor("GPU Temperature", data.GpuTemp);
        UpdateSensor("GPU Temperature (Hot Spot)", data.GpuHotSpot);
        UpdateSensor("Memory Temperature", data.MemoryTemp); // <--- ODŚWIEŻANIE
        
        UpdateSensor("Fan Speed (%)", (double)data.FanPercent);
        UpdateSensor("Fan Speed (RPM)", (double)data.FanRpm);
        
        UpdateSensor("GPU Load", (double)data.GpuLoad);
        UpdateSensor("Memory Controller Load", (double)data.MemControllerLoad);
        
        UpdateSensor("Memory Used (Dedicated)", data.MemoryUsed);
        UpdateSensor("Memory Used (Dynamic)", data.MemoryUsedDynamic);
        
        UpdateSensor("Board Power Draw", data.BoardPower);
        UpdateSensor("GPU Voltage", data.GpuVoltage);
        
        UpdateSensor("CPU Temperature", data.CpuTemperature);
        UpdateSensor("System Memory Used", data.SystemRamUsed);


        if (IsLogEnabled && !string.IsNullOrEmpty(_logFilePath) && Sensors != null)
        {
            try
            {
                string row = SensorLogService.BuildDataRow(Sensors);
                File.AppendAllText(_logFilePath, row + Environment.NewLine);
            }
            catch
            {
                // Ignorujemy pojedyncze błędy zapisu (np. plik zablokowany przez inny proces)
            }
        }


    }



    // Metoda Inicjalizująca Sensory (Wywołaj ją w konstruktorze lub LoadGpuData)
    private void InitSensors()
    {
        // 1. Tworzymy tymczasową sondę, żeby sprawdzić co sprzęt potrafi
        // (Używamy ID wybranej karty, lub domyślnej jeśli null)
        string gpuId = _selectedGpu?.Id ?? "card0";
        var probe = new LinuxAmdGpuProbe(gpuId); 
        var support = probe.GetSensorAvailability();

        // 2. Budujemy listę warunkowo
        var list = new ObservableCollection<SensorItemViewModel>();

        // Zawsze dodajemy podstawowe zegary (system zawsze raportuje sclk/mclk w jakiejś formie)
        list.Add(new SensorItemViewModel("GPU Clock", "MHz", 0, 100, false));
        list.Add(new SensorItemViewModel("Memory Clock", "MHz", 0, 1000, false));
        
        // Zawsze dodajemy GPU Temperature (Edge) - to standard absolutny
        list.Add(new SensorItemViewModel("GPU Temperature", "°C", 20, 60, false));

        // Warunkowe Hot Spot
        if (support.HasHotSpot)
            list.Add(new SensorItemViewModel("GPU Temperature (Hot Spot)", "°C", 20, 80, false));

        // Warunkowe Memory Temp
        if (support.HasMemTemp)
            list.Add(new SensorItemViewModel("Memory Temperature", "°C", 20, 60, false));

        // Warunkowe Wentylatory
        if (support.HasFan)
        {
            list.Add(new SensorItemViewModel("Fan Speed (%)", "%", 0, 100, true));
            list.Add(new SensorItemViewModel("Fan Speed (RPM)", "RPM", 0, 1000, false));
        }

        // Warunkowe Load
        if (support.HasGpuLoad)
            list.Add(new SensorItemViewModel("GPU Load", "%", 0, 100, true));

        if (support.HasMemControllerLoad)
            list.Add(new SensorItemViewModel("Memory Controller Load", "%", 0, 100, true));

        // Warunkowe Memory Usage (Prawie zawsze true na AMDGPU)
        if (support.HasMemUsed)
        {
            list.Add(new SensorItemViewModel("Memory Used (Dedicated)", "MB", 0, 512, false));
            list.Add(new SensorItemViewModel("Memory Used (Dynamic)", "MB", 0, 128, false));
        }

        // Warunkowe Power
        if (support.HasPower)
            list.Add(new SensorItemViewModel("Board Power Draw", "W", 0, 100, false));

        // Warunkowe Voltage
        if (support.HasVoltage)
            list.Add(new SensorItemViewModel("GPU Voltage", "V", 0, 1.0, false));

        // Dane systemowe (CPU/RAM) - dodajemy zawsze, bo to nie zależy od GPU
        list.Add(new SensorItemViewModel("CPU Temperature", "°C", 20, 70, false));
        list.Add(new SensorItemViewModel("System Memory Used", "MB", 0, 4096, false));

        // Przypisanie do głównej kolekcji
        Sensors = list;

        // Konfiguracja Timera
        _sensorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0)
        };
        _sensorTimer.Tick += SensorTimer_Tick;
        _sensorTimer.Start();
    }

    private void UpdateSensor(string name, double value)
    {
        var sensor = Sensors.FirstOrDefault(s => s.Name == name);
        if (sensor != null)
        {
            sensor.UpdateValue(value);
        }
    }


// Konwersja surowej wersji Vulkan (uint32) na format 1.2.3
    

// Helper do wyciągania wartości po znaku "="
private string GetVulkanValue(string line)
{
    var parts = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length > 1)
    {
        return parts[1].Trim();
    }
    return "";
}

private void PopulateVulkanAdvanced(ObservableCollection<AdvancedItemViewModel> list)
{
    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "vulkaninfo",
            Arguments = "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            AddAdvancedRow(list, "Error", "Could not start vulkaninfo");
            return;
        }

        using StreamReader reader = process.StandardOutput;
        string? line;

        // --- ZMIENNE STANU ---
        bool isTargetGpu = false;
        bool foundAnyMatch = false; 
        string currentSection = "";   
        
        var propsBuffer = new List<(string Key, string Value)>();

        // Zmienne dla Memory HEAPS
        string pendingHeapName = "";
        string pendingHeapSize = "";
        string pendingHeapBudget = "";
        string pendingHeapUsage = "";
        List<string> pendingHeapFlags = new List<string>();
        bool parsingHeapFlags = false;

        // Zmienne dla Memory TYPES
        string pendingTypeName = ""; 
        string pendingTypeHeapIndex = ""; 
        List<string> pendingTypeFlags = new List<string>();
        bool parsingTypeFlags = false;

        string targetIdHex = "";
        if (!string.IsNullOrEmpty(DeviceId))
        {
            var parts = DeviceId.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) targetIdHex = parts[1].Trim().ToUpper();
        }

        string ExtractValueInParenthesis(string rawLine)
        {
            int lastOpen = rawLine.LastIndexOf('(');
            int lastClose = rawLine.LastIndexOf(')');
            if (lastOpen != -1 && lastClose > lastOpen)
                return rawLine.Substring(lastOpen + 1, lastClose - lastOpen - 1);
            var parts = rawLine.Split('=');
            if (parts.Length > 1) return parts[1].Trim().Split(' ')[0];
            return "";
        }

        // --- FUNKCJE LOKALNE (Zdefiniowane PRZED pętlą) ---

        void CommitHeap()
        {
            if (string.IsNullOrEmpty(pendingHeapName)) return;

            string flagsStr = "None";
            if (pendingHeapFlags.Count > 0)
            {
                var cleanFlags = pendingHeapFlags
                    .Select(f => f.Replace("MEMORY_HEAP_", "").Replace("_BIT", "").Trim())
                    .Where(f => f != "None");
                if (cleanFlags.Any()) flagsStr = string.Join(", ", cleanFlags);
            }

            string details = "";
            var detailsParts = new List<string>();
            if (!string.IsNullOrEmpty(pendingHeapBudget)) detailsParts.Add($"Budget: {pendingHeapBudget}");
            if (!string.IsNullOrEmpty(pendingHeapUsage))  detailsParts.Add($"Usage: {pendingHeapUsage}");
            
            if (detailsParts.Count > 0) details = $"({string.Join(", ", detailsParts)})";

            string val = $"{pendingHeapSize} {details} ({flagsStr})".Trim();
            val = System.Text.RegularExpressions.Regex.Replace(val, @"\s+", " ");

            AddAdvancedRow(list, pendingHeapName, val);

            pendingHeapName = "";
            pendingHeapSize = "";
            pendingHeapBudget = "";
            pendingHeapUsage = "";
            pendingHeapFlags.Clear();
            parsingHeapFlags = false;
        }

        void CommitType()
        {
            if (string.IsNullOrEmpty(pendingTypeName)) return;

            // Uładniamy nazwę: "memoryTypes[0]" -> "Type 0"
            string displayName = pendingTypeName
                .Replace("memoryTypes", "Type ")
                .Replace("[", "")
                .Replace("]", "")
                .Trim();

            // 1. Najpierw zawsze wiersz z indeksem sterty
            AddAdvancedRow(list, displayName, $"Heap Index {pendingTypeHeapIndex}");

            // 2. Potem każda flaga w nowym wierszu
            if (pendingTypeFlags.Count > 0)
            {
                var cleanFlags = pendingTypeFlags
                    .Select(f => f.Replace("MEMORY_PROPERTY_", "").Replace("_BIT", "").Trim())
                    .Where(f => f != "None");

                foreach (var flag in cleanFlags)
                {
                    // Tutaj spełniamy prośbę: ten sam string po lewej, flaga po prawej
                    AddAdvancedRow(list, displayName, flag);
                }
            }

            // Reset zmiennych
            pendingTypeName = "";
            pendingTypeHeapIndex = "";
            pendingTypeFlags.Clear();
            parsingTypeFlags = false;
        }

        // Helper do zamykania wszystkiego co otwarte w Memory (teraz widoczny w całym scope)
        void CloseMemoryBlocks() 
        {
            if (currentSection == "MEMORY") 
            {
                CommitHeap();
                CommitType();
            }
        }

        // --- GŁÓWNA PĘTLA ---

        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // 1. ZMIANA KARTY
            if (trimmed.StartsWith("GPU id :") || (trimmed.StartsWith("GPU") && trimmed.EndsWith(":") && !trimmed.Contains("=")))
            {
                if (isTargetGpu) break; 
                
                isTargetGpu = false;
                propsBuffer.Clear();
                currentSection = "";
                continue;
            }

            // 2. ZMIANA SEKCJI
            bool sectionChanged = false;

            if (trimmed.StartsWith("VkPhysicalDeviceMemoryProperties:"))
            {
                CloseMemoryBlocks();
                currentSection = "MEMORY";
                if (isTargetGpu) AddAdvancedRow(list, "Memory Heaps", "", true);
                sectionChanged = true;
            }
            else if (trimmed.StartsWith("VkPhysicalDeviceFeatures:"))
            {
                CloseMemoryBlocks();
                currentSection = "FEATURES";
                if (isTargetGpu) AddAdvancedRow(list, "Device Features", "", true);
                sectionChanged = true;
            }
            else if (trimmed.StartsWith("VkPhysicalDeviceProperties:") || line.Contains("Device Properties and Extensions:"))
            {
                CloseMemoryBlocks();
                currentSection = "PROPERTIES";
                sectionChanged = true;
            }
            else if (trimmed.StartsWith("Device Extensions:"))
            {
                CloseMemoryBlocks();
                currentSection = "EXTENSIONS";
                if (isTargetGpu) AddAdvancedRow(list, "Extensions", "", true);
                sectionChanged = true;
            }
            else if (trimmed.EndsWith("Features:") || trimmed.EndsWith("FeaturesEXT:") || trimmed.EndsWith("FeaturesKHR:"))
            {
                CloseMemoryBlocks();
                currentSection = "FEATURES";
                sectionChanged = true;
            }
            else if (trimmed.EndsWith("Properties:") || trimmed.EndsWith("PropertiesEXT:") || trimmed.EndsWith("PropertiesKHR:"))
            {
                CloseMemoryBlocks();
                currentSection = "OTHER_PROPS"; 
                sectionChanged = true;
            }

            if (sectionChanged) continue;


            // 3. PARSOWANIE

            // --- A. PROPERTIES ---
            if (currentSection == "PROPERTIES")
            {
                if (trimmed.StartsWith("deviceID"))
                {
                    string val = GetVulkanValue(trimmed);
                    string currentIdHex = val.Replace("0x", "").Trim().ToUpper();

                    if (!string.IsNullOrEmpty(targetIdHex) && currentIdHex == targetIdHex)
                    {
                        isTargetGpu = true; 
                        foundAnyMatch = true;

                        AddAdvancedRow(list, "General", "", true);
                        foreach (var prop in propsBuffer) AddAdvancedRow(list, prop.Key, prop.Value);
                        propsBuffer.Clear();

                        AddAdvancedRow(list, "Device ID", $"0x{currentIdHex}");
                    }
                    else
                    {
                        isTargetGpu = false;
                        propsBuffer.Clear();
                    }
                }
                else
                {
                    string k = "", v = "";
                    if (trimmed.StartsWith("deviceName"))      { k = "Device Name"; v = GetVulkanValue(trimmed); }
                    else if (trimmed.StartsWith("apiVersion")) { k = "API Version"; v = GetVulkanValue(trimmed); }
                    else if (trimmed.StartsWith("driverVersion")) { k = "Driver Version"; v = GetVulkanValue(trimmed); }
                    else if (trimmed.StartsWith("deviceType")) { k = "Device Type"; v = GetVulkanValue(trimmed); }
                    else if (trimmed.StartsWith("vendorID"))   { k = "Vendor ID"; v = GetVulkanValue(trimmed); }

                    if (!string.IsNullOrEmpty(k))
                    {
                        if (isTargetGpu) AddAdvancedRow(list, k, v);
                        else propsBuffer.Add((k, v));
                    }
                }
            }

            if (!isTargetGpu) continue; 

            // --- B. MEMORY (HEAPS + TYPES) ---
            if (currentSection == "MEMORY")
            {
                // 1. HEAPS
                if (trimmed.StartsWith("memoryHeaps["))
                {
                    CommitHeap(); 
                    CommitType(); 
                    pendingHeapName = trimmed.Replace(":", ""); 
                }
                else if (trimmed.StartsWith("size") && !string.IsNullOrEmpty(pendingHeapName))
                {
                    pendingHeapSize = ExtractValueInParenthesis(line);
                }
                else if (trimmed.StartsWith("budget") && !string.IsNullOrEmpty(pendingHeapName))
                {
                    pendingHeapBudget = ExtractValueInParenthesis(line);
                }
                else if (trimmed.StartsWith("usage") && !string.IsNullOrEmpty(pendingHeapName))
                {
                    pendingHeapUsage = ExtractValueInParenthesis(line);
                }
                
                // 2. TYPES
                else if (trimmed.StartsWith("memoryTypes"))
                {
                    if (trimmed.Contains("count ="))
                    {
                        CommitHeap(); 
                        AddAdvancedRow(list, "Memory Types", "", true);
                    }
                    else if (trimmed.EndsWith("]:"))
                    {
                        CommitType(); 
                        pendingTypeName = trimmed.Replace(":", ""); 
                        parsingTypeFlags = false;
                    }
                }
                else if (trimmed.StartsWith("heapIndex"))
                {
                    pendingTypeHeapIndex = GetVulkanValue(trimmed);
                }
                else if (trimmed.StartsWith("propertyFlags"))
                {
                    parsingTypeFlags = true;
                }
                else if (trimmed.StartsWith("usable for:"))
                {
                    parsingTypeFlags = false;
                    CommitType(); 
                }
                
                // 3. FLAGI (Wspólne)
                else
                {
                    if (trimmed.StartsWith("flags:") && !string.IsNullOrEmpty(pendingHeapName))
                    {
                        parsingHeapFlags = true;
                    }
                    else if (parsingHeapFlags && !string.IsNullOrEmpty(pendingHeapName))
                    {
                        if (!line.StartsWith("\t") && !line.StartsWith("    ")) parsingHeapFlags = false;
                        else if (!trimmed.Contains("count =")) pendingHeapFlags.Add(trimmed);
                    }
                    else if (parsingTypeFlags && !string.IsNullOrEmpty(pendingTypeName))
                    {
                        if (trimmed.StartsWith("MEMORY_PROPERTY_"))
                        {
                            pendingTypeFlags.Add(trimmed);
                        }
                    }
                }
            }

            // --- C. EXTENSIONS ---
            else if (currentSection == "EXTENSIONS")
            {
                if (trimmed.StartsWith("VK_"))
                {
                    var parts = trimmed.Split(':');
                    AddAdvancedRow(list, parts[0].Trim(), "Supported");
                }
            }

            // --- D. FEATURES ---
            else if (currentSection == "FEATURES")
            {
                if (trimmed.Contains("=") && !trimmed.StartsWith("Userspace"))
                {
                    var kv = trimmed.Split(new[]{'='}, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length == 2)
                    {
                        string k = kv[0].Trim();
                        string val = kv[1].Trim();
                        if (val == "true") val = "Yes";
                        else if (val == "false") val = "No";
                        
                        AddAdvancedRow(list, k, val);
                    }
                }
            }
        }
        
        CloseMemoryBlocks(); // Koniec pliku

        process.WaitForExit();

        if (!foundAnyMatch)
        {
            AddAdvancedRow(list, "Info", "GPU Match Failed");
            AddAdvancedRow(list, "Target ID", targetIdHex);
        }
    }
    catch (Exception ex)
    {
        AddAdvancedRow(list, "Error", $"Parsing failed: {ex.Message}");
    }
}




    private void PopulateOpenClAdvanced(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            // 1. URUCHOMIENIE I ODCZYT (Z SANITYZACJĄ)
            var startInfo = new ProcessStartInfo
            {
                FileName = "clinfo",
                Arguments = "--json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                AddAdvancedRow(list, "Error", "Could not start clinfo");
                return;
            }

            string rawOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                AddAdvancedRow(list, "Error", "clinfo returned empty output");
                return;
            }

            // Naprawa problemu "r..." (śmieci przed JSON)
            int jsonStartIndex = rawOutput.IndexOf('{');
            if (jsonStartIndex == -1)
            {
                AddAdvancedRow(list, "Error", "No valid JSON found");
                return;
            }
            string jsonOutput = rawOutput.Substring(jsonStartIndex);

            var root = JsonNode.Parse(jsonOutput);
            var platforms = root?["platforms"] as JsonArray;
            var devicesGroups = root?["devices"] as JsonArray;

            if (platforms == null || devicesGroups == null)
            {
                AddAdvancedRow(list, "Error", "Invalid clinfo JSON structure");
                return;
            }

            // ---------------------------------------------------------
            // FAZA 1: PRZYGOTOWANIE PLATFORM (SŁOWNIK REFERENCYJNY)
            // ---------------------------------------------------------
            
            JsonNode? refClover = null;
            JsonNode? refRusticl = null;
            JsonNode? refAmdApp = null;

            foreach (var p in platforms)
            {
                string pName = p?["CL_PLATFORM_NAME"]?.ToString() ?? "";
                
                if (pName.Contains("Clover", StringComparison.OrdinalIgnoreCase))
                {
                    refClover = p;
                }
                else if (pName.Contains("rusticl", StringComparison.OrdinalIgnoreCase))
                {
                    refRusticl = p;
                }
                else if (refAmdApp == null && 
                         !pName.Contains("Clover", StringComparison.OrdinalIgnoreCase) && 
                         !pName.Contains("rusticl", StringComparison.OrdinalIgnoreCase))
                {
                    // Pierwsza inna platforma (zakładamy AMD APP)
                    refAmdApp = p;
                }
            }

            // ---------------------------------------------------------
            // FAZA 2: ITERACJA PO URZĄDZENIACH I LINKOWANIE
            // ---------------------------------------------------------

            var candidates = new List<(JsonNode Device, JsonNode? Platform, string DisplayName, string ImplType)>();

            foreach (var group in devicesGroups)
            {
                var onlineDevices = group?["online"] as JsonArray;
                if (onlineDevices == null || onlineDevices.Count == 0) continue;

                foreach (var device in onlineDevices)
                {
                    JsonNode? assignedPlatform = null;
                    string gpuDisplayName = "";
                    string implType = "Unknown";

                    string devName = device["CL_DEVICE_NAME"]?.ToString() ?? "";
                    string versionStr = device["CL_DEVICE_VERSION"]?.ToString() ?? "";
                    float verNum = ParseOpenClVersion(versionStr);

                    // --- HEURYSTYKA (Z POPRAWKAMI) ---

                    // 1. RUSTICL (radeonsi + ver >= 3.0)
                    if (devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase) && verNum >= 3.0f)
                    {
                        implType = "Rusticl";
                        assignedPlatform = refRusticl;
                        gpuDisplayName = devName; 
                    }
                    // 2. CLOVER (radeonsi + ver < 2.0) - USUNIĘTO WARUNEK "Mesa"
                    else if (devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase) && verNum < 2.0f)
                    {
                        implType = "Clover";
                        assignedPlatform = refClover;
                        gpuDisplayName = devName;
                    }
                    // 3. AMD APP (Brak "radeonsi")
                    else if (!devName.Contains("radeonsi", StringComparison.OrdinalIgnoreCase))
                    {
                        implType = "AMD APP";
                        assignedPlatform = refAmdApp;
                        
                        string boardName = device["CL_DEVICE_BOARD_NAME_AMD"]?.ToString() ?? "";
                        gpuDisplayName = !string.IsNullOrEmpty(boardName) ? boardName : devName;
                    }

                    if (assignedPlatform != null)
                    {
                        candidates.Add((device, assignedPlatform, gpuDisplayName, implType));
                    }
                }
            }

            // ---------------------------------------------------------
            // FAZA 3: FILTROWANIE I WYŚWIETLANIE
            // ---------------------------------------------------------
            
            string appGpuName = DeviceName ?? ""; 
            bool foundAny = false;

            foreach (var candidate in candidates)
            {
                // Obcinanie nazwy do nawiasu
                string cleanJsonName = candidate.DisplayName;
                int parenIndex = cleanJsonName.IndexOf('(');
                if (parenIndex > 0)
                {
                    cleanJsonName = cleanJsonName.Substring(0, parenIndex).Trim();
                }

                // Matchowanie
                if (!string.IsNullOrEmpty(cleanJsonName) && 
                    appGpuName.Contains(cleanJsonName, StringComparison.OrdinalIgnoreCase))
                {
                    foundAny = true;
                    RenderOpenClDevice(list, candidate.Device, candidate.Platform, candidate.ImplType);
                }
            }

            if (!foundAny)
            {
                AddAdvancedRow(list, "Info", "No matching OpenCL device found for selected GPU.");
                AddAdvancedRow(list, "App GPU Name", appGpuName);
            }
        }
        catch (Exception ex)
        {
            AddAdvancedRow(list, "Error", $"OpenCL logic failed: {ex.Message}");
        }
    }

    private void RenderOpenClDevice(ObservableCollection<AdvancedItemViewModel> list, JsonNode device, JsonNode? platform, string implType)
    {
        string platformName = platform?["CL_PLATFORM_NAME"]?.ToString() ?? "Unknown";
        
        // --- NAGŁÓWEK ---
        AddAdvancedRow(list, $"Implementation: {implType}", "", true); 

        // --- GENERAL ---
        AddAdvancedRow(list, "General Information", "", true);
        AddAdvancedRow(list, "Platform Name", platformName);
        AddAdvancedRow(list, "Device Name", device["CL_DEVICE_NAME"]?.ToString());
        
        var boardName = device["CL_DEVICE_BOARD_NAME_AMD"]?.ToString();
        if (!string.IsNullOrEmpty(boardName)) AddAdvancedRow(list, "Board Name", boardName);
        
        AddAdvancedRow(list, "Vendor", device["CL_DEVICE_VENDOR"]?.ToString());
        AddAdvancedRow(list, "Device Version", device["CL_DEVICE_VERSION"]?.ToString());
        AddAdvancedRow(list, "Driver Version", device["CL_DRIVER_VERSION"]?.ToString());
        AddAdvancedRow(list, "OpenCL C Version", device["CL_DEVICE_OPENCL_C_VERSION"]?.ToString());
        AddAdvancedRow(list, "Device Profile", device["CL_DEVICE_PROFILE"]?.ToString());
        AddAdvancedRow(list, "Device Available", device["CL_DEVICE_AVAILABLE"]?.ToString() == "true" ? "Yes" : "No");
        AddAdvancedRow(list, "Compiler Available", device["CL_DEVICE_COMPILER_AVAILABLE"]?.ToString() == "true" ? "Yes" : "No");

        // --- COMPUTE ---
        AddAdvancedRow(list, "Compute Capabilities", "", true);
        AddAdvancedRow(list, "Compute Units", device["CL_DEVICE_MAX_COMPUTE_UNITS"]?.ToString());
        AddAdvancedRow(list, "Max Clock", $"{device["CL_DEVICE_MAX_CLOCK_FREQUENCY"]} MHz");
        
        // AMD Specifics
        var simdPerCu = device["CL_DEVICE_SIMD_PER_COMPUTE_UNIT_AMD"];
        if (simdPerCu != null) AddAdvancedRow(list, "SIMD per CU", simdPerCu.ToString());
        
        var simdWidth = device["CL_DEVICE_SIMD_WIDTH_AMD"];
        if (simdWidth != null) AddAdvancedRow(list, "SIMD Width", simdWidth.ToString());
        
        var wavefront = device["CL_DEVICE_WAVEFRONT_WIDTH_AMD"];
        if (wavefront != null) AddAdvancedRow(list, "Wavefront Width", wavefront.ToString());

        var gfxIp = device["CL_DEVICE_GFXIP_MAJOR_AMD"];
        if (gfxIp != null)
        {
            string ipVer = $"{gfxIp}.{device["CL_DEVICE_GFXIP_MINOR_AMD"]}.{device["CL_DEVICE_GFXIP_STEPPING_AMD"]}";
            AddAdvancedRow(list, "GFX IP (AMD)", ipVer);
        }

        // --- FLOATING POINT CONFIG (ROZBITE NA WIERSZE) ---
        AddAdvancedRow(list, "Floating Point Capabilities", "", true);
        RenderFlagsList(list, "Single Precision (FP32)", device["CL_DEVICE_SINGLE_FP_CONFIG"], "CL_FP_");
        RenderFlagsList(list, "Double Precision (FP64)", device["CL_DEVICE_DOUBLE_FP_CONFIG"], "CL_FP_");
        RenderFlagsList(list, "Half Precision (FP16)", device["CL_DEVICE_HALF_FP_CONFIG"], "CL_FP_");

        // --- MEMORY ---
        AddAdvancedRow(list, "Memory", "", true);
        if (long.TryParse(device["CL_DEVICE_GLOBAL_MEM_SIZE"]?.ToString(), out long globalMem))
            AddAdvancedRow(list, "Global Memory Size", FormatSizeMb(globalMem));
        
        var memChannels = device["CL_DEVICE_GLOBAL_MEM_CHANNELS_AMD"];
        if (memChannels != null) AddAdvancedRow(list, "Memory Channels", memChannels.ToString());
        
        string cacheSize = device["CL_DEVICE_GLOBAL_MEM_CACHE_SIZE"]?.ToString() ?? "0";
        string cacheType = device["CL_DEVICE_GLOBAL_MEM_CACHE_TYPE"]?.ToString()?.Replace("CL_", "") ?? "None";
        AddAdvancedRow(list, "Global Cache", $"{FormatSizeBytes(cacheSize)} ({cacheType})");
        
        if (long.TryParse(device["CL_DEVICE_LOCAL_MEM_SIZE"]?.ToString(), out long localMem))
            AddAdvancedRow(list, "Local Memory", $"{FormatSizeKb(localMem)} ({device["CL_DEVICE_LOCAL_MEM_TYPE"]?.ToString()?.Replace("CL_", "")})"); 

        // --- SVM & QUEUES (ROZBITE NA WIERSZE) ---
        AddAdvancedRow(list, "Queue & SVM", "", true);
        RenderFlagsList(list, "SVM Support", device["CL_DEVICE_SVM_CAPABILITIES"], "CL_DEVICE_SVM_");
        RenderFlagsList(list, "Queue On Host", device["CL_DEVICE_QUEUE_ON_HOST_PROPERTIES"], "CL_QUEUE_");
        RenderFlagsList(list, "Queue On Device", device["CL_DEVICE_QUEUE_ON_DEVICE_PROPERTIES"], "CL_QUEUE_");

        // --- LIMITS ---
        AddAdvancedRow(list, "Limits", "", true);
        AddAdvancedRow(list, "Max Memory Allocation", FormatSizeMb(long.Parse(device["CL_DEVICE_MAX_MEM_ALLOC_SIZE"]?.ToString() ?? "0")));
        AddAdvancedRow(list, "Max Constant Buffer", FormatSizeMb(long.Parse(device["CL_DEVICE_MAX_CONSTANT_BUFFER_SIZE"]?.ToString() ?? "0")));
        AddAdvancedRow(list, "Max Constant Args", device["CL_DEVICE_MAX_CONSTANT_ARGS"]?.ToString());
        AddAdvancedRow(list, "Max Parameter Size", $"{device["CL_DEVICE_MAX_PARAMETER_SIZE"]} Bytes");
        
        AddAdvancedRow(list, "Max Work Group Size", device["CL_DEVICE_MAX_WORK_GROUP_SIZE"]?.ToString());
        AddAdvancedRow(list, "Max Work Item Dims", device["CL_DEVICE_MAX_WORK_ITEM_DIMENSIONS"]?.ToString());
        
        var workItemSizes = device["CL_DEVICE_MAX_WORK_ITEM_SIZES"] as JsonArray;
        if (workItemSizes != null)
            AddAdvancedRow(list, "Max Work Item Sizes", string.Join(" x ", workItemSizes));

        // WARUNKOWE WYŚWIETLANIE OBRAZÓW (dla Clover i innych bez wsparcia)
        string imgSupport = device["CL_DEVICE_IMAGE_SUPPORT"]?.ToString()?.ToLower() ?? "false";
        if (imgSupport == "true")
        {
            AddAdvancedRow(list, "Max Samplers", device["CL_DEVICE_MAX_SAMPLERS"]?.ToString());
            AddAdvancedRow(list, "Max Read Image Args", device["CL_DEVICE_MAX_READ_IMAGE_ARGS"]?.ToString());
            AddAdvancedRow(list, "Max Write Image Args", device["CL_DEVICE_MAX_WRITE_IMAGE_ARGS"]?.ToString());
            
            string w2d = device["CL_DEVICE_IMAGE2D_MAX_WIDTH"]?.ToString() ?? "?";
            string h2d = device["CL_DEVICE_IMAGE2D_MAX_HEIGHT"]?.ToString() ?? "?";
            AddAdvancedRow(list, "Max 2D Image", $"{w2d} x {h2d}");

            string w3d = device["CL_DEVICE_IMAGE3D_MAX_WIDTH"]?.ToString() ?? "?";
            string h3d = device["CL_DEVICE_IMAGE3D_MAX_HEIGHT"]?.ToString() ?? "?";
            string d3d = device["CL_DEVICE_IMAGE3D_MAX_DEPTH"]?.ToString() ?? "?";
            AddAdvancedRow(list, "Max 3D Image", $"{w3d} x {h3d} x {d3d}");
        }
        else
        {
            AddAdvancedRow(list, "Image Support", "No");
        }

        AddAdvancedRow(list, "Max Pipe Args", device["CL_DEVICE_MAX_PIPE_ARGS"]?.ToString());
        
        // --- NATIVE VECTORS ---
        AddAdvancedRow(list, "Native Vector Widths", "", true);
        string[] vecTypes = { "CHAR", "SHORT", "INT", "LONG", "FLOAT", "DOUBLE", "HALF" };
        foreach (var t in vecTypes)
        {
            var val = device[$"CL_DEVICE_NATIVE_VECTOR_WIDTH_{t}"];
            if (val != null) AddAdvancedRow(list, t, val.ToString());
        }

        // --- EXTENSIONS ---
        AddAdvancedRow(list, "Extensions", "", true);
        string exts = device["CL_DEVICE_EXTENSIONS"]?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(exts))
        {
            var extList = exts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x).ToList();
            AddAdvancedRow(list, "Extensions Count", extList.Count.ToString());
            
            foreach(var ext in extList) AddAdvancedRow(list, ext, "Supported");
        }

        AddAdvancedRow(list, " ", " "); 
    }

    // --- BRAKUJĄCE METODY POMOCNICZE (Wklej na dole klasy MainWindowViewModel) ---

    // Konwersja bajtów na MB/GB
    private string FormatSizeMb(long bytes)
    {
        double mb = bytes / (1024.0 * 1024.0);
        if (mb >= 1024)
            return $"{mb / 1024.0:0.##} GB";
        return $"{mb:0.##} MB";
    }

    // Konwersja bajtów na KB/MB
    private string FormatSizeKb(long bytes)
    {
        double kb = bytes / 1024.0;
        if (kb >= 1024)
            return $"{kb / 1024.0:0.##} MB";
        return $"{kb:0.##} KB";
    }

    // Inteligentne formatowanie ze stringa (dla Cache Size)
    private string FormatSizeBytes(string bytesStr)
    {
        if (long.TryParse(bytesStr, out long b))
        {
            if (b >= 1024 * 1024 * 1024) return $"{b / (1024.0 * 1024.0 * 1024.0):0.##} GB";
            if (b >= 1024 * 1024) return $"{b / (1024.0 * 1024.0):0.##} MB";
            if (b >= 1024) return $"{b / 1024.0:0.##} KB";
            return $"{b} B";
        }
        return bytesStr;
    }

    // Wyciąganie wersji OpenCL (float)
    private float ParseOpenClVersion(string versionStr)
    {
        if (string.IsNullOrEmpty(versionStr)) return 0.0f;
        var match = System.Text.RegularExpressions.Regex.Match(versionStr, @"OpenCL\s+(\d+\.\d+)");
        if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float ver))
        {
            return ver;
        }
        return 0.0f;
    }


// Helper do wyświetlania flag w osobnych wierszach (jak w GPU-Z / Vulkan Memory)
    private void RenderFlagsList(ObservableCollection<AdvancedItemViewModel> list, string headerName, JsonNode? node, string prefixToRemove)
    {
        if (node == null)
        {
            AddAdvancedRow(list, headerName, "N/A");
            return;
        }

        JsonArray? items = null;

        // Przypadek 1: Obiekt z kluczem (np. "config", "capabilities", "queue_prop")
        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("config")) items = obj["config"] as JsonArray;
            else if (obj.ContainsKey("capabilities")) items = obj["capabilities"] as JsonArray;
            else if (obj.ContainsKey("queue_prop")) items = obj["queue_prop"] as JsonArray;
        }
        // Przypadek 2: Bezpośrednia tablica (rzadziej w clinfo, ale możliwe)
        else if (node is JsonArray arr)
        {
            items = arr;
        }

        // Renderowanie
        if (items != null && items.Count > 0)
        {
            // Opcja A: Pierwszy element w linii nagłówka, reszta pod spodem (oszczędność miejsca)
            // Opcja B: Nagłówek w osobnej linii, flagi pod spodem (czytelniej) -> Wybieramy opcję C (jak w Vulkan)
            
            // Opcja C: Powtarzamy nagłówek dla każdej flagi
            foreach (var item in items)
            {
                string flagName = item?.ToString().Replace(prefixToRemove, "").Trim() ?? "";
                if (!string.IsNullOrEmpty(flagName))
                {
                    AddAdvancedRow(list, headerName, flagName);
                }
            }
        }
        else
        {
            // Jeśli pusta lista lub wartość 0
            string val = node["raw"]?.ToString();
            if (val == "0") AddAdvancedRow(list, headerName, "None");
            else AddAdvancedRow(list, headerName, "No capabilities reported");
        }
    }






    // ==========================================================
    // KONSTRUKTOR
    // ==========================================================
  public MainWindowViewModel()
    {
        // 1. Pobieramy listę ID (card0, card1...)
        var cardIds = LinuxAmdGpuProbe.GetAvailableCards();
        AvailableGpus = new ObservableCollection<GpuListItem>();

        // 2. Dla każdej karty robimy szybki probe, żeby poznać jej nazwę
        foreach (var id in cardIds)
        {
            // Tworzymy tymczasową sondę tylko po to, by pobrać nazwę
            var tempProbe = new LinuxAmdGpuProbe(id);
            var tempData = tempProbe.LoadStaticData();

            AvailableGpus.Add(new GpuListItem 
            { 
                Id = id, 
                // Format: "Nazwa Karty (card0)" - żeby było wiadomo, który to fizyczny slot
                DisplayName = $"{tempData.DeviceName} ({id})" 
            });
        }

        // 3. Wybieramy domyślnie pierwszą kartę
        if (AvailableGpus.Count > 0)
        {
            SelectedGpu = AvailableGpus[0];
        }

        SelectedRefreshRate = RefreshRates.FirstOrDefault(x => x.Seconds == 1.0) ?? RefreshRates[3];

        InitSensors();
    }


    // Ta metoda wywoła się automatycznie, gdy zmienisz wybór w ComboBoxie
    partial void OnSelectedGpuChanged(GpuListItem? value)
    {
        if (value != null)
        {
            LoadGpuData(value.Id); // Przekazujemy ID (np. "card0") do logiki ładowania

            if (IsLogEnabled)
            {
                WriteLogHeader();
            }
        }
    }

    private void LoadGpuData(string cardId)
    {
        // Przekazujemy wybraną kartę do Probe
        IGpuProbe probe = new LinuxAmdGpuProbe(cardId);
        var data = probe.LoadStaticData();

        // Przypisywanie danych...
        DeviceName = data.DeviceName;
        _currentLookupUrl = data.LookupUrl; // Zapamiętujemy URL dla przycisku
        
        // ... (Wszystkie przypisania z poprzednich kroków: BIOS, Driver, etc.) ...
        DeviceId = data.DeviceId;
        Subvendor = data.Subvendor;
        BusId = data.BusId;
        Revision = data.Revision;
        BiosVersion = data.BiosVersion;
        DriverVersion = data.DriverVersion;
        DriverDate = data.DriverDate;
        VulkanApi = data.VulkanApi;
        BusInterface = data.BusInterface;
        ResizableBar = data.ResizableBarState;
        
        GpuCodeName = data.GpuCodeName;
        Technology = data.Technology;
        DieSize = data.DieSize;
        ReleaseDate = data.ReleaseDate;
        Transistors = data.Transistors;
        
        RopsTmus = data.RopsTmus;
        Shaders = data.Shaders;
        ComputeUnits = data.ComputeUnits;
        PixelFillrate = data.PixelFillrate;
        TextureFillrate = data.TextureFillrate;
        
        MemoryType = data.MemoryType;
        BusWidth = data.BusWidth;
        MemorySize = data.MemorySize;
        Bandwidth = data.Bandwidth;
        
        DefaultGpuClock = data.DefaultGpuClock;
        DefaultMemoryClock = data.DefaultMemoryClock;
        DefaultBoostClock = data.DefaultBoostClock;

        // --- ZEGARY BIEŻĄCE (SNAPSHOT) ---
        GpuClock = data.CurrentGpuClock;
        MemoryClock = data.CurrentMemClock;
        BoostClock = data.BoostClock; // Tego nie mamy w prostym odczycie

        // Checkboxy
        IsHsaEnabled = data.IsHsaAvailable; // Teraz to faktycznie HIP (nazwa pola w VM może zostać stara, albo zmień na IsHipEnabled)
        IsOpenClEnabled = data.IsOpenClAvailable;
        IsCudaEnabled = data.IsCudaAvailable;
        IsRocmEnabled = data.IsRocmAvailable;
        IsVulkanEnabled = data.IsVulkanAvailable;
        IsUefiEnabled = data.IsUefiAvailable;
        IsRayTracingEnabled = data.IsRayTracingAvailable;
        IsPhysXEnabled = data.IsPhysXEnabled;
        IsOpenglEnabled = data.IsOpenglAvailable;
    }

    // --- KOMENDY ---
    
    [RelayCommand]
    private void CloseApp()
    {
        // Zamknięcie aplikacji w Avalonii
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private void LookupWeb()
    {
        if (!string.IsNullOrEmpty(_currentLookupUrl))
        {
            // Otwieramy dedykowany URL z bazy
            ShellHelper.OpenUrl(_currentLookupUrl);
        }
        else
        {
            // Fallback: Wyszukujemy nazwę karty
            string query = DeviceName.Replace(" ", "+");
            string url = $"https://www.techpowerup.com/gpu-specs/?q={query}";
            ShellHelper.OpenUrl(url);
        }
    }
}

