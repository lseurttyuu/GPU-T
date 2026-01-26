using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            // ... reszta case'ów ...
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
        
        // Driver Module (z poprawką ResolveLinkTarget)
        string driverModule = "Unknown";
        try 
        {
            var driverPath = $"/sys/class/drm/{_selectedGpu?.Id ?? "card0"}/device/driver";
            var dirInfo = new DirectoryInfo(driverPath);
            if (dirInfo.Exists)
            {
                // .NET 6+
                var target = dirInfo.ResolveLinkTarget(true); 
                driverModule = target != null ? target.Name : dirInfo.Name;
            }
        } 
        catch {}
        AddAdvancedRow(list, "Kernel Driver", driverModule);
        AddAdvancedRow(list, "OpenGL / Mesa", "Scanning implemented in next step...");

        // 3. Firmware (POPRAWIONA LOGIKA)
        AddAdvancedRow(list, "Firmware", "", true);
        
        string fwDirPath = $"/sys/class/drm/{_selectedGpu?.Id ?? "card0"}/device/fw_version";
        
        if (Directory.Exists(fwDirPath))
        {
            try
            {
                // Pobieramy wszystkie pliki *_fw_version
                var files = Directory.GetFiles(fwDirPath, "*_fw_version");
                
                // Sortujemy alfabetycznie, żeby był porządek
                Array.Sort(files);

                foreach (var filePath in files)
                {
                    // 1. Wyciągamy nazwę (np. "smc_fw_version" -> "SMC")
                    string fileName = Path.GetFileName(filePath);
                    string shortName = fileName.Replace("_fw_version", "").ToUpper();

                    // 2. Czytamy zawartość (wersję)
                    string version = File.ReadAllText(filePath).Trim();

                    AddAdvancedRow(list, shortName, version);
                }

                if (files.Length == 0)
                {
                     AddAdvancedRow(list, "Info", "No firmware files found in directory");
                }
            }
            catch (Exception ex)
            {
                AddAdvancedRow(list, "Error", ex.Message);
            }
        }
        else
        {
             // Fallback dla starszych kerneli lub innych sterowników, gdzie to może być plik
             if (File.Exists(fwDirPath))
             {
                 AddAdvancedRow(list, "Legacy Info", "Old kernel format detected");
             }
             else
             {
                 AddAdvancedRow(list, "Firmware Info", "Not available");
             }
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

