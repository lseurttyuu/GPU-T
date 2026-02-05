namespace GPU_T.Models;

// To jest nasz główny model danych technicznych karty
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
    // Zegary domyślne
    string DefGpuClock,
    string DefBoostClock,
    string DefMemClock,
    string LookupUrl
);