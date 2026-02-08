using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media; // Potrzebne do IBrush i SolidColorBrush

namespace GPU_T.ViewModels;

public partial class AdvancedItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isHeader;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _value = "";
    
    [ObservableProperty] private IBrush _background;

    public AdvancedItemViewModel(string name, string value, bool isHeader, string hexColor)
    {
        IsHeader = isHeader;
        Name = name;
        Value = value;
        
        Background = Brush.Parse(hexColor);
    }
}