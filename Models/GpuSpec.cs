namespace GPU_T.Models;

public record GpuSpec(
    string Name,
    string CodeName,
    string Technology,
    string DieSize,
    string ReleaseDate,
    string Transistors,
    string Rops,
    string Tmus,
    string Shaders,
    string ComputeUnits,
    string MemoryType,
    string BusWidth,
    string DefGpuClock,
    string DefBoostClock,
    string DefMemClock,
    string LookupUrl,
    bool IsExactMatch = true
);