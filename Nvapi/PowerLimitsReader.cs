using System;
using System.Runtime.InteropServices;

namespace GPU_T.Nvapi;

/// <summary>
/// NVIDIA Hardware Limits sidecar reader.
/// Extracts static hardware constraints like power, clock maximums, cooling limits, and PCIe boundaries.
/// </summary>
internal static class PowerLimitsReader
{
    private const string NvmlLibrary = "libnvidia-ml.so.1";

    // Initialization
    [DllImport(NvmlLibrary)]
    private static extern int nvmlInit_v2();

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetHandleByPciBusId_v2(string pciBusId, out IntPtr device);

    // Power
    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetPowerManagementLimit(IntPtr device, out uint limit);

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetPowerManagementDefaultLimit(IntPtr device, out uint defaultLimit);

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetPowerManagementLimitConstraints(IntPtr device, out uint minLimit, out uint maxLimit);

    // Thermal
    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetTemperatureThreshold(IntPtr device, int thresholdType, out uint temp);

    // Clocks
    private const int NVML_CLOCK_GRAPHICS = 0;
    private const int NVML_CLOCK_SM = 1;
    private const int NVML_CLOCK_MEM = 2;
    private const int NVML_CLOCK_VIDEO = 3;

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetMaxClockInfo(IntPtr device, int type, out uint clock);

    // Fans
    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetNumFans(IntPtr device, out int numFans);

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetMinMaxFanSpeed(IntPtr device, out int minSpeed, out int maxSpeed);

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetPowerSource(IntPtr device, out int powerSource);

    [DllImport(NvmlLibrary)]
    private static extern int nvmlDeviceGetPersistenceMode(IntPtr device, out int mode);


    /// <summary>
    /// Entry point for the Power Limits reader. Initializes NVML, retrieves power limits, clock maximums, thermal thresholds, and cooling capabilities, and prints them to the console.
    /// </summary>
    /// <param name="targetPciString">The PCI string of the target GPU device.</param>
    /// <returns>List of power limit values.</returns>
    public static int Run(string targetPciString)
    {
        try
        {
            if (nvmlInit_v2() != 0) return 1;
            if (nvmlDeviceGetHandleByPciBusId_v2(targetPciString, out IntPtr device) != 0) return 1;

            // POWER CONFIGURATION
            Console.WriteLine("[Power Limits]");
            
            if (nvmlDeviceGetPowerManagementLimit(device, out uint curr) == 0)
                Console.WriteLine($"Target Power Limit (TDP)={curr / 1000.0:0.0} W");

            if (nvmlDeviceGetPowerManagementDefaultLimit(device, out uint def) == 0)
                Console.WriteLine($"Default Power Limit={def / 1000.0:0.0} W");

            if (nvmlDeviceGetPowerManagementLimitConstraints(device, out uint min, out uint max) == 0)
            {
                Console.WriteLine($"Minimum Allowed Limit={min / 1000.0:0.0} W");
                Console.WriteLine($"Maximum Allowed Limit={max / 1000.0:0.0} W");
            }

            // Is the laptop on battery or AC?
            if (nvmlDeviceGetPowerSource(device, out int powerSource) == 0)
                Console.WriteLine($"Hardware Power Source={(powerSource == 0 ? "AC Adapter" : "Battery")}");

            // CURRENT MAX CLOCK LIMITS
            Console.WriteLine("[Current Clock Limits]");
            
            // Note: These represent the absolute maximum frequencies the hardware is rated to reach
            if (nvmlDeviceGetMaxClockInfo(device, NVML_CLOCK_GRAPHICS, out uint maxGfx) == 0)
                Console.WriteLine($"Maximum Graphics Clock={maxGfx} MHz");
                
            if (nvmlDeviceGetMaxClockInfo(device, NVML_CLOCK_MEM, out uint maxMem) == 0)
                Console.WriteLine($"Maximum Memory Clock={maxMem} MHz");

            if (nvmlDeviceGetMaxClockInfo(device, NVML_CLOCK_VIDEO, out uint maxVid) == 0)
                Console.WriteLine($"Maximum Video/NVENC Clock={maxVid} MHz");

            // LINUX DRIVER BEHAVIOR
            Console.WriteLine("[Driver State]");

            if (nvmlDeviceGetPersistenceMode(device, out int persistMode) == 0)
                Console.WriteLine($"Persistence Mode={(persistMode == 1 ? "Enabled (Fast Wake)" : "Disabled (Max Power Saving)")}");

            // THERMAL THRESHOLDS
            Console.WriteLine("[Thermal Limits]");
            
            // 0 = Shutdown Threshold, 1 = Slowdown Threshold
            if (nvmlDeviceGetTemperatureThreshold(device, 1, out uint slowdownTemp) == 0)
                Console.WriteLine($"Thermal Throttle Point={slowdownTemp} °C");

            if (nvmlDeviceGetTemperatureThreshold(device, 0, out uint shutdownTemp) == 0)
                Console.WriteLine($"Emergency Shutdown Point={shutdownTemp} °C");

            // COOLING & FANS
            Console.WriteLine("[Cooling Capabilities]");
            
            if (nvmlDeviceGetNumFans(device, out int numFans) == 0)
            {
                // We convert the tachometer count into a simple Yes/No capability
                Console.WriteLine($"Active Fan Control={(numFans > 0 ? "Supported" : "No")}");
                
                if (numFans > 0)
                {
                    try
                    {
                        if (nvmlDeviceGetMinMaxFanSpeed(device, out int minFan, out int maxFan) == 0)
                        {
                            Console.WriteLine($"Hardware Fan Range={minFan}% - {maxFan}%");
                            Console.WriteLine($"Zero RPM Mode Supported={(minFan == 0 ? "Yes" : "No")}");
                        }
                    }
                    catch 
                    {
                        // Catch in case the driver is too old to support MinMaxFanSpeed
                        Console.WriteLine("Hardware Fan Range=Data unavailable on this driver");
                    }
                }
                else
                {
                    Console.WriteLine("Cooling Type=Passive / Liquid / Undetected");
                }
            }

            return 0;
        }
        catch
        {
            return 1;
        }
    }
}