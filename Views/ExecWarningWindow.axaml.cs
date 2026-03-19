using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using GPU_T.Services;
using GPU_T.Models;

namespace GPU_T.Views;

public partial class ExecWarningWindow : Window
{
    public ExecWarningWindow()
    {
        InitializeComponent();
    }

    // Overloaded constructor accepting the list of missing tools
    public ExecWarningWindow(List<string> missingTools) : this()
    {
        MissingToolsList.ItemsSource = missingTools;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        // Check the checkbox state to see if we should ignore this warning on future launches
        bool shouldIgnoreNextTime = DoNotShowCheckBox.IsChecked ?? false;

        if (shouldIgnoreNextTime)
        {
            // Load existing settings to avoid overwriting other potential configurations
            UserSettings settings = UserSettingsManager.LoadSettings();

            // Flag the warning as ignored and save back to disk
            settings.IgnoreExecWarning = true;
            UserSettingsManager.SaveSettings(settings);
        }

        // Close the dialog window
        Close();
    }
}