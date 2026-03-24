using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GPU_T.Services;
using GPU_T.Models;
using Avalonia.Input;
using Avalonia.Styling;
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
    private UserSettings _currentSettings;

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
        // Read user settings
        _currentSettings = UserSettingsManager.LoadSettings();

        // Apply the saved theme preference on startup
        ApplyThemeState(_currentSettings.Theme);

        // If user has chosen to ignore warnings, we skip the check entirely.
        if (_currentSettings.IgnoreExecWarning)
            return;

        // Check the missing tools
        List<string> missingTools = ExecChecker.GetMissingTools();

        // Show the warning dialog if necessary
        if (missingTools.Count > 0)
        {
            var warningDialog = new ExecWarningWindow(missingTools);
            
            warningDialog.Show();
        }
    }



    private void ThemeToggleButton_Click(object? sender, RoutedEventArgs e)
    {
        // Rotate the theme state: Auto(0) -> Dark(1) -> Light(2) -> Auto(0)
        int nextThemeIndex = ((int)_currentSettings.Theme + 1) % 3;
        _currentSettings.Theme = (AppThemeMode)nextThemeIndex;

        // Apply visual changes and OS theme hook
        ApplyThemeState(_currentSettings.Theme);

        // Persist the choice to JSON
        UserSettingsManager.SaveSettings(_currentSettings);
    }

    private void ApplyThemeState(AppThemeMode mode)
    {
        // Safety check to ensure Application.Current exists
        if (Application.Current == null) return;

        switch (mode)
        {
            case AppThemeMode.Auto:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                ThemeIcon.Text = "🌗";
                ThemeLetter.Text = "A";
                break;

            case AppThemeMode.Dark:
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                ThemeIcon.Text = "🌕";
                ThemeLetter.Text = "D";
                break;

            case AppThemeMode.Light:
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                ThemeIcon.Text = "🌕";
                ThemeLetter.Text = "L";
                break;
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