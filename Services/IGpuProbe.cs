using System;

namespace GPU_T.Services;

// Kontener na wszystkie dane statyczne, które przesyłamy z Serwisu do ViewModela
public record GpuStaticData
{
    // === Podstawowe ===
    public string DeviceName { get; init; } = "Unknown";
    public string DeviceId { get; init; } = "Unknown";
    public string Subvendor { get; init; } = "Unknown";
    public string BiosVersion { get; init; } = "Unknown";
    public string BusId { get; init; } = "Unknown";
    public string DriverVersion { get; init; } = "Unknown";
    public string DriverDate { get; init; } = "N/A";
    public string VulkanApi { get; init; } = "N/A";
    public string BusInterface { get; init; } = "N/A";

    // === Architektura (Brakowało tych definicji) ===
    public string GpuCodeName { get; init; } = "N/A";
    public string Revision { get; init; } = "N/A";
    public string Technology { get; init; } = "N/A";
    public string DieSize { get; init; } = "N/A";
    public string ReleaseDate { get; init; } = "N/A";
    public string Transistors { get; init; } = "N/A";

    // === Jednostki i Wydajność (Brakowało tych definicji) ===
    public string RopsTmus { get; init; } = "N/A";
    public string Shaders { get; init; } = "N/A";
    public string ComputeUnits { get; init; } = "N/A";
    public string PixelFillrate { get; init; } = "N/A"; // To świeciło na czerwono
    public string TextureFillrate { get; init; } = "N/A"; // To też

    // === Pamięć ===
    public string MemoryType { get; init; } = "N/A";
    public string BusWidth { get; init; } = "N/A";
    public string MemorySize { get; init; } = "0 MB";
    public string Bandwidth { get; init; } = "N/A";

    // === Zegary Domyślne (Brakowało tych definicji) ===
    public string DefaultGpuClock { get; init; } = "N/A"; // To świeciło na czerwono
    public string DefaultMemoryClock { get; init; } = "N/A"; // To też
    public string DefaultBoostClock { get; init; } = "N/A"; // To też

    // === Statusy Funkcji (Boole) ===
    public string ResizableBarState { get; init; } = "N/A";    public bool IsOpenClAvailable { get; init; }
    public bool IsCudaAvailable { get; init; }
    public bool IsRocmAvailable { get; init; }
    public bool IsHsaAvailable { get; init; }
    public bool IsVulkanAvailable { get; init; }
    public bool IsRayTracingAvailable { get; init; }
    public bool IsOpenglAvailable { get; init; }
    public bool IsUefiAvailable { get; init; }
    public bool IsPhysXEnabled { get; init; }
    public string LookupUrl { get; init; } = ""; // <--- NOWE
    public string CurrentGpuClock { get; init; } = "0 MHz"; // <--- NOWE
    public string CurrentMemClock { get; init; } = "0 MHz"; // <--- NOWE
    public string BoostClock { get; init; } = "0 MHz"; // <--- NOWE
}

public interface IGpuProbe
{
    // Metoda, która przeskanuje system i zwróci dane statyczne
    GpuStaticData LoadStaticData();

    GpuSensorData LoadSensorData();
}