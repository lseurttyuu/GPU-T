using GPU_T.Models;

namespace GPU_T.Services;

public static class PciIdLookup
{
    // Metoda pomocnicza, która konwertuje DTO na Spec, jeśli istnieje
    public static GpuSpec? GetSpecs(string deviceId)
    {
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var dto))
        {
            return dto.ToGpuSpec();
        }
        return null;
    }

    public static string LookupDeviceName(string deviceId)
    {
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var dto))
        {
            return dto.Name;
        }
        return "Unknown AMD GPU";
    }

    public static string LookupVendorName(string vendorId)
    {
        if (DatabaseManager.Database.Vendors.TryGetValue(vendorId, out var name))
        {
            return name;
        }
        return $"Unknown ({vendorId})";
    }
}