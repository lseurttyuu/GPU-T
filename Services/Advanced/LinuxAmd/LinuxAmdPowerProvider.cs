using System;
using System.Collections.ObjectModel;
using System.IO;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced.LinuxAmd;

public class LinuxAmdPowerProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();
        
        try
        {
            string cardPath = $"/sys/class/drm/{selectedGpu?.Id ?? "card0"}/device";
            string hwmonPath = "";

            try 
            {
                var hwmonDirs = Directory.GetDirectories($"{cardPath}/hwmon");
                foreach (var dir in hwmonDirs)
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath) && File.ReadAllText(namePath).Trim() == "amdgpu")
                    {
                        hwmonPath = dir;
                        break;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(hwmonPath)) { AddRow(list, "Error", "Could not find AMDGPU hwmon directory"); return; }

            AddRow(list, "Power Configuration", "", true);
            string powerCap = ReadSysFs(Path.Combine(hwmonPath, "power1_cap"));
            string powerCapDefault = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_default"));
            string powerCapMin = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_min"));
            string powerCapMax = ReadSysFs(Path.Combine(hwmonPath, "power1_cap_max"));

            if (double.TryParse(powerCap, out double pCap)) AddRow(list, "Current Limit (TDP)", $"{pCap / 1000000.0:0.0} W");
            if (double.TryParse(powerCapDefault, out double pDef)) AddRow(list, "Default Limit", $"{pDef / 1000000.0:0.0} W");
            if (double.TryParse(powerCapMin, out double pMin) && double.TryParse(powerCapMax, out double pMax)) AddRow(list, "Allowed Range", $"{pMin / 1000000.0:0.0} W - {pMax / 1000000.0:0.0} W");

            AddRow(list, "Fan Control", "", true);
            string fanMode = ReadSysFs(Path.Combine(hwmonPath, "pwm1_enable"));
            AddRow(list, "Control Mode", fanMode == "1" ? "Manual" : (fanMode == "2" ? "Auto" : "Unknown"));

            string pwm = ReadSysFs(Path.Combine(hwmonPath, "pwm1"));
            string pwmMax = ReadSysFs(Path.Combine(hwmonPath, "pwm1_max"));
            if (double.TryParse(pwm, out double pwmVal) && double.TryParse(pwmMax, out double pwmMaxVal))
            {
                double percent = (pwmVal / pwmMaxVal) * 100.0;
                AddRow(list, "Current Signal (PWM)", $"{percent:0}% ({pwmVal}/{pwmMaxVal})");
            }
            
            string fanTarget = ReadSysFs(Path.Combine(hwmonPath, "fan1_target"));
            if (!string.IsNullOrEmpty(fanTarget)) AddRow(list, "Target RPM", $"{fanTarget} RPM");

            AddRow(list, "Power Profile", "", true);
            string profilePath = Path.Combine(cardPath, "pp_power_profile_mode");
            if (File.Exists(profilePath))
            {
                try
                {
                    var lines = File.ReadAllLines(profilePath);
                    foreach (var line in lines)
                    {
                        if (line.Contains("*"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2) AddRow(list, "Active Profile", parts[1].Replace("*", "").Replace(":", ""));
                        }
                    }
                }
                catch { AddRow(list, "Active Profile", "Error reading profiles"); }
            }
            else { AddRow(list, "Active Profile", "Not supported / file missing"); }

            string odPath = Path.Combine(cardPath, "pp_od_clk_voltage");
            if (File.Exists(odPath))
            {
                AddRow(list, "Overdrive Limits", "", true);
                try { var odLines = File.ReadAllLines(odPath); foreach (var l in odLines) AddRow(list, "OD Info", l); } catch {}
            }

            AddRow(list, "Driver Features (pp_features)", "", true);
            string featPath = Path.Combine(cardPath, "pp_features");
            if (File.Exists(featPath))
            {
                 try
                 {
                     var lines = File.ReadAllLines(featPath);
                     foreach (var line in lines)
                     {
                         string l = line.Trim();
                         if (string.IsNullOrWhiteSpace(l) || l.StartsWith("features high") || l.StartsWith("No. Feature")) continue;
                         var parts = l.Split(':');
                         if (parts.Length == 2)
                         {
                             string state = parts[1].Trim();
                             string leftSide = parts[0];
                             int dotIndex = leftSide.IndexOf('.');
                             int parenIndex = leftSide.IndexOf('(');
                             if (dotIndex != -1 && parenIndex > dotIndex) AddRow(list, leftSide.Substring(dotIndex + 1, parenIndex - dotIndex - 1).Trim(), state);
                             else AddRow(list, leftSide.Trim(), state);
                         }
                     }
                 }
                 catch (Exception ex) { AddRow(list, "Error", $"Features parsing error: {ex.Message}"); }
            }
            else { AddRow(list, "Features", "Not accessible (pp_features missing)"); }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"Power/Limits check failed: {ex.Message}");
        }
    }
}