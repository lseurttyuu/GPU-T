using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GPU_T.ViewModels;

/// <summary>
/// Represents a single sensor item including history, statistics and lightweight sparkline generation for the UI.
/// </summary>
public enum SensorMode
{
    Current,
    Min,
    Max,
    Avg
}

/// <summary>
/// ViewModel for an individual sensor exposed to the UI. Maintains history, statistics and provides formatted display values.
/// </summary>
public partial class SensorItemViewModel : ViewModelBase
{
    #region PRIVATE FIELDS

    private readonly bool _isFixedRange;
    private const int GraphWidth = 131; 
    private const double GraphHeight = 18.0; 

    private readonly List<double> _graphHistory = new();
    
    private double _globalMin = double.MaxValue;
    private double _globalMax = double.MinValue;
    private double _globalSum = 0;
    private long _globalCount = 0;
    
    private double _scaleMin = double.MaxValue;
    private double _scaleMax = double.MinValue;
    private bool _hasInitializedScale = false;

    private bool _isHovering = false;

    #endregion

    #region OBSERVABLE PROPERTIES

    /// <summary>
    /// Sensor display name (generated public property: Name).
    /// </summary>
    [ObservableProperty] private string _name;
    /// <summary>
    /// Sensor unit label (generated public property: Unit).
    /// </summary>
    [ObservableProperty] private string _unit;
    /// <summary>
    /// Formatted display string for the current sensor value (generated public property: DisplayValue).
    /// </summary>
    [ObservableProperty] private string _displayValue = "---";
    /// <summary>
    /// Label representing the current mode (MIN/MAX/AVG) (generated public property: ModeLabel).
    /// </summary>
    [ObservableProperty] private string _modeLabel = ""; 
    /// <summary>
    /// Currently selected display mode (generated public property: CurrentMode).
    /// </summary>
    [ObservableProperty] private SensorMode _currentMode = SensorMode.Current;
    /// <summary>
    /// Points used to render the sparkline polygon (generated public property: GraphPoints).
    /// </summary>
    [ObservableProperty] private List<Point> _graphPoints;

    /// <summary>
    /// Public access to the current raw numeric value (used by loggers and other services).
    /// </summary>
    public double CurrentValue { get; private set; }

    #endregion

    #region CONSTRUCTOR

    /// <summary>
    /// Initializes a new instance of <see cref="SensorItemViewModel"/>.
    /// </summary>
    /// <param name="name">Sensor name.</param>
    /// <param name="unit">Sensor unit string.</param>
    /// <param name="minLimit">Optional initial minimum scale.</param>
    /// <param name="maxLimit">Optional initial maximum scale.</param>
    /// <param name="isFixed">Specifies whether the scale is fixed.</param>
    public SensorItemViewModel(string name, string unit, double? minLimit = null, double? maxLimit = null, bool isFixed = false)
    {
        _name = name;
        _unit = unit;
        _graphPoints = new List<Point>();
        _isFixedRange = isFixed;

        if (minLimit.HasValue) _scaleMin = minLimit.Value;
        if (maxLimit.HasValue) _scaleMax = maxLimit.Value;
        
        if (minLimit.HasValue && maxLimit.HasValue) _hasInitializedScale = true;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Updates the sensor with a new raw value: appends history, updates statistics and regenerates UI data.
    /// </summary>
    /// <param name="rawValue">The latest sensor reading.</param>
    public void UpdateValue(double rawValue)
    {
        CurrentValue = rawValue;

        _graphHistory.Add(rawValue);
        if (_graphHistory.Count > GraphWidth) _graphHistory.RemoveAt(0);

        _globalCount++;
        _globalSum += rawValue;
        if (rawValue < _globalMin) _globalMin = rawValue;
        if (rawValue > _globalMax) _globalMax = rawValue;

        if (!_isFixedRange)
        {
            if (!_hasInitializedScale)
            {
                if (_scaleMin == double.MaxValue) _scaleMin = rawValue;
                if (_scaleMax == double.MinValue) _scaleMax = rawValue;
                _hasInitializedScale = true;
            }
            else
            {
                if (rawValue < _scaleMin) _scaleMin = rawValue;
                if (rawValue > _scaleMax) _scaleMax = rawValue;
            }
        }

        UpdateDisplayString(rawValue);
        GeneratePolygon();
    }

    /// <summary>
    /// Resets history and statistics while preserving scale extents derived from history.
    /// </summary>
    public void Reset()
    {
        _graphHistory.Clear();
        _graphPoints = new List<Point>();
        OnPropertyChanged(nameof(GraphPoints));
        
        _globalMin = double.MaxValue;
        _globalMax = double.MinValue;
        _globalSum = 0;
        _globalCount = 0;
        
        DisplayValue = "---";
        ModeLabel = "";
    }

    /// <summary>
    /// When hovering over the sparkline, shows the historical value corresponding to the provided x position.
    /// </summary>
    /// <param name="xPosition">X coordinate of the cursor relative to the control.</param>
    /// <param name="actualWidth">Rendered width of the sparkline control.</param>
    public void ShowHistoryAt(double xPosition, double actualWidth)
    {
        if (_graphHistory.Count == 0) return;

        _isHovering = true;
        ModeLabel = ""; 

        // Map cursor X to history index. The sparkline is rendered from right-to-left in history buffer.
        double offsetFromRight = actualWidth - xPosition;
        int indexFromEnd = (int)Math.Round(offsetFromRight);
        int index = _graphHistory.Count - 1 - indexFromEnd;

        if (index < 0 || index >= _graphHistory.Count)
        {
            DisplayValue = "---"; 
        }
        else
        {
            double value = _graphHistory[index];
            DisplayValue = $"@ {value.ToString(GetFormatString(), System.Globalization.CultureInfo.InvariantCulture)}";
        }
    }

    /// <summary>
    /// Stops hover mode and restores the standard display string for the current value.
    /// </summary>
    public void StopHovering()
    {
        _isHovering = false;
        if (_graphHistory.Count > 0)
        {
            UpdateDisplayString(CurrentValue); 
        }
    }

    #endregion

    #region COMMANDS

    [RelayCommand]
    private void ToggleMode()
    {
        CurrentMode = CurrentMode switch
        {
            SensorMode.Current => SensorMode.Min,
            SensorMode.Min => SensorMode.Max,
            SensorMode.Max => SensorMode.Avg,
            _ => SensorMode.Current
        };

        if (_graphHistory.Count > 0) UpdateDisplayString(CurrentValue);
    }

    #endregion

    #region PRIVATE METHODS

    private void UpdateDisplayString(double currentValue)
    {
        if (_isHovering) return;
        if (!_hasInitializedScale && _graphHistory.Count == 0) return;

        double valToShow = currentValue;
        string label = "";

        switch (CurrentMode)
        {
            case SensorMode.Min:
                valToShow = _globalMin;
                label = "MIN";
                break;
            case SensorMode.Max:
                valToShow = _globalMax;
                label = "MAX";
                break;
            case SensorMode.Avg:
                valToShow = (_globalCount > 0) ? (_globalSum / _globalCount) : 0;
                label = "AVG";
                break;
            case SensorMode.Current:
            default:
                valToShow = currentValue;
                label = "";
                break;
        }

        ModeLabel = label;
        DisplayValue = $"{valToShow.ToString(GetFormatString(), System.Globalization.CultureInfo.InvariantCulture)} {Unit}";
    }

    private void GeneratePolygon()
    {
        if (_graphHistory.Count < 2) return;

        double width = (double)GraphWidth;
        double height = GraphHeight;

        double min = _scaleMin;
        double max = _scaleMax;

        // Prevent division by zero when all history values are equal.
        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }
        
        double range = max - min;
        var points = new List<Point>();

        // Closing polygon points at bottom-right and bottom-left.
        points.Add(new Point(width, height)); 
        
        double startX = width - (_graphHistory.Count - 1);
        if (startX < 0) startX = 0;
        points.Add(new Point(startX, height));

        // Generate graph points left-to-right based on history buffer.
        for (int i = 0; i < _graphHistory.Count; i++)
        {
            double val = _graphHistory[i];
            
            double x = width - (_graphHistory.Count - 1 - i);
            double y = height - ((val - min) / range * height);
            
            // Clamp Y to the chart boundaries to avoid rendering artifacts.
            if (y < 0) y = 0;
            if (y > height) y = height;

            points.Add(new Point(x, y));
        }

        GraphPoints = points;
    }

    private string GetFormatString()
    {
        if (Unit == "RPM" || Unit == "%" || Unit == "MB") return "0";
        if (Unit == "V") return "0.000";
        return "0.0";
    }

    #endregion
}