using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GPU_T.ViewModels;
using System.Collections.Generic;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace GPU_T.Views;

public partial class MainWindow : Window
{
    private bool _isResizing;
    private Point _startMousePosition; 
    private double _startHeight;       // NOWE POLE: Wysokość okna w momencie kliknięcia
    
    
    public MainWindow()
    {
        InitializeComponent();
        
        
    }

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

  

    // --- LOGIKA RESIZINGU (NIEWIDZIALNA BELKA) ---

    private void ResizeBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isResizing = true;

            // 1. Zapamiętujemy stan POCZĄTKOWY (Start)
            // Pobieramy pozycję absolutną na ekranie
            var relativePoint = e.GetPosition(this);
            var screenPoint = this.PointToScreen(relativePoint);

            _startMousePosition = new Point(screenPoint.X, screenPoint.Y);
            _startHeight = this.Height; // Zapamiętujemy wysokość w momencie kliknięcia

            e.Pointer.Capture(sender as Control);
        }
    }

    private void ResizeBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing) return;

        // 1. Pobieramy obecną pozycję ekranową
        var relativePoint = e.GetPosition(this);
        var screenPoint = this.PointToScreen(relativePoint);

        // 2. Obliczamy CAŁKOWITE przesunięcie od momentu kliknięcia (Total Delta)
        // Dzięki temu nie kumulujemy błędów z poprzednich klatek
        var totalDeltaYPhysical = screenPoint.Y - _startMousePosition.Y;

        // 3. Konwertujemy na jednostki logiczne (DPI)
        var totalDeltaYLogical = totalDeltaYPhysical / RenderScaling;

        // 4. Obliczamy nową wysokość bazując na wysokości STARTOWEJ
        var newHeight = _startHeight + totalDeltaYLogical;

        // Ograniczenia
        if (newHeight > 400)
        {
            this.Height = newHeight;
        }
    }

    private void ResizeBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Pointer.Capture(null);
    }


    // Handler dla zaznaczenia checkboxa
    private async void LogCheckBox_Checked(object? sender, RoutedEventArgs e)
    {
        var checkbox = sender as CheckBox;
        if (DataContext is MainWindowViewModel vm)
        {
            // Jeśli logicznie logowanie jeszcze nie jest aktywne (bo dopiero kliknęliśmy),
            // to uruchamiamy procedurę wyboru pliku.
            // Jeśli vm.IsLogEnabled jest już true (np. przywrócone ze stanu), to pomijamy.
            if (!vm.IsLogEnabled) 
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                // 1. Otwórz Dialog Zapisywania Pliku
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Sensor Log",
                    SuggestedFileName = "GPU-T Sensor Log.txt", // Nasza domyślna nazwa
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
                    // Musimy to zrobić, bo checkbox "zaznaczył się" samym kliknięciem.
                    checkbox.IsChecked = false;
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