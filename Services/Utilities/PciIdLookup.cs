using System.Linq;
using GPU_T.Models;

namespace GPU_T.Services;

/// <summary>
/// Provides lookup and matching utilities for PCI device and vendor IDs against the GPU database.
/// </summary>
public static class PciIdLookup
{
    /// <summary>
    /// Retrieves GPU specification for the given device and revision ID, with fallback logic for variant matching.
    /// </summary>
    /// <param name="deviceId">The PCI device ID.</param>
    /// <param name="revisionId">The revision ID to match.</param>
    /// <returns>A <see cref="GpuSpec"/> instance if found; otherwise, null.</returns>
    public static GpuSpec? GetSpecs(string deviceId, string revisionId)
    {
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var variantsList))
        {
            if (variantsList == null || variantsList.Count == 0) return null;

            var exactMatch = variantsList.FirstOrDefault(v => v.Revisions != null && v.Revisions.Contains(revisionId));

            if (exactMatch != null)
            {
                return exactMatch.ToGpuSpec(isExactMatch: true);
            }

            var baseVariant = variantsList[0];

            // Fallback: present a combined name of all known variants to indicate a best-effort match.
            var combinedName = string.Join(" / ", variantsList.Select(v => v.Name).Distinct());

            return baseVariant.ToGpuSpec(isExactMatch: false, overrideName: combinedName);
        }

        return null;
    }

    /// <summary>
    /// Retrieves the representative device name for the given PCI device ID.
    /// </summary>
    /// <param name="deviceId">The PCI device ID.</param>
    /// <returns>The device name string, or "Unknown AMD GPU" if not found.</returns>
    public static string LookupDeviceName(string deviceId)
    {
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var list) && list.Count > 0)
        {
            // Returns the first variant name as a representative label.
            return list[0].Name;
        }
        return "Unknown AMD GPU";
    }

    /// <summary>
    /// Retrieves the vendor name for the given vendor ID.
    /// </summary>
    /// <param name="vendorId">The vendor ID string.</param>
    /// <returns>The vendor name, or "Unknown (vendorId)" if not found.</returns>
    public static string LookupVendorName(string vendorId)
    {
        if (DatabaseManager.Database.Vendors.TryGetValue(vendorId, out var name))
        {
            return name;
        }
        return $"Unknown ({vendorId})";
    }
}