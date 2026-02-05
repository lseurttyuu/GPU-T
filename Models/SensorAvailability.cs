namespace GPU_T.Models;

public class SensorAvailability
{
    public bool HasHotSpot { get; set; }
    public bool HasMemTemp { get; set; }
    public bool HasFan { get; set; }
    public bool HasGpuLoad { get; set; }
    public bool HasMemControllerLoad { get; set; }
    public bool HasPower { get; set; }
    public bool HasVoltage { get; set; }
    public bool HasMemUsed { get; set; } 
}