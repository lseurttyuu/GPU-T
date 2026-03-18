namespace GPU_T.Models;

public class UserSettings
{
    // Checkbox state to ignore warnings about missing tools (e.g., vulkaninfo missing)
    public bool IgnoreExecWarning { get; set; } = false;
}