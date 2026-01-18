using System;
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
    
    private DispatcherTimer _sensorTimer;

  // Właściwości sterujące oknem (read-only, zależą od SelectedTabIndex)
    public bool ShowResizeGrip => (SelectedTabIndex == 1 || SelectedTabIndex == 2); // 1 to Sensors, 2 to Settings
    public SizeToContent WindowSizeMode => SelectedTabIndex == 0 ? SizeToContent.Height : SizeToContent.Manual;

    // Metoda wywoływana automatycznie przy zmianie tabu (dzięki CommunityToolkit)
    partial void OnSelectedTabIndexChanged(int value)
    {
        // Powiadamiamy widok, że właściwości okna się zmieniły
        OnPropertyChanged(nameof(ShowResizeGrip));
        OnPropertyChanged(nameof(WindowSizeMode));
        
        // Opcjonalnie: Jeśli wracamy na pierwszą zakładkę, wymuś "zwinięcie" okna
        if (value == 0)
        {
            // Czasem trzeba wymusić odświeżenie layoutu, ale binding SizeToContent zazwyczaj wystarcza
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
    }



    // Metoda Inicjalizująca Sensory (Wywołaj ją w konstruktorze lub LoadGpuData)
    private void InitSensors()
    {
        Sensors = new ObservableCollection<SensorItemViewModel>
        {
            // Format: Nazwa, Jednostka, Min, Max, IsFixed (true=TAK, false=NIE)
            
            // GPU Clock | 0 | 100 | NIE
            new SensorItemViewModel("GPU Clock", "MHz", 0, 100, false),
            
            // Memory Clock | 0 | 1000 | NIE
            new SensorItemViewModel("Memory Clock", "MHz", 0, 1000, false),
            
            // GPU Temperature | 20 | 60 | NIE
            new SensorItemViewModel("GPU Temperature", "°C", 20, 60, false),
            
            // Hot Spot | 20 | 80 | NIE
            new SensorItemViewModel("GPU Temperature (Hot Spot)", "°C", 20, 80, false),
            
            // Memory Temperature | 20 | 60 | NIE
            new SensorItemViewModel("Memory Temperature", "°C", 20, 60, false),
            
            // Fan Speed (%) | 0 | 100 | TAK
            new SensorItemViewModel("Fan Speed (%)", "%", 0, 100, true),
            
            // Fan Speed (RPM) | 0 | 1000 | NIE
            new SensorItemViewModel("Fan Speed (RPM)", "RPM", 0, 1000, false),
            
            // GPU Load | 0 | 100 | TAK
            new SensorItemViewModel("GPU Load", "%", 0, 100, true),
            
            // Memory Controller Load | 0 | 100 | TAK
            new SensorItemViewModel("Memory Controller Load", "%", 0, 100, true),
            
            // Memory Used (Dedicated) | 0 | 512 | NIE
            new SensorItemViewModel("Memory Used (Dedicated)", "MB", 0, 512, false),
            
            // Memory Used (Dynamic) | 0 | 128 | NIE
            new SensorItemViewModel("Memory Used (Dynamic)", "MB", 0, 128, false),
            
            // Board Power Draw | 0 | 100 | NIE
            new SensorItemViewModel("Board Power Draw", "W", 0, 100, false),
            
            // GPU Voltage | 0 | 1 | NIE
            new SensorItemViewModel("GPU Voltage", "V", 0, 1.0, false),
            
            // CPU Temperature | 20 | 70 | NIE
            new SensorItemViewModel("CPU Temperature", "°C", 20, 70, false),
            
            // System Memory Used | 0 | 4096 | NIE
            new SensorItemViewModel("System Memory Used", "MB", 0, 4096, false),
        };

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

