using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GPU_T.Nvapi.Interop;

namespace GPU_T.Nvapi;

/// <summary>
/// NVIDIA Hardware Decoder (NVDEC) sidecar reader.
/// Uses native libcuda and libnvcuvid to extract hardware video decoding capabilities.
/// </summary>
internal static unsafe class NvdecReader
{
    private const string CudaLibrary = "libcuda.so.1";

    // CUDA Driver API (Required to establish a GPU context before querying NVDEC)
    [DllImport(CudaLibrary, EntryPoint = "cuInit")]
    private static extern int CuInit(uint flags);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetByPCIBusId", CharSet = CharSet.Ansi)]
    private static extern int CuDeviceGetByPCIBusId(out int device, string pciBusId);

    [DllImport(CudaLibrary, EntryPoint = "cuCtxCreate_v2")]
    private static extern int CuCtxCreate(out IntPtr pctx, uint flags, int dev);

    [DllImport(CudaLibrary, EntryPoint = "cuCtxDestroy_v2")]
    private static extern int CuCtxDestroy(IntPtr ctx);

    /// <summary>
    /// Entry point for the NVDEC reader. Initializes CUDA context and maps out the matrix of supported 
    /// decoding formats (Codecs, Bit Depths, and Chroma Subsampling).
    /// </summary>
    /// <param name="targetPciString">The PCI string of the target device.</param>
    /// <returns>0 on success, 1 on error.</returns>
    public static int Run(string targetPciString)
    {
        IntPtr cuContext = IntPtr.Zero;

        try
        {
            // Initialize CUDA Driver API
            if (CuInit(0) != 0) return 1;

            int cuDevice = 0;
            if (!string.IsNullOrEmpty(targetPciString))
            {
                if (CuDeviceGetByPCIBusId(out cuDevice, targetPciString) != 0) return 1;
            }

            // Establish a CUDA Context for this thread (Required by libnvcuvid)
            if (CuCtxCreate(out cuContext, 0, cuDevice) != 0) return 1;

            Console.WriteLine("[NVDEC]");

            // Extract the number of physical hardware decode engines (NVDEC units) on the die
            // We use H.264 4:2:0 8-bit as the baseline query to extract this hardware stat
            var baselineCaps = QueryCaps(cudaVideoCodec_enum.cudaVideoCodec_H264, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_420, 0);
            if (baselineCaps.bIsSupported == 1 && baselineCaps.nNumNVDECs > 0)
            {
                Console.WriteLine($"Hardware Decoder Engines={baselineCaps.nNumNVDECs}");
            }

            // Query modern and legacy codecs
            ReportCodec("H.264 (AVC)", cudaVideoCodec_enum.cudaVideoCodec_H264);
            ReportCodec("HEVC (H.265)", cudaVideoCodec_enum.cudaVideoCodec_HEVC);
            ReportCodec("AV1", cudaVideoCodec_enum.cudaVideoCodec_AV1);
            ReportCodec("VP9", cudaVideoCodec_enum.cudaVideoCodec_VP9);
            ReportCodec("VP8", cudaVideoCodec_enum.cudaVideoCodec_VP8);
            ReportCodec("MPEG-2", cudaVideoCodec_enum.cudaVideoCodec_MPEG2);
            ReportCodec("VC-1", cudaVideoCodec_enum.cudaVideoCodec_VC1);

            return 0;
        }
        catch
        {
            return 1;
        }
        finally
        {
            // Clean up CUDA context to prevent memory leaks
            if (cuContext != IntPtr.Zero)
            {
                CuCtxDestroy(cuContext);
            }
        }
    }

    /// <summary>
    /// Systematically queries the GPU for maximum resolutions and advanced color/bit-depth support 
    /// for a specific video codec, outputting a dense feature summary.
    /// </summary>
    /// <param name="label">Human readable codec name.</param>
    /// <param name="codec">The native CUDA video codec enum.</param>
    private static void ReportCodec(string label, cudaVideoCodec_enum codec)
    {
        // Baseline check: Does it support standard 8-bit 4:2:0?
        var baseCaps = QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_420, 0);
        if (baseCaps.bIsSupported == 0)
        {
            Console.WriteLine($"{label} Decode=No");
            return;
        }

        Console.WriteLine($"{label} Decode=Supported");
        Console.WriteLine($"{label} Max Resolution={baseCaps.nMaxWidth} x {baseCaps.nMaxHeight}");
        Console.WriteLine($"{label} Min Resolution={baseCaps.nMinWidth} x {baseCaps.nMinHeight}");

        // Build a dense matrix of advanced format support
        var features = new List<string> { "8-bit 4:2:0" };

        // Check 10-bit and 12-bit (4:2:0)
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_420, 2).bIsSupported == 1) features.Add("10-bit 4:2:0");
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_420, 4).bIsSupported == 1) features.Add("12-bit 4:2:0");

        // Check 4:2:2 Chroma
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_422, 0).bIsSupported == 1) features.Add("8-bit 4:2:2");
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_422, 2).bIsSupported == 1) features.Add("10-bit 4:2:2");
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_422, 4).bIsSupported == 1) features.Add("12-bit 4:2:2");

        // Check 4:4:4 Chroma
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_444, 0).bIsSupported == 1) features.Add("8-bit 4:4:4");
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_444, 2).bIsSupported == 1) features.Add("10-bit 4:4:4");
        if (QueryCaps(codec, cudaVideoChromaFormat_enum.cudaVideoChromaFormat_444, 4).bIsSupported == 1) features.Add("12-bit 4:4:4");

        Console.WriteLine($"{label} Supported Formats={string.Join(", ", features)}");
    }

    /// <summary>
    /// Executes the native cuvidGetDecoderCaps API call.
    /// </summary>
    /// <param name="codec">The target codec.</param>
    /// <param name="chroma">The target chroma sub-sampling format.</param>
    /// <param name="bitDepthMinus8">0 for 8-bit, 2 for 10-bit, 4 for 12-bit.</param>
    /// <returns>A fully populated _CUVIDDECODECAPS struct detailing hardware support.</returns>
    private static _CUVIDDECODECAPS QueryCaps(cudaVideoCodec_enum codec, cudaVideoChromaFormat_enum chroma, uint bitDepthMinus8)
    {
        var caps = new _CUVIDDECODECAPS
        {
            eCodecType = codec,
            eChromaFormat = chroma,
            nBitDepthMinus8 = bitDepthMinus8
        };

        // 0 represents CUDA_SUCCESS (mapped to 'int' in our ClangSharp setup)
        if (Methods.cuvidGetDecoderCaps(&caps) != 0)
        {
            caps.bIsSupported = 0;
        }

        return caps;
    }
}