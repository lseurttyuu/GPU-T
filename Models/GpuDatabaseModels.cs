using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPU_T.Models;

/// <summary>
/// Represents the root structure of the GPU database, including version, vendors, and GPU variants.
/// </summary>
public class GpuDatabaseRoot
{
    /// <summary>
    /// Gets or sets the database version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the vendor dictionary mapping vendor codes to names.
    /// </summary>
    [JsonPropertyName("vendors")]
    public Dictionary<string, string> Vendors { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary mapping GPU keys to lists of variant specifications.
    /// </summary>
    [JsonPropertyName("gpus")]
    public Dictionary<string, List<GpuSpecDto>> Gpus { get; set; } = new();
}

/// <summary>
/// Data transfer object representing a single GPU variant specification.
/// </summary>
public class GpuSpecDto
{
    /// <summary>
    /// Gets or sets the variant name.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
    
    /// <summary>
    /// Gets or sets the list of revision identifiers for this variant.
    /// </summary>
    [JsonPropertyName("revisions")] public List<string> Revisions { get; set; } = new();

    /// <summary>
    /// Gets or sets the code name of the GPU.
    /// </summary>
    [JsonPropertyName("codeName")] public string CodeName { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the manufacturing technology.
    /// </summary>
    [JsonPropertyName("technology")] public string Technology { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the die size.
    /// </summary>
    [JsonPropertyName("dieSize")] public string DieSize { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    [JsonPropertyName("releaseDate")] public string ReleaseDate { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the transistor count.
    /// </summary>
    [JsonPropertyName("transistors")] public string Transistors { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the number of ROPs.
    /// </summary>
    [JsonPropertyName("rops")] public string Rops { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the number of TMUs.
    /// </summary>
    [JsonPropertyName("tmus")] public string Tmus { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the number of shaders.
    /// </summary>
    [JsonPropertyName("shaders")] public string Shaders { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the number of compute units.
    /// </summary>
    [JsonPropertyName("computeUnits")] public string ComputeUnits { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the memory type.
    /// </summary>
    [JsonPropertyName("memoryType")] public string MemoryType { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the memory bus width.
    /// </summary>
    [JsonPropertyName("busWidth")] public string BusWidth { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the default GPU clock.
    /// </summary>
    [JsonPropertyName("defGpuClock")] public string DefGpuClock { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the default boost clock.
    /// </summary>
    [JsonPropertyName("defBoostClock")] public string DefBoostClock { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the default memory clock.
    /// </summary>
    [JsonPropertyName("defMemClock")] public string DefMemClock { get; set; } = "N/A";
    /// <summary>
    /// Gets or sets the lookup URL for external reference.
    /// </summary>
    [JsonPropertyName("lookupUrl")] public string LookupUrl { get; set; } = "";

    /// <summary>
    /// Converts this DTO to a GpuSpec instance, optionally overriding the name and specifying match accuracy.
    /// </summary>
    /// <param name="isExactMatch">Indicates whether the match is exact or fallback.</param>
    /// <param name="overrideName">Optional override for the GPU name.</param>
    /// <return>A GpuSpec instance representing this variant.</return>
    public GpuSpec ToGpuSpec(bool isExactMatch = true, string? overrideName = null)
    {
        // Uses overrideName if provided, otherwise uses the original variant name.
        return new GpuSpec(
            overrideName ?? Name,
            CodeName, Technology, DieSize, ReleaseDate, Transistors,
            Rops, Tmus, Shaders, ComputeUnits, MemoryType, BusWidth,
            DefGpuClock, DefBoostClock, DefMemClock, LookupUrl,
            isExactMatch
        );
    }
}