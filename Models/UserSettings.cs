namespace GPU_T.Models;


public enum AppThemeMode
{
    Auto = 0,
    Dark = 1,
    Light = 2
}

public class UserSettings
{
    // Checkbox state to ignore warnings about missing tools (e.g., vulkaninfo missing) - multi-vendor supported
    public bool IgnoreExecWarning { get; set; } = false;
    public bool IgnoreExecWarning_AMD { get; set; } = false;
    public bool IgnoreExecWarning_NVIDIA { get; set; } = false;

    // Stores the user's theme preference. Defaults to Auto.
    public AppThemeMode Theme { get; set; } = AppThemeMode.Auto;

    public double LastSensorWindowHeight { get; set; } = 525 - 1; // Default height for the sensor window

}