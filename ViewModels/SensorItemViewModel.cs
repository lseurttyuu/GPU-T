using CommunityToolkit.Mvvm.ComponentModel;

namespace GPU_T.ViewModels;

public partial class SensorItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _value;
    [ObservableProperty] private string _unit;
    
    // W przyszłości tutaj dodamy kolekcję punktów do wykresu (np. List<double>)
    
    public SensorItemViewModel(string name, string unit, string initialValue = "0")
    {
        _name = name;
        _unit = unit;
        _value = initialValue;
    }

    public void UpdateValue(string newValue)
    {
        Value = newValue;
    }
}