using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using GPU_T.Services;
using GPU_T.Models;

namespace GPU_T.Views;

public partial class ExecWarningWindow : Window
{
    // Store the list locally so we know what was shown when the user clicks OK
    private readonly List<string> _missingToolsShown = new();

    public ExecWarningWindow()
    {
        InitializeComponent();
    }

    // Overloaded constructor accepting the list of missing tools
    public ExecWarningWindow(List<string> missingTools) : this()
    {
        _missingToolsShown = missingTools;
        MissingToolsList.ItemsSource = _missingToolsShown;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        // Check the checkbox state to see if we should ignore this warning on future launches
        bool shouldIgnoreNextTime = DoNotShowCheckBox.IsChecked ?? false;

        if (shouldIgnoreNextTime)
        {
            // Load existing settings to avoid overwriting other potential configurations
            UserSettings settings = UserSettingsManager.LoadSettings();

            // Delegate the logic to the ExecChecker to selectively flip the correct flags
            ExecChecker.ApplyIgnoreFlags(_missingToolsShown, settings);
            
            // Save the updated settings back to disk
            UserSettingsManager.SaveSettings(settings);
        }

        // Close the dialog window
        Close();
    }
}