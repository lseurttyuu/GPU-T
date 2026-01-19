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
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _unit;
    
    // Konfiguracja limitów
    private readonly bool _isFixedRange; // Czy zakres jest "zabetonowany"
    
    // --- 1. DANE DO WYKRESU ---
    private readonly List<double> _graphHistory = new();
    private const int GraphWidth = 131; 
    private const double GraphHeight = 18.0; 

    [ObservableProperty] private List<Point> _graphPoints;

    // --- 2. DANE STATYSTYCZNE ---
    private double _globalMin = double.MaxValue;
    private double _globalMax = double.MinValue;
    private double _globalSum = 0;
    private long _globalCount = 0;
    
    // Globalne Min/Max dla SKALOWANIA wykresu
    // Inicjalizujemy je wartościami Max/Min, żeby pierwsze przypisanie zadziałało poprawnie,
    // chyba że podano limity w konstruktorze (wtedy startujemy od nich).
    private double _scaleMin = double.MaxValue;
    private double _scaleMax = double.MinValue;
    private bool _hasInitializedScale = false;

    // --- 3. WYŚWIETLANIE ---
    [ObservableProperty] private string _displayValue = "---";
    [ObservableProperty] private string _modeLabel = ""; 
    [ObservableProperty] private SensorMode _currentMode = SensorMode.Current;

    public double CurrentValue { get; private set; }

    private bool _isHovering = false;




    // ZMODYFIKOWANY KONSTRUKTOR
    public SensorItemViewModel(string name, string unit, double? minLimit = null, double? maxLimit = null, bool isFixed = false)
    {
        _name = name;
        _unit = unit;
        _graphPoints = new List<Point>();
        
        _isFixedRange = isFixed;

        // Jeśli podano limity startowe, ustawiamy je od razu
        if (minLimit.HasValue) _scaleMin = minLimit.Value;
        if (maxLimit.HasValue) _scaleMax = maxLimit.Value;
        
        // Jeśli podano oba, uznajemy skalę za zainicjowaną
        if (minLimit.HasValue && maxLimit.HasValue) _hasInitializedScale = true;
    }


    // Metoda wywoływana, gdy mysz przesuwa się nad wykresem
    public void ShowHistoryAt(double xPosition, double actualWidth)
    {
        if (_graphHistory.Count == 0) return;

        _isHovering = true;
        ModeLabel = ""; // Ukrywamy etykietę MIN/MAX/AVG

        // MATEMATYKA:
        // Wykres rysowany jest od prawej (indeks Count-1) do lewej (0).
        // xPosition to pozycja myszy od lewej strony (0..131).
        
        // Wzór z GeneratePolygon to: x = width - (Count - 1 - i)
        // Przekształcamy, aby znaleźć 'i' (indeks):
        // x - width = -Count + 1 + i
        // i = x - width + Count - 1
        
        // Ponieważ GraphWidth jest stałe (131), ale actualWidth może się różnić o piksele w renderingu,
        // użyjmy stałej GraphWidth dla spójności logicznej, lub przeskalujmy.
        // Zakładamy, że actualWidth jest bliskie GraphWidth.
        
        // Indeks w historii, który odpowiada pozycji myszy:
        // (Im większe X, tym nowsze dane -> większy indeks)
        // Jeśli mamy pełną historię (131 pkt), to X=0 -> i=0, X=131 -> i=130.
        // Jeśli historia jest krótka (np. 10 pkt), to wykres jest po prawej.
        // X=0..120 -> brak danych, X=121 -> i=0.
        
        // Przesunięcie wynikające z tego, że wykres jest "przyklejony" do prawej
        double offsetFromRight = actualWidth - xPosition;
        int indexFromEnd = (int)Math.Round(offsetFromRight);
        
        // Prawdziwy indeks w liście
        int index = _graphHistory.Count - 1 - indexFromEnd;

        // Zabezpieczenia zakresów
        if (index < 0 || index >= _graphHistory.Count)
        {
            // Mysz jest nad obszarem pustym (jeszcze nie ma danych)
            DisplayValue = "---"; 
        }
        else
        {
            double value = _graphHistory[index];
            string format = GetFormatString(); 
            
            // Format "@ 1500.0" (bez jednostki)
            DisplayValue = $"@ {value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)}";
        }
    }

    public void StopHovering()
    {
        _isHovering = false;
        // Przywracamy standardowy widok (Current/Min/Max) dla ostatniej znanej wartości
        if (_graphHistory.Count > 0)
        {
            UpdateDisplayString(CurrentValue); 
        }
    }


    // Pomocnicza do formatu (wydzielona, żeby nie duplikować)
    private string GetFormatString()
    {
        if (Unit == "RPM" || Unit == "%" || Unit == "MB") return "0";
        if (Unit == "V") return "0.000";
        return "0.0";
    }



    public void UpdateValue(double rawValue)
    {

        CurrentValue = rawValue;
        // 1. Historia
        _graphHistory.Add(rawValue);
        if (_graphHistory.Count > GraphWidth) _graphHistory.RemoveAt(0);

        // 2. Statystyki liczbowe (MIN/MAX/AVG tekstu) - to zawsze liczymy z rzeczywistych danych
        _globalCount++;
        _globalSum += rawValue;
        if (rawValue < _globalMin) _globalMin = rawValue;
        if (rawValue > _globalMax) _globalMax = rawValue;

        // 3. Skalowanie Wykresu (Logika Limitów)
        if (_isFixedRange)
        {
            // Jeśli zakres jest sztywny (Fixed), NIE zmieniamy _scaleMin/_scaleMax.
            // Wykres sam się "przytnie" w metodzie GeneratePolygon (przez clampowanie Y).
        }
        else
        {
            // Zakres elastyczny (Expandable)
            if (!_hasInitializedScale)
            {
                // Pierwszy odczyt (jeśli nie podano limitów w konstruktorze)
                if (_scaleMin == double.MaxValue) _scaleMin = rawValue;
                if (_scaleMax == double.MinValue) _scaleMax = rawValue;
                _hasInitializedScale = true;
            }
            else
            {
                // Kolejne odczyty - rozszerzamy tylko jeśli wartość wyskoczy poza zakres
                if (rawValue < _scaleMin) _scaleMin = rawValue;
                if (rawValue > _scaleMax) _scaleMax = rawValue;
            }
        }

        // 4. Update UI
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

        if (_graphHistory.Count > 0) UpdateDisplayString(_graphHistory.Last());
    }
    
    public void Reset()
    {
        _graphHistory.Clear();
        _graphPoints = new List<Point>();
        OnPropertyChanged(nameof(GraphPoints));
        
        // Reset statystyk liczbowych
        _globalMin = double.MaxValue;
        _globalMax = double.MinValue;
        _globalSum = 0;
        _globalCount = 0;
        
        DisplayValue = "---";
        ModeLabel = "";

        // Reset skali wykresu
        // Jeśli mamy zdefiniowane limity w konstruktorze, to do nich wracamy!
        // Niestety konstruktor już poszedł, więc musimy to sprytnie obsłużyć, 
        // ale najprościej: po prostu zresetujmy flagę inicjalizacji, 
        // chyba że jest Fixed (wtedy i tak nic się nie zmienia).
        
        if (!_isFixedRange)
        {
             // Tutaj mały problem: nie zapamiętaliśmy "startowego min/max" z konstruktora.
             // Ale w praktyce "Reset" w GPU-Z zazwyczaj zeruje wykres do "pustego".
             // Uznajmy, że resetujemy tylko statystyki, a skala wykresu zostaje "rozciągnięta" 
             // albo wraca do stanu początkowego. 
             // W GPU-Z po resecie skala zazwyczaj zostaje, dopóki nie zrestartujesz apki.
             // Zostawmy skalę taką jaka jest (najbardziej naturalne), resetując tylko liczby.
        }
    }
    
    // ... UpdateDisplayString i GeneratePolygon BEZ ZMIAN (są poprawne) ...
    // Pamiętaj tylko, aby wkleić resztę klasy z poprzedniego etapu!
    
    private void UpdateDisplayString(double currentValue)
    {

        if (_isHovering) return;
        // ... (Wklej kod z poprzedniej odpowiedzi) ...
         if (!_hasInitializedScale && _graphHistory.Count == 0) return; // Małe zabezpieczenie

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
        string format = "0.0";
        if (Unit == "RPM" || Unit == "%" || Unit == "MB") format = "0";
        if (Unit == "V") format = "0.000";

        DisplayValue = $"{valToShow.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {Unit}";
    }

    private void GeneratePolygon()
    {
        if (_graphHistory.Count < 2) return;

        double width = (double)GraphWidth;
        double height = GraphHeight;

        double min = _scaleMin;
        double max = _scaleMax;

        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }
        
        double range = max - min;
        var points = new List<Point>();

        points.Add(new Point(width, height)); 
        
        double startX = width - (_graphHistory.Count - 1);
        if (startX < 0) startX = 0;
        points.Add(new Point(startX, height));

        for (int i = 0; i < _graphHistory.Count; i++)
        {
            double val = _graphHistory[i];
            
            double x = width - (_graphHistory.Count - 1 - i);
            double y = height - ((val - min) / range * height);
            
            if (y < 0) y = 0;
            if (y > height) y = height;

            points.Add(new Point(x, y));
        }

        GraphPoints = points;
    }
}