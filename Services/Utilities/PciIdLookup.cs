using System.Linq;
using GPU_T.Models;

namespace GPU_T.Services;

public static class PciIdLookup
{
    // ZMIANA: Metoda przyjmuje teraz revisionId
    public static GpuSpec? GetSpecs(string deviceId, string revisionId)
    {
        // Sprawdzamy, czy mamy wpisy dla tego DeviceID
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var variantsList))
        {
            if (variantsList == null || variantsList.Count == 0) return null;

            // 1. Próba znalezienia dokładnego dopasowania po rewizji
            // Szukamy wariantu, którego lista 'Revisions' zawiera nasz 'revisionId'
            var exactMatch = variantsList.FirstOrDefault(v => v.Revisions != null && v.Revisions.Contains(revisionId));

            if (exactMatch != null)
            {
                // MAMY TO! Zwracamy dokładne dane
                return exactMatch.ToGpuSpec(isExactMatch: true);
            }

            // 2. FALLBACK: Nie znaleziono rewizji.
            // Bierzemy dane techniczne z pierwszego elementu (zakładamy, że architektura jest ta sama)
            var baseVariant = variantsList[0];

            // Łączymy nazwy wszystkich wariantów: "RX 7900 XTX / RX 7900 GRE"
            // Używamy Distinct(), żeby nie powielać nazw, jeśli występują wielokrotnie
            var combinedName = string.Join(" / ", variantsList.Select(v => v.Name).Distinct());

            // Zwracamy obiekt z flagą IsExactMatch = false
            return baseVariant.ToGpuSpec(isExactMatch: false, overrideName: combinedName);
        }

        return null;
    }

    // Ta metoda służy tylko do prostego wyciągnięcia nazwy (np. do listy wyboru),
    // tutaj możemy zwrócić pierwszą nazwę lub ogólną, jeśli nie znamy rewizji.
    public static string LookupDeviceName(string deviceId)
    {
        if (DatabaseManager.Database.Gpus.TryGetValue(deviceId, out var list) && list.Count > 0)
        {
            return list[0].Name; // Zwracamy pierwszą nazwę jako reprezentanta
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