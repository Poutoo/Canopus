namespace Canopus.App.Services;

/// <summary>
/// Snapshot of hardware metrics at a given instant.
/// </summary>
public record HardwareSnapshot(
    double? CpuTemperatureCelsius,
    double? GpuTemperatureCelsius,
    double? CpuLoadPercent,
    double? GpuLoadPercent,
    double? FanSpeedRpm,
    double? CpuFrequencyMhz,
    double? GpuFrequencyMhz,
    double? MemoryUsedPercent,
    double? MemoryUsedGigabytes,
    double? MemoryAvailableGigabytes
);

public interface IHardwareMonitorService
{
    /// <summary>
    /// Reads the current state of the available hardware sensors.
    /// Requires administrator rights (see app.manifest).
    /// </summary>
    HardwareSnapshot GetSnapshot();
}
