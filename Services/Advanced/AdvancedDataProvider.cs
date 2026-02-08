using System;
using System.Collections.ObjectModel;
using System.IO;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

/// <summary>
/// Abstract base class for advanced GPU data providers. 
/// Provides common row formatting and helper methods for derived classes.
/// </summary>
public abstract class AdvancedDataProvider
{
    private int _rowCounter = 0;

    /// <summary>
    /// Loads advanced GPU data into the provided collection.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="list">The collection to populate with advanced item view models.</param>
    /// <param name="selectedGpu">The currently selected GPU item, or null if not specified.</param>
    public abstract void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu);

    /// <summary>
    /// Adds a row to the collection with alternating background color for readability.
    /// </summary>
    /// <param name="list">The collection to add the row to.</param>
    /// <param name="name">The row name.</param>
    /// <param name="value">The row value.</param>
    /// <param name="isHeader">Indicates if the row is a header.</param>
    protected void AddRow(ObservableCollection<AdvancedItemViewModel> list, string name, string value = "", bool isHeader = false)
    {
        // Alternates row color for UI clarity.
        string color = (_rowCounter % 2 == 0) ? "#FFFFFF" : "#F4F4F4";
        list.Add(new AdvancedItemViewModel(name, value, isHeader, color));
        _rowCounter++;
    }

    /// <summary>
    /// Resets the row counter for color alternation.
    /// </summary>
    protected void ResetCounter()
    {
        _rowCounter = 0;
    }
 
    /// <summary>
    /// Reads a value from the specified sysfs path, returning trimmed content or empty string if unavailable.
    /// </summary>
    /// <param name="path">The sysfs file path.</param>
    /// <returns>The trimmed file content or empty string.</returns>
    protected string ReadSysFs(string path)
    {
        try { if (File.Exists(path)) return File.ReadAllText(path).Trim(); } catch {}
        return "";
    }

    /// <summary>
    /// Formats a byte value as a string in MB or GB units.
    /// </summary>
    /// <param name="bytes">The value in bytes.</param>
    /// <returns>A formatted string representing the size.</returns>
    protected string FormatSizeMb(long bytes)
    {
        double mb = bytes / (1024.0 * 1024.0);
        if (mb >= 1024) return $"{mb / 1024.0:0.##} GB";
        return $"{mb:0.##} MB";
    }

    /// <summary>
    /// Formats a byte value as a string in KB or MB units.
    /// </summary>
    /// <param name="bytes">The value in bytes.</param>
    /// <returns>A formatted string representing the size.</returns>
    protected string FormatSizeKb(long bytes)
    {
        double kb = bytes / 1024.0;
        if (kb >= 1024) return $"{kb / 1024.0:0.##} MB";
        return $"{kb:0.##} KB";
    }

    /// <summary>
    /// Formats a string representing bytes as a human-readable size.
    /// </summary>
    /// <param name="bytesStr">The string value in bytes.</param>
    /// <returns>A formatted string representing the size.</returns>
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