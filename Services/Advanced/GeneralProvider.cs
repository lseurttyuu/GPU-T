using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced;

public class GeneralProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();

        // 1. System
        AddRow(list, "System", "", true);
        string kernel = "Unknown";
        try { kernel = File.ReadAllText("/proc/sys/kernel/osrelease").Trim(); } catch {}
        AddRow(list, "Kernel Version", kernel);

        string session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "Unknown";
        AddRow(list, "Display Server", session.ToUpper());

        string desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "Unknown";
        AddRow(list, "Desktop Environment", desktop);

        // 2. Drivers
        AddRow(list, "Graphics Drivers", "", true);
        string driverModule = "Unknown";
        try 
        {
            var driverPath = $"/sys/class/drm/{selectedGpu?.Id ?? "card0"}/device/driver";
            var dirInfo = new DirectoryInfo(driverPath);
            if (dirInfo.Exists)
            {
                var target = dirInfo.ResolveLinkTarget(true); 
                driverModule = target != null ? target.Name : dirInfo.Name;
            }
        } 
        catch {}
        AddRow(list, "Kernel Driver", driverModule);

        // OpenGL
        CheckOpengl(list);

        // 3. Firmware
        AddRow(list, "Firmware", "", true);
        string fwDirPath = $"/sys/class/drm/{selectedGpu?.Id ?? "card0"}/device/fw_version";
        
        if (Directory.Exists(fwDirPath))
        {
            try
            {
                var files = Directory.GetFiles(fwDirPath, "*_fw_version");
                Array.Sort(files);
                foreach (var filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string shortName = fileName.Replace("_fw_version", "").ToUpper();
                    string version = File.ReadAllText(filePath).Trim();
                    AddRow(list, shortName, version);
                }
                if (files.Length == 0) AddRow(list, "Info", "No firmware files found");
            }
            catch (Exception ex) { AddRow(list, "Error", ex.Message); }
        }
        else
        {
             if (File.Exists(fwDirPath)) AddRow(list, "Legacy Info", "Old kernel format detected");
             else AddRow(list, "Firmware Info", "Not available");
        }
    }

    private void CheckOpengl(ObservableCollection<AdvancedItemViewModel> list)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "glxinfo", Arguments = "-B", 
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string glVersion = "", mesaVersion = "", renderer = "", directRendering = "No";
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    string t = line.Trim();
                    if (t.StartsWith("direct rendering:")) directRendering = t.Contains("Yes") ? "Yes" : "No";
                    else if (t.StartsWith("OpenGL core profile version string:"))
                    {
                        glVersion = t.Replace("OpenGL core profile version string:", "").Trim().Split(' ')[0];
                        int mesaIndex = t.IndexOf("Mesa");
                        if (mesaIndex != -1) mesaVersion = t.Substring(mesaIndex).Replace("Mesa", "").Trim();
                    }
                    else if (t.StartsWith("OpenGL renderer string:")) renderer = t.Replace("OpenGL renderer string:", "").Trim();
                }

                AddRow(list, "OpenGL Version", glVersion);
                AddRow(list, "Mesa Version", mesaVersion);
                AddRow(list, "Direct Rendering", directRendering);

                if (renderer.Contains("LLVM"))
                {
                    var match = Regex.Match(renderer, @"LLVM\s+([\d\.]+)");
                    if (match.Success) AddRow(list, "LLVM Version", match.Groups[1].Value);
                }
                if (renderer.Contains("llvmpipe")) AddRow(list, "Warning", "Software Rendering (llvmpipe) detected!");
            }
            else AddRow(list, "OpenGL Info", "Failed to start glxinfo");
        }
        catch { AddRow(list, "OpenGL Info", "Not available ('glxinfo' missing?)"); }
    }
}