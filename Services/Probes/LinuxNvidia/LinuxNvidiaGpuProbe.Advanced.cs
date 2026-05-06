using GPU_T.Services.Advanced;
using GPU_T.Services.Advanced.LinuxNvidia;

namespace GPU_T.Services.Probes.LinuxNvidia;

public partial class LinuxNvidiaGpuProbe
{
    public AdvancedDataProvider? GetAdvancedDataProvider(string category)
    {
        return category switch
        {
            "General" => new GeneralProvider(),
            "Vulkan" => new VulkanProvider(),
            "OpenCL" => new OpenClProvider(),
            "CUDA" => new LinuxNvidiaCudaProvider(),
            "Multimedia (NVENC/NVDEC)" => new LinuxNvidiaMultimediaProvider(),
            "Power & Limits" => new LinuxNvidiaPowerProvider(),
            "PCIe Resizable BAR" => new ResizableBarProvider(),
            _ => null
        };
    }

    public string[] GetAdvancedCategories()
    {
        return new[] { "General", "Vulkan", "OpenCL", "CUDA", "Multimedia (NVENC/NVDEC)", "Power & Limits", "PCIe Resizable BAR" };
    }
}