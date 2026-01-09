using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Platform;

namespace GPU_T.Services;

public static class DatabaseManager
{
    // Ścieżka do folderu użytkownika: ~/.local/share/GPU-T/
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "GPU-T");

    private static readonly string DbPath = Path.Combine(UserDataFolder, "gpu_db.json");
    private static readonly string HashPath = Path.Combine(UserDataFolder, "gpu_db.hash");

    // Tutaj trzymamy załadowaną bazę
    public static GpuDatabaseRoot Database { get; private set; } = new();

    public static void Initialize()
    {
        try
        {
            // 1. Upewnij się, że folder istnieje
            if (!Directory.Exists(UserDataFolder))
                Directory.CreateDirectory(UserDataFolder);

            // 2. Pobierz treść WBUDOWANEJ bazy (jako string)
            string internalJson = ReadInternalResource("avares://GPU-T/Assets/gpu_db.json");
            string internalHash = ComputeHash(internalJson);

            // 3. Sprawdź plik zewnętrzny
            if (!File.Exists(DbPath))
            {
                // Scenariusz: Pierwsze uruchomienie. Zapisz plik i hash.
                File.WriteAllText(DbPath, internalJson);
                File.WriteAllText(HashPath, internalHash);
                LoadDatabase(internalJson);
            }
            else
            {
                // Scenariusz: Plik istnieje. Sprawdzamy czy użytkownik modyfikował.
                string externalJson = File.ReadAllText(DbPath);
                string currentExternalHash = ComputeHash(externalJson);
                
                string originalHash = File.Exists(HashPath) ? File.ReadAllText(HashPath) : "";

                if (currentExternalHash == originalHash)
                {
                    // Użytkownik NIE dotykał pliku.
                    // Sprawdzamy czy my mamy nowszą wersję w aplikacji.
                    // (Dla uproszczenia: jeśli hash wewnętrzny jest inny niż zewnętrzny, to znaczy że zrobiliśmy update aplikacji)
                    if (internalHash != currentExternalHash)
                    {
                        // Update aplikacji! Nadpisujemy bezpiecznie.
                        File.WriteAllText(DbPath, internalJson);
                        File.WriteAllText(HashPath, internalHash);
                        LoadDatabase(internalJson);
                    }
                    else
                    {
                        // Wersje identyczne. Ładujemy z dysku.
                        LoadDatabase(externalJson);
                    }
                }
                else
                {
                    // Użytkownik ZMODYFIKOWAŁ plik (Hashe się różnią).
                    // Tutaj powinniśmy wyświetlić okno dialogowe.
                    // Na potrzeby backendu zrobimy "Safe Update":
                    // Robimy backup pliku użytkownika, a ładujemy nasz nowy (żeby nie popsuć appki).
                    // W wersji finalnej tu dodasz logikę "Ask User".
                    
                    // Spróbujmy załadować plik użytkownika. Jeśli jest poprawnym JSONem, użyjmy go.
                    try 
                    {
                        LoadDatabase(externalJson);
                        // Jeśli się udało, to znaczy że user ma swoją customową bazę. Zostawiamy ją!
                        // (Opcjonalnie: można sprawdzić wersję i zapytać o update)
                    }
                    catch
                    {
                        // Plik użytkownika uszkodzony? Wracamy do default.
                        File.WriteAllText(DbPath + ".bak", externalJson); // Backup
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
            // Fallback w razie totalnej katastrofy
            Database = new GpuDatabaseRoot(); 
        }
    }

    private static void LoadDatabase(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Database = JsonSerializer.Deserialize<GpuDatabaseRoot>(json, options) ?? new GpuDatabaseRoot();
    }

private static string ReadInternalResource(string uri)
    {
        // Nowy sposób dla Avalonia 11+
        // Wymaga: using Avalonia.Platform;
        
        // AssetLoader.Open zwraca Stream, więc używamy 'using'
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