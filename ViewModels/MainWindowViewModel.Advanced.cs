using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GPU_T.Services.Advanced;
using GPU_T.Services;

namespace GPU_T.ViewModels;

/// <summary>
/// ViewModel partial responsible for exposing and loading advanced GPU information categories
/// and their corresponding UI items.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Collection of available advanced categories presented in the UI.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _advancedCategories = new()
    {
        "General",
        "Vulkan",
        "OpenCL"
    };

    /// <summary>
    /// Currently selected advanced category; changing this triggers reloading of items.
    /// </summary>
    [ObservableProperty] private string _selectedAdvancedCategory = "General";

    /// <summary>
    /// Items displayed for the currently selected advanced category.
    /// This backing field is used by the source generator to create a public property.
    /// </summary>
    [ObservableProperty] private ObservableCollection<AdvancedItemViewModel> _advancedItems = new();

    /// <summary>
    /// Invoked by the generated property when the selected advanced category changes.
    /// </summary>
    /// <param name="value">The new selected category value.</param>
    partial void OnSelectedAdvancedCategoryChanged(string value)
    {
        LoadAdvancedData(value);
    }

    /// <summary>
    /// Populates the advanced items list for the provided category.
    /// </summary>
    /// <param name="category">The advanced category to load (for example, "General" or "Vulkan").</param>
    private void LoadAdvancedData(string category)
    {
        var list = new ObservableCollection<AdvancedItemViewModel>();

        if (_selectedGpu != null)
        {
            // Obtain a vendor-aware probe implementation for the selected GPU using the factory.
            // This ensures downstream provider resolution is handled by the probe implementation
            // which encapsulates vendor-specific selection logic.
            IGpuProbe probe = GpuProbeFactory.Create(_selectedGpu.Id);

            // Ask the probe for a provider responsible for the requested category.
            // The probe decides which concrete AdvancedDataProvider to return (or null).
            AdvancedDataProvider? provider = probe.GetAdvancedDataProvider(category);

            if (provider != null)
            {
                provider.LoadData(list, _selectedGpu);
            }
            else
            {
                list.Add(new AdvancedItemViewModel("Info", "", true, false));
                list.Add(new AdvancedItemViewModel("Status", "Not implemented", false, true));
            }
        }

        AdvancedItems = list;
    }
}