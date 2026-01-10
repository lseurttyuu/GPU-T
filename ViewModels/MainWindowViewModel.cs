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


    // Metoda Inicjalizująca Sensory (Wywołaj ją w konstruktorze lub LoadGpuData)
    private void InitSensors()
    {
        Sensors = new ObservableCollection<SensorItemViewModel>
        {
            new SensorItemViewModel("GPU Clock", "MHz"),
            new SensorItemViewModel("Memory Clock", "MHz"),
            new SensorItemViewModel("GPU Temperature", "°C"),
            new SensorItemViewModel("GPU Hot Spot", "°C"),
            new SensorItemViewModel("Memory Temperature", "°C"),
            new SensorItemViewModel("CPU Temperature", "°C"), // <-- NEW
            
            new SensorItemViewModel("Fan Speed", "RPM"),
            new SensorItemViewModel("Fan Speed (%)", "%"),
            
            new SensorItemViewModel("GPU Load", "%"),
            new SensorItemViewModel("Memory Controller Load", "%"), // <-- NEW
            
            new SensorItemViewModel("Memory Used (Dedicated)", "MB"), // Zmiana nazwy ze starego "Memory Used"
            new SensorItemViewModel("Memory Used (Dynamic)", "MB"),   // <-- NEW
            new SensorItemViewModel("System Memory Used", "MB"),      // <-- NEW
            
            new SensorItemViewModel("Board Power Draw", "W"),
            new SensorItemViewModel("GPU Voltage", "V"),
        };

        // Konfiguracja Timera
        _sensorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0)
        };
        _sensorTimer.Tick += SensorTimer_Tick;
        _sensorTimer.Start();
    }

    private void SensorTimer_Tick(object? sender, EventArgs e)
    {
        if (_selectedGpu == null) return;

        // Tworzymy lekką sondę tylko do odczytu sensorów (z tym samym ID co wybrana karta)
        // Możemy zoptymalizować to trzymając instancję probe w polu klasy, zamiast tworzyć nową.
        var probe = new LinuxAmdGpuProbe(_selectedGpu.Id); 
        var data = probe.LoadSensorData();

        // Aktualizacja UI (szukamy po nazwie i wpisujemy wartość)
        UpdateSensor("GPU Clock", $"{data.GpuClock:0.0}");
        UpdateSensor("Memory Clock", $"{data.MemoryClock:0.0}");
        UpdateSensor("GPU Temperature", $"{data.GpuTemp:0.0}");
        UpdateSensor("GPU Hot Spot", $"{data.GpuHotSpot:0.0}");
        UpdateSensor("Memory Temperature", $"{data.MemoryTemp:0.0}"); // Jeśli 0, można ukryć
        UpdateSensor("Fan Speed", $"{data.FanRpm}");
        UpdateSensor("Fan Speed (%)", $"{data.FanPercent}");
        UpdateSensor("GPU Load", $"{data.GpuLoad}");
        UpdateSensor("Memory Used (Dedicated)", $"{data.MemoryUsed:0}"); // Zmiana klucza
        UpdateSensor("Board Power Draw", $"{data.BoardPower:0.0}");
        UpdateSensor("GPU Voltage", $"{data.GpuVoltage:0.000}");

        UpdateSensor("Memory Controller Load", $"{data.MemControllerLoad}");
        UpdateSensor("Memory Used (Dynamic)", $"{data.MemoryUsedDynamic:0}");
        UpdateSensor("CPU Temperature", $"{data.CpuTemperature:0.0}");
        UpdateSensor("System Memory Used", $"{data.SystemRamUsed:0}"); // W MB
        
        // Przy okazji aktualizujemy snapshoty zegarów w zakładce głównej!
        GpuClock = $"{data.GpuClock:0} MHz";
        MemoryClock = $"{data.MemoryClock:0} MHz";
    }

    private void UpdateSensor(string name, string value)
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
        BoostClock = "0 MHz"; // Tego nie mamy w prostym odczycie

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

