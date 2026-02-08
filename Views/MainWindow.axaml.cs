using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace GPU_T.Views;

/// <summary>
/// Interaction logic for MainWindow that provides a custom invisible resize bar handling.
/// </summary>
/// <remarks>
/// Resizing captures the pointer to ensure continuous input during drag, uses screen coordinates
/// as a stable reference, and converts physical pixel deltas to Avalonia logical units to account
/// for display scaling and avoid cumulative rounding errors.
/// </remarks>
public partial class MainWindow : Window
{
    private bool _isResizing;
    private Point _startMousePosition;
    private double _startHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ResizeBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isResizing = true;

            // Capture absolute screen coordinates to establish a stable reference for delta calculations.
            var relativePoint = e.GetPosition(this);
            var screenPoint = this.PointToScreen(relativePoint);

            _startMousePosition = new Point(screenPoint.X, screenPoint.Y);
            _startHeight = this.Height;

            // Capture the pointer on the initiating control so subsequent pointer events are delivered during drag.
            e.Pointer.Capture(sender as Control);
        }
    }

    private void ResizeBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing) return;

        // Compute movement in physical pixels relative to the initially captured screen position.
        var relativePoint = e.GetPosition(this);
        var screenPoint = this.PointToScreen(relativePoint);

        var totalDeltaYPhysical = screenPoint.Y - _startMousePosition.Y;

        // Convert physical pixel delta to Avalonia logical units using RenderScaling to handle DPI.
        var totalDeltaYLogical = totalDeltaYPhysical / RenderScaling;

        // Apply the delta to the stored starting height to avoid accumulating incremental errors.
        var newHeight = _startHeight + totalDeltaYLogical;

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