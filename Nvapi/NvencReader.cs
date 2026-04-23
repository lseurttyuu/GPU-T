using System;
using System.Runtime.InteropServices;

namespace GPU_T.Nvapi;

/// <summary>
/// NVIDIA Hardware Encoder (NVENC) sidecar reader.
/// Uses native libcuda and libnvidia-encode to extract real hardware encoding capabilities.
/// </summary>
internal static unsafe class NvencReader
{
    private const string CudaLibrary = "libcuda.so.1";
    private const string NvencLibrary = "libnvidia-encode.so.1";

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

    // NVENC API
    [DllImport(NvencLibrary, EntryPoint = "NvEncodeAPIGetMaxSupportedVersion")]
    private static extern int NvEncodeAPIGetMaxSupportedVersion(out uint version);

    [DllImport(NvencLibrary, EntryPoint = "NvEncodeAPICreateInstance")]
    private static extern int NvEncodeAPICreateInstance(ref NV_ENCODE_API_FUNCTION_LIST functionList);

    // Common NVENC Codec GUIDs (v13.0 Header)
    private static readonly Guid NV_ENC_CODEC_H264_GUID = new Guid("6bc82762-4e63-4ca4-aa85-1e50f321f6bf");
    private static readonly Guid NV_ENC_CODEC_HEVC_GUID = new Guid("790cdc88-4522-4d7b-9425-bda9975f7603");
    private static readonly Guid NV_ENC_CODEC_AV1_GUID  = new Guid("0a352289-0aa7-4759-862d-5d15cd16d254");
    // H.264 Profiles
    private static readonly Guid NV_ENC_H264_PROFILE_BASELINE_GUID = new Guid("0727bcaa-78c4-4c83-8c2f-ef3dff267c6a");
    private static readonly Guid NV_ENC_H264_PROFILE_MAIN_GUID = new Guid("60b5c1d4-67fe-4790-94d5-c4726d7b6e6d");
    private static readonly Guid NV_ENC_H264_PROFILE_HIGH_GUID = new Guid("e7cbc309-4f7a-4b89-af2a-d537c92be310");

    // HEVC Profiles
    private static readonly Guid NV_ENC_HEVC_PROFILE_MAIN_GUID = new Guid("b514c39a-b55b-40fa-878f-f1253b4dfdec");
    private static readonly Guid NV_ENC_HEVC_PROFILE_MAIN10_GUID = new Guid("fa4d2b6c-3a5b-411a-8018-0a3f5e3c9be5");
    private static readonly Guid NV_ENC_HEVC_PROFILE_FREXT_GUID = new Guid("51ec32b5-1b4c-453c-9cbd-b616bd621341");

    // AV1 Profiles
    private static readonly Guid NV_ENC_AV1_PROFILE_MAIN_GUID = new Guid("5f2a39f5-f14e-4f95-9a9e-b76d568fcf97");

    // NVENC Preset GUIDs (v13.0)
    private static readonly Guid NV_ENC_PRESET_P1_GUID = new Guid("fc0a8d3e-45f8-4cf8-80c7-298871590ebf");
    private static readonly Guid NV_ENC_PRESET_P2_GUID = new Guid("f581cfb8-88d6-4381-93f0-df13f9c27dab");
    private static readonly Guid NV_ENC_PRESET_P3_GUID = new Guid("36850110-3a07-441f-94d5-3670631f91f6");
    private static readonly Guid NV_ENC_PRESET_P4_GUID = new Guid("90a7b826-df06-4862-b9d2-cd6d73a08681");
    private static readonly Guid NV_ENC_PRESET_P5_GUID = new Guid("21c6e6b4-297a-4cba-998f-b6cbde72ade3");
    private static readonly Guid NV_ENC_PRESET_P6_GUID = new Guid("8e75c279-6299-4ab6-8302-0b215a335cf5");
    private static readonly Guid NV_ENC_PRESET_P7_GUID = new Guid("84848c12-6f71-4c13-931b-53e283f57974");

    // NVENC Capabilities Enums (v13.0)
    private const int NV_ENC_CAPS_NUM_MAX_BFRAMES = 0;
    private const int NV_ENC_CAPS_WIDTH_MAX = 16;
    private const int NV_ENC_CAPS_HEIGHT_MAX = 17;
    private const int NV_ENC_CAPS_MB_PER_SEC_MAX = 32;
    private const int NV_ENC_CAPS_SUPPORT_YUV444_ENCODE = 33;
    private const int NV_ENC_CAPS_SUPPORT_LOSSLESS_ENCODE = 34;
    private const int NV_ENC_CAPS_SUPPORT_10BIT_ENCODE = 39;
    private const int NV_ENC_CAPS_NUM_ENCODER_ENGINES = 49;

    // Helper to replicate the NVENCAPI_STRUCT_VERSION C-Macro exactly
    private static uint StructVersion(uint ver, uint apiVersion)
    {
        return apiVersion | (ver << 16) | (0x7u << 28);
    }

    // NVENC Struct Definitions (Strict 64-bit alignment)
   [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NV_ENCODE_API_FUNCTION_LIST
    {
        public uint version;
        public uint reserved;
        public IntPtr nvEncOpenEncodeSession;
        public IntPtr nvEncGetEncodeGUIDCount;
        public IntPtr nvEncGetEncodeProfileGUIDCount;
        public IntPtr nvEncGetEncodeProfileGUIDs;
        public IntPtr nvEncGetEncodeGUIDs;
        public IntPtr nvEncGetInputFormatCount;
        public IntPtr nvEncGetInputFormats;
        public IntPtr nvEncGetEncodeCaps;
        public IntPtr nvEncGetEncodePresetCount;
        public IntPtr nvEncGetEncodePresetGUIDs;
        public IntPtr nvEncGetEncodePresetConfig;
        public IntPtr nvEncInitializeEncoder;
        public IntPtr nvEncCreateInputBuffer;
        public IntPtr nvEncDestroyInputBuffer;
        public IntPtr nvEncCreateBitstreamBuffer;
        public IntPtr nvEncDestroyBitstreamBuffer;
        public IntPtr nvEncEncodePicture;
        public IntPtr nvEncLockBitstream;
        public IntPtr nvEncUnlockBitstream;
        public IntPtr nvEncLockInputBuffer;
        public IntPtr nvEncUnlockInputBuffer;
        public IntPtr nvEncGetEncodeStats;
        public IntPtr nvEncGetSequenceParams;
        public IntPtr nvEncRegisterAsyncEvent;
        public IntPtr nvEncUnregisterAsyncEvent;
        public IntPtr nvEncMapInputResource;
        public IntPtr nvEncUnmapInputResource;
        public IntPtr nvEncDestroyEncoder;
        public IntPtr nvEncInvalidateRefFrames;
        public IntPtr nvEncOpenEncodeSessionEx;
        public IntPtr nvEncRegisterResource;
        public IntPtr nvEncUnregisterResource;
        public IntPtr nvEncReconfigureEncoder;
        public IntPtr reserved1;
        public IntPtr nvEncCreateMVBuffer;
        public IntPtr nvEncDestroyMVBuffer;
        public IntPtr nvEncRunMotionEstimationOnly;
        public IntPtr nvEncGetLastErrorString;
        public IntPtr nvEncSetIOCudaStreams;
        public IntPtr nvEncGetEncodePresetConfigEx;
        public IntPtr nvEncGetSequenceParamEx;
        public IntPtr nvEncRestoreEncoderState;
        public IntPtr nvEncLookaheadPicture;
        public fixed ulong reserved2[275]; // Exactly 275 per the header
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS
    {
        public uint version;
        public uint deviceType; 
        public IntPtr device;   
        public IntPtr reserved;
        public uint apiVersion;
        public fixed uint reserved1[253];
        public fixed ulong reserved2[64]; 
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NV_ENC_CAPS_PARAM
    {
        public uint version;
        public int capsToQuery;
        public fixed uint reserved[62];
    }

    // Function Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncOpenEncodeSessionEx(ref NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS openParams, out IntPtr encoder);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodeGUIDCount(IntPtr encoder, out uint encodeGUIDCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodeGUIDs(IntPtr encoder, Guid* GUIDs, uint guidArraySize, out uint GUIDCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodeCaps(IntPtr encoder, Guid encodeGUID, ref NV_ENC_CAPS_PARAM capsParam, out int capsVal);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncDestroyEncoder(IntPtr encoder);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodeProfileGUIDCount(IntPtr encoder, Guid encodeGUID, out uint encodeProfileGUIDCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodeProfileGUIDs(IntPtr encoder, Guid encodeGUID, Guid* profileGUIDs, uint guidArraySize, out uint GUIDCount);

    // Preset Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodePresetCount(IntPtr encoder, Guid encodeGUID, out uint encodePresetGUIDCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEncGetEncodePresetGUIDs(IntPtr encoder, Guid encodeGUID, Guid* presetGUIDs, uint guidArraySize, out uint encodePresetGUIDCount);

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
        IntPtr encoder = IntPtr.Zero;
        NV_ENCODE_API_FUNCTION_LIST api = new NV_ENCODE_API_FUNCTION_LIST();

        try
        {
            if (CuInit(0) != 0) return 1;

            int cuDevice = 0;
            if (!string.IsNullOrEmpty(targetPciString))
            {
                if (CuDeviceGetByPCIBusId(out cuDevice, targetPciString) != 0) return 1;
            }

            // 1. Mandatory Contract: Output Compute Capability for the Hybrid Fallback safety net.
            CuDeviceGetAttribute(out int ccMajor, 75, cuDevice);
            CuDeviceGetAttribute(out int ccMinor, 76, cuDevice);
            Console.WriteLine($"Compute Capability={ccMajor}.{ccMinor}");

            // Create CUDA Context (Required to initialize NVENC)
            if (CuCtxCreate(out cuContext, 0, cuDevice) != 0) return 0; 

            // 2. Fetch the dynamic API Version from the local NVIDIA Driver
            if (NvEncodeAPIGetMaxSupportedVersion(out uint rawApiVersion) != 0) return 0;

            // Reformat the raw version (Major << 4 | Minor) to the Struct format (Major | Minor << 24)
            uint major = rawApiVersion >> 4;
            uint minor = rawApiVersion & 0xF;
            uint apiVersion = major | (minor << 24); 

            // 3. Load NVENC API using the magic struct version macro
            api.version = StructVersion(2, apiVersion);

            // 3. Load NVENC API using the magic struct version macro
            api.version = StructVersion(2, apiVersion); // NV_ENCODE_API_FUNCTION_LIST_VER = 2

            if (NvEncodeAPICreateInstance(ref api) != 0) return 0;

            var OpenSessionEx = Marshal.GetDelegateForFunctionPointer<NvEncOpenEncodeSessionEx>(api.nvEncOpenEncodeSessionEx);
            
            // 4. Open Encode Session
            var sessionParams = new NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS
            {
                version = StructVersion(1, apiVersion), // NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER = 1
                deviceType = 1, // NV_ENC_DEVICE_TYPE_CUDA = 1
                device = cuContext,
                apiVersion = apiVersion
            };

            if (OpenSessionEx(ref sessionParams, out encoder) != 0) return 0;

            // 5. Query Codecs
            var GetGuidCount = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodeGUIDCount>(api.nvEncGetEncodeGUIDCount);
            var GetGuids = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodeGUIDs>(api.nvEncGetEncodeGUIDs);
            var GetCaps = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodeCaps>(api.nvEncGetEncodeCaps);

            if (GetGuidCount(encoder, out uint guidCount) == 0 && guidCount > 0)
            {
                Guid[] guids = new Guid[guidCount];
                fixed (Guid* pGuids = guids)
                if (GetGuids(encoder, pGuids, guidCount, out uint actualCount) == 0)
                {
                    bool hasH264 = false, hasHevc = false, hasAv1 = false;
                    foreach (var g in guids)
                    {
                        if (g == NV_ENC_CODEC_H264_GUID) hasH264 = true;
                        if (g == NV_ENC_CODEC_HEVC_GUID) hasHevc = true;
                        if (g == NV_ENC_CODEC_AV1_GUID) hasAv1 = true;
                    }

                    // 6. Output Native Data
                    Console.WriteLine("[NVENC]");
                    
                    if (hasH264)
                    {
                        Console.WriteLine($"H.264 (AVC) Encode=Supported");
                        Console.WriteLine($"H.264 B-Frames={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_NUM_MAX_BFRAMES, apiVersion) > 0 ? "Supported" : "No")}");
                        Console.WriteLine($"H.264 Profiles={QueryProfiles(encoder, NV_ENC_CODEC_H264_GUID, api)}");
                        Console.WriteLine($"H.264 Presets={QueryPresets(encoder, NV_ENC_CODEC_H264_GUID, api)}");
                    }
                    else
                    {
                        Console.WriteLine($"H.264 (AVC) Encode=No");
                    }

                    if (hasHevc)
                    {
                        Console.WriteLine($"HEVC (H.265) Encode=Supported");
                        Console.WriteLine($"HEVC 10-bit Encode={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_HEVC_GUID, NV_ENC_CAPS_SUPPORT_10BIT_ENCODE, apiVersion) == 1 ? "Supported" : "No")}");
                        Console.WriteLine($"HEVC 4:4:4 Chroma={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_HEVC_GUID, NV_ENC_CAPS_SUPPORT_YUV444_ENCODE, apiVersion) == 1 ? "Supported" : "No")}");
                        Console.WriteLine($"HEVC B-Frames={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_HEVC_GUID, NV_ENC_CAPS_NUM_MAX_BFRAMES, apiVersion) > 0 ? "Supported" : "No")}");
                        Console.WriteLine($"HEVC Profiles={QueryProfiles(encoder, NV_ENC_CODEC_HEVC_GUID, api)}");
                        Console.WriteLine($"HEVC Presets={QueryPresets(encoder, NV_ENC_CODEC_HEVC_GUID, api)}");
                    }
                    else
                    {
                        Console.WriteLine($"HEVC (H.265) Encode=No");
                    }

                    if (hasAv1)
                    {
                        Console.WriteLine($"AV1 Encode=Supported");
                        Console.WriteLine($"AV1 10-bit Encode={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_AV1_GUID, NV_ENC_CAPS_SUPPORT_10BIT_ENCODE, apiVersion) == 1 ? "Supported" : "No")}");
                        Console.WriteLine($"AV1 Profiles={QueryProfiles(encoder, NV_ENC_CODEC_AV1_GUID, api)}");
                        Console.WriteLine($"AV1 Presets={QueryPresets(encoder, NV_ENC_CODEC_AV1_GUID, api)}");
                    }
                    else
                    {
                        Console.WriteLine($"AV1 Encode=No");
                    }

                    // Print Max Resolution (Uses H264 as a baseline)
                    int maxW = QueryCap(GetCaps, encoder, hasHevc ? NV_ENC_CODEC_HEVC_GUID : NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_WIDTH_MAX, apiVersion);
                    int maxH = QueryCap(GetCaps, encoder, hasHevc ? NV_ENC_CODEC_HEVC_GUID : NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_HEIGHT_MAX, apiVersion);
                    if (maxW > 0 && maxH > 0) Console.WriteLine($"Max Encoding Resolution={maxW} x {maxH}");

                    int engines = QueryCap(GetCaps, encoder, NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_NUM_ENCODER_ENGINES, apiVersion);
                    if (engines > 0) Console.WriteLine($"Hardware Encoder Engines={engines}");

                    int mbPerSec = QueryCap(GetCaps, encoder, NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_MB_PER_SEC_MAX, apiVersion);
                    if (mbPerSec > 0) Console.WriteLine($"Max Throughput={mbPerSec} Macroblocks/sec");

                    Console.WriteLine($"Lossless Encoding={(QueryCap(GetCaps, encoder, NV_ENC_CODEC_H264_GUID, NV_ENC_CAPS_SUPPORT_LOSSLESS_ENCODE, apiVersion) == 1 ? "Supported" : "No")}");
                }
            }

            return 0;
        }
        catch
        {
            return 0; 
        }
        finally
        {
            // Clean up Native Pointers to prevent context leaking
            if (encoder != IntPtr.Zero && api.nvEncDestroyEncoder != IntPtr.Zero)
            {
                var Destroy = Marshal.GetDelegateForFunctionPointer<NvEncDestroyEncoder>(api.nvEncDestroyEncoder);
                Destroy(encoder);
            }
            if (cuContext != IntPtr.Zero) CuCtxDestroy(cuContext);
        }
    }

    private static int QueryCap(NvEncGetEncodeCaps getCapsFunc, IntPtr encoder, Guid codec, int capId, uint apiVersion)
    {
        var capParam = new NV_ENC_CAPS_PARAM
        {
            version = StructVersion(1, apiVersion), // NV_ENC_CAPS_PARAM_VER = 1
            capsToQuery = capId
        };
        
        if (getCapsFunc(encoder, codec, ref capParam, out int val) == 0) return val;
        return 0;
    }

    /// <summary>
    /// Helper to query and format supported presets for a given codec. Presets are predefined configurations that 
    /// optimize encoding settings for specific use cases (e.g., low latency, high quality). This function checks 
    /// which presets are supported by the hardware for the specified codec and returns a human-readable list of those presets.
    /// </summary>
    /// <param name="encoder">The encoder instance.</param>
    /// <param name="codec">The codec for which to query presets.</param>
    /// <param name="api">The NV_ENCODE_API_FUNCTION_LIST instance.</param>
    /// <returns>The list of supported presets for the specified codec.</returns>
    private static string QueryPresets(IntPtr encoder, Guid codec, NV_ENCODE_API_FUNCTION_LIST api)
    {
        var GetPresetCount = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodePresetCount>(api.nvEncGetEncodePresetCount);
        var GetPresets = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodePresetGUIDs>(api.nvEncGetEncodePresetGUIDs);

        if (GetPresetCount(encoder, codec, out uint presetCount) != 0 || presetCount == 0) return "None";

        Guid[] presets = new Guid[presetCount];
        fixed (Guid* pPresets = presets)
        {
            if (GetPresets(encoder, codec, pPresets, presetCount, out uint actualCount) == 0)
            {
                var supported = new System.Collections.Generic.List<string>();
                foreach (var p in presets)
                {
                    if (p == NV_ENC_PRESET_P1_GUID) supported.Add("P1");
                    if (p == NV_ENC_PRESET_P2_GUID) supported.Add("P2");
                    if (p == NV_ENC_PRESET_P3_GUID) supported.Add("P3");
                    if (p == NV_ENC_PRESET_P4_GUID) supported.Add("P4");
                    if (p == NV_ENC_PRESET_P5_GUID) supported.Add("P5");
                    if (p == NV_ENC_PRESET_P6_GUID) supported.Add("P6");
                    if (p == NV_ENC_PRESET_P7_GUID) supported.Add("P7");
                }
                if (supported.Count > 0) return string.Join(", ", supported);
            }
        }
        return "None";
    }

    /// <summary>
    /// Helper to query and format supported profiles for a given codec. Profiles are a key aspect of encoding capabilities,
    /// defining specific feature sets and constraints. This function checks which profiles are supported 
    /// by the hardware for the specified codec and returns a human-readable list of those profiles.
    /// </summary>
    /// <param name="encoder">The encoder instance.</param>
    /// <param name="codec">The codec for which to query profiles.</param>
    /// <param name="api">The NV_ENCODE_API_FUNCTION_LIST instance.</param>
    /// <returns>The list of supported profiles for the specified codec.</returns>
    private static string QueryProfiles(IntPtr encoder, Guid codec, NV_ENCODE_API_FUNCTION_LIST api)
    {
        var GetProfileCount = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodeProfileGUIDCount>(api.nvEncGetEncodeProfileGUIDCount);
        var GetProfiles = Marshal.GetDelegateForFunctionPointer<NvEncGetEncodeProfileGUIDs>(api.nvEncGetEncodeProfileGUIDs);

        if (GetProfileCount(encoder, codec, out uint profCount) != 0 || profCount == 0) return "None";

        Guid[] profiles = new Guid[profCount];
        fixed (Guid* pProfiles = profiles)
        {
            if (GetProfiles(encoder, codec, pProfiles, profCount, out uint actualCount) == 0)
            {
                var supported = new System.Collections.Generic.List<string>();
                foreach (var p in profiles)
                {
                    if (p == NV_ENC_H264_PROFILE_BASELINE_GUID) supported.Add("Baseline");
                    else if (p == NV_ENC_H264_PROFILE_MAIN_GUID) supported.Add("Main");
                    else if (p == NV_ENC_H264_PROFILE_HIGH_GUID) supported.Add("High");
                    else if (p == NV_ENC_HEVC_PROFILE_MAIN_GUID) supported.Add("Main");
                    else if (p == NV_ENC_HEVC_PROFILE_MAIN10_GUID) supported.Add("Main10");
                    else if (p == NV_ENC_HEVC_PROFILE_FREXT_GUID) supported.Add("FREXT");
                    else if (p == NV_ENC_AV1_PROFILE_MAIN_GUID) supported.Add("Main");
                }
                if (supported.Count > 0) return string.Join(", ", supported);
            }
        }
        return "None";
    }

}