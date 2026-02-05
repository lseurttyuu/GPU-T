using System;
using System.Collections.ObjectModel;
using System.IO;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

public abstract class AdvancedDataProvider
{
    private int _rowCounter = 0;

    // Metoda główna, którą każda klasa pochodna musi zaimplementować
    public abstract void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu);

    // Wspólna metoda dodawania wierszy z obsługą kolorów
    protected void AddRow(ObservableCollection<AdvancedItemViewModel> list, string name, string value = "", bool isHeader = false)
    {
        string color = (_rowCounter % 2 == 0) ? "#FFFFFF" : "#F4F4F4";
        list.Add(new AdvancedItemViewModel(name, value, isHeader, color));
        _rowCounter++;
    }

    protected void ResetCounter()
    {
        _rowCounter = 0;
    }
 
    // --- WSPÓLNE HELPERY ---

    protected string ReadSysFs(string path)
    {
        try { if (File.Exists(path)) return File.ReadAllText(path).Trim(); } catch {}
        return "";
    }

    protected string FormatSizeMb(long bytes)
    {
        double mb = bytes / (1024.0 * 1024.0);
        if (mb >= 1024) return $"{mb / 1024.0:0.##} GB";
        return $"{mb:0.##} MB";
    }

    protected string FormatSizeKb(long bytes)
    {
        double kb = bytes / 1024.0;
        if (kb >= 1024) return $"{kb / 1024.0:0.##} MB";
        return $"{kb:0.##} KB";
    }

    protected string FormatSizeBytes(string bytesStr)
    {
        if (long.TryParse(bytesStr, out long b))
        {
            if (b >= 1024 * 1024 * 1024) return $"{b / (1024.0 * 1024.0 * 1024.0):0.##} GB";
            if (b >= 1024 * 1024) return $"{b / (1024.0 * 1024.0):0.##} MB";
            if (b >= 1024) return $"{b / 1024.0:0.##} KB";
            return $"{b} B";
        }
        return bytesStr;
    }
}