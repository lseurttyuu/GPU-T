using CommunityToolkit.Mvvm.ComponentModel;

namespace GPU_T.ViewModels;

public partial class AdvancedItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isHeader;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _value = "";
    [ObservableProperty] private bool _isAlternate;

    public AdvancedItemViewModel(string name, string value, bool isHeader, bool isAlternate)
    {
        IsHeader = isHeader;
        Name = name;
        Value = value;
        _isAlternate=isAlternate;
    }
}