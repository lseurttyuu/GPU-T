using System;
using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Potrzebne do RelayCommand
using GPU_T.Services;
using Avalonia.Threading; // Do DispatcherTimer
using System.Linq;
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


    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ObservableCollection<SensorItemViewModel> _sensors;


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

