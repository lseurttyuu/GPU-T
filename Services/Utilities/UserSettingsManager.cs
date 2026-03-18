using System;
using System.IO;
using System.Text.Json;
using GPU_T.Models;

namespace GPU_T.Services;

public static class UserSettingsManager
{
    private const string ConfigFolderName = "GPU-T";
    private const string ConfigFileName = "gpu_t_settings.json";

    private static string GetConfigPath()
    {
        // Resolves to ~/.config/GPU-T/gpu_t_settings.json on Linux systems
        string configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            ConfigFolderName);
        
        // Ensure the configuration directory exists before trying to access the file
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        return Path.Combine(configDir, ConfigFileName);
    }

    public static UserSettings LoadSettings()
    {
        string path = GetConfigPath();
        
        if (!File.Exists(path))
        {
            // If the configuration file doesn't exist yet, return fresh default settings
            return new UserSettings();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            // In case of any deserialization error (e.g., corrupted JSON file), fallback to defaults
            return new UserSettings();
        }
    }

    public static void SaveSettings(UserSettings settings)
    {
        try
        {
            string path = GetConfigPath();
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true // Makes the JSON human-readable
            });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Suppress write errors for now (could be logged in future implementations)
        }
    }
}