using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GPU_T.Services;
using GPU_T.Models;
using Avalonia.Input;
using System.Collections.Generic;

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

        //check for missing tools and show warning if needed (post-init to ensure UI is ready)
        Loaded += MainWindow_Loaded;
    }


    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // 1. Read user settings to check if we should ignore warnings about missing tools.
        UserSettings settings = UserSettingsManager.LoadSettings();

        // 2. If user has chosen to ignore warnings, we skip the check entirely.
        if (settings.IgnoreExecWarning)
            return;

        // 3. Check the missing tools
        List<string> missingTools = ExecChecker.GetMissingTools();

        // 4. Show the warning dialog if necessary
        if (missingTools.Count > 0)
        {
            var warningDialog = new ExecWarningWindow(missingTools);
            
            warningDialog.Show();
        }
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