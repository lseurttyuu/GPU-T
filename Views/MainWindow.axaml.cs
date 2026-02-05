using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace GPU_T.Views;

public partial class MainWindow : Window
{
    private bool _isResizing;
    private Point _startMousePosition;
    private double _startHeight; // Wysokość okna w momencie kliknięcia

    public MainWindow()
    {
        InitializeComponent();
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
}