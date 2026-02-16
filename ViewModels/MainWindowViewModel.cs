using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPU_T.Services;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace GPU_T.ViewModels;

/// <summary>
/// ViewModel for the main application window. Responsible for exposing GPU metadata,
/// UI state, and commands required by the view. Uses CommunityToolkit MVVM source generators
/// to produce observable properties and relay commands.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    #region PRIVATE FIELDS & STATE

    /// <summary>
    /// Stores the current lookup URL returned by the GPU probe; used by the Lookup command.
    /// </summary>
    private string _currentLookupUrl = "";

    /// <summary>
    /// Tracks the current vendor name so we can refresh the logo when the OS theme changes.
    /// </summary>
    private string _currentVendorName = "";

    /// <summary>
    /// Preserves the last user-selected window height when switching tabs that require manual sizing.
    /// </summary>
    private double _lastUserHeight = 525 - 1;

    #endregion

    #region OBSERVABLE PROPERTIES - MAIN INFO

    /// <summary>
    /// Collection of available GPUs detected on the system.
    /// </summary>
    [ObservableProperty] private ObservableCollection<GpuListItem> _availableGpus;

    /// <summary>
    /// Currently selected GPU item from AvailableGpus.
    /// </summary>
    [ObservableProperty] private GpuListItem? _selectedGpu;

    /// <summary>
    /// Display string for the detected device name or status during detection.
    /// </summary>
    [ObservableProperty] private string _deviceName = "Detecting...";

    /// <summary>
    /// Indicates whether the lookup warning should be visible when an exact variant was not matched.
    /// </summary>
    [ObservableProperty] private bool _showLookupWarning;

    /// <summary>
    /// Image representing the detected GPU vendor logo.
    /// </summary>
    [ObservableProperty] private IImage? _vendorLogo;

    /// <summary>
    /// GPU architecture codename.
    /// </summary>
    [ObservableProperty] private string _gpuCodeName = "N/A";

    /// <summary>
    /// Revision identifier for the GPU.
    /// </summary>
    [ObservableProperty] private string _revision = "N/A";

    /// <summary>
    /// Manufacturing technology node information.
    /// </summary>
    [ObservableProperty] private string _technology = "N/A";

    /// <summary>
    /// Die size specification.
    /// </summary>
    [ObservableProperty] private string _dieSize = "N/A";

    /// <summary>
    /// Official release date of the GPU variant.
    /// </summary>
    [ObservableProperty] private string _releaseDate = "N/A";

    /// <summary>
    /// Number of transistors descriptor.
    /// </summary>
    [ObservableProperty] private string _transistors = "N/A";

    /// <summary>
    /// BIOS/firmware version string for the GPU.
    /// </summary>
    [ObservableProperty] private string _biosVersion = "Unknown";

    /// <summary>
    /// Indicates whether UEFI is enabled for the GPU.
    /// </summary>
    [ObservableProperty] private bool _isUefiEnabled;

    /// <summary>
    /// Subvendor string reported by the GPU.
    /// </summary>
    [ObservableProperty] private string _subvendor = "Unknown";

    /// <summary>
    /// PCI device identifier string.
    /// </summary>
    [ObservableProperty] private string _deviceId = "Unknown";

    /// <summary>
    /// Bus interface type (e.g., PCIe x16).
    /// </summary>
    [ObservableProperty] private string _busInterface = "N/A";

    /// <summary>
    /// PCI bus ID in domain:bus:slot.function format.
    /// </summary>
    [ObservableProperty] private string _busId = "0000:00:00.0";

    /// <summary>
    /// Resizable BAR state representation.
    /// </summary>
    [ObservableProperty] private string _resizableBar = "N/A";

    /// <summary>
    /// Combined ROPs and TMUs description.
    /// </summary>
    [ObservableProperty] private string _ropsTmus = "N/A";

    /// <summary>
    /// Shader count string.
    /// </summary>
    [ObservableProperty] private string _shaders = "N/A";

    /// <summary>
    /// Compute units or equivalent compute block count.
    /// </summary>
    [ObservableProperty] private string _computeUnits = "N/A"; 

    /// <summary>
    /// Pixel fillrate information.
    /// </summary>
    [ObservableProperty] private string _pixelFillrate = "N/A";

    /// <summary>
    /// Texture fillrate information.
    /// </summary>
    [ObservableProperty] private string _textureFillrate = "N/A";

    /// <summary>
    /// Memory type (e.g., GDDR6).
    /// </summary>
    [ObservableProperty] private string _memoryType = "N/A";

    /// <summary>
    /// Memory bus width specification.
    /// </summary>
    [ObservableProperty] private string _busWidth = "N/A";

    /// <summary>
    /// Total memory size string.
    /// </summary>
    [ObservableProperty] private string _memorySize = "0 MB";

    /// <summary>
    /// Memory bandwidth value.
    /// </summary>
    [ObservableProperty] private string _bandwidth = "N/A";

    /// <summary>
    /// Driver version string detected on the system.
    /// </summary>
    [ObservableProperty] private string _driverVersion = "Unknown";

    /// <summary>
    /// Driver release date string.
    /// </summary>
    [ObservableProperty] private string _driverDate = "N/A";      

    /// <summary>
    /// Supported Vulkan API version descriptor.
    /// </summary>
    [ObservableProperty] private string _vulkanApi = "N/A";       

    /// <summary>
    /// Current GPU core clock display string.
    /// </summary>
    [ObservableProperty] private string _gpuClock = "0 MHz";

    /// <summary>
    /// Current memory clock display string.
    /// </summary>
    [ObservableProperty] private string _memoryClock = "0 MHz";

    /// <summary>
    /// Current boost clock display string.
    /// </summary>
    [ObservableProperty] private string _boostClock = "0 MHz";

    /// <summary>
    /// Default GPU core clock as reported by probe data.
    /// </summary>
    [ObservableProperty] private string _defaultGpuClock = "0 MHz";

    /// <summary>
    /// Default memory clock as reported by probe data.
    /// </summary>
    [ObservableProperty] private string _defaultMemoryClock = "0 MHz";

    /// <summary>
    /// Default boost clock as reported by probe data.
    /// </summary>
    [ObservableProperty] private string _defaultBoostClock = "0 MHz";

    /// <summary>
    /// Indicates availability of OpenCL on the detected GPU.
    /// </summary>
    [ObservableProperty] private bool _isOpenClEnabled;

    /// <summary>
    /// Indicates availability of CUDA on the detected GPU.
    /// </summary>
    [ObservableProperty] private bool _isCudaEnabled;

    /// <summary>
    /// Indicates availability of ROCm on the detected GPU.
    /// </summary>
    [ObservableProperty] private bool _isRocmEnabled;

    /// <summary>
    /// Indicates availability of HSA on the detected GPU.
    /// </summary>
    [ObservableProperty] private bool _isHsaEnabled;

    /// <summary>
    /// Indicates availability of Vulkan on the detected GPU.
    /// </summary>
    [ObservableProperty] private bool _isVulkanEnabled;

    /// <summary>
    /// Indicates whether hardware ray tracing is supported.
    /// </summary>
    [ObservableProperty] private bool _isRayTracingEnabled;

    /// <summary>
    /// Indicates whether PhysX acceleration is present.
    /// </summary>
    [ObservableProperty] private bool _isPhysXEnabled;

    /// <summary>
    /// Indicates OpenGL availability on the device.
    /// </summary>
    [ObservableProperty] private bool _isOpenglEnabled;

    #endregion

    #region OBSERVABLE PROPERTIES - UI STATE

    /// <summary>
    /// Index of the currently selected tab in the main view.
    /// </summary>
    [ObservableProperty] private int _selectedTabIndex;

    /// <summary>
    /// Current window height as used for manual sizing modes.
    /// </summary>
    [ObservableProperty] private double _windowHeight = 525 - 1;

    /// <summary>
    /// Message shown when the probe could not determine the exact GPU variant.
    /// </summary>
    public string LookupWarningText => "Could not detect your GPU specific variant.\nInformation shown on this tab may be incorrect.";

    /// <summary>
    /// Controls whether the resize grip should be visible based on the active tab.
    /// </summary>
    public bool ShowResizeGrip => (SelectedTabIndex == 1 || SelectedTabIndex == 2);

    /// <summary>
    /// Determines whether the window should size to content or use manual sizing depending on the active tab.
    /// </summary>
    public SizeToContent WindowSizeMode => SelectedTabIndex == 0 ? SizeToContent.Height : SizeToContent.Manual;

    #endregion

    #region CONSTRUCTOR

    /// <summary>
    /// Initializes the ViewModel, loads available GPU list, sets defaults and starts sensors.
    /// </summary>
    public MainWindowViewModel()
    {
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

        if (Avalonia.Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    VendorLogo = LoadBitmapFromAssets(GetVendorLogoPath());
                });
            };
        }

        InitSensors();
    }

    #endregion

    #region COMMANDS

    /// <summary>
    /// Closes the application by invoking a shutdown on the classic desktop lifetime.
    /// </summary>
    [RelayCommand]
    private void CloseApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>
    /// Opens a web lookup for the currently selected GPU. Uses explicit lookup URL if available,
    /// otherwise falls back to a TechPowerUp search query constructed from DeviceName.
    /// </summary>
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
            // Ensure assignment executes on the UI thread to avoid cross-thread layout issues.
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
        ShowLookupWarning = !data.IsExactMatch;
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
            _currentVendorName = "NVIDIA";
        else if (data.DeviceName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            _currentVendorName = "Intel";
        else
            _currentVendorName = "AMD";

        VendorLogo = LoadBitmapFromAssets(GetVendorLogoPath());

        // Update advanced categories based on probe capabilities
        var categories = probe.GetAdvancedCategories();
        AdvancedCategories = new System.Collections.ObjectModel.ObservableCollection<string>(categories);
        if (categories.Length > 0)
            SelectedAdvancedCategory = categories[0];



    }


    private string GetVendorLogoPath()
    {
        if (_currentVendorName == "NVIDIA")
        {
            var isDark = Avalonia.Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            return isDark ? "/Assets/nvidia_logo_dark.png" : "/Assets/nvidia_logo.png";
        }

        // AMD and Intel (placeholder) use the same logo with no dark variant
        return "/Assets/amd_logo.png";
    }

    private Bitmap? LoadBitmapFromAssets(string path)
    {
        try
        {
            // Construct an avares URI referencing the assembly resource; the AssetLoader requires this format.
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