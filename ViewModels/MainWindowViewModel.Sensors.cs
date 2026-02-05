using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using GPU_T.Services;

namespace GPU_T.ViewModels;

// ==========================================================
// CZĘŚĆ 2 - SENSORY I LOGOWANIE
// ==========================================================

public partial class MainWindowViewModel
{
    private DispatcherTimer _sensorTimer;
    private string _logFilePath = "";

    [ObservableProperty] private ObservableCollection<SensorItemViewModel> _sensors;
    [ObservableProperty] private bool _isLogEnabled;
    
    [ObservableProperty] private ObservableCollection<RefreshRateItem> _refreshRates = new()
    {
        new RefreshRateItem { Label = "0.1 s", Seconds = 0.1 },
        new RefreshRateItem { Label = "0.2 s", Seconds = 0.2 },
        new RefreshRateItem { Label = "0.5 s", Seconds = 0.5 },
        new RefreshRateItem { Label = "1.0 s", Seconds = 1.0 },
        new RefreshRateItem { Label = "2.0 s", Seconds = 2.0 },
        new RefreshRateItem { Label = "5.0 s", Seconds = 5.0 },
        new RefreshRateItem { Label = "10.0 s", Seconds = 10.0 },
    };
    
    [ObservableProperty] private RefreshRateItem _selectedRefreshRate;

    partial void OnSelectedRefreshRateChanged(RefreshRateItem value)
    {
        if (_sensorTimer != null && value != null)
        {
            _sensorTimer.Interval = TimeSpan.FromSeconds(value.Seconds);
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

    public void StartLogging(string filePath)
    {
        _logFilePath = filePath;
        IsLogEnabled = true;
        WriteLogHeader();
    }

    public void StopLogging()
    {
        IsLogEnabled = false;
        _logFilePath = "";
    }

    private void InitSensors()
    {
        string gpuId = _selectedGpu?.Id ?? "card0";
        var probe = GpuProbeFactory.Create(gpuId);
        var support = probe.GetSensorAvailability();

        var list = new ObservableCollection<SensorItemViewModel>();

        list.Add(new SensorItemViewModel("GPU Clock", "MHz", 0, 100, false));
        list.Add(new SensorItemViewModel("Memory Clock", "MHz", 0, 1000, false));
        list.Add(new SensorItemViewModel("GPU Temperature", "°C", 20, 60, false));

        if (support.HasHotSpot)
            list.Add(new SensorItemViewModel("GPU Temperature (Hot Spot)", "°C", 20, 80, false));

        if (support.HasMemTemp)
            list.Add(new SensorItemViewModel("Memory Temperature", "°C", 20, 60, false));

        if (support.HasFan)
        {
            list.Add(new SensorItemViewModel("Fan Speed (%)", "%", 0, 100, true));
            list.Add(new SensorItemViewModel("Fan Speed (RPM)", "RPM", 0, 1000, false));
        }

        if (support.HasGpuLoad)
            list.Add(new SensorItemViewModel("GPU Load", "%", 0, 100, true));

        if (support.HasMemControllerLoad)
            list.Add(new SensorItemViewModel("Memory Controller Load", "%", 0, 100, true));

        if (support.HasMemUsed)
        {
            list.Add(new SensorItemViewModel("Memory Used (Dedicated)", "MB", 0, 512, false));
            list.Add(new SensorItemViewModel("Memory Used (Dynamic)", "MB", 0, 128, false));
        }

        if (support.HasPower)
            list.Add(new SensorItemViewModel("Board Power Draw", "W", 0, 100, false));

        if (support.HasVoltage)
            list.Add(new SensorItemViewModel("GPU Voltage", "V", 0, 1.0, false));

        list.Add(new SensorItemViewModel("CPU Temperature", "°C", 20, 70, false));
        list.Add(new SensorItemViewModel("System Memory Used", "MB", 0, 4096, false));

        Sensors = list;

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
        
        var probe = GpuProbeFactory.Create(_selectedGpu.Id, MemoryType);
        var data = probe.LoadSensorData();

        UpdateSensor("GPU Clock", data.GpuClock);
        UpdateSensor("Memory Clock", data.MemoryClock);
        UpdateSensor("GPU Temperature", data.GpuTemp);
        UpdateSensor("GPU Temperature (Hot Spot)", data.GpuHotSpot);
        UpdateSensor("Memory Temperature", data.MemoryTemp);
        
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
            catch { /* Ignore lock errors */ }
        }
    }

    private void UpdateSensor(string name, double value)
    {
        var sensor = Sensors.FirstOrDefault(s => s.Name == name);
        if (sensor != null) sensor.UpdateValue(value);
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
            Console.WriteLine($"Log write error: {ex.Message}");
            StopLogging();
        }
    }
}