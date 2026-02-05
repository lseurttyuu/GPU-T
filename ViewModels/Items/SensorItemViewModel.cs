using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GPU_T.ViewModels;

public enum SensorMode
{
    Current,
    Min,
    Max,
    Avg
}

public partial class SensorItemViewModel : ViewModelBase
{
    #region PRIVATE FIELDS

    // Konfiguracja
    private readonly bool _isFixedRange;
    private const int GraphWidth = 131; 
    private const double GraphHeight = 18.0; 

    // Dane Wykresu
    private readonly List<double> _graphHistory = new();
    
    // Dane Statystyczne
    private double _globalMin = double.MaxValue;
    private double _globalMax = double.MinValue;
    private double _globalSum = 0;
    private long _globalCount = 0;
    
    // Skalowanie Wykresu
    private double _scaleMin = double.MaxValue;
    private double _scaleMax = double.MinValue;
    private bool _hasInitializedScale = false;

    // Stan UI
    private bool _isHovering = false;

    #endregion

    #region OBSERVABLE PROPERTIES

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _unit;
    [ObservableProperty] private string _displayValue = "---";
    [ObservableProperty] private string _modeLabel = ""; 
    [ObservableProperty] private SensorMode _currentMode = SensorMode.Current;
    [ObservableProperty] private List<Point> _graphPoints;

    // Publiczny dostęp do aktualnej wartości (dla logera)
    public double CurrentValue { get; private set; }

    #endregion

    #region CONSTRUCTOR

    public SensorItemViewModel(string name, string unit, double? minLimit = null, double? maxLimit = null, bool isFixed = false)
    {
        _name = name;
        _unit = unit;
        _graphPoints = new List<Point>();
        _isFixedRange = isFixed;

        // Inicjalizacja limitów startowych
        if (minLimit.HasValue) _scaleMin = minLimit.Value;
        if (maxLimit.HasValue) _scaleMax = maxLimit.Value;
        
        if (minLimit.HasValue && maxLimit.HasValue) _hasInitializedScale = true;
    }

    #endregion

    #region PUBLIC METHODS

    public void UpdateValue(double rawValue)
    {
        CurrentValue = rawValue;

        // 1. Historia
        _graphHistory.Add(rawValue);
        if (_graphHistory.Count > GraphWidth) _graphHistory.RemoveAt(0);

        // 2. Statystyki
        _globalCount++;
        _globalSum += rawValue;
        if (rawValue < _globalMin) _globalMin = rawValue;
        if (rawValue > _globalMax) _globalMax = rawValue;

        // 3. Skalowanie Wykresu
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

        // 4. Update UI
        UpdateDisplayString(rawValue);
        GeneratePolygon();
    }

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
        
        // Nie resetujemy _scaleMin/_scaleMax (zostają "rozciągnięte" z historii)
    }

    // Interakcja myszą (Hover)
    public void ShowHistoryAt(double xPosition, double actualWidth)
    {
        if (_graphHistory.Count == 0) return;

        _isHovering = true;
        ModeLabel = ""; 

        // Obliczanie indeksu pod kursorem (wykres rysowany od prawej)
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

        // Zapobieganie dzieleniu przez zero
        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }
        
        double range = max - min;
        var points = new List<Point>();

        // Punkty zamykające poligon (dół-prawa, dół-lewa)
        points.Add(new Point(width, height)); 
        
        double startX = width - (_graphHistory.Count - 1);
        if (startX < 0) startX = 0;
        points.Add(new Point(startX, height));

        // Punkty wykresu (od lewej do prawej)
        for (int i = 0; i < _graphHistory.Count; i++)
        {
            double val = _graphHistory[i];
            
            double x = width - (_graphHistory.Count - 1 - i);
            double y = height - ((val - min) / range * height);
            
            // Clampowanie Y (dla FixedRange)
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