using System;
using System.Diagnostics;

namespace GPU_T.Services;

public static class ShellHelper
{
    public static string RunCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Ignorujemy błędy, żeby nie śmiecić
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            // Czytamy wynik. Czekamy max 1000ms, żeby nie zawiesić aplikacji
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000); 
            
            return output.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    // Dodaj to do klasy ShellHelper
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { /* Ignorujemy błędy otwarcia */ }
    }
}