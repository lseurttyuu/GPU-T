using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced.LinuxIntel;

/// <summary>
/// Provides advanced multimedia codec and VA-API support information for Intel GPUs on Linux.
/// </summary>
public class LinuxIntelMultimediaProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {
        ResetCounter();

        if (selectedGpu == null) return;

        try
        {
            var renderNodes = Directory.GetFiles("/dev/dri", "renderD*");
            if (renderNodes.Length == 0)
            {
                AddRow(list, "Error", "No /dev/dri/renderD* devices found.");
                return;
            }

            var probe = GpuProbeFactory.Create(selectedGpu.Id);
            var staticData = probe.LoadStaticData();

            var parts = staticData.DeviceId.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // targetDevice will be e.g. "9A49" (stripped from "0X9A49")
            string targetDevice = parts.Length >= 2 ? parts[1].Replace("0X", "").ToUpper() : "";
            string targetRevision = staticData.Revision.Replace("0X", "").ToUpper();

            bool foundDevice = false;

            foreach (var node in renderNodes)
            {
                string nodeName = Path.GetFileName(node);
                string deviceDir = $"/sys/class/drm/{nodeName}/device";

                if (!Directory.Exists(deviceDir)) continue;

                string nodeDevice = ReadSysfsValue($"{deviceDir}/device").Replace("0X", "").Replace("0x", "").ToUpper();
                string nodeRevision = ReadSysfsValue($"{deviceDir}/revision").Replace("0X", "").Replace("0x", "").ToUpper();

                if (targetDevice == nodeDevice && targetRevision == nodeRevision)
                {
                    foundDevice = true;
                    ExecuteAndParseVaInfo(list, node);
                    break;
                }
            }

            if (!foundDevice)
            {
                AddRow(list, "Info", "No matching VA-API device found.");
                AddRow(list, "Note", "Ensure 'vainfo' is installed.");
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"VA-API check failed: {ex.Message}");
        }
    }

    private void ExecuteAndParseVaInfo(ObservableCollection<AdvancedItemViewModel> list, string node)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "vainfo",
            Arguments = $"--display drm --device {node}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string fullOutput = output + "\n" + error;

        string driverInfo = "Unknown";
        using (var reader = new StringReader(fullOutput))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("Driver version:"))
                {
                    driverInfo = line.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "Unknown";
                    break;
                }
            }
        }

        AddRow(list, "General", "", true);
        AddRow(list, "Device Node", node);
        AddRow(list, "Driver Info", driverInfo);
        AddRow(list, "Supported Codecs", "", true);

        using (var reader = new StringReader(fullOutput))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("VAProfile") && line.Contains(":"))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string profileNice = CleanVaProfile(parts[0].Trim());
                        string entryNice = CleanVaEntrypoint(parts[1].Trim());
                        if (profileNice != "None") AddRow(list, profileNice, entryNice);
                    }
                }
            }
        }
    }

    private string ReadSysfsValue(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        }
        catch { return ""; }
    }

    private string CleanVaProfile(string profile)
    {
        string p = profile.Replace("VAProfile", "").Trim();
        p = p.Replace("MPEG2", "MPEG-2 ");
        p = p.Replace("MPEG4", "MPEG-4 ");
        p = p.Replace("H264", "H.264 ");
        p = p.Replace("HEVC", "H.265 (HEVC) ");
        p = p.Replace("VC1", "VC-1 ");
        p = p.Replace("VP8", "VP8 ");
        p = p.Replace("VP9", "VP9 ");
        p = p.Replace("AV1", "AV1 ");
        p = p.Replace("JPEGBaseline", "JPEG Baseline");
        p = p.Replace("None", "None");
        return p.Trim();
    }

    private string CleanVaEntrypoint(string entrypoint)
    {
        if (entrypoint.Contains("VLD")) return "Decode";
        if (entrypoint.Contains("EncSlice")) return "Encode";
        if (entrypoint.Contains("EncPicture")) return "Encode (Picture)";
        if (entrypoint.Contains("VideoProc")) return "Video Processing";
        return entrypoint.Replace("VAEntrypoint", "");
    }
}
