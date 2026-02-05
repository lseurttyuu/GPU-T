using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GPU_T.Services.Advanced;
using GPU_T.Services;

namespace GPU_T.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] 
    private ObservableCollection<string> _advancedCategories = new()
    {
        "General",
        "Vulkan",
        "OpenCL",
        "Multimedia (VA-API)",
        "Power & Limits",
        "PCIe Resizable BAR"
    };

    [ObservableProperty] private string _selectedAdvancedCategory = "General";
    [ObservableProperty] private ObservableCollection<AdvancedItemViewModel> _advancedItems = new();

    partial void OnSelectedAdvancedCategoryChanged(string value)
    {
        LoadAdvancedData(value);
    }

    private void LoadAdvancedData(string category)
    {
        var list = new ObservableCollection<AdvancedItemViewModel>();
        
        if (_selectedGpu != null)
        {
            // 1. Tworzymy sondę dla wybranej karty (Factory wie, czy to AMD czy Nvidia)
            IGpuProbe probe = GpuProbeFactory.Create(_selectedGpu.Id);

            // 2. Pytamy sondę o providera dla danej kategorii
            // To Sonda decyduje: "Dla Power & Limits użyj PowerProvider (bo jestem AMD)"
            AdvancedDataProvider? provider = probe.GetAdvancedDataProvider(category);

            if (provider != null)
            {
                provider.LoadData(list, _selectedGpu);
            }
            else
            {
                list.Add(new AdvancedItemViewModel("Info", "", true, "#FFFFFF"));
                list.Add(new AdvancedItemViewModel("Status", "Not implemented", false, "#F4F4F4"));
            }
        }


        AdvancedItems = list;
    }
}