namespace GPU_T.Models;

public record GpuSensorData
{
    // Zegary
public double GpuClock { get; init; }      // MHz
    public double MemoryClock { get; init; }   // MHz
    
    public double GpuTemp { get; init; }       // °C (Edge)
    public double GpuHotSpot { get; init; }    // °C (Junction)
    public double MemoryTemp { get; init; }    // °C
    
    public int FanRpm { get; init; }           // RPM
    public int FanPercent { get; init; }       // % (wyliczymy z pwm1 / pwm1_max)
    
    public double BoardPower { get; init; }    // Watts
    public int GpuLoad { get; init; }          // %
    public double MemoryUsed { get; init; }    // MB
    
    public double GpuVoltage { get; init; }    // V (Nowość!)

    public int MemControllerLoad { get; init; } // %
    public double MemoryUsedDynamic { get; init; } // MB (GTT)
    
    public double CpuTemperature { get; init; } // °C
    public double SystemRamUsed { get; init; }  // GB lub MB
}