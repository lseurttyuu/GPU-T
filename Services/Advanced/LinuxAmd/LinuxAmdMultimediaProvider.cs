using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GPU_T.ViewModels;

namespace GPU_T.Services.Advanced.LinuxAmd;

public class LinuxAmdMultimediaProvider : AdvancedDataProvider
{
    public override void LoadData(ObservableCollection<AdvancedItemViewModel> list, GpuListItem? selectedGpu)
    {

        ResetCounter();
        // (Tutaj wklej całą logikę z PopulateMultimediaAdvanced, zamieniając AddAdvancedRow na AddRow)
        // Ze względu na limit znaków, zakładam, że poradzisz sobie z przeniesieniem ciała metody.
        // Pamiętaj o helperach CleanVaProfile i CleanVaEntrypoint.
        // Jeśli potrzebujesz pełnego kodu tego pliku, daj znać!

        try
        {
            var renderNodes = Directory.GetFiles("/dev/dri", "renderD*");
            if (renderNodes.Length == 0)
            {
                AddRow(list, "Error", "No /dev/dri/renderD* devices found.");
                return;
            }

            var gpuNameParts = selectedGpu?.DisplayName?
                .Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 2 && !p.Equals("AMD", StringComparison.OrdinalIgnoreCase) && !p.Equals("Radeon", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<string>();
            
            bool foundDevice = false;

            foreach (var node in renderNodes)
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
                bool isMatch = false;
                string driverLine = "";
                
                using (var reader = new StringReader(fullOutput))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("Driver version:")) { driverLine = line; break; }
                    }
                }

                if (!string.IsNullOrEmpty(driverLine))
                {
                    foreach (var part in gpuNameParts)
                    {
                        if (driverLine.Contains(part, StringComparison.OrdinalIgnoreCase)) { isMatch = true; break; }
                    }
                }

                if (isMatch)
                {
                    foundDevice = true;
                    AddRow(list, "General", "", true);
                    AddRow(list, "Device Node", node);
                    var driverInfo = driverLine.Split(new[] { ':' }, 2).LastOrDefault()?.Trim() ?? "Unknown";
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
                    break;
                }
            }

            if (!foundDevice)
            {
                AddRow(list, "Info", "No matching VA-API device found.");
                AddRow(list, "Note", "Ensure 'vainfo' is installed (package: libva-utils).");
            }
        }
        catch (Exception ex)
        {
            AddRow(list, "Error", $"VA-API check failed: {ex.Message}");
        }
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