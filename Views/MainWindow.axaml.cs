using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace GPU_T.Views;

public partial class MainWindow : Window
{
    private bool _isResizing;
    private Point _lastMousePosition;

    public MainWindow()
    {
        InitializeComponent();
        
        // Logika zakładek (taka jak ustaliliśmy wcześniej)
        var tabs = this.FindControl<TabControl>("MainTabs");
        if (tabs != null)
        {
            tabs.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TabControl.SelectedIndexProperty)
                {
                    HandleTabChange(tabs.SelectedIndex);
                }
            };
            // Inicjalizacja stanu
            HandleTabChange(tabs.SelectedIndex);
        }
    }

    private void HandleTabChange(int newIndex)
    {
        if (newIndex == 0) // Graphics Card
        {
            CanResize = false;
            SizeToContent = SizeToContent.Height;
            Width = 385;
        }
        else if (newIndex == 1) // Sensors
        {
            SizeToContent = SizeToContent.Manual;
            // CanResize zostawiamy na FALSE, bo obsługujemy resize sami naszą belką!
            CanResize = false; 

            if (Height < 600) Height = 650;
        }
        else // Advanced etc.
        {
            SizeToContent = SizeToContent.Manual;
            CanResize = false; 
        }
    }

    // --- LOGIKA RESIZINGU (NIEWIDZIALNA BELKA) ---

    private void ResizeBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isResizing = true;
            _lastMousePosition = e.GetPosition(this);
            e.Pointer.Capture(sender as Control); // Łapiemy kursor, żeby nie uciekł przy szybkim ruchu
        }
    }

    private void ResizeBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing) return;

        var currentPosition = e.GetPosition(this);
        var deltaY = currentPosition.Y - _lastMousePosition.Y;

        var newHeight = this.Height + deltaY;

        // Ograniczenia: Min 400px, Max np. wysokość ekranu (choć system sam ograniczy)
        if (newHeight > 400)
        {
            this.Height = newHeight;
        }

        _lastMousePosition = currentPosition;
    }

    private void ResizeBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Pointer.Capture(null); // Puszczamy kursor
    }
}