using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GPU_T.Nvapi;

/// <summary>
/// NVIDIA CUDA GPU static info reader using libcuda.so.1.
/// </summary>
internal static class CudaReader
{
    private const string CudaLibrary = "libcuda.so.1";

    [DllImport(CudaLibrary, EntryPoint = "cuInit")]
    private static extern int CuInit(uint flags);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetByPCIBusId", CharSet = CharSet.Ansi)]
    private static extern int CuDeviceGetByPCIBusId(out int device, string pciBusId);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetAttribute")]
    private static extern int CuDeviceGetAttribute(out int pi, int attrib, int dev);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetName", CharSet = CharSet.Ansi)]
    private static extern int CuDeviceGetName(StringBuilder name, int len, int dev);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceTotalMem_v2")]
    private static extern int CuDeviceTotalMem(out ulong bytes, int dev);

    public static int Run(string targetPciString)
    {
        try
        {
            if (CuInit(0) != 0) return 1;

            int cuDevice = 0;
            if (!string.IsNullOrEmpty(targetPciString))
            {
                 if (CuDeviceGetByPCIBusId(out cuDevice, targetPciString) != 0) return 1;
            }

            // 1. Get String & 64-bit values
            StringBuilder nameBuilder = new StringBuilder(256);
            CuDeviceGetName(nameBuilder, 256, cuDevice);
            CuDeviceTotalMem(out ulong totalMem, cuDevice);

            // Helper function for the 90+ integer attributes
            int GetAttr(int attrib)
            {
                CuDeviceGetAttribute(out int val, attrib, cuDevice);
                return val;
            }

            // Compute Mode Decoder
            string computeMode = GetAttr(20) switch {
                0 => "Default", 1 => "Exclusive Thread", 2 => "Prohibited", 3 => "Exclusive Process", _ => "Unknown"
            };

            // ==========================================
            // GENERAL
            // ==========================================
            Console.WriteLine("[General]");
            Console.WriteLine($"CUDA Device Name={nameBuilder}");
            Console.WriteLine($"Compute Capability={GetAttr(75)}.{GetAttr(76)}");
            Console.WriteLine($"Processor Count={GetAttr(16)}"); // SMs
            Console.WriteLine($"GPU Clock Rate={GetAttr(13) / 1000.0:0.0} MHz"); // Returned in KHz
            Console.WriteLine($"Memory Clock Rate={GetAttr(36) / 1000.0:0.0} MHz");
            Console.WriteLine($"Memory Bus Width={GetAttr(37)} Bit");
            Console.WriteLine($"L2 Cache Size={GetAttr(38) / 1024} KB");
            Console.WriteLine($"Global Memory Size={totalMem / (1024 * 1024)} MB");
            Console.WriteLine($"Async Engines={GetAttr(40)}");
            Console.WriteLine($"SP to DP Ratio=1:{GetAttr(87)}");
            Console.WriteLine($"ECC Supported={(GetAttr(32) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Using TCC Driver={(GetAttr(35) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Compute Mode={computeMode}");
            Console.WriteLine($"Multi-GPU Board={(GetAttr(84) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"PCI ID=Bus {GetAttr(33)}, Dev {GetAttr(34)}, Domain {GetAttr(50)}");
            Console.WriteLine($"Threads per Multiprocessor={GetAttr(39)}");
            Console.WriteLine($"Max Shmem per Multiprocessor={GetAttr(81) / 1024} KB");
            Console.WriteLine($"Execute Multiple Kernels={(GetAttr(31) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Preemption Supported={(GetAttr(90) == 1 ? "Yes" : "No")}");

            // ==========================================
            // MEMORY & EXECUTION
            // ==========================================
            Console.WriteLine("[Memory]");
            Console.WriteLine($"Native Atomic Supported={(GetAttr(86) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Unified Address Space={(GetAttr(41) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Integrated w/ Host Memory={(GetAttr(18) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Can map Host Memory={(GetAttr(19) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Can allocate Managed Memory={(GetAttr(83) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Pageable Memory Access={(GetAttr(88) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Concurrent Managed Memory={(GetAttr(89) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Can use Host Memory Pointers={(GetAttr(91) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Supports Stream Priorities={(GetAttr(78) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Can Cache Globals in L1={(GetAttr(79) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Can Cache Locals in L1={(GetAttr(80) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Max Block Size={GetAttr(2)} x {GetAttr(3)} x {GetAttr(4)}");
            Console.WriteLine($"Max # of Threads per Block={GetAttr(1)}");
            Console.WriteLine($"Max Shmem per Block={GetAttr(8) / 1024} KB");
            Console.WriteLine($"Max Grid Size={GetAttr(5)} x {GetAttr(6)} x {GetAttr(7)}");
            Console.WriteLine($"Max Registers per Block={GetAttr(12)}");
            Console.WriteLine($"Total Constant Memory={GetAttr(9) / 1024} KB");
            Console.WriteLine($"Warp Size={GetAttr(10)} Threads");
            Console.WriteLine($"Maximum Pitch={GetAttr(11) / 1024} KB");
            Console.WriteLine($"Texture Alignment={GetAttr(14)} Bytes");
            Console.WriteLine($"Surface Alignment={GetAttr(30)} Bytes");
            Console.WriteLine($"Texture Pitch Alignment={GetAttr(51)} Bytes");
            Console.WriteLine($"GPU Overlap={(GetAttr(15) == 1 ? "Yes" : "No")}");
            Console.WriteLine($"Kernel Runtime Limit={(GetAttr(17) == 1 ? "Yes" : "No")}");

            // ==========================================
            // SIZE CONSTRAINTS
            // ==========================================
            Console.WriteLine("[Size Constraints]");
            Console.WriteLine($"1D Texture Size={GetAttr(21)}");
            Console.WriteLine($"1D Layered Texture Size={GetAttr(42)} x {GetAttr(43)}");
            Console.WriteLine($"2D Texture Size={GetAttr(22)} x {GetAttr(23)}");
            Console.WriteLine($"2D Layered Texture Size={GetAttr(27)} x {GetAttr(28)} x {GetAttr(29)}");
            Console.WriteLine($"2D Texture Size Gather={GetAttr(45)} x {GetAttr(46)}");
            Console.WriteLine($"3D Texture Size={GetAttr(24)} x {GetAttr(25)} x {GetAttr(26)}");
            Console.WriteLine($"3D Texture Size Alt={GetAttr(47)} x {GetAttr(48)} x {GetAttr(49)}");
            Console.WriteLine($"Cubemap Texture Size={GetAttr(52)}");
            Console.WriteLine($"Layered Cubemap Texture Size={GetAttr(53)} x {GetAttr(54)}");
            Console.WriteLine($"1D Surface Size={GetAttr(55)}");
            Console.WriteLine($"1D Layered Surface Size={GetAttr(61)} x {GetAttr(62)}");
            Console.WriteLine($"2D Surface Size={GetAttr(56)} x {GetAttr(57)}");
            Console.WriteLine($"2D Layered Surface Size={GetAttr(63)} x {GetAttr(64)} x {GetAttr(65)}");
            Console.WriteLine($"3D Surface Size={GetAttr(58)} x {GetAttr(59)} x {GetAttr(60)}");
            Console.WriteLine($"Cubemap Surface Size={GetAttr(66)}");
            Console.WriteLine($"Cubemap Layered Surface Size={GetAttr(67)} x {GetAttr(68)}");
            Console.WriteLine($"1D Linear Texture Size={GetAttr(69)}");
            Console.WriteLine($"2D Linear Texture Size={GetAttr(70)} x {GetAttr(71)}");
            Console.WriteLine($"2D Linear Texture Pitch={GetAttr(72)} Bytes");
            Console.WriteLine($"1D Mipmapped Texture Size={GetAttr(77)}");
            Console.WriteLine($"2D Mipmapped Texture Size={GetAttr(73)} x {GetAttr(74)}");

            return 0;
        }
        catch
        {
            return 1;
        }
    }
}