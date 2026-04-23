using System.Runtime.InteropServices;
using GPU_T.Nvapi.Interop;

namespace GPU_T.Nvapi;


/// <summary>
/// Hardcoded SDK constants matching the nvEncodeAPI.h header used to generate the bindings.
/// Currently mapped to NVENC SDK v13.0.
/// </summary>
internal static class NvencApi
{
    public const uint MAJOR_VERSION = 13;
    public const uint MINOR_VERSION = 0;
    
    // The version format expected inside the structs
    public const uint SDK_VERSION = MAJOR_VERSION | (MINOR_VERSION << 24);

    // The C-macro replication
    private static uint StructVersion(uint ver) => SDK_VERSION | (ver << 16) | (0x7u << 28);

    //Tuple Decoder for the API Version
    public static (uint Major, uint Minor) DecodeVersion(uint rawVer) => (rawVer >> 4, rawVer & 0xF);

    // The exact constants defined in the NVIDIA header
    public static readonly uint NV_ENCODE_API_FUNCTION_LIST_VER = StructVersion(2);
    public static readonly uint NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER = StructVersion(1);
    public static readonly uint NV_ENC_CAPS_PARAM_VER = StructVersion(1);
}



/// <summary>
/// NVIDIA Hardware Encoder (NVENC) sidecar reader.
/// Uses native libcuda and libnvidia-encode to extract real hardware encoding capabilities.
/// </summary>
internal static unsafe class NvencReader
{
    private const string CudaLibrary = "libcuda.so.1";

    // CUDA Driver API
    [DllImport(CudaLibrary, EntryPoint = "cuInit")]
    private static extern int CuInit(uint flags);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetByPCIBusId", CharSet = CharSet.Ansi)]
    private static extern int CuDeviceGetByPCIBusId(out int device, string pciBusId);

    [DllImport(CudaLibrary, EntryPoint = "cuCtxCreate_v2")]
    private static extern int CuCtxCreate(out IntPtr pctx, uint flags, int dev);

    [DllImport(CudaLibrary, EntryPoint = "cuCtxDestroy_v2")]
    private static extern int CuCtxDestroy(IntPtr ctx);

    [DllImport(CudaLibrary, EntryPoint = "cuDeviceGetAttribute")]
    private static extern int CuDeviceGetAttribute(out int pi, int attrib, int dev);

 
    /// <summary>
    /// Entry point for the NVENC reader. Initializes CUDA context, creates NVENC session, and queries encoding capabilities.
    /// Outputs results in a structured format for GPU-T to consume. Implements robust error handling to ensure
    /// that any failure in the NVENC initialization or querying process results in a graceful exit with a return code of 0.
    /// </summary>
    /// <param name="targetPciString">The PCI string of the target device.</param>
    /// <returns>Complete set of encoding capabilities for the target device.</returns>
    public static int Run(string targetPciString)
    {
        IntPtr cuContext = IntPtr.Zero;
        void* encoder = null;
        _NV_ENCODE_API_FUNCTION_LIST api = default;

        try
        {
            if (CuInit(0) != 0) return 1;

            int cuDevice = 0;
            if (!string.IsNullOrEmpty(targetPciString))
            {
                if (CuDeviceGetByPCIBusId(out cuDevice, targetPciString) != 0) return 1;
            }

            // Mandatory Contract: Output Compute Capability for the Hybrid Fallback safety net.
            CuDeviceGetAttribute(out int ccMajor, 75, cuDevice);
            CuDeviceGetAttribute(out int ccMinor, 76, cuDevice);
            Console.WriteLine($"Compute Capability={ccMajor}.{ccMinor}");

            // Create CUDA Context (Required to initialize NVENC)
            if (CuCtxCreate(out cuContext, 0, cuDevice) != 0) return 1; 

            // Fetch the dynamic API Version from the local NVIDIA Driver
            uint rawApiVersion;
            if (Methods.NvEncodeAPIGetMaxSupportedVersion(&rawApiVersion) != _NVENCSTATUS.NV_ENC_SUCCESS) return 1;

            // Reformat the raw version to the Struct format
            var apiVersion = NvencApi.DecodeVersion(rawApiVersion);

            // Validate the driver is new enough to understand our v13.0 C# structs
            if (apiVersion.Major < NvencApi.MAJOR_VERSION || 
            (apiVersion.Major == NvencApi.MAJOR_VERSION && apiVersion.Minor < NvencApi.MINOR_VERSION))
            {
                Console.WriteLine($"NVENC Driver too old! Requires v{NvencApi.MAJOR_VERSION}.{NvencApi.MINOR_VERSION}, found v{apiVersion.Major}.{apiVersion.Minor}");
                return 1;
            }
        
            // Load NVENC API using the magic struct version macro
            api.version = NvencApi.NV_ENCODE_API_FUNCTION_LIST_VER;

            // The CreateInstance function will populate the rest of the function pointers in the struct based on the version we passed in.
            if (Methods.NvEncodeAPICreateInstance(&api) != _NVENCSTATUS.NV_ENC_SUCCESS) return 1;

            // We should now have a fully populated _NV_ENCODE_API_FUNCTION_LIST struct with all supported functions up to the version we specified.
            var sessionParams = new _NV_ENC_OPEN_ENCODE_SESSIONEX_PARAMS
            {
                version = NvencApi.NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER,
                deviceType = _NV_ENC_DEVICE_TYPE.NV_ENC_DEVICE_TYPE_CUDA,
                device = (void*)cuContext,
                apiVersion = NvencApi.SDK_VERSION
            };

            // Create an NVENC session for the target GPU
            if (api.nvEncOpenEncodeSessionEx(&sessionParams, &encoder) != _NVENCSTATUS.NV_ENC_SUCCESS) return 1;

            // Query Codecs
            uint guidCount = 0;
            if (api.nvEncGetEncodeGUIDCount(encoder, &guidCount) == _NVENCSTATUS.NV_ENC_SUCCESS && guidCount > 0)
            {
                Guid[] guids = new Guid[guidCount];
                fixed (Guid* pGuids = guids)
                {
                    if (api.nvEncGetEncodeGUIDs(encoder, pGuids, guidCount, &guidCount) == _NVENCSTATUS.NV_ENC_SUCCESS)
                    {
                        ProcessHardwareReport(encoder, api, guids);
                    }
                }
            }

            return 0;

        }
        catch
        {
            return 1; 
        }
        finally
        {
            // 1. Clean up NVENC session FIRST (if it was successfully created)
            if (encoder != null && api.nvEncDestroyEncoder != null)
            {
                api.nvEncDestroyEncoder(encoder);
            }

            // 2. Clean up CUDA context LAST
            if (cuContext != IntPtr.Zero) 
            {
                CuCtxDestroy(cuContext);
            }
        }
    }


/// <summary>
    /// Analyzes the supported codecs and global hardware metrics, then outputs a formatted report to the console.
    /// </summary>
    /// <param name="encoder">The initialized NVENC encoder session pointer.</param>
    /// <param name="api">The populated NVENC API function list.</param>
    /// <param name="guids">Array of supported codec GUIDs retrieved from the hardware.</param>
    private static void ProcessHardwareReport(void* encoder, _NV_ENCODE_API_FUNCTION_LIST api, Guid[] guids)
    {
        // Determine baseline support for major codecs
        bool hasH264 = Array.Exists(guids, g => g == Methods.NV_ENC_CODEC_H264_GUID);
        bool hasHevc = Array.Exists(guids, g => g == Methods.NV_ENC_CODEC_HEVC_GUID);
        bool hasAv1  = Array.Exists(guids, g => g == Methods.NV_ENC_CODEC_AV1_GUID);

        Console.WriteLine("[NVENC]");

        // Query total number of physical NVENC engines on the GPU die
        int engines = QueryCap(encoder, api, Methods.NV_ENC_CODEC_H264_GUID, _NV_ENC_CAPS.NV_ENC_CAPS_NUM_ENCODER_ENGINES);
        if (engines > 0) Console.WriteLine($"Hardware Encoder Engines={engines}");

        // Report specific capabilities for each codec
        ReportCodec("H.264 (AVC)", hasH264, Methods.NV_ENC_CODEC_H264_GUID, encoder, api);
        ReportCodec("HEVC (H.265)", hasHevc, Methods.NV_ENC_CODEC_HEVC_GUID, encoder, api);
        ReportCodec("AV1", hasAv1, Methods.NV_ENC_CODEC_AV1_GUID, encoder, api);

        // --- Global Hardware Metrics ---
        // Use HEVC as the primary metric baseline if available; otherwise fallback to H.264
        Guid primaryCodec = hasHevc ? Methods.NV_ENC_CODEC_HEVC_GUID : Methods.NV_ENC_CODEC_H264_GUID;
        
        // Query maximum encoding resolution
        int maxW = QueryCap(encoder, api, primaryCodec, _NV_ENC_CAPS.NV_ENC_CAPS_WIDTH_MAX);
        int maxH = QueryCap(encoder, api, primaryCodec, _NV_ENC_CAPS.NV_ENC_CAPS_HEIGHT_MAX);
        if (maxW > 0) Console.WriteLine($"Max Encoding Resolution={maxW} x {maxH}");

        // Query maximum macroblock throughput per second (indicates maximum framerate/resolution capability)
        int mbPerSec = QueryCap(encoder, api, Methods.NV_ENC_CODEC_H264_GUID, _NV_ENC_CAPS.NV_ENC_CAPS_MB_PER_SEC_MAX);
        if (mbPerSec > 0) Console.WriteLine($"Max Throughput={mbPerSec} Macroblocks/sec");

        // Check for lossless encoding support
        Console.WriteLine($"Lossless Encoding={(QueryCap(encoder, api, primaryCodec, _NV_ENC_CAPS.NV_ENC_CAPS_SUPPORT_LOSSLESS_ENCODE) == 1 ? "Supported" : "No")}");
    }

    /// <summary>
    /// Queries and prints detailed feature support (10-bit, B-frames, Chroma, Profiles, Presets) for a specific codec.
    /// </summary>
    /// <param name="label">The human-readable name of the codec (e.g., "H.264 (AVC)").</param>
    /// <param name="supported">Whether the hardware supports this codec at all.</param>
    /// <param name="guid">The NVENC GUID for the codec being queried.</param>
    /// <param name="encoder">The initialized NVENC encoder session pointer.</param>
    /// <param name="api">The populated NVENC API function list.</param>
    private static void ReportCodec(string label, bool supported, Guid guid, void* encoder, _NV_ENCODE_API_FUNCTION_LIST api)
    {
        // Fast-fail if the GPU doesn't support this codec
        if (!supported)
        {
            Console.WriteLine($"{label} Encode=No");
            return;
        }

        Console.WriteLine($"{label} Encode=Supported");

        // 10-Bit Check (Specific to newer architectures handling HEVC and AV1; H.264 is excluded)
        if (label != "H.264 (AVC)")
        {
            bool bit10 = QueryCap(encoder, api, guid, _NV_ENC_CAPS.NV_ENC_CAPS_SUPPORT_10BIT_ENCODE) == 1;
            Console.WriteLine($"{label} 10-bit Encode={(bit10 ? "Supported" : "No")}");
        }
        
        // B-Frames check (Evaluates if the maximum number of B-frames is greater than 0)
        bool bFrames = QueryCap(encoder, api, guid, _NV_ENC_CAPS.NV_ENC_CAPS_NUM_MAX_BFRAMES) > 0;
        Console.WriteLine($"{label} B-Frames={(bFrames ? "Supported" : "No")}");

        // 4:4:4 Chroma Check (Supported by select GPU architectures for high color fidelity)
        bool chroma444 = QueryCap(encoder, api, guid, _NV_ENC_CAPS.NV_ENC_CAPS_SUPPORT_YUV444_ENCODE) == 1;
        Console.WriteLine($"{label} 4:4:4 Chroma={(chroma444 ? "Supported" : "No")}");

        // Print dynamically queried profiles and presets
        Console.WriteLine($"{label} Profiles={QueryProfiles(encoder, api, guid)}");
        Console.WriteLine($"{label} Presets={QueryPresets(encoder, api, guid)}");
    }

    /// <summary>
    /// Helper method to safely invoke the native nvEncGetEncodeCaps function and return an integer value.
    /// </summary>
    /// <param name="encoder">The initialized NVENC encoder session pointer.</param>
    /// <param name="api">The populated NVENC API function list.</param>
    /// <param name="codec">The target codec GUID to query against.</param>
    /// <param name="cap">The specific capability enum to query.</param>
    /// <returns>The integer value of the capability, or 0 if unsupported/failed.</returns>
    private static int QueryCap(void* encoder, _NV_ENCODE_API_FUNCTION_LIST api, Guid codec, _NV_ENC_CAPS cap)
    {
        // Prepare the unmanaged struct with the necessary hardcoded version and capability target
        var param = new _NV_ENC_CAPS_PARAM 
        { 
            version = NvencApi.NV_ENC_CAPS_PARAM_VER, 
            capsToQuery = cap 
        };

        int val = 0;
        // Execute the native call via C# 9 function pointer; return the output value if successful
        return api.nvEncGetEncodeCaps(encoder, codec, &param, &val) == _NVENCSTATUS.NV_ENC_SUCCESS ? val : 0;
    }

    /// <summary>
    /// Queries the hardware for supported encoding profiles (e.g., Main, High) for a given codec.
    /// </summary>
    /// <param name="encoder">The initialized NVENC encoder session pointer.</param>
    /// <param name="api">The populated NVENC API function list.</param>
    /// <param name="codec">The specific codec GUID to query.</param>
    /// <returns>A comma-separated string of supported profiles, or "None"/"Unknown".</returns>
    private static string QueryProfiles(void* encoder, _NV_ENCODE_API_FUNCTION_LIST api, Guid codec)
    {
        uint count = 0;
        // Step 1: Query how many profiles are supported to allocate the correct buffer size
        if (api.nvEncGetEncodeProfileGUIDCount(encoder, codec, &count) != _NVENCSTATUS.NV_ENC_SUCCESS || count == 0) return "None";

        Guid[] profiles = new Guid[count];
        // Pin the array in memory so unmanaged code can safely write to it
        fixed (Guid* p = profiles)
        {
            // Step 2: Fetch the actual profile GUIDs into the pinned buffer
            if (api.nvEncGetEncodeProfileGUIDs(encoder, codec, p, count, &count) != _NVENCSTATUS.NV_ENC_SUCCESS) return "None";
            
            var result = new List<string>();
            // Map the raw hardware GUIDs to human-readable string names
            foreach (var g in profiles)
            {
                if (g == Methods.NV_ENC_H264_PROFILE_BASELINE_GUID) result.Add("Baseline");
                else if (g == Methods.NV_ENC_H264_PROFILE_MAIN_GUID) result.Add("Main");
                else if (g == Methods.NV_ENC_H264_PROFILE_HIGH_GUID) result.Add("High");
                else if (g == Methods.NV_ENC_HEVC_PROFILE_MAIN_GUID) result.Add("Main");
                else if (g == Methods.NV_ENC_HEVC_PROFILE_MAIN10_GUID) result.Add("Main10");
                else if (g == Methods.NV_ENC_HEVC_PROFILE_FREXT_GUID) result.Add("FREXT");
                else if (g == Methods.NV_ENC_AV1_PROFILE_MAIN_GUID) result.Add("Main");
            }
            
            return result.Count > 0 ? string.Join(", ", result) : "Unknown";
        }
    }

    /// <summary>
    /// Queries the hardware for supported encoding tuning presets (e.g., P1 through P7) for a given codec.
    /// </summary>
    /// <param name="encoder">The initialized NVENC encoder session pointer.</param>
    /// <param name="api">The populated NVENC API function list.</param>
    /// <param name="codec">The specific codec GUID to query.</param>
    /// <returns>A comma-separated string of supported presets, or "None".</returns>
    private static string QueryPresets(void* encoder, _NV_ENCODE_API_FUNCTION_LIST api, Guid codec)
    {
        uint count = 0;
        // Step 1: Query the total number of supported presets to size the buffer
        if (api.nvEncGetEncodePresetCount(encoder, codec, &count) != _NVENCSTATUS.NV_ENC_SUCCESS || count == 0) return "None";

        Guid[] presets = new Guid[count];
        // Pin the array so the native driver can populate it
        fixed (Guid* p = presets)
        {
            // Step 2: Fetch the actual preset GUIDs
            if (api.nvEncGetEncodePresetGUIDs(encoder, codec, p, count, &count) != _NVENCSTATUS.NV_ENC_SUCCESS) return "None";
            
            var result = new List<string>();
            
            // Loop through P1 to P7 and use reflection to dynamically check if the hardware array contains that preset GUID
            for (int i = 1; i <= 7; i++)
            {
                if (Array.Exists(presets, g => g == (Guid)typeof(Methods).GetField($"NV_ENC_PRESET_P{i}_GUID").GetValue(null)))
                    result.Add($"P{i}");
            }
            
            return result.Count > 0 ? string.Join(", ", result) : "None";
        }
    }

}