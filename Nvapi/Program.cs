using System;
using System.Runtime.InteropServices;

namespace GPU_T.Nvapi;

/// <summary>
/// Entry point for the GPU-T NVIDIA Sidecar. 
/// Routes CLI arguments to the appropriate hardware abstraction module.
/// </summary>
class Program
{
    static unsafe int Main(string[] args)
    {
        if (args.Length == 0) return 1;
        bool isCheck = false, isRead = false, isCuda = false;
        bool isNvenc = false;
        uint targetBusId = uint.MaxValue;
        string targetPciString = "";

        // Parse command-line arguments for operation mode and GPU selection
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--check") isCheck = true;
            if (args[i] == "--read") isRead = true;
            if (args[i] == "--cuda") isCuda = true;
            if (args[i] == "--nvenc") isNvenc = true;
            if (args[i] == "--bus" && i + 1 < args.Length) uint.TryParse(args[i + 1], out targetBusId);
            if (args[i] == "--pci" && i + 1 < args.Length) targetPciString = args[i + 1];
        }

        try
        {
            // Route to CUDA static info reader
            if (isCuda)
            {
                return CudaReader.Run(targetPciString);
            }
            
            // Route to NVAPI/NVML Telemetry reader
            if (isCheck || isRead)
            {
                return TelemetryReader.Run(isCheck, targetBusId, targetPciString);
            }

            // Route to NVENC reader
            if (isNvenc)
            {
                return NvencReader.Run(targetPciString);
            }

            return 1;
        }
        catch
        {
            return 1; // Failsafe return
        }

        
    }
}