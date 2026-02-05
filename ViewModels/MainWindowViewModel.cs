using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPU_T.Services;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Media;           // Dla IImage
using Avalonia.Media.Imaging;   // Dla Bitmap
using Avalonia.Platform;        // Dla AssetLoader

namespace GPU_T.ViewModels;


public partial class MainWindowViewModel : ViewModelBase
{
    #region PRIVATE FIELDS & STATE

    private string _currentLookupUrl = "";
    private double _lastUserHeight = 525 - 1;

    #endregion

    #region OBSERVABLE PROPERTIES - MAIN INFO

    [ObservableProperty] private ObservableCollection<GpuListItem> _availableGpus;
    [ObservableProperty] private GpuListItem? _selectedGpu;
    [ObservableProperty] private string _deviceName = "Detecting...";
    [ObservableProperty] private IImage? _vendorLogo;

    // Architektura
    [ObservableProperty] private string _gpuCodeName = "N/A";
    [ObservableProperty] private string _revision = "N/A";
    [ObservableProperty] private string _technology = "N/A";
    [ObservableProperty] private string _dieSize = "N/A";
    [ObservableProperty] private string _releaseDate = "N/A";
    [ObservableProperty] private string _transistors = "N/A";

    // Systemowe
    [ObservableProperty] private string _biosVersion = "Unknown";
    [ObservableProperty] private bool _isUefiEnabled;
    [ObservableProperty] private string _subvendor = "Unknown";
    [ObservableProperty] private string _deviceId = "Unknown";
    [ObservableProperty] private string _busInterface = "N/A";
    [ObservableProperty] private string _busId = "0000:00:00.0";
    [ObservableProperty] private string _resizableBar = "N/A";

    // Jednostki
    [ObservableProperty] private string _ropsTmus = "N/A";
    [ObservableProperty] private string _shaders = "N/A";
    [ObservableProperty] private string _computeUnits = "N/A"; 
    [ObservableProperty] private string _pixelFillrate = "N/A";
    [ObservableProperty] private string _textureFillrate = "N/A";

    // Pamięć
    [ObservableProperty] private string _memoryType = "N/A";
    [ObservableProperty] private string _busWidth = "N/A";
    [ObservableProperty] private string _memorySize = "0 MB";
    [ObservableProperty] private string _bandwidth = "N/A";

    // Sterowniki
    [ObservableProperty] private string _driverVersion = "Unknown";
    [ObservableProperty] private string _driverDate = "N/A";      
    [ObservableProperty] private string _vulkanApi = "N/A";       

    // Zegary
    [ObservableProperty] private string _gpuClock = "0 MHz";
    [ObservableProperty] private string _memoryClock = "0 MHz";
    [ObservableProperty] private string _boostClock = "0 MHz";
    [ObservableProperty] private string _defaultGpuClock = "0 MHz";
    [ObservableProperty] private string _defaultMemoryClock = "0 MHz";
    [ObservableProperty] private string _defaultBoostClock = "0 MHz";

    // Technologie (Checkboxy)
    [ObservableProperty] private bool _isOpenClEnabled;
    [ObservableProperty] private bool _isCudaEnabled;
    [ObservableProperty] private bool _isRocmEnabled;
    [ObservableProperty] private bool _isHsaEnabled;
    [ObservableProperty] private bool _isVulkanEnabled;
    [ObservableProperty] private bool _isRayTracingEnabled;
    [ObservableProperty] private bool _isPhysXEnabled;
    [ObservableProperty] private bool _isOpenglEnabled;

    #endregion

    #region OBSERVABLE PROPERTIES - UI STATE

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private double _windowHeight = 525 - 1;

    public bool ShowResizeGrip => (SelectedTabIndex == 1 || SelectedTabIndex == 2);
    public SizeToContent WindowSizeMode => SelectedTabIndex == 0 ? SizeToContent.Height : SizeToContent.Manual;

    #endregion

    #region CONSTRUCTOR

    public MainWindowViewModel()
    {
        VendorLogo = LoadBitmapFromAssets("/Assets/amd_logo.png");

        var cardIds = GpuProbeFactory.GetAvailableCards();
        AvailableGpus = new ObservableCollection<GpuListItem>();

        foreach (var id in cardIds)
        {
            var tempProbe = GpuProbeFactory.Create(id);
            var tempData = tempProbe.LoadStaticData();

            AvailableGpus.Add(new GpuListItem 
            { 
                Id = id, 
                DisplayName = $"{tempData.DeviceName} ({id})" 
            });
        }

        if (AvailableGpus.Count > 0)
        {
            SelectedGpu = AvailableGpus[0];
        }

        SelectedRefreshRate = RefreshRates.FirstOrDefault(x => x.Seconds == 1.0) ?? RefreshRates[3];

        InitSensors();
    }

    #endregion

    #region COMMANDS

    [RelayCommand]
    private void CloseApp()
    {
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
            ShellHelper.OpenUrl(_currentLookupUrl);
        }
        else
        {
            string query = DeviceName.Replace(" ", "+");
            string url = $"https://www.techpowerup.com/gpu-specs/?q={query}";
            ShellHelper.OpenUrl(url);
        }
    }

    #endregion

    #region PARTIAL METHODS (EVENT HANDLERS)

    partial void OnSelectedGpuChanged(GpuListItem? value)
    {
        if (value != null)
        {
            LoadGpuData(value.Id);
            if (IsLogEnabled) WriteLogHeader();
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowResizeGrip));
        OnPropertyChanged(nameof(WindowSizeMode));

        if (value == 0)
        {
            WindowHeight = double.NaN;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                WindowHeight = _lastUserHeight;
            });
        }

        if (value == 2)
        {
            LoadAdvancedData(SelectedAdvancedCategory);
        }
    }

    partial void OnWindowHeightChanged(double value)
    {
        if (_selectedTabIndex != 0 && value > 100)
        {
            _lastUserHeight = value;
        }
    }

    #endregion

    #region PRIVATE METHODS - CORE LOGIC

    private void LoadGpuData(string cardId)
    {
        IGpuProbe probe = GpuProbeFactory.Create(cardId);
        var data = probe.LoadStaticData();

        DeviceName = data.DeviceName;
        _currentLookupUrl = data.LookupUrl;
        
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

        GpuClock = data.CurrentGpuClock;
        MemoryClock = data.CurrentMemClock;
        BoostClock = data.BoostClock; 

        IsHsaEnabled = data.IsHsaAvailable; 
        IsOpenClEnabled = data.IsOpenClAvailable;
        IsCudaEnabled = data.IsCudaAvailable;
        IsRocmEnabled = data.IsRocmAvailable;
        IsVulkanEnabled = data.IsVulkanAvailable;
        IsUefiEnabled = data.IsUefiAvailable;
        IsRayTracingEnabled = data.IsRayTracingAvailable;
        IsPhysXEnabled = data.IsPhysXEnabled;
        IsOpenglEnabled = data.IsOpenglAvailable;


       if (data.DeviceName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            //future implementation: Nvidia GPUs
            //VendorLogo = LoadBitmapFromAssets("/Assets/nvidia_logo.png");
            //ComputeUnitsLabel = "SM Count";
        }
        else if (data.DeviceName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            //future implementation: Intel GPUs
            //VendorLogo = LoadBitmapFromAssets("/Assets/intel_logo.png");
            //ComputeUnitsLabel = "Execution Units";
        }
        else
        {
            VendorLogo = LoadBitmapFromAssets("/Assets/amd_logo.png");
            //ComputeUnitsLabel = "Compute Units";
        }



    }


    private Bitmap? LoadBitmapFromAssets(string path)
    {
        try
        {
            // Konstrukcja URI: avares://NazwaProjektu/Sciezka
            // Upewnij się, że "GPU-T" to dokładna nazwa Twojego Assembly (projektu)
            var uri = new Uri($"avares://GPU-T{path}"); 
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {ex.Message}");
            return null;
        }
    }

    #endregion
}