using System.Collections.Generic;
using System.IO;
using GPU_T.Models;

namespace GPU_T.Services.Probes.LinuxNvidia;

/// <summary>
/// Probe implementation for NVIDIA GPUs on Linux. Uses nvidia-smi as primary data source
/// with sysfs/hwmon fallback for sensor and static data collection.
/// </summary>
public partial class LinuxNvidiaGpuProbe : IGpuProbe
{
    private readonly string _basePath;
    private readonly string _hwmonPath = string.Empty;
    private readonly string _gpuId;
    private readonly string _busId;
    private readonly string _memoryType;

    private class ProbeStateCache
    {
        // Availability Cache
        public bool IsAvailabilityCached = false;
        public SensorAvailability Availability = new();
        public bool? IsNvapiSupported;

        // Smart Polling Cache
        public bool HasInitialData = false;
        public GpuSensorData LastData = new();
        public bool IsUpdating = false;
        public readonly object LockObj = new object();
    }

    private static readonly Dictionary<string, ProbeStateCache> _stateCache = new();

    /// <summary>
    /// Cached result of nvidia-smi availability check. Null means unchecked.
    /// </summary>
    private static bool? _nvidiaSmiAvailable;

    public LinuxNvidiaGpuProbe(string gpuId, string memoryType = "")
    {
        _gpuId = gpuId;
        _memoryType = memoryType;
        _basePath = $"/sys/class/drm/{gpuId}/device";

        // Workaround for legacy proprietary drivers / simple-framebuffer bindings
        if (!File.Exists(Path.Combine(_basePath, "vendor")))
        {
            DirectoryInfo currentDir = new DirectoryInfo($"/sys/class/drm/{gpuId}");

            if (currentDir.Exists)
            {
                // Resolve the sysfs symlink to get the absolute physical path
                // (e.g., /sys/devices/pci0000:00/.../simple-framebuffer.0/drm/card0)
                FileSystemInfo? resolvedTarget = currentDir.ResolveLinkTarget(true);
                
                // Convert back to DirectoryInfo (fallback to currentDir if it wasn't a symlink)
                DirectoryInfo? searchDir = resolvedTarget as DirectoryInfo ?? currentDir;

                // Path-Resolving algo: walk up the tree until we find the PCI device root
                while (searchDir != null)
                {
                    if (File.Exists(Path.Combine(searchDir.FullName, "vendor")))
                    {
                        // Found the actual hardware device directory
                        _basePath = searchDir.FullName;
                        break;
                    }
                    // Move one directory level up
                    searchDir = searchDir.Parent;
                }
            }
        }

        if (Directory.Exists($"{_basePath}/hwmon"))
        {
            var dirs = Directory.GetDirectories($"{_basePath}/hwmon");
            if (dirs.Length > 0) _hwmonPath = dirs[0];
        }

        _busId = Utilities.GpuFeatureDetection.GetBusId(_basePath);
    }

    private ProbeStateCache GetState()
    {
        if (!_stateCache.TryGetValue(_gpuId, out var state))
        {
            state = new ProbeStateCache();
            _stateCache[_gpuId] = state;
        }
        return state;
    }
}