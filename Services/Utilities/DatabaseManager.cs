using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Platform;
using GPU_T.Models;

namespace GPU_T.Services;

/// <summary>
/// Manages the GPU database lifecycle, including initialization, loading, and hash verification.
/// </summary>
public static class DatabaseManager
{
    /// <summary>
    /// Gets the path to the user data folder (~/.local/share/GPU-T/).
    /// </summary>
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "GPU-T");

    /// <summary>
    /// Gets the path to the database file.
    /// </summary>
    private static readonly string DbPath = Path.Combine(UserDataFolder, "gpu_db.json");

    /// <summary>
    /// Gets the path to the database hash file.
    /// </summary>
    private static readonly string HashPath = Path.Combine(UserDataFolder, "gpu_db.hash");

    /// <summary>
    /// Holds the loaded GPU database instance.
    /// </summary>
    public static GpuDatabaseRoot Database { get; private set; } = new();

    /// <summary>
    /// Initializes the database, handling internal/external file logic and user modifications.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Ensures user data folder exists.
            if (!Directory.Exists(UserDataFolder))
                Directory.CreateDirectory(UserDataFolder);

            // Loads internal database JSON and computes its hash.
            string internalJson = ReadInternalResource("avares://GPU-T/Assets/gpu_db.json");
            string internalHash = ComputeHash(internalJson);

            // Handles external database file scenarios.
            if (!File.Exists(DbPath))
            {
                // First launch: save internal database and hash.
                File.WriteAllText(DbPath, internalJson);
                File.WriteAllText(HashPath, internalHash);
                LoadDatabase(internalJson);
            }
            else
            {
                // Existing file: check for user modification.
                string externalJson = File.ReadAllText(DbPath);
                string currentExternalHash = ComputeHash(externalJson);
                
                string originalHash = File.Exists(HashPath) ? File.ReadAllText(HashPath) : "";

                if (currentExternalHash == originalHash)
                {
                    // User has not modified the file.
                    // If internal hash differs, update database for app upgrade.
                    if (internalHash != currentExternalHash)
                    {
                        File.WriteAllText(DbPath, internalJson);
                        File.WriteAllText(HashPath, internalHash);
                        LoadDatabase(internalJson);
                    }
                    else
                    {
                        LoadDatabase(externalJson);
                    }
                }
                else
                {
                    // User has modified the file.
                    // Attempts safe update and backup if user file is invalid.
                    try 
                    {
                        LoadDatabase(externalJson);
                    }
                    catch
                    {
                        File.WriteAllText(DbPath + ".bak", externalJson);
                        File.WriteAllText(DbPath, internalJson);
                        File.WriteAllText(HashPath, internalHash);
                        LoadDatabase(internalJson);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Database Error: {ex.Message}");
            // Fallback to empty database in case of unrecoverable error.
            Database = new GpuDatabaseRoot(); 
        }
    }

    /// <summary>
    /// Loads the database from a JSON string.
    /// </summary>
    /// <param name="json">The JSON content to deserialize.</param>
    private static void LoadDatabase(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Database = JsonSerializer.Deserialize<GpuDatabaseRoot>(json, options) ?? new GpuDatabaseRoot();
    }

    /// <summary>
    /// Reads an internal resource file using Avalonia AssetLoader.
    /// </summary>
    /// <param name="uri">The resource URI.</param>
    /// <returns>The file content as a string.</returns>
    private static string ReadInternalResource(string uri)
    {
        // Uses Avalonia AssetLoader to read embedded resources.
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Computes the MD5 hash of the provided content and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>The hexadecimal hash string.</returns>
    private static string ComputeHash(string content)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}