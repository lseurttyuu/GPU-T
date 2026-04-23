
using System;
using System.Runtime.InteropServices;

namespace GPU_T.Nvapi.Interop
{
    /* public unsafe partial struct Guid
    {
        [NativeTypeName("uint32_t")]
        public uint Data1;

        [NativeTypeName("uint16_t")]
        public ushort Data2;

        [NativeTypeName("uint16_t")]
        public ushort Data3;

        [NativeTypeName("uint8_t[8]")]
        public fixed byte Data4[8];
    }

    */ public partial struct _NVENC_RECT
    {
        [NativeTypeName("uint32_t")]
        public uint left;

        [NativeTypeName("uint32_t")]
        public uint top;

        [NativeTypeName("uint32_t")]
        public uint right;

        [NativeTypeName("uint32_t")]
        public uint bottom;
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_PARAMS_FRAME_FIELD_MODE : uint
    {
        NV_ENC_PARAMS_FRAME_FIELD_MODE_FRAME = 0x01,
        NV_ENC_PARAMS_FRAME_FIELD_MODE_FIELD = 0x02,
        NV_ENC_PARAMS_FRAME_FIELD_MODE_MBAFF = 0x03,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_PARAMS_RC_MODE : uint
    {
        NV_ENC_PARAMS_RC_CONSTQP = 0x0,
        NV_ENC_PARAMS_RC_VBR = 0x1,
        NV_ENC_PARAMS_RC_CBR = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_MULTI_PASS : uint
    {
        NV_ENC_MULTI_PASS_DISABLED = 0x0,
        NV_ENC_TWO_PASS_QUARTER_RESOLUTION = 0x1,
        NV_ENC_TWO_PASS_FULL_RESOLUTION = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_STATE_RESTORE_TYPE : uint
    {
        NV_ENC_STATE_RESTORE_FULL = 0x01,
        NV_ENC_STATE_RESTORE_RATE_CONTROL = 0x02,
        NV_ENC_STATE_RESTORE_ENCODE = 0x03,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_OUTPUT_STATS_LEVEL : uint
    {
        NV_ENC_OUTPUT_STATS_NONE = 0,
        NV_ENC_OUTPUT_STATS_BLOCK_LEVEL = 1,
        NV_ENC_OUTPUT_STATS_ROW_LEVEL = 2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_EMPHASIS_MAP_LEVEL : uint
    {
        NV_ENC_EMPHASIS_MAP_LEVEL_0 = 0x0,
        NV_ENC_EMPHASIS_MAP_LEVEL_1 = 0x1,
        NV_ENC_EMPHASIS_MAP_LEVEL_2 = 0x2,
        NV_ENC_EMPHASIS_MAP_LEVEL_3 = 0x3,
        NV_ENC_EMPHASIS_MAP_LEVEL_4 = 0x4,
        NV_ENC_EMPHASIS_MAP_LEVEL_5 = 0x5,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_QP_MAP_MODE : uint
    {
        NV_ENC_QP_MAP_DISABLED = 0x0,
        NV_ENC_QP_MAP_EMPHASIS = 0x1,
        NV_ENC_QP_MAP_DELTA = 0x2,
        NV_ENC_QP_MAP = 0x3,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_PIC_STRUCT : uint
    {
        NV_ENC_PIC_STRUCT_FRAME = 0x01,
        NV_ENC_PIC_STRUCT_FIELD_TOP_BOTTOM = 0x02,
        NV_ENC_PIC_STRUCT_FIELD_BOTTOM_TOP = 0x03,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_DISPLAY_PIC_STRUCT : uint
    {
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME = 0x00,
        NV_ENC_PIC_STRUCT_DISPLAY_FIELD_TOP_BOTTOM = 0x01,
        NV_ENC_PIC_STRUCT_DISPLAY_FIELD_BOTTOM_TOP = 0x02,
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME_DOUBLING = 0x03,
        NV_ENC_PIC_STRUCT_DISPLAY_FRAME_TRIPLING = 0x04,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_PIC_TYPE : uint
    {
        NV_ENC_PIC_TYPE_P = 0x0,
        NV_ENC_PIC_TYPE_B = 0x01,
        NV_ENC_PIC_TYPE_I = 0x02,
        NV_ENC_PIC_TYPE_IDR = 0x03,
        NV_ENC_PIC_TYPE_BI = 0x04,
        NV_ENC_PIC_TYPE_SKIPPED = 0x05,
        NV_ENC_PIC_TYPE_INTRA_REFRESH = 0x06,
        NV_ENC_PIC_TYPE_NONREF_P = 0x07,
        NV_ENC_PIC_TYPE_SWITCH = 0x08,
        NV_ENC_PIC_TYPE_UNKNOWN = 0xFF,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_MV_PRECISION : uint
    {
        NV_ENC_MV_PRECISION_DEFAULT = 0x0,
        NV_ENC_MV_PRECISION_FULL_PEL = 0x01,
        NV_ENC_MV_PRECISION_HALF_PEL = 0x02,
        NV_ENC_MV_PRECISION_QUARTER_PEL = 0x03,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_BUFFER_FORMAT : uint
    {
        NV_ENC_BUFFER_FORMAT_UNDEFINED = 0x00000000,
        NV_ENC_BUFFER_FORMAT_NV12 = 0x00000001,
        NV_ENC_BUFFER_FORMAT_YV12 = 0x00000010,
        NV_ENC_BUFFER_FORMAT_IYUV = 0x00000100,
        NV_ENC_BUFFER_FORMAT_YUV444 = 0x00001000,
        NV_ENC_BUFFER_FORMAT_YUV420_10BIT = 0x00010000,
        NV_ENC_BUFFER_FORMAT_YUV444_10BIT = 0x00100000,
        NV_ENC_BUFFER_FORMAT_ARGB = 0x01000000,
        NV_ENC_BUFFER_FORMAT_ARGB10 = 0x02000000,
        NV_ENC_BUFFER_FORMAT_AYUV = 0x04000000,
        NV_ENC_BUFFER_FORMAT_ABGR = 0x10000000,
        NV_ENC_BUFFER_FORMAT_ABGR10 = 0x20000000,
        NV_ENC_BUFFER_FORMAT_U8 = 0x40000000,
        NV_ENC_BUFFER_FORMAT_NV16 = 0x40000001,
        NV_ENC_BUFFER_FORMAT_P210 = 0x40000002,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_LEVEL : uint
    {
        NV_ENC_LEVEL_AUTOSELECT = 0,
        NV_ENC_LEVEL_H264_1 = 10,
        NV_ENC_LEVEL_H264_1b = 9,
        NV_ENC_LEVEL_H264_11 = 11,
        NV_ENC_LEVEL_H264_12 = 12,
        NV_ENC_LEVEL_H264_13 = 13,
        NV_ENC_LEVEL_H264_2 = 20,
        NV_ENC_LEVEL_H264_21 = 21,
        NV_ENC_LEVEL_H264_22 = 22,
        NV_ENC_LEVEL_H264_3 = 30,
        NV_ENC_LEVEL_H264_31 = 31,
        NV_ENC_LEVEL_H264_32 = 32,
        NV_ENC_LEVEL_H264_4 = 40,
        NV_ENC_LEVEL_H264_41 = 41,
        NV_ENC_LEVEL_H264_42 = 42,
        NV_ENC_LEVEL_H264_5 = 50,
        NV_ENC_LEVEL_H264_51 = 51,
        NV_ENC_LEVEL_H264_52 = 52,
        NV_ENC_LEVEL_H264_60 = 60,
        NV_ENC_LEVEL_H264_61 = 61,
        NV_ENC_LEVEL_H264_62 = 62,
        NV_ENC_LEVEL_HEVC_1 = 30,
        NV_ENC_LEVEL_HEVC_2 = 60,
        NV_ENC_LEVEL_HEVC_21 = 63,
        NV_ENC_LEVEL_HEVC_3 = 90,
        NV_ENC_LEVEL_HEVC_31 = 93,
        NV_ENC_LEVEL_HEVC_4 = 120,
        NV_ENC_LEVEL_HEVC_41 = 123,
        NV_ENC_LEVEL_HEVC_5 = 150,
        NV_ENC_LEVEL_HEVC_51 = 153,
        NV_ENC_LEVEL_HEVC_52 = 156,
        NV_ENC_LEVEL_HEVC_6 = 180,
        NV_ENC_LEVEL_HEVC_61 = 183,
        NV_ENC_LEVEL_HEVC_62 = 186,
        NV_ENC_TIER_HEVC_MAIN = 0,
        NV_ENC_TIER_HEVC_HIGH = 1,
        NV_ENC_LEVEL_AV1_2 = 0,
        NV_ENC_LEVEL_AV1_21 = 1,
        NV_ENC_LEVEL_AV1_22 = 2,
        NV_ENC_LEVEL_AV1_23 = 3,
        NV_ENC_LEVEL_AV1_3 = 4,
        NV_ENC_LEVEL_AV1_31 = 5,
        NV_ENC_LEVEL_AV1_32 = 6,
        NV_ENC_LEVEL_AV1_33 = 7,
        NV_ENC_LEVEL_AV1_4 = 8,
        NV_ENC_LEVEL_AV1_41 = 9,
        NV_ENC_LEVEL_AV1_42 = 10,
        NV_ENC_LEVEL_AV1_43 = 11,
        NV_ENC_LEVEL_AV1_5 = 12,
        NV_ENC_LEVEL_AV1_51 = 13,
        NV_ENC_LEVEL_AV1_52 = 14,
        NV_ENC_LEVEL_AV1_53 = 15,
        NV_ENC_LEVEL_AV1_6 = 16,
        NV_ENC_LEVEL_AV1_61 = 17,
        NV_ENC_LEVEL_AV1_62 = 18,
        NV_ENC_LEVEL_AV1_63 = 19,
        NV_ENC_LEVEL_AV1_7 = 20,
        NV_ENC_LEVEL_AV1_71 = 21,
        NV_ENC_LEVEL_AV1_72 = 22,
        NV_ENC_LEVEL_AV1_73 = 23,
        NV_ENC_LEVEL_AV1_AUTOSELECT,
        NV_ENC_TIER_AV1_0 = 0,
        NV_ENC_TIER_AV1_1 = 1,
    }

    [NativeTypeName("unsigned int")]
    public enum _NVENCSTATUS : uint
    {
        NV_ENC_SUCCESS,
        NV_ENC_ERR_NO_ENCODE_DEVICE,
        NV_ENC_ERR_UNSUPPORTED_DEVICE,
        NV_ENC_ERR_INVALID_ENCODERDEVICE,
        NV_ENC_ERR_INVALID_DEVICE,
        NV_ENC_ERR_DEVICE_NOT_EXIST,
        NV_ENC_ERR_INVALID_PTR,
        NV_ENC_ERR_INVALID_EVENT,
        NV_ENC_ERR_INVALID_PARAM,
        NV_ENC_ERR_INVALID_CALL,
        NV_ENC_ERR_OUT_OF_MEMORY,
        NV_ENC_ERR_ENCODER_NOT_INITIALIZED,
        NV_ENC_ERR_UNSUPPORTED_PARAM,
        NV_ENC_ERR_LOCK_BUSY,
        NV_ENC_ERR_NOT_ENOUGH_BUFFER,
        NV_ENC_ERR_INVALID_VERSION,
        NV_ENC_ERR_MAP_FAILED,
        NV_ENC_ERR_NEED_MORE_INPUT,
        NV_ENC_ERR_ENCODER_BUSY,
        NV_ENC_ERR_EVENT_NOT_REGISTERD,
        NV_ENC_ERR_GENERIC,
        NV_ENC_ERR_INCOMPATIBLE_CLIENT_KEY,
        NV_ENC_ERR_UNIMPLEMENTED,
        NV_ENC_ERR_RESOURCE_REGISTER_FAILED,
        NV_ENC_ERR_RESOURCE_NOT_REGISTERED,
        NV_ENC_ERR_RESOURCE_NOT_MAPPED,
        NV_ENC_ERR_NEED_MORE_OUTPUT,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_PIC_FLAGS : uint
    {
        NV_ENC_PIC_FLAG_FORCEINTRA = 0x1,
        NV_ENC_PIC_FLAG_FORCEIDR = 0x2,
        NV_ENC_PIC_FLAG_OUTPUT_SPSPPS = 0x4,
        NV_ENC_PIC_FLAG_EOS = 0x8,
        NV_ENC_PIC_FLAG_DISABLE_ENC_STATE_ADVANCE = 0x10,
        NV_ENC_PIC_FLAG_OUTPUT_RECON_FRAME = 0x20,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_MEMORY_HEAP : uint
    {
        NV_ENC_MEMORY_HEAP_AUTOSELECT = 0,
        NV_ENC_MEMORY_HEAP_VID = 1,
        NV_ENC_MEMORY_HEAP_SYSMEM_CACHED = 2,
        NV_ENC_MEMORY_HEAP_SYSMEM_UNCACHED = 3,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_BFRAME_REF_MODE : uint
    {
        NV_ENC_BFRAME_REF_MODE_DISABLED = 0x0,
        NV_ENC_BFRAME_REF_MODE_EACH = 0x1,
        NV_ENC_BFRAME_REF_MODE_MIDDLE = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_H264_ENTROPY_CODING_MODE : uint
    {
        NV_ENC_H264_ENTROPY_CODING_MODE_AUTOSELECT = 0x0,
        NV_ENC_H264_ENTROPY_CODING_MODE_CABAC = 0x1,
        NV_ENC_H264_ENTROPY_CODING_MODE_CAVLC = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_H264_BDIRECT_MODE : uint
    {
        NV_ENC_H264_BDIRECT_MODE_AUTOSELECT = 0x0,
        NV_ENC_H264_BDIRECT_MODE_DISABLE = 0x1,
        NV_ENC_H264_BDIRECT_MODE_TEMPORAL = 0x2,
        NV_ENC_H264_BDIRECT_MODE_SPATIAL = 0x3,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_H264_FMO_MODE : uint
    {
        NV_ENC_H264_FMO_AUTOSELECT = 0x0,
        NV_ENC_H264_FMO_ENABLE = 0x1,
        NV_ENC_H264_FMO_DISABLE = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_H264_ADAPTIVE_TRANSFORM_MODE : uint
    {
        NV_ENC_H264_ADAPTIVE_TRANSFORM_AUTOSELECT = 0x0,
        NV_ENC_H264_ADAPTIVE_TRANSFORM_DISABLE = 0x1,
        NV_ENC_H264_ADAPTIVE_TRANSFORM_ENABLE = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_STEREO_PACKING_MODE : uint
    {
        NV_ENC_STEREO_PACKING_MODE_NONE = 0x0,
        NV_ENC_STEREO_PACKING_MODE_CHECKERBOARD = 0x1,
        NV_ENC_STEREO_PACKING_MODE_COLINTERLEAVE = 0x2,
        NV_ENC_STEREO_PACKING_MODE_ROWINTERLEAVE = 0x3,
        NV_ENC_STEREO_PACKING_MODE_SIDEBYSIDE = 0x4,
        NV_ENC_STEREO_PACKING_MODE_TOPBOTTOM = 0x5,
        NV_ENC_STEREO_PACKING_MODE_FRAMESEQ = 0x6,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_INPUT_RESOURCE_TYPE : uint
    {
        NV_ENC_INPUT_RESOURCE_TYPE_DIRECTX = 0x0,
        NV_ENC_INPUT_RESOURCE_TYPE_CUDADEVICEPTR = 0x1,
        NV_ENC_INPUT_RESOURCE_TYPE_CUDAARRAY = 0x2,
        NV_ENC_INPUT_RESOURCE_TYPE_OPENGL_TEX = 0x3,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_BUFFER_USAGE : uint
    {
        NV_ENC_INPUT_IMAGE = 0x0,
        NV_ENC_OUTPUT_MOTION_VECTOR = 0x1,
        NV_ENC_OUTPUT_BITSTREAM = 0x2,
        NV_ENC_OUTPUT_RECON = 0x4,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_DEVICE_TYPE : uint
    {
        NV_ENC_DEVICE_TYPE_DIRECTX = 0x0,
        NV_ENC_DEVICE_TYPE_CUDA = 0x1,
        NV_ENC_DEVICE_TYPE_OPENGL = 0x2,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_NUM_REF_FRAMES : uint
    {
        NV_ENC_NUM_REF_FRAMES_AUTOSELECT = 0x0,
        NV_ENC_NUM_REF_FRAMES_1 = 0x1,
        NV_ENC_NUM_REF_FRAMES_2 = 0x2,
        NV_ENC_NUM_REF_FRAMES_3 = 0x3,
        NV_ENC_NUM_REF_FRAMES_4 = 0x4,
        NV_ENC_NUM_REF_FRAMES_5 = 0x5,
        NV_ENC_NUM_REF_FRAMES_6 = 0x6,
        NV_ENC_NUM_REF_FRAMES_7 = 0x7,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_TEMPORAL_FILTER_LEVEL : uint
    {
        NV_ENC_TEMPORAL_FILTER_LEVEL_0 = 0,
        NV_ENC_TEMPORAL_FILTER_LEVEL_4 = 4,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_CAPS : uint
    {
        NV_ENC_CAPS_NUM_MAX_BFRAMES,
        NV_ENC_CAPS_SUPPORTED_RATECONTROL_MODES,
        NV_ENC_CAPS_SUPPORT_FIELD_ENCODING,
        NV_ENC_CAPS_SUPPORT_MONOCHROME,
        NV_ENC_CAPS_SUPPORT_FMO,
        NV_ENC_CAPS_SUPPORT_QPELMV,
        NV_ENC_CAPS_SUPPORT_BDIRECT_MODE,
        NV_ENC_CAPS_SUPPORT_CABAC,
        NV_ENC_CAPS_SUPPORT_ADAPTIVE_TRANSFORM,
        NV_ENC_CAPS_SUPPORT_STEREO_MVC,
        NV_ENC_CAPS_NUM_MAX_TEMPORAL_LAYERS,
        NV_ENC_CAPS_SUPPORT_HIERARCHICAL_PFRAMES,
        NV_ENC_CAPS_SUPPORT_HIERARCHICAL_BFRAMES,
        NV_ENC_CAPS_LEVEL_MAX,
        NV_ENC_CAPS_LEVEL_MIN,
        NV_ENC_CAPS_SEPARATE_COLOUR_PLANE,
        NV_ENC_CAPS_WIDTH_MAX,
        NV_ENC_CAPS_HEIGHT_MAX,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_SVC,
        NV_ENC_CAPS_SUPPORT_DYN_RES_CHANGE,
        NV_ENC_CAPS_SUPPORT_DYN_BITRATE_CHANGE,
        NV_ENC_CAPS_SUPPORT_DYN_FORCE_CONSTQP,
        NV_ENC_CAPS_SUPPORT_DYN_RCMODE_CHANGE,
        NV_ENC_CAPS_SUPPORT_SUBFRAME_READBACK,
        NV_ENC_CAPS_SUPPORT_CONSTRAINED_ENCODING,
        NV_ENC_CAPS_SUPPORT_INTRA_REFRESH,
        NV_ENC_CAPS_SUPPORT_CUSTOM_VBV_BUF_SIZE,
        NV_ENC_CAPS_SUPPORT_DYNAMIC_SLICE_MODE,
        NV_ENC_CAPS_SUPPORT_REF_PIC_INVALIDATION,
        NV_ENC_CAPS_PREPROC_SUPPORT,
        NV_ENC_CAPS_ASYNC_ENCODE_SUPPORT,
        NV_ENC_CAPS_MB_NUM_MAX,
        NV_ENC_CAPS_MB_PER_SEC_MAX,
        NV_ENC_CAPS_SUPPORT_YUV444_ENCODE,
        NV_ENC_CAPS_SUPPORT_LOSSLESS_ENCODE,
        NV_ENC_CAPS_SUPPORT_SAO,
        NV_ENC_CAPS_SUPPORT_MEONLY_MODE,
        NV_ENC_CAPS_SUPPORT_LOOKAHEAD,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_AQ,
        NV_ENC_CAPS_SUPPORT_10BIT_ENCODE,
        NV_ENC_CAPS_NUM_MAX_LTR_FRAMES,
        NV_ENC_CAPS_SUPPORT_WEIGHTED_PREDICTION,
        NV_ENC_CAPS_DYNAMIC_QUERY_ENCODER_CAPACITY,
        NV_ENC_CAPS_SUPPORT_BFRAME_REF_MODE,
        NV_ENC_CAPS_SUPPORT_EMPHASIS_LEVEL_MAP,
        NV_ENC_CAPS_WIDTH_MIN,
        NV_ENC_CAPS_HEIGHT_MIN,
        NV_ENC_CAPS_SUPPORT_MULTIPLE_REF_FRAMES,
        NV_ENC_CAPS_SUPPORT_ALPHA_LAYER_ENCODING,
        NV_ENC_CAPS_NUM_ENCODER_ENGINES,
        NV_ENC_CAPS_SINGLE_SLICE_INTRA_REFRESH,
        NV_ENC_CAPS_DISABLE_ENC_STATE_ADVANCE,
        NV_ENC_CAPS_OUTPUT_RECON_SURFACE,
        NV_ENC_CAPS_OUTPUT_BLOCK_STATS,
        NV_ENC_CAPS_OUTPUT_ROW_STATS,
        NV_ENC_CAPS_SUPPORT_TEMPORAL_FILTER,
        NV_ENC_CAPS_SUPPORT_LOOKAHEAD_LEVEL,
        NV_ENC_CAPS_SUPPORT_UNIDIRECTIONAL_B,
        NV_ENC_CAPS_SUPPORT_MVHEVC_ENCODE,
        NV_ENC_CAPS_SUPPORT_YUV422_ENCODE,
        NV_ENC_CAPS_EXPOSED_COUNT,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_HEVC_CUSIZE : uint
    {
        NV_ENC_HEVC_CUSIZE_AUTOSELECT = 0,
        NV_ENC_HEVC_CUSIZE_8x8 = 1,
        NV_ENC_HEVC_CUSIZE_16x16 = 2,
        NV_ENC_HEVC_CUSIZE_32x32 = 3,
        NV_ENC_HEVC_CUSIZE_64x64 = 4,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_AV1_PART_SIZE : uint
    {
        NV_ENC_AV1_PART_SIZE_AUTOSELECT = 0,
        NV_ENC_AV1_PART_SIZE_4x4 = 1,
        NV_ENC_AV1_PART_SIZE_8x8 = 2,
        NV_ENC_AV1_PART_SIZE_16x16 = 3,
        NV_ENC_AV1_PART_SIZE_32x32 = 4,
        NV_ENC_AV1_PART_SIZE_64x64 = 5,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_VUI_VIDEO_FORMAT : uint
    {
        NV_ENC_VUI_VIDEO_FORMAT_COMPONENT = 0,
        NV_ENC_VUI_VIDEO_FORMAT_PAL = 1,
        NV_ENC_VUI_VIDEO_FORMAT_NTSC = 2,
        NV_ENC_VUI_VIDEO_FORMAT_SECAM = 3,
        NV_ENC_VUI_VIDEO_FORMAT_MAC = 4,
        NV_ENC_VUI_VIDEO_FORMAT_UNSPECIFIED = 5,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_VUI_COLOR_PRIMARIES : uint
    {
        NV_ENC_VUI_COLOR_PRIMARIES_UNDEFINED = 0,
        NV_ENC_VUI_COLOR_PRIMARIES_BT709 = 1,
        NV_ENC_VUI_COLOR_PRIMARIES_UNSPECIFIED = 2,
        NV_ENC_VUI_COLOR_PRIMARIES_RESERVED = 3,
        NV_ENC_VUI_COLOR_PRIMARIES_BT470M = 4,
        NV_ENC_VUI_COLOR_PRIMARIES_BT470BG = 5,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE170M = 6,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE240M = 7,
        NV_ENC_VUI_COLOR_PRIMARIES_FILM = 8,
        NV_ENC_VUI_COLOR_PRIMARIES_BT2020 = 9,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE428 = 10,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE431 = 11,
        NV_ENC_VUI_COLOR_PRIMARIES_SMPTE432 = 12,
        NV_ENC_VUI_COLOR_PRIMARIES_JEDEC_P22 = 22,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_VUI_TRANSFER_CHARACTERISTIC : uint
    {
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_UNDEFINED = 0,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT709 = 1,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_UNSPECIFIED = 2,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_RESERVED = 3,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT470M = 4,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT470BG = 5,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE170M = 6,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE240M = 7,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LINEAR = 8,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LOG = 9,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_LOG_SQRT = 10,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_IEC61966_2_4 = 11,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT1361_ECG = 12,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SRGB = 13,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT2020_10 = 14,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_BT2020_12 = 15,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE2084 = 16,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_SMPTE428 = 17,
        NV_ENC_VUI_TRANSFER_CHARACTERISTIC_ARIB_STD_B67 = 18,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_VUI_MATRIX_COEFFS : uint
    {
        NV_ENC_VUI_MATRIX_COEFFS_RGB = 0,
        NV_ENC_VUI_MATRIX_COEFFS_BT709 = 1,
        NV_ENC_VUI_MATRIX_COEFFS_UNSPECIFIED = 2,
        NV_ENC_VUI_MATRIX_COEFFS_RESERVED = 3,
        NV_ENC_VUI_MATRIX_COEFFS_FCC = 4,
        NV_ENC_VUI_MATRIX_COEFFS_BT470BG = 5,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE170M = 6,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE240M = 7,
        NV_ENC_VUI_MATRIX_COEFFS_YCGCO = 8,
        NV_ENC_VUI_MATRIX_COEFFS_BT2020_NCL = 9,
        NV_ENC_VUI_MATRIX_COEFFS_BT2020_CL = 10,
        NV_ENC_VUI_MATRIX_COEFFS_SMPTE2085 = 11,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_LOOKAHEAD_LEVEL : uint
    {
        NV_ENC_LOOKAHEAD_LEVEL_0 = 0,
        NV_ENC_LOOKAHEAD_LEVEL_1 = 1,
        NV_ENC_LOOKAHEAD_LEVEL_2 = 2,
        NV_ENC_LOOKAHEAD_LEVEL_3 = 3,
        NV_ENC_LOOKAHEAD_LEVEL_AUTOSELECT = 15,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_BIT_DEPTH : uint
    {
        NV_ENC_BIT_DEPTH_INVALID = 0,
        NV_ENC_BIT_DEPTH_8 = 8,
        NV_ENC_BIT_DEPTH_10 = 10,
    }

    public unsafe partial struct _NV_ENC_CAPS_PARAM
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("NV_ENC_CAPS")]
        public _NV_ENC_CAPS capsToQuery;

        [NativeTypeName("uint32_t[62]")]
        public fixed uint reserved[62];
    }

    public unsafe partial struct _NV_ENC_RESTORE_ENCODER_STATE_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint bufferIdx;

        [NativeTypeName("NV_ENC_STATE_RESTORE_TYPE")]
        public _NV_ENC_STATE_RESTORE_TYPE state;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* outputBitstream;

        public void* completionEvent;

        [NativeTypeName("uint32_t[64]")]
        public fixed uint reserved1[64];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_OUTPUT_STATS_BLOCK
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint8_t")]
        public byte QP;

        [NativeTypeName("uint8_t[3]")]
        public fixed byte reserved[3];

        [NativeTypeName("uint32_t")]
        public uint bitcount;

        [NativeTypeName("uint32_t")]
        public uint satdCost;

        [NativeTypeName("uint32_t[12]")]
        public fixed uint reserved1[12];
    }

    public unsafe partial struct _NV_ENC_OUTPUT_STATS_ROW
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint8_t")]
        public byte QP;

        [NativeTypeName("uint8_t[3]")]
        public fixed byte reserved[3];

        [NativeTypeName("uint32_t")]
        public uint bitcount;

        [NativeTypeName("uint32_t")]
        public uint satdCost;

        [NativeTypeName("uint32_t[12]")]
        public fixed uint reserved1[12];
    }

    public unsafe partial struct _NV_ENC_ENCODE_OUT_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint bitstreamSizeInBytes;

        [NativeTypeName("uint32_t[62]")]
        public fixed uint reserved[62];
    }

    public unsafe partial struct _NV_ENC_LOOKAHEAD_PIC_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* inputBuffer;

        [NativeTypeName("NV_ENC_PIC_TYPE")]
        public _NV_ENC_PIC_TYPE pictureType;

        [NativeTypeName("uint32_t[63]")]
        public fixed uint reserved1[63];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_CREATE_INPUT_BUFFER
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint width;

        [NativeTypeName("uint32_t")]
        public uint height;

        [NativeTypeName("NV_ENC_MEMORY_HEAP")]
        public _NV_ENC_MEMORY_HEAP memoryHeap;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT bufferFmt;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* inputBuffer;

        public void* pSysMemBuffer;

        [NativeTypeName("uint32_t[58]")]
        public fixed uint reserved1[58];

        [NativeTypeName("void *[63]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_CREATE_BITSTREAM_BUFFER
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint size;

        [NativeTypeName("NV_ENC_MEMORY_HEAP")]
        public _NV_ENC_MEMORY_HEAP memoryHeap;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* bitstreamBuffer;

        public void* bitstreamBufferPtr;

        [NativeTypeName("uint32_t[58]")]
        public fixed uint reserved1[58];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public partial struct _NV_ENC_MVECTOR
    {
        [NativeTypeName("int16_t")]
        public short mvx;

        [NativeTypeName("int16_t")]
        public short mvy;
    }

    public partial struct _NV_ENC_H264_MV_DATA
    {
        [NativeTypeName("NV_ENC_MVECTOR[4]")]
        public _mv_e__FixedBuffer mv;

        [NativeTypeName("uint8_t")]
        public byte mbType;

        [NativeTypeName("uint8_t")]
        public byte partitionType;

        [NativeTypeName("uint16_t")]
        public ushort reserved;

        [NativeTypeName("uint32_t")]
        public uint mbCost;

        public partial struct _mv_e__FixedBuffer
        {
            public _NV_ENC_MVECTOR e0;
            public _NV_ENC_MVECTOR e1;
            public _NV_ENC_MVECTOR e2;
            public _NV_ENC_MVECTOR e3;

            public ref _NV_ENC_MVECTOR this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NV_ENC_MVECTOR> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 4);
        }
    }

    public partial struct _NV_ENC_HEVC_MV_DATA
    {
        [NativeTypeName("NV_ENC_MVECTOR[4]")]
        public _mv_e__FixedBuffer mv;

        [NativeTypeName("uint8_t")]
        public byte cuType;

        [NativeTypeName("uint8_t")]
        public byte cuSize;

        [NativeTypeName("uint8_t")]
        public byte partitionMode;

        [NativeTypeName("uint8_t")]
        public byte lastCUInCTB;

        public partial struct _mv_e__FixedBuffer
        {
            public _NV_ENC_MVECTOR e0;
            public _NV_ENC_MVECTOR e1;
            public _NV_ENC_MVECTOR e2;
            public _NV_ENC_MVECTOR e3;

            public ref _NV_ENC_MVECTOR this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NV_ENC_MVECTOR> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 4);
        }
    }

    public unsafe partial struct _NV_ENC_CREATE_MV_BUFFER
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* mvBuffer;

        [NativeTypeName("uint32_t[254]")]
        public fixed uint reserved1[254];

        [NativeTypeName("void *[63]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public partial struct _NV_ENC_QP
    {
        [NativeTypeName("uint32_t")]
        public uint qpInterP;

        [NativeTypeName("uint32_t")]
        public uint qpInterB;

        [NativeTypeName("uint32_t")]
        public uint qpIntra;
    }

    public unsafe partial struct _NV_ENC_RC_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("NV_ENC_PARAMS_RC_MODE")]
        public _NV_ENC_PARAMS_RC_MODE rateControlMode;

        [NativeTypeName("NV_ENC_QP")]
        public _NV_ENC_QP constQP;

        [NativeTypeName("uint32_t")]
        public uint averageBitRate;

        [NativeTypeName("uint32_t")]
        public uint maxBitRate;

        [NativeTypeName("uint32_t")]
        public uint vbvBufferSize;

        [NativeTypeName("uint32_t")]
        public uint vbvInitialDelay;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint enableMinQP
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableMaxQP
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableInitialRCQP
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableAQ
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint reservedBitField1
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableLookahead
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableIadapt
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableBadapt
        {
            readonly get
            {
                return (_bitfield >> 7) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableTemporalAQ
        {
            readonly get
            {
                return (_bitfield >> 8) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 8)) | ((value & 0x1u) << 8);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint zeroReorderDelay
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableNonRefP
        {
            readonly get
            {
                return (_bitfield >> 10) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 10)) | ((value & 0x1u) << 10);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint strictGOPTarget
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 11)) | ((value & 0x1u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint aqStrength
        {
            readonly get
            {
                return (_bitfield >> 12) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 12)) | ((value & 0xFu) << 12);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableExtLookahead
        {
            readonly get
            {
                return (_bitfield >> 16) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 16)) | ((value & 0x1u) << 16);
            }
        }

        [NativeTypeName("uint32_t : 15")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 17) & 0x7FFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFu << 17)) | ((value & 0x7FFFu) << 17);
            }
        }

        [NativeTypeName("NV_ENC_QP")]
        public _NV_ENC_QP minQP;

        [NativeTypeName("NV_ENC_QP")]
        public _NV_ENC_QP maxQP;

        [NativeTypeName("NV_ENC_QP")]
        public _NV_ENC_QP initialRCQP;

        [NativeTypeName("uint32_t")]
        public uint temporallayerIdxMask;

        [NativeTypeName("uint8_t[8]")]
        public fixed byte temporalLayerQP[8];

        [NativeTypeName("uint8_t")]
        public byte targetQuality;

        [NativeTypeName("uint8_t")]
        public byte targetQualityLSB;

        [NativeTypeName("uint16_t")]
        public ushort lookaheadDepth;

        [NativeTypeName("uint8_t")]
        public byte lowDelayKeyFrameScale;

        [NativeTypeName("int8_t")]
        public sbyte yDcQPIndexOffset;

        [NativeTypeName("int8_t")]
        public sbyte uDcQPIndexOffset;

        [NativeTypeName("int8_t")]
        public sbyte vDcQPIndexOffset;

        [NativeTypeName("NV_ENC_QP_MAP_MODE")]
        public _NV_ENC_QP_MAP_MODE qpMapMode;

        [NativeTypeName("NV_ENC_MULTI_PASS")]
        public _NV_ENC_MULTI_PASS multiPass;

        [NativeTypeName("uint32_t")]
        public uint alphaLayerBitrateRatio;

        [NativeTypeName("int8_t")]
        public sbyte cbQPIndexOffset;

        [NativeTypeName("int8_t")]
        public sbyte crQPIndexOffset;

        [NativeTypeName("uint16_t")]
        public ushort reserved2;

        [NativeTypeName("NV_ENC_LOOKAHEAD_LEVEL")]
        public _NV_ENC_LOOKAHEAD_LEVEL lookaheadLevel;

        [NativeTypeName("uint8_t[7]")]
        public fixed byte viewBitrateRatios[7];

        [NativeTypeName("uint8_t")]
        public byte reserved3;

        [NativeTypeName("uint32_t")]
        public uint reserved1;
    }

    public partial struct _NV_ENC_CLOCK_TIMESTAMP_SET
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint countingType
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint discontinuityFlag
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint cntDroppedFrames
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 8")]
        public uint nFrames
        {
            readonly get
            {
                return (_bitfield >> 3) & 0xFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFu << 3)) | ((value & 0xFFu) << 3);
            }
        }

        [NativeTypeName("uint32_t : 6")]
        public uint secondsValue
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x3Fu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3Fu << 11)) | ((value & 0x3Fu) << 11);
            }
        }

        [NativeTypeName("uint32_t : 6")]
        public uint minutesValue
        {
            readonly get
            {
                return (_bitfield >> 17) & 0x3Fu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3Fu << 17)) | ((value & 0x3Fu) << 17);
            }
        }

        [NativeTypeName("uint32_t : 5")]
        public uint hoursValue
        {
            readonly get
            {
                return (_bitfield >> 23) & 0x1Fu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1Fu << 23)) | ((value & 0x1Fu) << 23);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint reserved2
        {
            readonly get
            {
                return (_bitfield >> 28) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 28)) | ((value & 0xFu) << 28);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint timeOffset;
    }

    public partial struct _NV_ENC_TIME_CODE
    {
        [NativeTypeName("NV_ENC_DISPLAY_PIC_STRUCT")]
        public _NV_ENC_DISPLAY_PIC_STRUCT displayPicStruct;

        [NativeTypeName("NV_ENC_CLOCK_TIMESTAMP_SET[3]")]
        public _clockTimestamp_e__FixedBuffer clockTimestamp;

        [NativeTypeName("uint32_t")]
        public uint skipClockTimestampInsertion;

        public partial struct _clockTimestamp_e__FixedBuffer
        {
            public _NV_ENC_CLOCK_TIMESTAMP_SET e0;
            public _NV_ENC_CLOCK_TIMESTAMP_SET e1;
            public _NV_ENC_CLOCK_TIMESTAMP_SET e2;

            public ref _NV_ENC_CLOCK_TIMESTAMP_SET this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NV_ENC_CLOCK_TIMESTAMP_SET> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 3);
        }
    }

    public unsafe partial struct _HEVC_3D_REFERENCE_DISPLAY_INFO
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint refViewingDistanceFlag
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint threeDimensionalReferenceDisplaysExtensionFlag
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 30")]
        public uint reserved
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x3FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2);
            }
        }

        [NativeTypeName("int32_t")]
        public int precRefDisplayWidth;

        [NativeTypeName("int32_t")]
        public int precRefViewingDist;

        [NativeTypeName("int32_t")]
        public int numRefDisplaysMinus1;

        [NativeTypeName("int32_t[32]")]
        public fixed int leftViewId[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int rightViewId[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int exponentRefDisplayWidth[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int mantissaRefDisplayWidth[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int exponentRefViewingDistance[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int mantissaRefViewingDistance[32];

        [NativeTypeName("int32_t[32]")]
        public fixed int numSampleShiftPlus512[32];

        [NativeTypeName("uint8_t[32]")]
        public fixed byte additionalShiftPresentFlag[32];

        [NativeTypeName("uint32_t[4]")]
        public fixed uint reserved2[4];
    }

    public partial struct _CHROMA_POINTS
    {
        [NativeTypeName("uint16_t")]
        public ushort x;

        [NativeTypeName("uint16_t")]
        public ushort y;
    }

    public partial struct _MASTERING_DISPLAY_INFO
    {
        [NativeTypeName("CHROMA_POINTS")]
        public _CHROMA_POINTS g;

        [NativeTypeName("CHROMA_POINTS")]
        public _CHROMA_POINTS b;

        [NativeTypeName("CHROMA_POINTS")]
        public _CHROMA_POINTS r;

        [NativeTypeName("CHROMA_POINTS")]
        public _CHROMA_POINTS whitePoint;

        [NativeTypeName("uint32_t")]
        public uint maxLuma;

        [NativeTypeName("uint32_t")]
        public uint minLuma;
    }

    public partial struct _CONTENT_LIGHT_LEVEL
    {
        [NativeTypeName("uint16_t")]
        public ushort maxContentLightLevel;

        [NativeTypeName("uint16_t")]
        public ushort maxPicAverageLightLevel;
    }

    public unsafe partial struct _NV_ENC_CONFIG_H264_VUI_PARAMETERS
    {
        [NativeTypeName("uint32_t")]
        public uint overscanInfoPresentFlag;

        [NativeTypeName("uint32_t")]
        public uint overscanInfo;

        [NativeTypeName("uint32_t")]
        public uint videoSignalTypePresentFlag;

        [NativeTypeName("NV_ENC_VUI_VIDEO_FORMAT")]
        public _NV_ENC_VUI_VIDEO_FORMAT videoFormat;

        [NativeTypeName("uint32_t")]
        public uint videoFullRangeFlag;

        [NativeTypeName("uint32_t")]
        public uint colourDescriptionPresentFlag;

        [NativeTypeName("NV_ENC_VUI_COLOR_PRIMARIES")]
        public _NV_ENC_VUI_COLOR_PRIMARIES colourPrimaries;

        [NativeTypeName("NV_ENC_VUI_TRANSFER_CHARACTERISTIC")]
        public _NV_ENC_VUI_TRANSFER_CHARACTERISTIC transferCharacteristics;

        [NativeTypeName("NV_ENC_VUI_MATRIX_COEFFS")]
        public _NV_ENC_VUI_MATRIX_COEFFS colourMatrix;

        [NativeTypeName("uint32_t")]
        public uint chromaSampleLocationFlag;

        [NativeTypeName("uint32_t")]
        public uint chromaSampleLocationTop;

        [NativeTypeName("uint32_t")]
        public uint chromaSampleLocationBot;

        [NativeTypeName("uint32_t")]
        public uint bitstreamRestrictionFlag;

        [NativeTypeName("uint32_t")]
        public uint timingInfoPresentFlag;

        [NativeTypeName("uint32_t")]
        public uint numUnitInTicks;

        [NativeTypeName("uint32_t")]
        public uint timeScale;

        [NativeTypeName("uint32_t[12]")]
        public fixed uint reserved[12];
    }

    public unsafe partial struct _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 4")]
        public uint numCandsPerBlk16x16
        {
            readonly get
            {
                return _bitfield & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~0xFu) | (value & 0xFu);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numCandsPerBlk16x8
        {
            readonly get
            {
                return (_bitfield >> 4) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 4)) | ((value & 0xFu) << 4);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numCandsPerBlk8x16
        {
            readonly get
            {
                return (_bitfield >> 8) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 8)) | ((value & 0xFu) << 8);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numCandsPerBlk8x8
        {
            readonly get
            {
                return (_bitfield >> 12) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 12)) | ((value & 0xFu) << 12);
            }
        }

        [NativeTypeName("uint32_t : 8")]
        public uint numCandsPerSb
        {
            readonly get
            {
                return (_bitfield >> 16) & 0xFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFu << 16)) | ((value & 0xFFu) << 16);
            }
        }

        [NativeTypeName("uint32_t : 8")]
        public uint reserved
        {
            readonly get
            {
                return (_bitfield >> 24) & 0xFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFu << 24)) | ((value & 0xFFu) << 24);
            }
        }

        [NativeTypeName("uint32_t[3]")]
        public fixed uint reserved1[3];
    }

    public partial struct _NVENC_EXTERNAL_ME_HINT
    {
        public int _bitfield;

        [NativeTypeName("int32_t : 12")]
        public int mvx
        {
            readonly get
            {
                return (_bitfield << 20) >> 20;
            }

            set
            {
                _bitfield = (_bitfield & ~0xFFF) | (value & 0xFFF);
            }
        }

        [NativeTypeName("int32_t : 10")]
        public int mvy
        {
            readonly get
            {
                return (_bitfield << 10) >> 22;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FF << 12)) | ((value & 0x3FF) << 12);
            }
        }

        [NativeTypeName("int32_t : 5")]
        public int refidx
        {
            readonly get
            {
                return (_bitfield << 5) >> 27;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1F << 22)) | ((value & 0x1F) << 22);
            }
        }

        [NativeTypeName("int32_t : 1")]
        public int dir
        {
            readonly get
            {
                return (_bitfield << 4) >> 31;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1 << 27)) | ((value & 0x1) << 27);
            }
        }

        [NativeTypeName("int32_t : 2")]
        public int partType
        {
            readonly get
            {
                return (_bitfield << 2) >> 30;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3 << 28)) | ((value & 0x3) << 28);
            }
        }

        [NativeTypeName("int32_t : 1")]
        public int lastofPart
        {
            readonly get
            {
                return (_bitfield << 1) >> 31;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1 << 30)) | ((value & 0x1) << 30);
            }
        }

        [NativeTypeName("int32_t : 1")]
        public int lastOfMB
        {
            readonly get
            {
                return (_bitfield << 0) >> 31;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1 << 31)) | ((value & 0x1) << 31);
            }
        }
    }

    public partial struct _NVENC_EXTERNAL_ME_SB_HINT
    {
        public short _bitfield1;

        [NativeTypeName("int16_t : 5")]
        public short refidx
        {
            readonly get
            {
                return (short)((_bitfield1 << 11) >> 11);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~0x1F) | (value & 0x1F));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short direction
        {
            readonly get
            {
                return (short)((_bitfield1 << 10) >> 15);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x1 << 5)) | ((value & 0x1) << 5));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short bi
        {
            readonly get
            {
                return (short)((_bitfield1 << 9) >> 15);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x1 << 6)) | ((value & 0x1) << 6));
            }
        }

        [NativeTypeName("int16_t : 3")]
        public short partition_type
        {
            readonly get
            {
                return (short)((_bitfield1 << 6) >> 13);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x7 << 7)) | ((value & 0x7) << 7));
            }
        }

        [NativeTypeName("int16_t : 3")]
        public short x8
        {
            readonly get
            {
                return (short)((_bitfield1 << 3) >> 13);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x7 << 10)) | ((value & 0x7) << 10));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short last_of_cu
        {
            readonly get
            {
                return (short)((_bitfield1 << 2) >> 15);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x1 << 13)) | ((value & 0x1) << 13));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short last_of_sb
        {
            readonly get
            {
                return (short)((_bitfield1 << 1) >> 15);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x1 << 14)) | ((value & 0x1) << 14));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short reserved0
        {
            readonly get
            {
                return (short)((_bitfield1 << 0) >> 15);
            }

            set
            {
                _bitfield1 = (short)((_bitfield1 & ~(0x1 << 15)) | ((value & 0x1) << 15));
            }
        }

        public short _bitfield2;

        [NativeTypeName("int16_t : 14")]
        public short mvx
        {
            readonly get
            {
                return (short)((_bitfield2 << 2) >> 2);
            }

            set
            {
                _bitfield2 = (short)((_bitfield2 & ~0x3FFF) | (value & 0x3FFF));
            }
        }

        [NativeTypeName("int16_t : 2")]
        public short cu_size
        {
            readonly get
            {
                return (short)((_bitfield2 << 0) >> 14);
            }

            set
            {
                _bitfield2 = (short)((_bitfield2 & ~(0x3 << 14)) | ((value & 0x3) << 14));
            }
        }

        public short _bitfield3;

        [NativeTypeName("int16_t : 12")]
        public short mvy
        {
            readonly get
            {
                return (short)((_bitfield3 << 4) >> 4);
            }

            set
            {
                _bitfield3 = (short)((_bitfield3 & ~0xFFF) | (value & 0xFFF));
            }
        }

        [NativeTypeName("int16_t : 3")]
        public short y8
        {
            readonly get
            {
                return (short)((_bitfield3 << 1) >> 13);
            }

            set
            {
                _bitfield3 = (short)((_bitfield3 & ~(0x7 << 12)) | ((value & 0x7) << 12));
            }
        }

        [NativeTypeName("int16_t : 1")]
        public short reserved1
        {
            readonly get
            {
                return (short)((_bitfield3 << 0) >> 15);
            }

            set
            {
                _bitfield3 = (short)((_bitfield3 & ~(0x1 << 15)) | ((value & 0x1) << 15));
            }
        }
    }

    public unsafe partial struct _NV_ENC_CONFIG_H264
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint enableTemporalSVC
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableStereoMVC
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint hierarchicalPFrames
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint hierarchicalBFrames
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputBufferingPeriodSEI
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputPictureTimingSEI
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputAUD
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableSPSPPS
        {
            readonly get
            {
                return (_bitfield >> 7) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputFramePackingSEI
        {
            readonly get
            {
                return (_bitfield >> 8) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 8)) | ((value & 0x1u) << 8);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputRecoveryPointSEI
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableIntraRefresh
        {
            readonly get
            {
                return (_bitfield >> 10) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 10)) | ((value & 0x1u) << 10);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableConstrainedEncoding
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 11)) | ((value & 0x1u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint repeatSPSPPS
        {
            readonly get
            {
                return (_bitfield >> 12) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 12)) | ((value & 0x1u) << 12);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableVFR
        {
            readonly get
            {
                return (_bitfield >> 13) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 13)) | ((value & 0x1u) << 13);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableLTR
        {
            readonly get
            {
                return (_bitfield >> 14) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 14)) | ((value & 0x1u) << 14);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint qpPrimeYZeroTransformBypassFlag
        {
            readonly get
            {
                return (_bitfield >> 15) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 15)) | ((value & 0x1u) << 15);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint useConstrainedIntraPred
        {
            readonly get
            {
                return (_bitfield >> 16) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 16)) | ((value & 0x1u) << 16);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableFillerDataInsertion
        {
            readonly get
            {
                return (_bitfield >> 17) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 17)) | ((value & 0x1u) << 17);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableSVCPrefixNalu
        {
            readonly get
            {
                return (_bitfield >> 18) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 18)) | ((value & 0x1u) << 18);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableScalabilityInfoSEI
        {
            readonly get
            {
                return (_bitfield >> 19) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 19)) | ((value & 0x1u) << 19);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint singleSliceIntraRefresh
        {
            readonly get
            {
                return (_bitfield >> 20) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 20)) | ((value & 0x1u) << 20);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableTimeCode
        {
            readonly get
            {
                return (_bitfield >> 21) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 21)) | ((value & 0x1u) << 21);
            }
        }

        [NativeTypeName("uint32_t : 10")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 22) & 0x3FFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFu << 22)) | ((value & 0x3FFu) << 22);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint level;

        [NativeTypeName("uint32_t")]
        public uint idrPeriod;

        [NativeTypeName("uint32_t")]
        public uint separateColourPlaneFlag;

        [NativeTypeName("uint32_t")]
        public uint disableDeblockingFilterIDC;

        [NativeTypeName("uint32_t")]
        public uint numTemporalLayers;

        [NativeTypeName("uint32_t")]
        public uint spsId;

        [NativeTypeName("uint32_t")]
        public uint ppsId;

        [NativeTypeName("NV_ENC_H264_ADAPTIVE_TRANSFORM_MODE")]
        public _NV_ENC_H264_ADAPTIVE_TRANSFORM_MODE adaptiveTransformMode;

        [NativeTypeName("NV_ENC_H264_FMO_MODE")]
        public _NV_ENC_H264_FMO_MODE fmoMode;

        [NativeTypeName("NV_ENC_H264_BDIRECT_MODE")]
        public _NV_ENC_H264_BDIRECT_MODE bdirectMode;

        [NativeTypeName("NV_ENC_H264_ENTROPY_CODING_MODE")]
        public _NV_ENC_H264_ENTROPY_CODING_MODE entropyCodingMode;

        [NativeTypeName("NV_ENC_STEREO_PACKING_MODE")]
        public _NV_ENC_STEREO_PACKING_MODE stereoMode;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshPeriod;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshCnt;

        [NativeTypeName("uint32_t")]
        public uint maxNumRefFrames;

        [NativeTypeName("uint32_t")]
        public uint sliceMode;

        [NativeTypeName("uint32_t")]
        public uint sliceModeData;

        [NativeTypeName("NV_ENC_CONFIG_H264_VUI_PARAMETERS")]
        public _NV_ENC_CONFIG_H264_VUI_PARAMETERS h264VUIParameters;

        [NativeTypeName("uint32_t")]
        public uint ltrNumFrames;

        [NativeTypeName("uint32_t")]
        public uint ltrTrustMode;

        [NativeTypeName("uint32_t")]
        public uint chromaFormatIDC;

        [NativeTypeName("uint32_t")]
        public uint maxTemporalLayers;

        [NativeTypeName("NV_ENC_BFRAME_REF_MODE")]
        public _NV_ENC_BFRAME_REF_MODE useBFramesAsRef;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numRefL0;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numRefL1;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH outputBitDepth;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH inputBitDepth;

        [NativeTypeName("NV_ENC_TEMPORAL_FILTER_LEVEL")]
        public _NV_ENC_TEMPORAL_FILTER_LEVEL tfLevel;

        [NativeTypeName("uint32_t[264]")]
        public fixed uint reserved1[264];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_CONFIG_HEVC
    {
        [NativeTypeName("uint32_t")]
        public uint level;

        [NativeTypeName("uint32_t")]
        public uint tier;

        [NativeTypeName("NV_ENC_HEVC_CUSIZE")]
        public _NV_ENC_HEVC_CUSIZE minCUSize;

        [NativeTypeName("NV_ENC_HEVC_CUSIZE")]
        public _NV_ENC_HEVC_CUSIZE maxCUSize;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint useConstrainedIntraPred
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableDeblockAcrossSliceBoundary
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputBufferingPeriodSEI
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputPictureTimingSEI
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputAUD
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableLTR
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableSPSPPS
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint repeatSPSPPS
        {
            readonly get
            {
                return (_bitfield >> 7) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableIntraRefresh
        {
            readonly get
            {
                return (_bitfield >> 8) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 8)) | ((value & 0x1u) << 8);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint chromaFormatIDC
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 9)) | ((value & 0x3u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 3")]
        public uint reserved3
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x7u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7u << 11)) | ((value & 0x7u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableFillerDataInsertion
        {
            readonly get
            {
                return (_bitfield >> 14) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 14)) | ((value & 0x1u) << 14);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableConstrainedEncoding
        {
            readonly get
            {
                return (_bitfield >> 15) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 15)) | ((value & 0x1u) << 15);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableAlphaLayerEncoding
        {
            readonly get
            {
                return (_bitfield >> 16) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 16)) | ((value & 0x1u) << 16);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint singleSliceIntraRefresh
        {
            readonly get
            {
                return (_bitfield >> 17) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 17)) | ((value & 0x1u) << 17);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputRecoveryPointSEI
        {
            readonly get
            {
                return (_bitfield >> 18) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 18)) | ((value & 0x1u) << 18);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputTimeCodeSEI
        {
            readonly get
            {
                return (_bitfield >> 19) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 19)) | ((value & 0x1u) << 19);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableTemporalSVC
        {
            readonly get
            {
                return (_bitfield >> 20) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 20)) | ((value & 0x1u) << 20);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableMVHEVC
        {
            readonly get
            {
                return (_bitfield >> 21) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 21)) | ((value & 0x1u) << 21);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputHevc3DReferenceDisplayInfo
        {
            readonly get
            {
                return (_bitfield >> 22) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 22)) | ((value & 0x1u) << 22);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputMaxCll
        {
            readonly get
            {
                return (_bitfield >> 23) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 23)) | ((value & 0x1u) << 23);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputMasteringDisplay
        {
            readonly get
            {
                return (_bitfield >> 24) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 24)) | ((value & 0x1u) << 24);
            }
        }

        [NativeTypeName("uint32_t : 7")]
        public uint reserved
        {
            readonly get
            {
                return (_bitfield >> 25) & 0x7Fu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7Fu << 25)) | ((value & 0x7Fu) << 25);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint idrPeriod;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshPeriod;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshCnt;

        [NativeTypeName("uint32_t")]
        public uint maxNumRefFramesInDPB;

        [NativeTypeName("uint32_t")]
        public uint ltrNumFrames;

        [NativeTypeName("uint32_t")]
        public uint vpsId;

        [NativeTypeName("uint32_t")]
        public uint spsId;

        [NativeTypeName("uint32_t")]
        public uint ppsId;

        [NativeTypeName("uint32_t")]
        public uint sliceMode;

        [NativeTypeName("uint32_t")]
        public uint sliceModeData;

        [NativeTypeName("uint32_t")]
        public uint maxTemporalLayersMinus1;

        [NativeTypeName("NV_ENC_CONFIG_HEVC_VUI_PARAMETERS")]
        public _NV_ENC_CONFIG_H264_VUI_PARAMETERS hevcVUIParameters;

        [NativeTypeName("uint32_t")]
        public uint ltrTrustMode;

        [NativeTypeName("NV_ENC_BFRAME_REF_MODE")]
        public _NV_ENC_BFRAME_REF_MODE useBFramesAsRef;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numRefL0;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numRefL1;

        [NativeTypeName("NV_ENC_TEMPORAL_FILTER_LEVEL")]
        public _NV_ENC_TEMPORAL_FILTER_LEVEL tfLevel;

        [NativeTypeName("uint32_t")]
        public uint disableDeblockingFilterIDC;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH outputBitDepth;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH inputBitDepth;

        [NativeTypeName("uint32_t")]
        public uint numTemporalLayers;

        [NativeTypeName("uint32_t")]
        public uint numViews;

        [NativeTypeName("uint32_t[208]")]
        public fixed uint reserved1[208];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_FILM_GRAIN_PARAMS_AV1
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint applyGrain
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint chromaScalingFromLuma
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint overlapFlag
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint clipToRestrictedRange
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint grainScalingMinus8
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 4)) | ((value & 0x3u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint arCoeffLag
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 6)) | ((value & 0x3u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numYPoints
        {
            readonly get
            {
                return (_bitfield >> 8) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 8)) | ((value & 0xFu) << 8);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numCbPoints
        {
            readonly get
            {
                return (_bitfield >> 12) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 12)) | ((value & 0xFu) << 12);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint numCrPoints
        {
            readonly get
            {
                return (_bitfield >> 16) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 16)) | ((value & 0xFu) << 16);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint arCoeffShiftMinus6
        {
            readonly get
            {
                return (_bitfield >> 20) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 20)) | ((value & 0x3u) << 20);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint grainScaleShift
        {
            readonly get
            {
                return (_bitfield >> 22) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 22)) | ((value & 0x3u) << 22);
            }
        }

        [NativeTypeName("uint32_t : 8")]
        public uint reserved1
        {
            readonly get
            {
                return (_bitfield >> 24) & 0xFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFu << 24)) | ((value & 0xFFu) << 24);
            }
        }

        [NativeTypeName("uint8_t[14]")]
        public fixed byte pointYValue[14];

        [NativeTypeName("uint8_t[14]")]
        public fixed byte pointYScaling[14];

        [NativeTypeName("uint8_t[10]")]
        public fixed byte pointCbValue[10];

        [NativeTypeName("uint8_t[10]")]
        public fixed byte pointCbScaling[10];

        [NativeTypeName("uint8_t[10]")]
        public fixed byte pointCrValue[10];

        [NativeTypeName("uint8_t[10]")]
        public fixed byte pointCrScaling[10];

        [NativeTypeName("uint8_t[24]")]
        public fixed byte arCoeffsYPlus128[24];

        [NativeTypeName("uint8_t[25]")]
        public fixed byte arCoeffsCbPlus128[25];

        [NativeTypeName("uint8_t[25]")]
        public fixed byte arCoeffsCrPlus128[25];

        [NativeTypeName("uint8_t[2]")]
        public fixed byte reserved2[2];

        [NativeTypeName("uint8_t")]
        public byte cbMult;

        [NativeTypeName("uint8_t")]
        public byte cbLumaMult;

        [NativeTypeName("uint16_t")]
        public ushort cbOffset;

        [NativeTypeName("uint8_t")]
        public byte crMult;

        [NativeTypeName("uint8_t")]
        public byte crLumaMult;

        [NativeTypeName("uint16_t")]
        public ushort crOffset;
    }

    public unsafe partial struct _NV_ENC_CONFIG_AV1
    {
        [NativeTypeName("uint32_t")]
        public uint level;

        [NativeTypeName("uint32_t")]
        public uint tier;

        [NativeTypeName("NV_ENC_AV1_PART_SIZE")]
        public _NV_ENC_AV1_PART_SIZE minPartSize;

        [NativeTypeName("NV_ENC_AV1_PART_SIZE")]
        public _NV_ENC_AV1_PART_SIZE maxPartSize;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint outputAnnexBFormat
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableTimingInfo
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableDecoderModelInfo
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableFrameIdNumbers
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableSeqHdr
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint repeatSeqHdr
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableIntraRefresh
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint chromaFormatIDC
        {
            readonly get
            {
                return (_bitfield >> 7) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 7)) | ((value & 0x3u) << 7);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableBitstreamPadding
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableCustomTileConfig
        {
            readonly get
            {
                return (_bitfield >> 10) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 10)) | ((value & 0x1u) << 10);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableFilmGrainParams
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 11)) | ((value & 0x1u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableLTR
        {
            readonly get
            {
                return (_bitfield >> 12) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 12)) | ((value & 0x1u) << 12);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableTemporalSVC
        {
            readonly get
            {
                return (_bitfield >> 13) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 13)) | ((value & 0x1u) << 13);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputMaxCll
        {
            readonly get
            {
                return (_bitfield >> 14) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 14)) | ((value & 0x1u) << 14);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint outputMasteringDisplay
        {
            readonly get
            {
                return (_bitfield >> 15) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 15)) | ((value & 0x1u) << 15);
            }
        }

        [NativeTypeName("uint32_t : 2")]
        public uint reserved4
        {
            readonly get
            {
                return (_bitfield >> 16) & 0x3u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3u << 16)) | ((value & 0x3u) << 16);
            }
        }

        [NativeTypeName("uint32_t : 14")]
        public uint reserved
        {
            readonly get
            {
                return (_bitfield >> 18) & 0x3FFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFFu << 18)) | ((value & 0x3FFFu) << 18);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint idrPeriod;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshPeriod;

        [NativeTypeName("uint32_t")]
        public uint intraRefreshCnt;

        [NativeTypeName("uint32_t")]
        public uint maxNumRefFramesInDPB;

        [NativeTypeName("uint32_t")]
        public uint numTileColumns;

        [NativeTypeName("uint32_t")]
        public uint numTileRows;

        [NativeTypeName("uint32_t")]
        public uint reserved2;

        [NativeTypeName("uint32_t *")]
        public uint* tileWidths;

        [NativeTypeName("uint32_t *")]
        public uint* tileHeights;

        [NativeTypeName("uint32_t")]
        public uint maxTemporalLayersMinus1;

        [NativeTypeName("NV_ENC_VUI_COLOR_PRIMARIES")]
        public _NV_ENC_VUI_COLOR_PRIMARIES colorPrimaries;

        [NativeTypeName("NV_ENC_VUI_TRANSFER_CHARACTERISTIC")]
        public _NV_ENC_VUI_TRANSFER_CHARACTERISTIC transferCharacteristics;

        [NativeTypeName("NV_ENC_VUI_MATRIX_COEFFS")]
        public _NV_ENC_VUI_MATRIX_COEFFS matrixCoefficients;

        [NativeTypeName("uint32_t")]
        public uint colorRange;

        [NativeTypeName("uint32_t")]
        public uint chromaSamplePosition;

        [NativeTypeName("NV_ENC_BFRAME_REF_MODE")]
        public _NV_ENC_BFRAME_REF_MODE useBFramesAsRef;

        [NativeTypeName("NV_ENC_FILM_GRAIN_PARAMS_AV1 *")]
        public _NV_ENC_FILM_GRAIN_PARAMS_AV1* filmGrainParams;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numFwdRefs;

        [NativeTypeName("NV_ENC_NUM_REF_FRAMES")]
        public _NV_ENC_NUM_REF_FRAMES numBwdRefs;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH outputBitDepth;

        [NativeTypeName("NV_ENC_BIT_DEPTH")]
        public _NV_ENC_BIT_DEPTH inputBitDepth;

        [NativeTypeName("uint32_t")]
        public uint ltrNumFrames;

        [NativeTypeName("uint32_t")]
        public uint numTemporalLayers;

        [NativeTypeName("NV_ENC_TEMPORAL_FILTER_LEVEL")]
        public _NV_ENC_TEMPORAL_FILTER_LEVEL tfLevel;

        [NativeTypeName("uint32_t[230]")]
        public fixed uint reserved1[230];

        [NativeTypeName("void *[62]")]
        public _reserved3_e__FixedBuffer reserved3;

        public unsafe partial struct _reserved3_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_CONFIG_H264_MEONLY
    {
        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint disablePartition16x16
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disablePartition8x16
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disablePartition16x8
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disablePartition8x8
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint disableIntraSearch
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint bStereoEnable
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 26")]
        public uint reserved
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x3FFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFFFFFu << 6)) | ((value & 0x3FFFFFFu) << 6);
            }
        }

        [NativeTypeName("uint32_t[255]")]
        public fixed uint reserved1[255];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_CONFIG_HEVC_MEONLY
    {
        [NativeTypeName("uint32_t[256]")]
        public fixed uint reserved[256];

        [NativeTypeName("void *[64]")]
        public _reserved1_e__FixedBuffer reserved1;

        public unsafe partial struct _reserved1_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _NV_ENC_CODEC_CONFIG
    {
        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_CONFIG_H264")]
        public _NV_ENC_CONFIG_H264 h264Config;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_CONFIG_HEVC")]
        public _NV_ENC_CONFIG_HEVC hevcConfig;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_CONFIG_AV1")]
        public _NV_ENC_CONFIG_AV1 av1Config;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_CONFIG_H264_MEONLY")]
        public _NV_ENC_CONFIG_H264_MEONLY h264MeOnlyConfig;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_CONFIG_HEVC_MEONLY")]
        public _NV_ENC_CONFIG_HEVC_MEONLY hevcMeOnlyConfig;

        [FieldOffset(0)]
        [NativeTypeName("uint32_t[320]")]
        public fixed uint reserved[320];
    }

    public unsafe partial struct _NV_ENC_CONFIG
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        public Guid profileGUID;

        [NativeTypeName("uint32_t")]
        public uint gopLength;

        [NativeTypeName("int32_t")]
        public int frameIntervalP;

        [NativeTypeName("uint32_t")]
        public uint monoChromeEncoding;

        [NativeTypeName("NV_ENC_PARAMS_FRAME_FIELD_MODE")]
        public _NV_ENC_PARAMS_FRAME_FIELD_MODE frameFieldMode;

        [NativeTypeName("NV_ENC_MV_PRECISION")]
        public _NV_ENC_MV_PRECISION mvPrecision;

        [NativeTypeName("NV_ENC_RC_PARAMS")]
        public _NV_ENC_RC_PARAMS rcParams;

        [NativeTypeName("NV_ENC_CODEC_CONFIG")]
        public _NV_ENC_CODEC_CONFIG encodeCodecConfig;

        [NativeTypeName("uint32_t[278]")]
        public fixed uint reserved[278];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    [NativeTypeName("unsigned int")]
    public enum NV_ENC_TUNING_INFO : uint
    {
        NV_ENC_TUNING_INFO_UNDEFINED = 0,
        NV_ENC_TUNING_INFO_HIGH_QUALITY = 1,
        NV_ENC_TUNING_INFO_LOW_LATENCY = 2,
        NV_ENC_TUNING_INFO_ULTRA_LOW_LATENCY = 3,
        NV_ENC_TUNING_INFO_LOSSLESS = 4,
        NV_ENC_TUNING_INFO_ULTRA_HIGH_QUALITY = 5,
        NV_ENC_TUNING_INFO_COUNT,
    }

    [NativeTypeName("unsigned int")]
    public enum _NV_ENC_SPLIT_ENCODE_MODE : uint
    {
        NV_ENC_SPLIT_AUTO_MODE = 0,
        NV_ENC_SPLIT_AUTO_FORCED_MODE = 1,
        NV_ENC_SPLIT_TWO_FORCED_MODE = 2,
        NV_ENC_SPLIT_THREE_FORCED_MODE = 3,
        NV_ENC_SPLIT_FOUR_FORCED_MODE = 4,
        NV_ENC_SPLIT_DISABLE_MODE = 15,
    }

    public unsafe partial struct _NV_ENC_INITIALIZE_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        public Guid encodeGUID;

        public Guid presetGUID;

        [NativeTypeName("uint32_t")]
        public uint encodeWidth;

        [NativeTypeName("uint32_t")]
        public uint encodeHeight;

        [NativeTypeName("uint32_t")]
        public uint darWidth;

        [NativeTypeName("uint32_t")]
        public uint darHeight;

        [NativeTypeName("uint32_t")]
        public uint frameRateNum;

        [NativeTypeName("uint32_t")]
        public uint frameRateDen;

        [NativeTypeName("uint32_t")]
        public uint enableEncodeAsync;

        [NativeTypeName("uint32_t")]
        public uint enablePTD;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint reportSliceOffsets
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableSubFrameWrite
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableExternalMEHints
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableMEOnlyMode
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableWeightedPrediction
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 4")]
        public uint splitEncodeMode
        {
            readonly get
            {
                return (_bitfield >> 5) & 0xFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFu << 5)) | ((value & 0xFu) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableOutputInVidmem
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableReconFrameOutput
        {
            readonly get
            {
                return (_bitfield >> 10) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 10)) | ((value & 0x1u) << 10);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableOutputStats
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 11)) | ((value & 0x1u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableUniDirectionalB
        {
            readonly get
            {
                return (_bitfield >> 12) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 12)) | ((value & 0x1u) << 12);
            }
        }

        [NativeTypeName("uint32_t : 19")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 13) & 0x7FFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFu << 13)) | ((value & 0x7FFFFu) << 13);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint privDataSize;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        public void* privData;

        [NativeTypeName("NV_ENC_CONFIG *")]
        public _NV_ENC_CONFIG* encodeConfig;

        [NativeTypeName("uint32_t")]
        public uint maxEncodeWidth;

        [NativeTypeName("uint32_t")]
        public uint maxEncodeHeight;

        [NativeTypeName("NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE[2]")]
        public _maxMEHintCountsPerBlock_e__FixedBuffer maxMEHintCountsPerBlock;

        public NV_ENC_TUNING_INFO tuningInfo;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT bufferFormat;

        [NativeTypeName("uint32_t")]
        public uint numStateBuffers;

        [NativeTypeName("NV_ENC_OUTPUT_STATS_LEVEL")]
        public _NV_ENC_OUTPUT_STATS_LEVEL outputStatsLevel;

        [NativeTypeName("uint32_t[284]")]
        public fixed uint reserved1[284];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public partial struct _maxMEHintCountsPerBlock_e__FixedBuffer
        {
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e0;
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e1;

            public ref _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 2);
        }

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public partial struct _NV_ENC_RECONFIGURE_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INITIALIZE_PARAMS")]
        public _NV_ENC_INITIALIZE_PARAMS reInitEncodeParams;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint resetEncoder
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint forceIDR
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 30")]
        public uint reserved1
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x3FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint reserved2;
    }

    public unsafe partial struct _NV_ENC_PRESET_CONFIG
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_CONFIG")]
        public _NV_ENC_CONFIG presetCfg;

        [NativeTypeName("uint32_t[256]")]
        public fixed uint reserved1[256];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_PIC_PARAMS_MVC
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint viewID;

        [NativeTypeName("uint32_t")]
        public uint temporalID;

        [NativeTypeName("uint32_t")]
        public uint priorityID;

        [NativeTypeName("uint32_t[12]")]
        public fixed uint reserved1[12];

        [NativeTypeName("void *[8]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _NV_ENC_PIC_PARAMS_H264_EXT
    {
        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_PIC_PARAMS_MVC")]
        public _NV_ENC_PIC_PARAMS_MVC mvcPicParams;

        [FieldOffset(0)]
        [NativeTypeName("uint32_t[32]")]
        public fixed uint reserved1[32];
    }

    public unsafe partial struct _NV_ENC_SEI_PAYLOAD
    {
        [NativeTypeName("uint32_t")]
        public uint payloadSize;

        [NativeTypeName("uint32_t")]
        public uint payloadType;

        [NativeTypeName("uint8_t *")]
        public byte* payload;
    }

    public unsafe partial struct _NV_ENC_PIC_PARAMS_H264
    {
        [NativeTypeName("uint32_t")]
        public uint displayPOCSyntax;

        [NativeTypeName("uint32_t")]
        public uint reserved3;

        [NativeTypeName("uint32_t")]
        public uint refPicFlag;

        [NativeTypeName("uint32_t")]
        public uint colourPlaneId;

        [NativeTypeName("uint32_t")]
        public uint forceIntraRefreshWithFrameCnt;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint constrainedFrame
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint sliceModeDataUpdate
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrMarkFrame
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrUseFrames
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 28")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 4) & 0xFFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFFFFFFu << 4)) | ((value & 0xFFFFFFFu) << 4);
            }
        }

        [NativeTypeName("uint8_t *")]
        public byte* sliceTypeData;

        [NativeTypeName("uint32_t")]
        public uint sliceTypeArrayCnt;

        [NativeTypeName("uint32_t")]
        public uint seiPayloadArrayCnt;

        [NativeTypeName("NV_ENC_SEI_PAYLOAD *")]
        public _NV_ENC_SEI_PAYLOAD* seiPayloadArray;

        [NativeTypeName("uint32_t")]
        public uint sliceMode;

        [NativeTypeName("uint32_t")]
        public uint sliceModeData;

        [NativeTypeName("uint32_t")]
        public uint ltrMarkFrameIdx;

        [NativeTypeName("uint32_t")]
        public uint ltrUseFrameBitmap;

        [NativeTypeName("uint32_t")]
        public uint ltrUsageMode;

        [NativeTypeName("uint32_t")]
        public uint forceIntraSliceCount;

        [NativeTypeName("uint32_t *")]
        public uint* forceIntraSliceIdx;

        [NativeTypeName("NV_ENC_PIC_PARAMS_H264_EXT")]
        public _NV_ENC_PIC_PARAMS_H264_EXT h264ExtPicParams;

        [NativeTypeName("NV_ENC_TIME_CODE")]
        public _NV_ENC_TIME_CODE timeCode;

        [NativeTypeName("uint32_t[202]")]
        public fixed uint reserved[202];

        [NativeTypeName("void *[61]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_PIC_PARAMS_HEVC
    {
        [NativeTypeName("uint32_t")]
        public uint displayPOCSyntax;

        [NativeTypeName("uint32_t")]
        public uint refPicFlag;

        [NativeTypeName("uint32_t")]
        public uint temporalId;

        [NativeTypeName("uint32_t")]
        public uint forceIntraRefreshWithFrameCnt;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint constrainedFrame
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint sliceModeDataUpdate
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrMarkFrame
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrUseFrames
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint temporalConfigUpdate
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 27")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x7FFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFFFu << 5)) | ((value & 0x7FFFFFFu) << 5);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint reserved1;

        [NativeTypeName("uint8_t *")]
        public byte* sliceTypeData;

        [NativeTypeName("uint32_t")]
        public uint sliceTypeArrayCnt;

        [NativeTypeName("uint32_t")]
        public uint sliceMode;

        [NativeTypeName("uint32_t")]
        public uint sliceModeData;

        [NativeTypeName("uint32_t")]
        public uint ltrMarkFrameIdx;

        [NativeTypeName("uint32_t")]
        public uint ltrUseFrameBitmap;

        [NativeTypeName("uint32_t")]
        public uint ltrUsageMode;

        [NativeTypeName("uint32_t")]
        public uint seiPayloadArrayCnt;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_SEI_PAYLOAD *")]
        public _NV_ENC_SEI_PAYLOAD* seiPayloadArray;

        [NativeTypeName("NV_ENC_TIME_CODE")]
        public _NV_ENC_TIME_CODE timeCode;

        [NativeTypeName("uint32_t")]
        public uint numTemporalLayers;

        [NativeTypeName("uint32_t")]
        public uint viewId;

        [NativeTypeName("HEVC_3D_REFERENCE_DISPLAY_INFO *")]
        public _HEVC_3D_REFERENCE_DISPLAY_INFO* p3DReferenceDisplayInfo;

        [NativeTypeName("CONTENT_LIGHT_LEVEL *")]
        public _CONTENT_LIGHT_LEVEL* pMaxCll;

        [NativeTypeName("MASTERING_DISPLAY_INFO *")]
        public _MASTERING_DISPLAY_INFO* pMasteringDisplay;

        [NativeTypeName("uint32_t[234]")]
        public fixed uint reserved2[234];

        [NativeTypeName("void *[58]")]
        public _reserved3_e__FixedBuffer reserved3;

        public unsafe partial struct _reserved3_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_PIC_PARAMS_AV1
    {
        [NativeTypeName("uint32_t")]
        public uint displayPOCSyntax;

        [NativeTypeName("uint32_t")]
        public uint refPicFlag;

        [NativeTypeName("uint32_t")]
        public uint temporalId;

        [NativeTypeName("uint32_t")]
        public uint forceIntraRefreshWithFrameCnt;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint goldenFrameFlag
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint arfFrameFlag
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint arf2FrameFlag
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint bwdFrameFlag
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint overlayFrameFlag
        {
            readonly get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint showExistingFrameFlag
        {
            readonly get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint errorResilientModeFlag
        {
            readonly get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint tileConfigUpdate
        {
            readonly get
            {
                return (_bitfield >> 7) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint enableCustomTileConfig
        {
            readonly get
            {
                return (_bitfield >> 8) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 8)) | ((value & 0x1u) << 8);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint filmGrainParamsUpdate
        {
            readonly get
            {
                return (_bitfield >> 9) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrMarkFrame
        {
            readonly get
            {
                return (_bitfield >> 10) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 10)) | ((value & 0x1u) << 10);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrUseFrames
        {
            readonly get
            {
                return (_bitfield >> 11) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 11)) | ((value & 0x1u) << 11);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint temporalConfigUpdate
        {
            readonly get
            {
                return (_bitfield >> 12) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 12)) | ((value & 0x1u) << 12);
            }
        }

        [NativeTypeName("uint32_t : 19")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 13) & 0x7FFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFu << 13)) | ((value & 0x7FFFFu) << 13);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint numTileColumns;

        [NativeTypeName("uint32_t")]
        public uint numTileRows;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("uint32_t *")]
        public uint* tileWidths;

        [NativeTypeName("uint32_t *")]
        public uint* tileHeights;

        [NativeTypeName("uint32_t")]
        public uint obuPayloadArrayCnt;

        [NativeTypeName("uint32_t")]
        public uint reserved1;

        [NativeTypeName("NV_ENC_SEI_PAYLOAD *")]
        public _NV_ENC_SEI_PAYLOAD* obuPayloadArray;

        [NativeTypeName("NV_ENC_FILM_GRAIN_PARAMS_AV1 *")]
        public _NV_ENC_FILM_GRAIN_PARAMS_AV1* filmGrainParams;

        [NativeTypeName("uint32_t")]
        public uint ltrMarkFrameIdx;

        [NativeTypeName("uint32_t")]
        public uint ltrUseFrameBitmap;

        [NativeTypeName("uint32_t")]
        public uint numTemporalLayers;

        [NativeTypeName("uint32_t")]
        public uint reserved4;

        [NativeTypeName("CONTENT_LIGHT_LEVEL *")]
        public _CONTENT_LIGHT_LEVEL* pMaxCll;

        [NativeTypeName("MASTERING_DISPLAY_INFO *")]
        public _MASTERING_DISPLAY_INFO* pMasteringDisplay;

        [NativeTypeName("uint32_t[242]")]
        public fixed uint reserved2[242];

        [NativeTypeName("void *[59]")]
        public _reserved3_e__FixedBuffer reserved3;

        public unsafe partial struct _reserved3_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _NV_ENC_CODEC_PIC_PARAMS
    {
        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_PIC_PARAMS_H264")]
        public _NV_ENC_PIC_PARAMS_H264 h264PicParams;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_PIC_PARAMS_HEVC")]
        public _NV_ENC_PIC_PARAMS_HEVC hevcPicParams;

        [FieldOffset(0)]
        [NativeTypeName("NV_ENC_PIC_PARAMS_AV1")]
        public _NV_ENC_PIC_PARAMS_AV1 av1PicParams;

        [FieldOffset(0)]
        [NativeTypeName("uint32_t[256]")]
        public fixed uint reserved[256];
    }

    public unsafe partial struct _NV_ENC_PIC_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint inputWidth;

        [NativeTypeName("uint32_t")]
        public uint inputHeight;

        [NativeTypeName("uint32_t")]
        public uint inputPitch;

        [NativeTypeName("uint32_t")]
        public uint encodePicFlags;

        [NativeTypeName("uint32_t")]
        public uint frameIdx;

        [NativeTypeName("uint64_t")]
        public nuint inputTimeStamp;

        [NativeTypeName("uint64_t")]
        public nuint inputDuration;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* inputBuffer;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* outputBitstream;

        public void* completionEvent;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT bufferFmt;

        [NativeTypeName("NV_ENC_PIC_STRUCT")]
        public _NV_ENC_PIC_STRUCT pictureStruct;

        [NativeTypeName("NV_ENC_PIC_TYPE")]
        public _NV_ENC_PIC_TYPE pictureType;

        [NativeTypeName("NV_ENC_CODEC_PIC_PARAMS")]
        public _NV_ENC_CODEC_PIC_PARAMS codecPicParams;

        [NativeTypeName("NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE[2]")]
        public _meHintCountsPerBlock_e__FixedBuffer meHintCountsPerBlock;

        [NativeTypeName("NVENC_EXTERNAL_ME_HINT *")]
        public _NVENC_EXTERNAL_ME_HINT* meExternalHints;

        [NativeTypeName("uint32_t[7]")]
        public fixed uint reserved2[7];

        [NativeTypeName("void *[2]")]
        public _reserved5_e__FixedBuffer reserved5;

        [NativeTypeName("int8_t *")]
        public sbyte* qpDeltaMap;

        [NativeTypeName("uint32_t")]
        public uint qpDeltaMapSize;

        [NativeTypeName("uint32_t")]
        public uint reservedBitFields;

        [NativeTypeName("uint16_t[2]")]
        public fixed ushort meHintRefPicDist[2];

        [NativeTypeName("uint32_t")]
        public uint reserved4;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* alphaBuffer;

        [NativeTypeName("NVENC_EXTERNAL_ME_SB_HINT *")]
        public _NVENC_EXTERNAL_ME_SB_HINT* meExternalSbHints;

        [NativeTypeName("uint32_t")]
        public uint meSbHintsCount;

        [NativeTypeName("uint32_t")]
        public uint stateBufferIdx;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* outputReconBuffer;

        [NativeTypeName("uint32_t[284]")]
        public fixed uint reserved3[284];

        [NativeTypeName("void *[57]")]
        public _reserved6_e__FixedBuffer reserved6;

        public partial struct _meHintCountsPerBlock_e__FixedBuffer
        {
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e0;
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e1;

            public ref _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 2);
        }

        public unsafe partial struct _reserved5_e__FixedBuffer
        {
            public void* e0;
            public void* e1;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }

        public unsafe partial struct _reserved6_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_MEONLY_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint inputWidth;

        [NativeTypeName("uint32_t")]
        public uint inputHeight;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* inputBuffer;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* referenceFrame;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* mvBuffer;

        [NativeTypeName("uint32_t")]
        public uint reserved2;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT bufferFmt;

        public void* completionEvent;

        [NativeTypeName("uint32_t")]
        public uint viewID;

        [NativeTypeName("NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE[2]")]
        public _meHintCountsPerBlock_e__FixedBuffer meHintCountsPerBlock;

        [NativeTypeName("NVENC_EXTERNAL_ME_HINT *")]
        public _NVENC_EXTERNAL_ME_HINT* meExternalHints;

        [NativeTypeName("uint32_t[241]")]
        public fixed uint reserved1[241];

        [NativeTypeName("void *[59]")]
        public _reserved3_e__FixedBuffer reserved3;

        public partial struct _meHintCountsPerBlock_e__FixedBuffer
        {
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e0;
            public _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE e1;

            public ref _NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE this[int index]
            {
                get
                {
                    return ref AsSpan()[index];
                }
            }

            public Span<_NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 2);
        }

        public unsafe partial struct _reserved3_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_LOCK_BITSTREAM
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint doNotWait
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ltrFrame
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint getRCStats
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 29")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 3) & 0x1FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1FFFFFFFu << 3)) | ((value & 0x1FFFFFFFu) << 3);
            }
        }

        public void* outputBitstream;

        [NativeTypeName("uint32_t *")]
        public uint* sliceOffsets;

        [NativeTypeName("uint32_t")]
        public uint frameIdx;

        [NativeTypeName("uint32_t")]
        public uint hwEncodeStatus;

        [NativeTypeName("uint32_t")]
        public uint numSlices;

        [NativeTypeName("uint32_t")]
        public uint bitstreamSizeInBytes;

        [NativeTypeName("uint64_t")]
        public nuint outputTimeStamp;

        [NativeTypeName("uint64_t")]
        public nuint outputDuration;

        public void* bitstreamBufferPtr;

        [NativeTypeName("NV_ENC_PIC_TYPE")]
        public _NV_ENC_PIC_TYPE pictureType;

        [NativeTypeName("NV_ENC_PIC_STRUCT")]
        public _NV_ENC_PIC_STRUCT pictureStruct;

        [NativeTypeName("uint32_t")]
        public uint frameAvgQP;

        [NativeTypeName("uint32_t")]
        public uint frameSatd;

        [NativeTypeName("uint32_t")]
        public uint ltrFrameIdx;

        [NativeTypeName("uint32_t")]
        public uint ltrFrameBitmap;

        [NativeTypeName("uint32_t")]
        public uint temporalId;

        [NativeTypeName("uint32_t")]
        public uint intraMBCount;

        [NativeTypeName("uint32_t")]
        public uint interMBCount;

        [NativeTypeName("int32_t")]
        public int averageMVX;

        [NativeTypeName("int32_t")]
        public int averageMVY;

        [NativeTypeName("uint32_t")]
        public uint alphaLayerSizeInBytes;

        [NativeTypeName("uint32_t")]
        public uint outputStatsPtrSize;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        public void* outputStatsPtr;

        [NativeTypeName("uint32_t")]
        public uint frameIdxDisplay;

        [NativeTypeName("uint32_t[219]")]
        public fixed uint reserved1[219];

        [NativeTypeName("void *[63]")]
        public _reserved2_e__FixedBuffer reserved2;

        [NativeTypeName("uint32_t[8]")]
        public fixed uint reservedInternal[8];

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_LOCK_INPUT_BUFFER
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint doNotWait
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 31")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x7FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFFFFu << 1)) | ((value & 0x7FFFFFFFu) << 1);
            }
        }

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* inputBuffer;

        public void* bufferDataPtr;

        [NativeTypeName("uint32_t")]
        public uint pitch;

        [NativeTypeName("uint32_t[251]")]
        public fixed uint reserved1[251];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_MAP_INPUT_RESOURCE
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint subResourceIndex;

        public void* inputResource;

        [NativeTypeName("NV_ENC_REGISTERED_PTR")]
        public void* registeredResource;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* mappedResource;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT mappedBufferFmt;

        [NativeTypeName("uint32_t[251]")]
        public fixed uint reserved1[251];

        [NativeTypeName("void *[63]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public partial struct _NV_ENC_INPUT_RESOURCE_OPENGL_TEX
    {
        [NativeTypeName("uint32_t")]
        public uint texture;

        [NativeTypeName("uint32_t")]
        public uint target;
    }

    public unsafe partial struct _NV_ENC_FENCE_POINT_D3D12
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        public void* pFence;

        [NativeTypeName("uint64_t")]
        public nuint waitValue;

        [NativeTypeName("uint64_t")]
        public nuint signalValue;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint bWait
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint bSignal
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 30")]
        public uint reservedBitField
        {
            readonly get
            {
                return (_bitfield >> 2) & 0x3FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2);
            }
        }

        [NativeTypeName("uint32_t[7]")]
        public fixed uint reserved1[7];
    }

    public unsafe partial struct _NV_ENC_INPUT_RESOURCE_D3D12
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* pInputBuffer;

        [NativeTypeName("NV_ENC_FENCE_POINT_D3D12")]
        public _NV_ENC_FENCE_POINT_D3D12 inputFencePoint;

        [NativeTypeName("uint32_t[16]")]
        public fixed uint reserved1[16];

        [NativeTypeName("void *[16]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_OUTPUT_RESOURCE_D3D12
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_INPUT_PTR")]
        public void* pOutputBuffer;

        [NativeTypeName("NV_ENC_FENCE_POINT_D3D12")]
        public _NV_ENC_FENCE_POINT_D3D12 outputFencePoint;

        [NativeTypeName("uint32_t[16]")]
        public fixed uint reserved1[16];

        [NativeTypeName("void *[16]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_REGISTER_RESOURCE
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("NV_ENC_INPUT_RESOURCE_TYPE")]
        public _NV_ENC_INPUT_RESOURCE_TYPE resourceType;

        [NativeTypeName("uint32_t")]
        public uint width;

        [NativeTypeName("uint32_t")]
        public uint height;

        [NativeTypeName("uint32_t")]
        public uint pitch;

        [NativeTypeName("uint32_t")]
        public uint subResourceIndex;

        public void* resourceToRegister;

        [NativeTypeName("NV_ENC_REGISTERED_PTR")]
        public void* registeredResource;

        [NativeTypeName("NV_ENC_BUFFER_FORMAT")]
        public _NV_ENC_BUFFER_FORMAT bufferFormat;

        [NativeTypeName("NV_ENC_BUFFER_USAGE")]
        public _NV_ENC_BUFFER_USAGE bufferUsage;

        [NativeTypeName("NV_ENC_FENCE_POINT_D3D12 *")]
        public _NV_ENC_FENCE_POINT_D3D12* pInputFencePoint;

        [NativeTypeName("uint32_t[2]")]
        public fixed uint chromaOffset[2];

        [NativeTypeName("uint32_t[2]")]
        public fixed uint chromaOffsetIn[2];

        [NativeTypeName("uint32_t[244]")]
        public fixed uint reserved1[244];

        [NativeTypeName("void *[61]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_STAT
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("NV_ENC_OUTPUT_PTR")]
        public void* outputBitStream;

        [NativeTypeName("uint32_t")]
        public uint bitStreamSize;

        [NativeTypeName("uint32_t")]
        public uint picType;

        [NativeTypeName("uint32_t")]
        public uint lastValidByteOffset;

        [NativeTypeName("uint32_t[16]")]
        public fixed uint sliceOffsets[16];

        [NativeTypeName("uint32_t")]
        public uint picIdx;

        [NativeTypeName("uint32_t")]
        public uint frameAvgQP;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint ltrFrame
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 31")]
        public uint reservedBitFields
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x7FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFFFFu << 1)) | ((value & 0x7FFFFFFFu) << 1);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint ltrFrameIdx;

        [NativeTypeName("uint32_t")]
        public uint intraMBCount;

        [NativeTypeName("uint32_t")]
        public uint interMBCount;

        [NativeTypeName("int32_t")]
        public int averageMVX;

        [NativeTypeName("int32_t")]
        public int averageMVY;

        [NativeTypeName("uint32_t[227]")]
        public fixed uint reserved1[227];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_SEQUENCE_PARAM_PAYLOAD
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint inBufferSize;

        [NativeTypeName("uint32_t")]
        public uint spsId;

        [NativeTypeName("uint32_t")]
        public uint ppsId;

        public void* spsppsBuffer;

        [NativeTypeName("uint32_t *")]
        public uint* outSPSPPSPayloadSize;

        [NativeTypeName("uint32_t[250]")]
        public fixed uint reserved[250];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_EVENT_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        public void* completionEvent;

        [NativeTypeName("uint32_t[254]")]
        public fixed uint reserved1[254];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENC_OPEN_ENCODE_SESSIONEX_PARAMS
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("NV_ENC_DEVICE_TYPE")]
        public _NV_ENC_DEVICE_TYPE deviceType;

        public void* device;

        public void* reserved;

        [NativeTypeName("uint32_t")]
        public uint apiVersion;

        [NativeTypeName("uint32_t[253]")]
        public fixed uint reserved1[253];

        [NativeTypeName("void *[64]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public unsafe partial struct _NV_ENCODE_API_FUNCTION_LIST
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint reserved;

        [NativeTypeName("PNVENCOPENENCODESESSION")]
        public delegate* unmanaged[Cdecl]<void*, uint, void**, _NVENCSTATUS> nvEncOpenEncodeSession;

        [NativeTypeName("PNVENCGETENCODEGUIDCOUNT")]
        public delegate* unmanaged[Cdecl]<void*, uint*, _NVENCSTATUS> nvEncGetEncodeGUIDCount;

        [NativeTypeName("PNVENCGETENCODEPROFILEGUIDCOUNT")]
        public delegate* unmanaged[Cdecl]<void*, Guid, uint*, _NVENCSTATUS> nvEncGetEncodeProfileGUIDCount;

        [NativeTypeName("PNVENCGETENCODEPROFILEGUIDS")]
        public delegate* unmanaged[Cdecl]<void*, Guid, Guid*, uint, uint*, _NVENCSTATUS> nvEncGetEncodeProfileGUIDs;

        [NativeTypeName("PNVENCGETENCODEGUIDS")]
        public delegate* unmanaged[Cdecl]<void*, Guid*, uint, uint*, _NVENCSTATUS> nvEncGetEncodeGUIDs;

        [NativeTypeName("PNVENCGETINPUTFORMATCOUNT")]
        public delegate* unmanaged[Cdecl]<void*, Guid, uint*, _NVENCSTATUS> nvEncGetInputFormatCount;

        [NativeTypeName("PNVENCGETINPUTFORMATS")]
        public delegate* unmanaged[Cdecl]<void*, Guid, _NV_ENC_BUFFER_FORMAT*, uint, uint*, _NVENCSTATUS> nvEncGetInputFormats;

        [NativeTypeName("PNVENCGETENCODECAPS")]
        public delegate* unmanaged[Cdecl]<void*, Guid, _NV_ENC_CAPS_PARAM*, int*, _NVENCSTATUS> nvEncGetEncodeCaps;

        [NativeTypeName("PNVENCGETENCODEPRESETCOUNT")]
        public delegate* unmanaged[Cdecl]<void*, Guid, uint*, _NVENCSTATUS> nvEncGetEncodePresetCount;

        [NativeTypeName("PNVENCGETENCODEPRESETGUIDS")]
        public delegate* unmanaged[Cdecl]<void*, Guid, Guid*, uint, uint*, _NVENCSTATUS> nvEncGetEncodePresetGUIDs;

        [NativeTypeName("PNVENCGETENCODEPRESETCONFIG")]
        public delegate* unmanaged[Cdecl]<void*, Guid, Guid, _NV_ENC_PRESET_CONFIG*, _NVENCSTATUS> nvEncGetEncodePresetConfig;

        [NativeTypeName("PNVENCINITIALIZEENCODER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_INITIALIZE_PARAMS*, _NVENCSTATUS> nvEncInitializeEncoder;

        [NativeTypeName("PNVENCCREATEINPUTBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_CREATE_INPUT_BUFFER*, _NVENCSTATUS> nvEncCreateInputBuffer;

        [NativeTypeName("PNVENCDESTROYINPUTBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncDestroyInputBuffer;

        [NativeTypeName("PNVENCCREATEBITSTREAMBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_CREATE_BITSTREAM_BUFFER*, _NVENCSTATUS> nvEncCreateBitstreamBuffer;

        [NativeTypeName("PNVENCDESTROYBITSTREAMBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncDestroyBitstreamBuffer;

        [NativeTypeName("PNVENCENCODEPICTURE")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_PIC_PARAMS*, _NVENCSTATUS> nvEncEncodePicture;

        [NativeTypeName("PNVENCLOCKBITSTREAM")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_LOCK_BITSTREAM*, _NVENCSTATUS> nvEncLockBitstream;

        [NativeTypeName("PNVENCUNLOCKBITSTREAM")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncUnlockBitstream;

        [NativeTypeName("PNVENCLOCKINPUTBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_LOCK_INPUT_BUFFER*, _NVENCSTATUS> nvEncLockInputBuffer;

        [NativeTypeName("PNVENCUNLOCKINPUTBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncUnlockInputBuffer;

        [NativeTypeName("PNVENCGETENCODESTATS")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_STAT*, _NVENCSTATUS> nvEncGetEncodeStats;

        [NativeTypeName("PNVENCGETSEQUENCEPARAMS")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_SEQUENCE_PARAM_PAYLOAD*, _NVENCSTATUS> nvEncGetSequenceParams;

        [NativeTypeName("PNVENCREGISTERASYNCEVENT")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_EVENT_PARAMS*, _NVENCSTATUS> nvEncRegisterAsyncEvent;

        [NativeTypeName("PNVENCUNREGISTERASYNCEVENT")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_EVENT_PARAMS*, _NVENCSTATUS> nvEncUnregisterAsyncEvent;

        [NativeTypeName("PNVENCMAPINPUTRESOURCE")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_MAP_INPUT_RESOURCE*, _NVENCSTATUS> nvEncMapInputResource;

        [NativeTypeName("PNVENCUNMAPINPUTRESOURCE")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncUnmapInputResource;

        [NativeTypeName("PNVENCDESTROYENCODER")]
        public delegate* unmanaged[Cdecl]<void*, _NVENCSTATUS> nvEncDestroyEncoder;

        [NativeTypeName("PNVENCINVALIDATEREFFRAMES")]
        public delegate* unmanaged[Cdecl]<void*, nuint, _NVENCSTATUS> nvEncInvalidateRefFrames;

        [NativeTypeName("PNVENCOPENENCODESESSIONEX")]
        public delegate* unmanaged[Cdecl]<_NV_ENC_OPEN_ENCODE_SESSIONEX_PARAMS*, void**, _NVENCSTATUS> nvEncOpenEncodeSessionEx;

        [NativeTypeName("PNVENCREGISTERRESOURCE")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_REGISTER_RESOURCE*, _NVENCSTATUS> nvEncRegisterResource;

        [NativeTypeName("PNVENCUNREGISTERRESOURCE")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncUnregisterResource;

        [NativeTypeName("PNVENCRECONFIGUREENCODER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_RECONFIGURE_PARAMS*, _NVENCSTATUS> nvEncReconfigureEncoder;

        public void* reserved1;

        [NativeTypeName("PNVENCCREATEMVBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_CREATE_MV_BUFFER*, _NVENCSTATUS> nvEncCreateMVBuffer;

        [NativeTypeName("PNVENCDESTROYMVBUFFER")]
        public delegate* unmanaged[Cdecl]<void*, void*, _NVENCSTATUS> nvEncDestroyMVBuffer;

        [NativeTypeName("PNVENCRUNMOTIONESTIMATIONONLY")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_MEONLY_PARAMS*, _NVENCSTATUS> nvEncRunMotionEstimationOnly;

        [NativeTypeName("PNVENCGETLASTERROR")]
        public delegate* unmanaged[Cdecl]<void*, sbyte*> nvEncGetLastErrorString;

        [NativeTypeName("PNVENCSETIOCUDASTREAMS")]
        public delegate* unmanaged[Cdecl]<void*, void*, void*, _NVENCSTATUS> nvEncSetIOCudaStreams;

        [NativeTypeName("PNVENCGETENCODEPRESETCONFIGEX")]
        public delegate* unmanaged[Cdecl]<void*, Guid, Guid, NV_ENC_TUNING_INFO, _NV_ENC_PRESET_CONFIG*, _NVENCSTATUS> nvEncGetEncodePresetConfigEx;

        [NativeTypeName("PNVENCGETSEQUENCEPARAMEX")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_INITIALIZE_PARAMS*, _NV_ENC_SEQUENCE_PARAM_PAYLOAD*, _NVENCSTATUS> nvEncGetSequenceParamEx;

        [NativeTypeName("PNVENCRESTOREENCODERSTATE")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_RESTORE_ENCODER_STATE_PARAMS*, _NVENCSTATUS> nvEncRestoreEncoderState;

        [NativeTypeName("PNVENCLOOKAHEADPICTURE")]
        public delegate* unmanaged[Cdecl]<void*, _NV_ENC_LOOKAHEAD_PIC_PARAMS*, _NVENCSTATUS> nvEncLookaheadPicture;

        [NativeTypeName("void *[275]")]
        public _reserved2_e__FixedBuffer reserved2;

        public unsafe partial struct _reserved2_e__FixedBuffer
        {
            public void* e0;
            public void* e1;
            public void* e2;
            public void* e3;
            public void* e4;
            public void* e5;
            public void* e6;
            public void* e7;
            public void* e8;
            public void* e9;
            public void* e10;
            public void* e11;
            public void* e12;
            public void* e13;
            public void* e14;
            public void* e15;
            public void* e16;
            public void* e17;
            public void* e18;
            public void* e19;
            public void* e20;
            public void* e21;
            public void* e22;
            public void* e23;
            public void* e24;
            public void* e25;
            public void* e26;
            public void* e27;
            public void* e28;
            public void* e29;
            public void* e30;
            public void* e31;
            public void* e32;
            public void* e33;
            public void* e34;
            public void* e35;
            public void* e36;
            public void* e37;
            public void* e38;
            public void* e39;
            public void* e40;
            public void* e41;
            public void* e42;
            public void* e43;
            public void* e44;
            public void* e45;
            public void* e46;
            public void* e47;
            public void* e48;
            public void* e49;
            public void* e50;
            public void* e51;
            public void* e52;
            public void* e53;
            public void* e54;
            public void* e55;
            public void* e56;
            public void* e57;
            public void* e58;
            public void* e59;
            public void* e60;
            public void* e61;
            public void* e62;
            public void* e63;
            public void* e64;
            public void* e65;
            public void* e66;
            public void* e67;
            public void* e68;
            public void* e69;
            public void* e70;
            public void* e71;
            public void* e72;
            public void* e73;
            public void* e74;
            public void* e75;
            public void* e76;
            public void* e77;
            public void* e78;
            public void* e79;
            public void* e80;
            public void* e81;
            public void* e82;
            public void* e83;
            public void* e84;
            public void* e85;
            public void* e86;
            public void* e87;
            public void* e88;
            public void* e89;
            public void* e90;
            public void* e91;
            public void* e92;
            public void* e93;
            public void* e94;
            public void* e95;
            public void* e96;
            public void* e97;
            public void* e98;
            public void* e99;
            public void* e100;
            public void* e101;
            public void* e102;
            public void* e103;
            public void* e104;
            public void* e105;
            public void* e106;
            public void* e107;
            public void* e108;
            public void* e109;
            public void* e110;
            public void* e111;
            public void* e112;
            public void* e113;
            public void* e114;
            public void* e115;
            public void* e116;
            public void* e117;
            public void* e118;
            public void* e119;
            public void* e120;
            public void* e121;
            public void* e122;
            public void* e123;
            public void* e124;
            public void* e125;
            public void* e126;
            public void* e127;
            public void* e128;
            public void* e129;
            public void* e130;
            public void* e131;
            public void* e132;
            public void* e133;
            public void* e134;
            public void* e135;
            public void* e136;
            public void* e137;
            public void* e138;
            public void* e139;
            public void* e140;
            public void* e141;
            public void* e142;
            public void* e143;
            public void* e144;
            public void* e145;
            public void* e146;
            public void* e147;
            public void* e148;
            public void* e149;
            public void* e150;
            public void* e151;
            public void* e152;
            public void* e153;
            public void* e154;
            public void* e155;
            public void* e156;
            public void* e157;
            public void* e158;
            public void* e159;
            public void* e160;
            public void* e161;
            public void* e162;
            public void* e163;
            public void* e164;
            public void* e165;
            public void* e166;
            public void* e167;
            public void* e168;
            public void* e169;
            public void* e170;
            public void* e171;
            public void* e172;
            public void* e173;
            public void* e174;
            public void* e175;
            public void* e176;
            public void* e177;
            public void* e178;
            public void* e179;
            public void* e180;
            public void* e181;
            public void* e182;
            public void* e183;
            public void* e184;
            public void* e185;
            public void* e186;
            public void* e187;
            public void* e188;
            public void* e189;
            public void* e190;
            public void* e191;
            public void* e192;
            public void* e193;
            public void* e194;
            public void* e195;
            public void* e196;
            public void* e197;
            public void* e198;
            public void* e199;
            public void* e200;
            public void* e201;
            public void* e202;
            public void* e203;
            public void* e204;
            public void* e205;
            public void* e206;
            public void* e207;
            public void* e208;
            public void* e209;
            public void* e210;
            public void* e211;
            public void* e212;
            public void* e213;
            public void* e214;
            public void* e215;
            public void* e216;
            public void* e217;
            public void* e218;
            public void* e219;
            public void* e220;
            public void* e221;
            public void* e222;
            public void* e223;
            public void* e224;
            public void* e225;
            public void* e226;
            public void* e227;
            public void* e228;
            public void* e229;
            public void* e230;
            public void* e231;
            public void* e232;
            public void* e233;
            public void* e234;
            public void* e235;
            public void* e236;
            public void* e237;
            public void* e238;
            public void* e239;
            public void* e240;
            public void* e241;
            public void* e242;
            public void* e243;
            public void* e244;
            public void* e245;
            public void* e246;
            public void* e247;
            public void* e248;
            public void* e249;
            public void* e250;
            public void* e251;
            public void* e252;
            public void* e253;
            public void* e254;
            public void* e255;
            public void* e256;
            public void* e257;
            public void* e258;
            public void* e259;
            public void* e260;
            public void* e261;
            public void* e262;
            public void* e263;
            public void* e264;
            public void* e265;
            public void* e266;
            public void* e267;
            public void* e268;
            public void* e269;
            public void* e270;
            public void* e271;
            public void* e272;
            public void* e273;
            public void* e274;

            public ref void* this[int index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public static unsafe partial class Methods
    {
        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_CODEC_H264_GUID = new Guid(0x6bc82762, 0x4e63, 0x4ca4, 0xaa, 0x85, 0x1e, 0x50, 0xf3, 0x21, 0xf6, 0xbf);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_CODEC_HEVC_GUID = new Guid(0x790cdc88, 0x4522, 0x4d7b, 0x94, 0x25, 0xbd, 0xa9, 0x97, 0x5f, 0x76, 0x3);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_CODEC_AV1_GUID = new Guid(0x0a352289, 0x0aa7, 0x4759, 0x86, 0x2d, 0x5d, 0x15, 0xcd, 0x16, 0xd2, 0x54);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_CODEC_PROFILE_AUTOSELECT_GUID = new Guid(0xbfd6f8e7, 0x233c, 0x4341, 0x8b, 0x3e, 0x48, 0x18, 0x52, 0x38, 0x3, 0xf4);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_BASELINE_GUID = new Guid(0x727bcaa, 0x78c4, 0x4c83, 0x8c, 0x2f, 0xef, 0x3d, 0xff, 0x26, 0x7c, 0x6a);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_MAIN_GUID = new Guid(0x60b5c1d4, 0x67fe, 0x4790, 0x94, 0xd5, 0xc4, 0x72, 0x6d, 0x7b, 0x6e, 0x6d);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_HIGH_GUID = new Guid(0xe7cbc309, 0x4f7a, 0x4b89, 0xaf, 0x2a, 0xd5, 0x37, 0xc9, 0x2b, 0xe3, 0x10);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_HIGH_10_GUID = new Guid(0x8f0c337e, 0x186c, 0x48e9, 0xa6, 0x9d, 0x7a, 0x83, 0x34, 0x08, 0x97, 0x58);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_HIGH_422_GUID = new Guid(0xff3242e9, 0x613c, 0x4295, 0xa1, 0xe8, 0x2a, 0x7f, 0xe9, 0x4d, 0x81, 0x33);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_HIGH_444_GUID = new Guid(0x7ac663cb, 0xa598, 0x4960, 0xb8, 0x44, 0x33, 0x9b, 0x26, 0x1a, 0x7d, 0x52);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_STEREO_GUID = new Guid(0x40847bf5, 0x33f7, 0x4601, 0x90, 0x84, 0xe8, 0xfe, 0x3c, 0x1d, 0xb8, 0xb7);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_PROGRESSIVE_HIGH_GUID = new Guid(0xb405afac, 0xf32b, 0x417b, 0x89, 0xc4, 0x9a, 0xbe, 0xed, 0x3e, 0x59, 0x78);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_H264_PROFILE_CONSTRAINED_HIGH_GUID = new Guid(0xaec1bd87, 0xe85b, 0x48f2, 0x84, 0xc3, 0x98, 0xbc, 0xa6, 0x28, 0x50, 0x72);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_HEVC_PROFILE_MAIN_GUID = new Guid(0xb514c39a, 0xb55b, 0x40fa, 0x87, 0x8f, 0xf1, 0x25, 0x3b, 0x4d, 0xfd, 0xec);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_HEVC_PROFILE_MAIN10_GUID = new Guid(0xfa4d2b6c, 0x3a5b, 0x411a, 0x80, 0x18, 0x0a, 0x3f, 0x5e, 0x3c, 0x9b, 0xe5);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_HEVC_PROFILE_FREXT_GUID = new Guid(0x51ec32b5, 0x1b4c, 0x453c, 0x9c, 0xbd, 0xb6, 0x16, 0xbd, 0x62, 0x13, 0x41);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_AV1_PROFILE_MAIN_GUID = new Guid(0x5f2a39f5, 0xf14e, 0x4f95, 0x9a, 0x9e, 0xb7, 0x6d, 0x56, 0x8f, 0xcf, 0x97);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P1_GUID = new Guid(0xfc0a8d3e, 0x45f8, 0x4cf8, 0x80, 0xc7, 0x29, 0x88, 0x71, 0x59, 0xe, 0xbf);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P2_GUID = new Guid(0xf581cfb8, 0x88d6, 0x4381, 0x93, 0xf0, 0xdf, 0x13, 0xf9, 0xc2, 0x7d, 0xab);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P3_GUID = new Guid(0x36850110, 0x3a07, 0x441f, 0x94, 0xd5, 0x36, 0x70, 0x63, 0x1f, 0x91, 0xf6);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P4_GUID = new Guid(0x90a7b826, 0xdf06, 0x4862, 0xb9, 0xd2, 0xcd, 0x6d, 0x73, 0xa0, 0x86, 0x81);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P5_GUID = new Guid(0x21c6e6b4, 0x297a, 0x4cba, 0x99, 0x8f, 0xb6, 0xcb, 0xde, 0x72, 0xad, 0xe3);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P6_GUID = new Guid(0x8e75c279, 0x6299, 0x4ab6, 0x83, 0x2, 0xb, 0x21, 0x5a, 0x33, 0x5c, 0xf5);

        [NativeTypeName("const GUID")]
        public static readonly Guid NV_ENC_PRESET_P7_GUID = new Guid(0x84848c12, 0x6f71, 0x4c13, 0x93, 0x1b, 0x53, 0xe2, 0x83, 0xf5, 0x79, 0x74);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncOpenEncodeSession(void* device, [NativeTypeName("uint32_t")] uint deviceType, void** encoder);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeGUIDCount(void* encoder, [NativeTypeName("uint32_t *")] uint* encodeGUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeGUIDs(void* encoder, Guid* GUIDs, [NativeTypeName("uint32_t")] uint guidArraySize, [NativeTypeName("uint32_t *")] uint* GUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeProfileGUIDCount(void* encoder, Guid encodeGUID, [NativeTypeName("uint32_t *")] uint* encodeProfileGUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeProfileGUIDs(void* encoder, Guid encodeGUID, Guid* profileGUIDs, [NativeTypeName("uint32_t")] uint guidArraySize, [NativeTypeName("uint32_t *")] uint* GUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetInputFormatCount(void* encoder, Guid encodeGUID, [NativeTypeName("uint32_t *")] uint* inputFmtCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetInputFormats(void* encoder, Guid encodeGUID, [NativeTypeName("NV_ENC_BUFFER_FORMAT *")] _NV_ENC_BUFFER_FORMAT* inputFmts, [NativeTypeName("uint32_t")] uint inputFmtArraySize, [NativeTypeName("uint32_t *")] uint* inputFmtCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeCaps(void* encoder, Guid encodeGUID, [NativeTypeName("NV_ENC_CAPS_PARAM *")] _NV_ENC_CAPS_PARAM* capsParam, int* capsVal);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodePresetCount(void* encoder, Guid encodeGUID, [NativeTypeName("uint32_t *")] uint* encodePresetGUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodePresetGUIDs(void* encoder, Guid encodeGUID, Guid* presetGUIDs, [NativeTypeName("uint32_t")] uint guidArraySize, [NativeTypeName("uint32_t *")] uint* encodePresetGUIDCount);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodePresetConfig(void* encoder, Guid encodeGUID, Guid presetGUID, [NativeTypeName("NV_ENC_PRESET_CONFIG *")] _NV_ENC_PRESET_CONFIG* presetConfig);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodePresetConfigEx(void* encoder, Guid encodeGUID, Guid presetGUID, NV_ENC_TUNING_INFO tuningInfo, [NativeTypeName("NV_ENC_PRESET_CONFIG *")] _NV_ENC_PRESET_CONFIG* presetConfig);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncInitializeEncoder(void* encoder, [NativeTypeName("NV_ENC_INITIALIZE_PARAMS *")] _NV_ENC_INITIALIZE_PARAMS* createEncodeParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncCreateInputBuffer(void* encoder, [NativeTypeName("NV_ENC_CREATE_INPUT_BUFFER *")] _NV_ENC_CREATE_INPUT_BUFFER* createInputBufferParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncDestroyInputBuffer(void* encoder, [NativeTypeName("NV_ENC_INPUT_PTR")] void* inputBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncSetIOCudaStreams(void* encoder, [NativeTypeName("NV_ENC_CUSTREAM_PTR")] void* inputStream, [NativeTypeName("NV_ENC_CUSTREAM_PTR")] void* outputStream);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncCreateBitstreamBuffer(void* encoder, [NativeTypeName("NV_ENC_CREATE_BITSTREAM_BUFFER *")] _NV_ENC_CREATE_BITSTREAM_BUFFER* createBitstreamBufferParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncDestroyBitstreamBuffer(void* encoder, [NativeTypeName("NV_ENC_OUTPUT_PTR")] void* bitstreamBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncEncodePicture(void* encoder, [NativeTypeName("NV_ENC_PIC_PARAMS *")] _NV_ENC_PIC_PARAMS* encodePicParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncLockBitstream(void* encoder, [NativeTypeName("NV_ENC_LOCK_BITSTREAM *")] _NV_ENC_LOCK_BITSTREAM* lockBitstreamBufferParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncUnlockBitstream(void* encoder, [NativeTypeName("NV_ENC_OUTPUT_PTR")] void* bitstreamBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncRestoreEncoderState(void* encoder, [NativeTypeName("NV_ENC_RESTORE_ENCODER_STATE_PARAMS *")] _NV_ENC_RESTORE_ENCODER_STATE_PARAMS* restoreState);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncLockInputBuffer(void* encoder, [NativeTypeName("NV_ENC_LOCK_INPUT_BUFFER *")] _NV_ENC_LOCK_INPUT_BUFFER* lockInputBufferParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncUnlockInputBuffer(void* encoder, [NativeTypeName("NV_ENC_INPUT_PTR")] void* inputBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetEncodeStats(void* encoder, [NativeTypeName("NV_ENC_STAT *")] _NV_ENC_STAT* encodeStats);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetSequenceParams(void* encoder, [NativeTypeName("NV_ENC_SEQUENCE_PARAM_PAYLOAD *")] _NV_ENC_SEQUENCE_PARAM_PAYLOAD* sequenceParamPayload);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncGetSequenceParamEx(void* encoder, [NativeTypeName("NV_ENC_INITIALIZE_PARAMS *")] _NV_ENC_INITIALIZE_PARAMS* encInitParams, [NativeTypeName("NV_ENC_SEQUENCE_PARAM_PAYLOAD *")] _NV_ENC_SEQUENCE_PARAM_PAYLOAD* sequenceParamPayload);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncRegisterAsyncEvent(void* encoder, [NativeTypeName("NV_ENC_EVENT_PARAMS *")] _NV_ENC_EVENT_PARAMS* eventParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncUnregisterAsyncEvent(void* encoder, [NativeTypeName("NV_ENC_EVENT_PARAMS *")] _NV_ENC_EVENT_PARAMS* eventParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncMapInputResource(void* encoder, [NativeTypeName("NV_ENC_MAP_INPUT_RESOURCE *")] _NV_ENC_MAP_INPUT_RESOURCE* mapInputResParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncUnmapInputResource(void* encoder, [NativeTypeName("NV_ENC_INPUT_PTR")] void* mappedInputBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncDestroyEncoder(void* encoder);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncInvalidateRefFrames(void* encoder, [NativeTypeName("uint64_t")] nuint invalidRefFrameTimeStamp);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncOpenEncodeSessionEx([NativeTypeName("NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS *")] _NV_ENC_OPEN_ENCODE_SESSIONEX_PARAMS* openSessionExParams, void** encoder);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncRegisterResource(void* encoder, [NativeTypeName("NV_ENC_REGISTER_RESOURCE *")] _NV_ENC_REGISTER_RESOURCE* registerResParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncUnregisterResource(void* encoder, [NativeTypeName("NV_ENC_REGISTERED_PTR")] void* registeredResource);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncReconfigureEncoder(void* encoder, [NativeTypeName("NV_ENC_RECONFIGURE_PARAMS *")] _NV_ENC_RECONFIGURE_PARAMS* reInitEncodeParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncCreateMVBuffer(void* encoder, [NativeTypeName("NV_ENC_CREATE_MV_BUFFER *")] _NV_ENC_CREATE_MV_BUFFER* createMVBufferParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncDestroyMVBuffer(void* encoder, [NativeTypeName("NV_ENC_OUTPUT_PTR")] void* mvBuffer);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncRunMotionEstimationOnly(void* encoder, [NativeTypeName("NV_ENC_MEONLY_PARAMS *")] _NV_ENC_MEONLY_PARAMS* meOnlyParams);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncodeAPIGetMaxSupportedVersion([NativeTypeName("uint32_t *")] uint* version);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* NvEncGetLastErrorString(void* encoder);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncLookaheadPicture(void* encoder, [NativeTypeName("NV_ENC_LOOKAHEAD_PIC_PARAMS *")] _NV_ENC_LOOKAHEAD_PIC_PARAMS* lookaheadParamas);

        [DllImport("libnvidia-encode.so.1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NVENCSTATUS")]
        public static extern _NVENCSTATUS NvEncodeAPICreateInstance([NativeTypeName("NV_ENCODE_API_FUNCTION_LIST *")] _NV_ENCODE_API_FUNCTION_LIST* functionList);
    }
}
