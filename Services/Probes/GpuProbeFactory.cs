using System.Collections.Generic;
using System.IO;
using GPU_T.Services.Probes.LinuxAmd;

namespace GPU_T.Services;

public static class GpuProbeFactory
{
    // Scenariusz 1: Pobieranie listy dostępnych kart
    // Fabryka pyta wszystkich znanych providerów i łączy wyniki
    public static List<string> GetAvailableCards()
    {
        var allCards = new List<string>();

        // 1. AMD
        allCards.AddRange(LinuxAmdGpuProbe.GetAvailableCards());

        // 2. W przyszłości: NVIDIA
        // allCards.AddRange(LinuxNvidiaGpuProbe.GetAvailableCards());

        // 3. W przyszłości: INTEL
        // allCards.AddRange(LinuxIntelGpuProbe.GetAvailableCards());

        // Sortowanie, żeby card0 było przed card1
        allCards.Sort();
        
        return allCards;
    }

    // Scenariusz 2 i 3: Tworzenie instancji (z opcjonalnym memoryType)
    public static IGpuProbe Create(string gpuId, string memoryType = "")
    {
        // Tutaj następuje detekcja producenta na podstawie Vendor ID
        // (Logika przygotowana pod Krok 3)
        string vendorId = GetVendorId(gpuId);

        /*
        if (vendorId == "0x10DE") // NVIDIA
        {
             return new LinuxNvidiaGpuProbe(gpuId); 
        }
        else if (vendorId == "0x8086") // INTEL
        {
             return new LinuxIntelGpuProbe(gpuId);
        }
        */

        // Domyślnie (i dla 0x1002) zwracamy AMD
        // Przekazujemy memoryType, jeśli został podany
        return new LinuxAmdGpuProbe(gpuId, memoryType);
    }

    // Helper do odczytu Vendor ID z systemu plików
    private static string GetVendorId(string gpuId)
    {
        try
        {
            string path = $"/sys/class/drm/{gpuId}/device/vendor";
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim().ToUpper(); // np. "0X1002" -> "0X1002"
            }
        }
        catch { }
        return "";
    }
}