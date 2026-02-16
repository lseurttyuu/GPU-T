using System;
using GPU_T.Services.Advanced;
using GPU_T.Models;

namespace GPU_T.Services;

/// <summary>
/// Container for static GPU data returned by probes to view models.
/// </summary>
public record GpuStaticData
{
    /// <summary>Device display name.</summary>
    public string DeviceName { get; init; } = "Unknown";
    /// <summary>Indicates whether the detected spec is an exact match from the database.</summary>
    public bool IsExactMatch { get; init; } = true;
    /// <summary>PCI device identifier.</summary>
    public string DeviceId { get; init; } = "Unknown";
    /// <summary>Subvendor identifier.</summary>
    public string Subvendor { get; init; } = "Unknown";
    /// <summary>BIOS version string.</summary>
    public string BiosVersion { get; init; } = "Unknown";
    /// <summary>PCI Bus ID string.</summary>
    public string BusId { get; init; } = "Unknown";
    /// <summary>Driver version string.</summary>
    public string DriverVersion { get; init; } = "Unknown";
    /// <summary>Driver build date or N/A.</summary>
    public string DriverDate { get; init; } = "N/A";
    /// <summary>Reported Vulkan API version.</summary>
    public string VulkanApi { get; init; } = "N/A";
    /// <summary>Bus interface description (e.g., PCIe x16).</summary>
    public string BusInterface { get; init; } = "N/A";

    /// <summary>GPU codename or internal identifier.</summary>
    public string GpuCodeName { get; init; } = "N/A";
    /// <summary>Revision identifier.</summary>
    public string Revision { get; init; } = "N/A";
    /// <summary>Manufacturing technology.</summary>
    public string Technology { get; init; } = "N/A";
    /// <summary>Die size description.</summary>
    public string DieSize { get; init; } = "N/A";
    /// <summary>Release date string.</summary>
    public string ReleaseDate { get; init; } = "N/A";
    /// <summary>Transistor count or description.</summary>
    public string Transistors { get; init; } = "N/A";

    /// <summary>ROPs and TMUs summary.</summary>
    public string RopsTmus { get; init; } = "N/A";
    /// <summary>Number of shaders.</summary>
    public string Shaders { get; init; } = "N/A";
    /// <summary>Number of compute units.</summary>
    public string ComputeUnits { get; init; } = "N/A";
    /// <summary>Approximate pixel fillrate.</summary>
    public string PixelFillrate { get; init; } = "N/A";
    /// <summary>Approximate texture fillrate.</summary>
    public string TextureFillrate { get; init; } = "N/A";

    /// <summary>Memory type description.</summary>
    public string MemoryType { get; init; } = "N/A";
    /// <summary>Memory bus width description.</summary>
    public string BusWidth { get; init; } = "N/A";
    /// <summary>Total memory size (human-readable).</summary>
    public string MemorySize { get; init; } = "0 MB";
    /// <summary>Memory bandwidth description.</summary>
    public string Bandwidth { get; init; } = "N/A";

    /// <summary>Default GPU clock.</summary>
    public string DefaultGpuClock { get; init; } = "N/A";
    /// <summary>Default memory clock.</summary>
    public string DefaultMemoryClock { get; init; } = "N/A";
    /// <summary>Default boost clock.</summary>
    public string DefaultBoostClock { get; init; } = "N/A";

    /// <summary>Resizable BAR state description.</summary>
    public string ResizableBarState { get; init; } = "N/A";
    /// <summary>Indicates if OpenCL is available.</summary>
    public bool IsOpenClAvailable { get; init; }
    /// <summary>Indicates if CUDA is available.</summary>
    public bool IsCudaAvailable { get; init; }
    /// <summary>Indicates if ROCm is available.</summary>
    public bool IsRocmAvailable { get; init; }
    /// <summary>Indicates if HSA is available.</summary>
    public bool IsHsaAvailable { get; init; }
    /// <summary>Indicates if Vulkan is available.</summary>
    public bool IsVulkanAvailable { get; init; }
    /// <summary>Indicates if Ray Tracing is available.</summary>
    public bool IsRayTracingAvailable { get; init; }
    /// <summary>Indicates if OpenGL is available.</summary>
    public bool IsOpenglAvailable { get; init; }
    /// <summary>Indicates if UEFI is available.</summary>
    public bool IsUefiAvailable { get; init; }
    /// <summary>Indicates if PhysX support is enabled.</summary>
    public bool IsPhysXEnabled { get; init; }
    /// <summary>Reference lookup URL for the GPU entry.</summary>
    public string LookupUrl { get; init; } = "";
    /// <summary>Current GPU clock as a formatted string.</summary>
    public string CurrentGpuClock { get; init; } = "0 MHz";
    /// <summary>Current memory clock as a formatted string.</summary>
    public string CurrentMemClock { get; init; } = "0 MHz";
    /// <summary>Current boost clock as a formatted string.</summary>
    public string BoostClock { get; init; } = "0 MHz";
}

/// <summary>
/// Interface defining operations a GPU probe must implement to provide static and sensor data.
/// </summary>
public interface IGpuProbe
{
    /// <summary>
    /// Scans the system and returns static GPU information.
    /// </summary>
    /// <returns>A <see cref="GpuStaticData"/> instance with discovered values.</returns>
    GpuStaticData LoadStaticData();

    /// <summary>
    /// Reads and returns live sensor data for the GPU.
    /// </summary>
    /// <returns>A <see cref="GpuSensorData"/> object containing sensor readings.</returns>
    GpuSensorData LoadSensorData();

    /// <summary>
    /// Returns availability information for sensors on this GPU.
    /// </summary>
    /// <returns>A <see cref="SensorAvailability"/> instance describing available sensors.</returns>
    SensorAvailability GetSensorAvailability();

    /// <summary>
    /// Returns an advanced data provider instance for the specified category, or null if unsupported.
    /// </summary>
    /// <param name="category">The advanced category name.</param>
    /// <returns>An <see cref="AdvancedDataProvider"/> instance or null.</returns>
    AdvancedDataProvider? GetAdvancedDataProvider(string category);

    /// <summary>
    /// Returns the list of advanced categories supported by this probe.
    /// </summary>
    string[] GetAdvancedCategories();
}