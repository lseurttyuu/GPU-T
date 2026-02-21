using GPU_T.Services.Utilities;

namespace GPU_T.Services.Probes.LinuxAmd;

/// <summary>
/// Provides feature detection methods for Linux AMD GPU probes, delegating to shared utilities.
/// </summary>
public partial class LinuxAmdGpuProbe
{
    /// <summary>
    /// Checks whether OpenGL direct rendering is supported and not using software rendering.
    /// </summary>
    private bool CheckOpenglSupport() => GpuFeatureDetection.CheckOpenglSupport();

    /// <summary>
    /// Checks Ray Tracing support for the specified Vulkan device ID using cached results.
    /// </summary>
    private bool CheckRayTracingSupportVulkan(string currentDeviceIdHex)
        => GpuFeatureDetection.CheckRayTracingSupportVulkan(currentDeviceIdHex);
}
