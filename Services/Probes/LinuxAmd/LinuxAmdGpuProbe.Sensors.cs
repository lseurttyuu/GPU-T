using System;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using GPU_T.Models;

namespace GPU_T.Services.Probes.LinuxAmd;

public partial class LinuxAmdGpuProbe
{
    public SensorAvailability GetSensorAvailability()
    {
        var avail = new SensorAvailability();

        if (Directory.Exists(_hwmonPath))
        {
            for (int i = 1; i <= 4; i++)
            {
                string labelPath = Path.Combine(_hwmonPath, $"temp{i}_label");
                if (File.Exists(labelPath))
                {
                    string label = File.ReadAllText(labelPath).Trim().ToLower();
                    if (label.Contains("junction") || label.Contains("hotspot")) avail.HasHotSpot = true;
                    if (label.Contains("mem")) avail.HasMemTemp = true;
                }
            }

            if (File.Exists(Path.Combine(_hwmonPath, "fan1_input"))) avail.HasFan = true;
            if (File.Exists(Path.Combine(_hwmonPath, "power1_average")) || 
                File.Exists(Path.Combine(_hwmonPath, "power1_input"))) avail.HasPower = true;
            if (File.Exists(Path.Combine(_hwmonPath, "in0_input"))) avail.HasVoltage = true;
        }

        if (File.Exists(Path.Combine(_basePath, "gpu_busy_percent"))) avail.HasGpuLoad = true;
        if (File.Exists(Path.Combine(_basePath, "mem_busy_percent"))) avail.HasMemControllerLoad = true;
        if (File.Exists(Path.Combine(_basePath, "mem_info_vram_used"))) avail.HasMemUsed = true;

        return avail;
    }

    public GpuSensorData LoadSensorData()
    {
        double coreClk = ReadFreq("freq1", "sclk"); 
        double memClk  = ReadFreq("freq2", "mclk")*_memClockMultiplier; 

        if (coreClk == 0) coreClk = ParseClock(GetCurrentClock("pp_dpm_sclk"));
        if (memClk == 0)  memClk  = ParseClock(GetCurrentClock("pp_dpm_mclk"))*_memClockMultiplier; 

        double tEdge = 0, tSpot = 0, tMem = 0;
        for (int i = 1; i <= 3; i++)
        {
            string label = ReadFileFromHwmon($"temp{i}_label", "").ToLower();
            double val = ReadHwmonDouble($"temp{i}_input") / 1000.0;

            if (label.Contains("edge") || label == "") tEdge = val;
            else if (label.Contains("junction") || label.Contains("hotspot")) tSpot = val;
            else if (label.Contains("mem")) tMem = val;
        }
        if (tEdge == 0) tEdge = ReadHwmonDouble("temp1_input") / 1000.0;

        int fanRpm = (int)ReadHwmonDouble("fan1_input");
        int fanPct = 0;
        double pwmNow = ReadHwmonDouble("pwm1");
        double pwmMax = ReadHwmonDouble("pwm1_max");
        if (pwmMax > 0) fanPct = (int)((pwmNow / pwmMax) * 100.0);

        double powerW = ReadHwmonDouble("power1_average");
        if (powerW == 0) powerW = ReadHwmonDouble("power1_input");
        powerW /= 1000000.0;

        double voltage = ReadHwmonDouble("in0_input") / 1000.0;

        int load = 0;
        int.TryParse(ReadFile("gpu_busy_percent", "0"), out load);

        double memUsedMb = 0;
        if (long.TryParse(ReadFile("mem_info_vram_used", "0"), out long memBytes))
            memUsedMb = memBytes / (1024.0 * 1024.0);

        int memLoad = 0;
        int.TryParse(ReadFile("mem_busy_percent", "0"), out memLoad);

        double memGttMb = 0;
        if (long.TryParse(ReadFile("mem_info_gtt_used", "0"), out long gttBytes))
            memGttMb = gttBytes / (1024.0 * 1024.0);

        double cpuTemp = GetCpuTemperature();
        double sysRam = GetSystemRamUsage();

        return new GpuSensorData
        {
            GpuClock = coreClk,
            MemoryClock = memClk,
            GpuTemp = tEdge,
            GpuHotSpot = tSpot,
            MemoryTemp = tMem,
            FanRpm = fanRpm,
            FanPercent = fanPct,
            BoardPower = powerW,
            GpuLoad = load,
            MemoryUsed = memUsedMb,
            GpuVoltage = voltage,
            MemControllerLoad = memLoad,
            MemoryUsedDynamic = memGttMb,
            CpuTemperature = cpuTemp,
            SystemRamUsed = sysRam
        };
    }

    private double GetCpuTemperature()
    {
        try
        {
            var baseDir = "/sys/class/hwmon/";
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    string namePath = Path.Combine(dir, "name");
                    if (File.Exists(namePath))
                    {
                        string name = File.ReadAllText(namePath).Trim();
                        if (name == "k10temp" || name == "coretemp")
                        {
                            string tempPath = Path.Combine(dir, "temp1_input");
                            if (File.Exists(tempPath))
                            {
                                if (double.TryParse(File.ReadAllText(tempPath), out double val))
                                    return val / 1000.0;
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    private double GetSystemRamUsage()
    {
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                string[] lines = File.ReadAllLines("/proc/meminfo");
                double total = 0;
                double avail = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:")) total = ExtractKb(line);
                    else if (line.StartsWith("MemAvailable:")) avail = ExtractKb(line);
                    if (total > 0 && avail > 0) break;
                }
                return (total - avail) / 1024.0;
            }
        }
        catch { }
        return 0;
    }

    private double ExtractKb(string line)
    {
        var match = Regex.Match(line, @"(\d+)");
        if (match.Success && double.TryParse(match.Value, out double val)) return val;
        return 0;
    }

    private double ReadFreq(string prefix, string expectedLabelContent)
    {
        string label = ReadFileFromHwmon($"{prefix}_label", "").ToLower();
        if (string.IsNullOrEmpty(label) || label.Contains(expectedLabelContent))
        {
            return ReadHwmonDouble($"{prefix}_input") / 1000000.0;
        }
        return 0;
    }

    private string ReadFileFromHwmon(string filename, string fallback)
    {
        if (string.IsNullOrEmpty(_hwmonPath)) return fallback;
        try {
            string p = Path.Combine(_hwmonPath, filename);
            return File.Exists(p) ? File.ReadAllText(p).Trim() : fallback;
        } catch { return fallback; }
    }

    private double ReadHwmonDouble(string filename)
    {
        if (string.IsNullOrEmpty(_hwmonPath)) return 0;
        try
        {
            string path = Path.Combine(_hwmonPath, filename);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    return val;
            }
        }
        catch { }
        return 0;
    }

    private string GetCurrentClock(string fileName)
    {
        try
        {
            string path = Path.Combine(_basePath, fileName);
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (line.Contains("*"))
                    {
                        var match = Regex.Match(line, @"(\d+)Mhz");
                        if (match.Success) return $"{match.Groups[1].Value} MHz";
                    }
                }
            }
        }
        catch {}
        return "0 MHz";
    }
}