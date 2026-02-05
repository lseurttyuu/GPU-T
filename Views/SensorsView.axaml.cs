using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GPU_T.ViewModels;
using System.Collections.Generic;

namespace GPU_T.Views;

public partial class SensorsView : UserControl
{
    public SensorsView()
    {
        InitializeComponent();
    }

    // --- OBSŁUGA WYKRESÓW ---

    private void Graph_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Sprawdzamy, czy element wywołujący to kontrolka (np. Grid)
        if (sender is Control control && control.DataContext is SensorItemViewModel vm)
        {
            // Pobieramy pozycję myszy względem tego Grida/Wykresu
            var point = e.GetPosition(control);
            // Przekazujemy X oraz szerokość kontrolki do ViewModela
            vm.ShowHistoryAt(point.X, control.Bounds.Width);
        }
    }

    private void Graph_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control control && control.DataContext is SensorItemViewModel vm)
        {
            // Mysz uciekła z wykresu -> przywracamy normalny stan
            vm.StopHovering();
        }
    }

    // --- OBSŁUGA LOGOWANIA (FILE PICKER) ---

    // Handler dla zaznaczenia checkboxa
    private async void LogCheckBox_Checked(object? sender, RoutedEventArgs e)
    {
        var checkbox = sender as CheckBox;
        
        // DataContext UserControla jest dziedziczony z Window, więc to MainWindowViewModel
        if (DataContext is MainWindowViewModel vm)
        {
            // Jeśli logicznie logowanie jeszcze nie jest aktywne (bo dopiero kliknęliśmy),
            // to uruchamiamy procedurę wyboru pliku.
            if (!vm.IsLogEnabled)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                // 1. Otwórz Dialog Zapisywania Pliku
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Sensor Log",
                    SuggestedFileName = "GPU-T Sensor Log.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("Text Files") { Patterns = new[] { "*.txt" } },
                        new("All Files") { Patterns = new[] { "*.*" } }
                    }
                });

                if (file != null)
                {
                    // 2. Użytkownik wybrał plik -> Przekazujemy ścieżkę do ViewModela
                    string path = file.Path.LocalPath;
                    vm.StartLogging(path);
                }
                else
                {
                    // 3. Użytkownik anulował (X lub Cancel) -> Odznaczamy checkbox
                    if (checkbox != null) checkbox.IsChecked = false;
                }
            }
        }
    }

    // Handler dla odznaczenia checkboxa
    private void LogCheckBox_Unchecked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StopLogging();
        }
    }
}