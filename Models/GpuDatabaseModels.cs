using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPU_T.Models;

public class GpuDatabaseRoot
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("vendors")]
    public Dictionary<string, string> Vendors { get; set; } = new();

    // ZMIANA: Teraz klucz wskazuje na LISTĘ wariantów, a nie jeden obiekt
    [JsonPropertyName("gpus")]
    public Dictionary<string, List<GpuSpecDto>> Gpus { get; set; } = new();
}

public class GpuSpecDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
    
    // ZMIANA: Lista rewizji dla tego konkretnego wariantu (np. ["C8", "CA"])
    [JsonPropertyName("revisions")] public List<string> Revisions { get; set; } = new();

    [JsonPropertyName("codeName")] public string CodeName { get; set; } = "N/A";
    [JsonPropertyName("technology")] public string Technology { get; set; } = "N/A";
    [JsonPropertyName("dieSize")] public string DieSize { get; set; } = "N/A";
    [JsonPropertyName("releaseDate")] public string ReleaseDate { get; set; } = "N/A";
    [JsonPropertyName("transistors")] public string Transistors { get; set; } = "N/A";
    [JsonPropertyName("rops")] public string Rops { get; set; } = "N/A";
    [JsonPropertyName("tmus")] public string Tmus { get; set; } = "N/A";
    [JsonPropertyName("shaders")] public string Shaders { get; set; } = "N/A";
    [JsonPropertyName("computeUnits")] public string ComputeUnits { get; set; } = "N/A";
    [JsonPropertyName("memoryType")] public string MemoryType { get; set; } = "N/A";
    [JsonPropertyName("busWidth")] public string BusWidth { get; set; } = "N/A";
    [JsonPropertyName("defGpuClock")] public string DefGpuClock { get; set; } = "N/A";
    [JsonPropertyName("defBoostClock")] public string DefBoostClock { get; set; } = "N/A";
    [JsonPropertyName("defMemClock")] public string DefMemClock { get; set; } = "N/A";
    [JsonPropertyName("lookupUrl")] public string LookupUrl { get; set; } = "";

    // ZMIANA: Dodano parametry opcjonalne do obsługi Fallbacku
    public GpuSpec ToGpuSpec(bool isExactMatch = true, string? overrideName = null)
    {
        return new GpuSpec(
            overrideName ?? Name, // Użyj nadpisanej nazwy lub oryginalnej
            CodeName, Technology, DieSize, ReleaseDate, Transistors,
            Rops, Tmus, Shaders, ComputeUnits, MemoryType, BusWidth,
            DefGpuClock, DefBoostClock, DefMemClock, LookupUrl,
            isExactMatch // Przekazujemy flagę dalej
        );
    }
}