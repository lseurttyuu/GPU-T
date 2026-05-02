namespace GPU_T.Models;

public record GpuSensorData
{
    public double GpuClock { get; init; }      // MHz
    public double MemoryClock { get; init; }   // MHz
    
    public double GpuTemp { get; init; }       // °C (Edge)
    public double GpuHotSpot { get; init; }    // °C (Junction)
    public double MemoryTemp { get; init; }    // °C
    
    public int FanRpm { get; init; }           // RPM
    public int FanPercent { get; init; }       // % (pwm1 / pwm1_max)
    
    public double BoardPower { get; init; }    // Watts
    public int GpuLoad { get; init; }          // %
    public int EncoderLoad { get; set; }
    public int DecoderLoad { get; set; }
    public double MemoryUsed { get; init; }    // MB
    public string PerfCapReason { get; set; } = "None";
    public double GpuVoltage { get; init; }    // V

    public int MemControllerLoad { get; init; } // %
    public double PcieTx { get; set; }
    public double PcieRx { get; set; }
    public double MemoryUsedDynamic { get; init; } // MB (GTT)
    
    // Overclocking offsets read from NVAPI sidecar - they allow to show proper clocks at GPU-T runtime
    // plus they can be used to recalculate pixel fillrate and bandwidth values on the fly (main tab)
    public int CoreOcOffset { get; set; }
    public int MemOcOffset { get; set; }

    //Allows to update the PCIe link status dynamically
    public string BusInterface { get; set; } = "N/A";
    
    //standard system readings
    public double CpuTemperature { get; init; } // °C
    public double SystemRamUsed { get; init; }  // GB or MB
}