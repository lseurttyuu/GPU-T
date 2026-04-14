using System;
using System.Runtime.InteropServices;

namespace GPU_T.Nvapi;

/// <summary>
/// NVIDIA GPU telemetry reader using NVAPI and NVML libraries.
/// Retrieves thermal data, voltage, and PCIe throughput metrics from NVIDIA GPUs.
/// </summary>
class Program
{
    private const string NvApiLibrary = "libnvidia-api.so.1";
    private const string NvmlLibrary = "libnvidia-ml.so.1";

    // NVAPI query interface IDs for function resolution
    private const uint QUERY_NVAPI_INITIALIZE = 0x0150e828;
    private const uint QUERY_NVAPI_ENUM_PHYSICAL_GPUS = 0xe5ac921f;
    private const uint QUERY_NVAPI_THERMALS = 0x65fe3aad;
    private const uint QUERY_NVAPI_VOLTAGE = 0x465f9bcf;
    private const uint QUERY_NVAPI_GET_BUS_ID = 0x1be0b8e5;

    /// <summary>
    /// Structure for NVAPI thermal sensor data. Values are encoded as fixed-point integers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct NvApiThermals
    {
        public uint version;
        public int mask;
        public fixed int values[40];
    }

    /// <summary>
    /// Structure for NVAPI voltage readout. Voltage is expressed in microvolts (uV).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct NvApiVoltage
    {
        public uint version;
        public uint flags;
        public fixed uint padding_1[8];
        public uint value_uv; // uV
        public fixed uint padding_2[8];
    }

    [DllImport(NvApiLibrary, EntryPoint = "nvapi_QueryInterface")]
    public static extern IntPtr NvAPI_QueryInterface(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_Initialize();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_EnumPhysicalGPUs([Out] IntPtr[] handles, out uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_GetThermals(IntPtr handle, ref NvApiThermals sensors);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_GetVoltage(IntPtr handle, ref NvApiVoltage data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_GetBusId(IntPtr handle, out uint busId);

    [DllImport(NvmlLibrary, EntryPoint = "nvmlInit_v2")]
    private static extern int NvmlInit();

    [DllImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetHandleByPciBusId_v2", CharSet = CharSet.Ansi)]
    private static extern int NvmlDeviceGetHandleByPciBusId(string pciBusId, out IntPtr device);

    [DllImport(NvmlLibrary, EntryPoint = "nvmlDeviceGetPcieThroughput")]
    private static extern int NvmlDeviceGetPcieThroughput(IntPtr device, uint counter, out uint value);

    /// <summary>
    /// Entry point. Provides GPU metrics in CSV format.
    /// Arguments: --check (validation), --read (full telemetry), --bus [id], --pci [string]
    /// Returns: 0 on success, 1 on failure.
    /// </summary>
    static unsafe int Main(string[] args)
    {
        if (args.Length == 0) return 1;
        bool isCheck = false, isRead = false;
        uint targetBusId = uint.MaxValue;
        string targetPciString = "";

        // Parse command-line arguments for operation mode and GPU selection
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--check") isCheck = true;
            if (args[i] == "--read") isRead = true;
            if (args[i] == "--bus" && i + 1 < args.Length)
            if (args[i] == "--bus" && i + 1 < args.Length) uint.TryParse(args[i + 1], out targetBusId);
            if (args[i] == "--pci" && i + 1 < args.Length) targetPciString = args[i + 1];
        }

        if (!isCheck && !isRead) return 1;

        try
        {
            // Initialize NVAPI
            IntPtr initPtr = NvAPI_QueryInterface(QUERY_NVAPI_INITIALIZE);
            if (initPtr == IntPtr.Zero) return 1;
            var initialize = Marshal.GetDelegateForFunctionPointer<NvAPI_Initialize>(initPtr);
            if (initialize() != 0) return 1;

            // Enumerate all physical GPUs
            IntPtr enumPtr = NvAPI_QueryInterface(QUERY_NVAPI_ENUM_PHYSICAL_GPUS);
            var enumGpus = Marshal.GetDelegateForFunctionPointer<NvAPI_EnumPhysicalGPUs>(enumPtr);
            
            IntPtr[] gpuHandles = new IntPtr[64];
            if (enumGpus(gpuHandles, out uint gpuCount) != 0 || gpuCount == 0) return 1;
            
            /// Select target GPU by bus ID if specified, otherwise use first GPU
            IntPtr myGpu = gpuHandles[0];
            
            if (targetBusId != uint.MaxValue)
            {
                IntPtr busIdPtr = NvAPI_QueryInterface(QUERY_NVAPI_GET_BUS_ID);
                if (busIdPtr != IntPtr.Zero)
                {
                    var getBusId = Marshal.GetDelegateForFunctionPointer<NvAPI_GetBusId>(busIdPtr);
                    for (int i = 0; i < gpuCount; i++)
                    {
                        if (getBusId(gpuHandles[i], out uint busId) == 0 && busId == targetBusId)
                        {
                            myGpu = gpuHandles[i];
                            break;
                        }
                    }
                }
            }

            // Retrieve and validate thermal sensor capabilities
            IntPtr thermalsPtr = NvAPI_QueryInterface(QUERY_NVAPI_THERMALS);
            if (thermalsPtr == IntPtr.Zero) return 1;
            
            var getThermals = Marshal.GetDelegateForFunctionPointer<NvAPI_GetThermals>(thermalsPtr);

            uint structVersion = (uint)sizeof(NvApiThermals) | (2u << 16);
            int validMask = 1;
            NvApiThermals maskTest = new NvApiThermals { version = structVersion, mask = 1 };
            
            // Determine which thermal sensors are available by testing each bit
            for (int bit = 0; bit < 32; bit++)
            {
                maskTest.mask = 1 << bit;
                if (getThermals(myGpu, ref maskTest) != 0)
                {
                    validMask = maskTest.mask - 1;
                    break;
                }
            }

            if (isCheck) return 0;

            // Initialize output values
            NvApiThermals sensors = new NvApiThermals { version = structVersion, mask = validMask };
            int finalHotspot = -1;
            int finalVram = -1;
            int finalVoltageMv = -1;

            // Read thermal sensor data (indices 9 and 15 contain hotspot and VRAM temps)
            if (getThermals(myGpu, ref sensors) == 0)
            {
                int hotspot = sensors.values[9] / 256;
                int vram = sensors.values[15] / 256;

                finalHotspot = (hotspot > 0 && hotspot < 255) ? hotspot : -1;
                finalVram = (vram > 0 && vram < 255) ? vram : -1;
            }

            // Read GPU core voltage
            IntPtr voltagePtr = NvAPI_QueryInterface(QUERY_NVAPI_VOLTAGE);
            if (voltagePtr != IntPtr.Zero)
            {
                var getVoltage = Marshal.GetDelegateForFunctionPointer<NvAPI_GetVoltage>(voltagePtr);
                NvApiVoltage voltageData = new NvApiVoltage
                {
                    version = (uint)sizeof(NvApiVoltage) | (1u << 16)
                };

                if (getVoltage(myGpu, ref voltageData) == 0)
                {
                    // Convert Microvolts (uV) to Millivolts (mV) safely
                    if (voltageData.value_uv > 0)
                    {
                        finalVoltageMv = (int)(voltageData.value_uv / 1000);
                    }
                }
            }

            // Read PCIe throughput metrics via NVML
            int finalTxKbps = -1;
            int finalRxKbps = -1;

            if (!string.IsNullOrEmpty(targetPciString))
            {
                if (NvmlInit() == 0)
                {
                    if (NvmlDeviceGetHandleByPciBusId(targetPciString, out IntPtr nvmlDevice) == 0)
                    {
                        // 0 = TX (Transmit), 1 = RX (Receive). NVML returns values in KB/s.
                        if (NvmlDeviceGetPcieThroughput(nvmlDevice, 0, out uint tx) == 0) finalTxKbps = (int)tx;
                        if (NvmlDeviceGetPcieThroughput(nvmlDevice, 1, out uint rx) == 0) finalRxKbps = (int)rx;
                    }
                }
            }

            // Output 5 values: hotspot(degC), vram(degC), voltage(mV), tx(KB/s), rx(KB/s)
            Console.WriteLine($"{finalHotspot},{finalVram},{finalVoltageMv},{finalTxKbps},{finalRxKbps}");
            return 0;

        }
        catch
        {
            return 1;
        }
    }
}