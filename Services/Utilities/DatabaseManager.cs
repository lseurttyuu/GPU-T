using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Platform;
using GPU_T.Models;

namespace GPU_T.Services;

/// <summary>
/// Manages the GPU database lifecycle, dynamically loading only the required vendor databases based on present hardware.
/// </summary>
public static class DatabaseManager
{
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "GPU-T");

    /// <summary>
    /// Maps PCI Vendor IDs to their respective database filenames.
    /// </summary>
    private static readonly Dictionary<string, string> VendorDatabaseMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "0x1002", "amd_gpu_db.json" },    // AMD
        { "0x1022", "amd_gpu_db.json" },    // AMD (Alternative)
        { "0x10de", "nvidia_gpu_db.json" }, // NVIDIA
        { "0x8086", "intel_gpu_db.json" }   // Intel
    };

    /// <summary>
    /// Holds the unified, aggregated GPU database instance for the application to query.
    /// </summary>
    public static GpuDatabaseRoot Database { get; private set; } = new();

    /// <summary>
    /// Holds the  Max-Q specific GPU database (Nvidia RTX 3000+) (compact database).
    /// </summary>
    public static Dictionary<string, GpuSpecDto> MaxqGpus { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the database, scans PCI bus for vendors, and loads only the necessary JSON files.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            if (!Directory.Exists(UserDataFolder))
                Directory.CreateDirectory(UserDataFolder);

            // 1. Always load the Vendors DB (lightweight, resolves subvendors for all cards)
            ProcessDatabaseFile<GpuDatabaseRoot>("gpu_vendors_db.json", loadedDb => 
            {
                foreach (var kvp in loadedDb.Vendors)
                {
                    Database.Vendors[kvp.Key] = kvp.Value;
                }
            });

            // 2. Fast pre-scan of the PCI bus to see which GPU vendors actually exist on this machine
            HashSet<string> presentVendors = ScanForPresentGpuVendors();
            
            // 3. Determine which database files need to be loaded
            HashSet<string> filesToLoad = new();
            foreach (var vendor in presentVendors)
            {
                if (VendorDatabaseMap.TryGetValue(vendor, out string? fileName))
                {
                    filesToLoad.Add(fileName);
                }
            }

            // 4. Load the required massive GPU databases
            foreach (var fileName in filesToLoad)
            {
                ProcessDatabaseFile<GpuDatabaseRoot>(fileName, MergeGpusIntoMaster);
            }

            // 5. Special handling for Max-Q database since it's a different structure and only relevant for Nvidia GPUs
            if(presentVendors.Contains("0x10de"))
            {
                // Load the Max-Q database if an Nvidia GPU is present
                ProcessDatabaseFile<MaxqDatabaseRoot>("nvidia_maxq_gpu_db.json", loadedDb =>
                {
                    foreach (var kvp in loadedDb.Gpus)
                    {
                        MaxqGpus[kvp.Key] = kvp.Value;
                    }
                });
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical Database Initialization Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Merges the GPUs from a loaded vendor database into the master Database object.
    /// </summary>
    private static void MergeGpusIntoMaster(GpuDatabaseRoot vendorDb)
    {
        foreach (var kvp in vendorDb.Gpus)
        {
            Database.Gpus[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Abstracts the internal/external file logic, hashing, and safe-loading for any database file.
    /// </summary>
    private static void ProcessDatabaseFile<T>(string fileName, Action<T> mergeAction) where T : new()
    {
        string dbPath = Path.Combine(UserDataFolder, fileName);
        string hashPath = Path.Combine(UserDataFolder, fileName.Replace(".json", ".hash"));

        try
        {
            string internalJson = ReadInternalResource($"avares://GPU-T/Assets/{fileName}");
            string internalHash = ComputeHash(internalJson);

            if (!File.Exists(dbPath))
            {
                // First launch for this specific file
                File.WriteAllText(dbPath, internalJson);
                File.WriteAllText(hashPath, internalHash);
                mergeAction(JsonSerializer.Deserialize<T>(internalJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T());
            }
            else
            {
                // Existing file: check for user modification
                string externalJson = File.ReadAllText(dbPath);
                string currentExternalHash = ComputeHash(externalJson);
                string originalHash = File.Exists(hashPath) ? File.ReadAllText(hashPath) : "";

                if (currentExternalHash == originalHash)
                {
                    // User has not modified the file. Check if app update brought a newer internal file.
                    if (internalHash != currentExternalHash)
                    {
                        File.WriteAllText(dbPath, internalJson);
                        File.WriteAllText(hashPath, internalHash);
                        mergeAction(JsonSerializer.Deserialize<T>(internalJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T());
                    }
                    else
                    {
                        mergeAction(JsonSerializer.Deserialize<T>(externalJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T());
                    }
                }
                else
                {
                    // User has modified the file. Attempt safe load.
                    try 
                    {
                        mergeAction(JsonSerializer.Deserialize<T>(externalJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T());
                    }
                    catch
                    {
                        // Backup corrupted mod and overwrite with internal default
                        File.WriteAllText(dbPath + ".bak", externalJson);
                        File.WriteAllText(dbPath, internalJson);
                        File.WriteAllText(hashPath, internalHash);
                        mergeAction(JsonSerializer.Deserialize<T>(internalJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing database file '{fileName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a lightning-fast scan of the sysfs DRM subsystem to extract physical hardware vendor IDs.
    /// </summary>
    private static HashSet<string> ScanForPresentGpuVendors()
    {
        var vendors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            // We look at /sys/class/drm/card* because it perfectly isolates actual graphics adapters
            var cardDirs = Directory.GetDirectories("/sys/class/drm", "card*");
            foreach (var dir in cardDirs)
            {
                // Skip display connectors (like card0-DP-1), we only want the core GPU nodes (card0, card1)
                if (Path.GetFileName(dir).Contains("-")) continue;

                string vendorPath = Path.Combine(dir, "device", "vendor");
                if (File.Exists(vendorPath))
                {
                    // sysfs usually reports in lowercase like "0x1002"
                    string vendorId = File.ReadAllText(vendorPath).Trim().ToLowerInvariant();
                    vendors.Add(vendorId);
                }
            }
        }
        catch
        {
            // In case of restrictive permissions or weird OS configurations, fallback handled gracefully
        }
        return vendors;
    }

    /// <summary>
    /// Deserializes a JSON string into a GpuDatabaseRoot instance.
    /// </summary>
    private static GpuDatabaseRoot DeserializeJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GpuDatabaseRoot>(json, options) ?? new GpuDatabaseRoot();
    }

    private static string ReadInternalResource(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ComputeHash(string content)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}