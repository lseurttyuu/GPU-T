using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GPU_T.ViewModels;

namespace GPU_T.Services;

public static class SensorLogService
{
    private const int DateColumnWidth = 20;

    public static string BuildHeader(IEnumerable<SensorItemViewModel> sensors)
    {
        var sb = new StringBuilder();
        
        // 1. Kolumna Daty (Stała szerokość 20, wyśrodkowana)
        // Wynik: "        Date         ,"
        sb.Append(PadCenter("Date", DateColumnWidth) + ",");

        // 2. Kolumny Sensorów
        foreach (var sensor in sensors)
        {
            string label = $"{sensor.Name} [{sensor.Unit}]";
            
            // Reguła: Szerokość = Spacja + Nazwa + Spacja
            // Wynik: " GPU Clock [MHz] ,"
            sb.Append(" " + label + " ,");
        }

        return sb.ToString();
    }

    public static string BuildDataRow(IEnumerable<SensorItemViewModel> sensors)
    {
        var sb = new StringBuilder();

        // 1. Data (Format yyyy-MM-dd HH:mm:ss + 1 spacja)
        // Szerokość wynikowa to dokładnie 20 znaków (19 znaków daty + 1 spacji)
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        sb.Append(dateStr + " ,"); 

        // 2. Wartości Sensorów
        foreach (var sensor in sensors)
        {
            // A. Odtwarzamy szerokość kolumny nagłówka
            string label = $"{sensor.Name} [{sensor.Unit}]";
            int headerWidth = 1 + label.Length + 1; // Spacja lewa + tekst + spacja prawa

            // B. Formatujemy wartość
            string valStr = sensor.CurrentValue.ToString(GetFormat(sensor.Unit), CultureInfo.InvariantCulture);

            // C. Obliczamy padding (spacje z lewej strony)
            // Reguła: Wartość + 3 spacje muszą się zmieścić w headerWidth.
            // Padding = SzerokośćNagłówka - DługośćWartości - 3 (stałe spacje na końcu)
            int paddingCount = headerWidth - valStr.Length - 3;
            
            // Zabezpieczenie, gdyby wartość była dłuższa niż nagłówek (mało prawdopodobne, ale możliwe)
            if (paddingCount < 0) paddingCount = 0;

            string leftPadding = new string(' ', paddingCount);
            string rightPadding = "   "; // Zawsze 3 spacje po wartości

            // Wynik: "         400.0   ,"
            sb.Append(leftPadding + valStr + rightPadding + ",");
        }

        return sb.ToString();
    }

    // Helper: Centrowanie tekstu (dla kolumny Date)
    private static string PadCenter(string text, int width)
    {
        if (text.Length >= width) return text;
        int leftPadding = (width - text.Length) / 2;
        int rightPadding = width - text.Length - leftPadding;
        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }

    private static string GetFormat(string unit)
    {
        // Taki sam format jak w UI, tylko wymuszamy kropkę przez InvariantCulture
        if (unit == "RPM" || unit == "%" || unit == "MB") return "0";
        if (unit == "V") return "0.000"; // Napięcia mają 3 miejsca po przecinku (np. 0.725)
        return "0.0"; // Zegary i temperatury 1 miejsce po przecinku
    }
}