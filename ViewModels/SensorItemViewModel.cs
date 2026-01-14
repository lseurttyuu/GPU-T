using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media; // Do RenderOptions (potrzebne w XAML, tu tylko logicznie)
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
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _unit;
    
    // --- 1. DANE DO WYKRESU (Tylko widoczne okno) ---
    private readonly List<double> _graphHistory = new();
    private const int GraphWidth = 131; // Zmienione na 131
    private const double GraphHeight = 18.0; // Wewnętrzna wysokość (20px - 2px ramki)

    // Punkty do Polygonu
    [ObservableProperty] private List<Point> _graphPoints;

    // --- 2. DANE STATYSTYCZNE (Globalne, od uruchomienia/resetu) ---
    // Używamy metody "Running Stats" - O(1) pamięci i CPU.
    private double _globalMin = double.MaxValue;
    private double _globalMax = double.MinValue;
    private double _globalSum = 0;
    private long _globalCount = 0;
    
    // Globalne Min/Max tylko dla SKALOWANIA wykresu (żeby nie skakał)
    private double _scaleMin = double.MaxValue;
    private double _scaleMax = double.MinValue;
    private bool _hasData = false;

    // --- 3. WYŚWIETLANIE ---
    [ObservableProperty] private string _displayValue = "---";
    
    // Etykieta trybu (MIN, MAX, AVG) - pusta dla Current
    [ObservableProperty] private string _modeLabel = ""; 
    
    [ObservableProperty] private SensorMode _currentMode = SensorMode.Current;

    public SensorItemViewModel(string name, string unit)
    {
        _name = name;
        _unit = unit;
        _graphPoints = new List<Point>();
    }

    public void UpdateValue(double rawValue)
    {
        // A. Aktualizacja wykresu (tylko 133 punkty)
        _graphHistory.Add(rawValue);
        if (_graphHistory.Count > GraphWidth) _graphHistory.RemoveAt(0);

        // B. Aktualizacja statystyk globalnych (Running Stats)
        _globalCount++;
        _globalSum += rawValue;
        if (rawValue < _globalMin) _globalMin = rawValue;
        if (rawValue > _globalMax) _globalMax = rawValue;

        // C. Aktualizacja skali wykresu (tylko puchnie)
        if (!_hasData)
        {
            _scaleMin = rawValue;
            _scaleMax = rawValue;
            _hasData = true;
        }
        else
        {
            if (rawValue < _scaleMin) _scaleMin = rawValue;
            if (rawValue > _scaleMax) _scaleMax = rawValue;
        }

        // D. Odświeżenie widoku
        UpdateDisplayString(rawValue);
        GeneratePolygon();
    }

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

        // Aktualizujemy etykietę i wartość (używając ostatniej znanej wartości bieżącej)
        if (_graphHistory.Count > 0)
        {
            UpdateDisplayString(_graphHistory.Last());
        }
    }
    
    public void Reset()
    {
        // Czyścimy wszystko
        _graphHistory.Clear();
        _graphPoints = new List<Point>();
        OnPropertyChanged(nameof(GraphPoints));
        
        _globalMin = double.MaxValue;
        _globalMax = double.MinValue;
        _globalSum = 0;
        _globalCount = 0;
        
        _scaleMin = double.MaxValue;
        _scaleMax = double.MinValue;
        _hasData = false;
        
        DisplayValue = "---";
        ModeLabel = "";
        // Opcjonalnie: CurrentMode = SensorMode.Current; // Jeśli chcesz wracać do Current po resecie
    }

    private void UpdateDisplayString(double currentValue)
    {
        if (!_hasData) return;

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

        // Ustalanie formatu
        string format = "0.0"; // Domyślny
        if (Unit == "RPM" || Unit == "%" || Unit == "MB") format = "0";
        if (Unit == "V") format = "0.000"; // ZMIANA: 3 miejsca po przecinku dla Voltów

        // ZMIANA: Wymuszenie InvariantCulture (kropka zamiast przecinka)
        DisplayValue = $"{valToShow.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {Unit}";
    }

    private void GeneratePolygon()
    {
        if (_graphHistory.Count < 2) return;

        // Używamy dokładnych wymiarów wewnętrznych
        double width = (double)GraphWidth; // 131.0
        double height = GraphHeight;       // 18.0

        double min = _scaleMin;
        double max = _scaleMax;

        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }
        
        double range = max - min;
        var points = new List<Point>();

        // 1. Punkty zamykające (dół) - dokładnie w narożnikach
        points.Add(new Point(width, height)); 
        
        double startX = width - (_graphHistory.Count - 1);
        if (startX < 0) startX = 0;
        points.Add(new Point(startX, height));

        // 2. Punkty wykresu
        for (int i = 0; i < _graphHistory.Count; i++)
        {
            double val = _graphHistory[i];
            
            // X: Od prawej
            double x = width - (_graphHistory.Count - 1 - i);
            
            // Y: Skalowanie (0 to góra, height to dół)
            double y = height - ((val - min) / range * height);
            
            // Clamp: Zabezpieczenie na wszelki wypadek
            if (y < 0) y = 0;
            if (y > height) y = height;

            points.Add(new Point(x, y));
        }

        GraphPoints = points;
    }
}