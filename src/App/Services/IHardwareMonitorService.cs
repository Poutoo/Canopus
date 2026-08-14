namespace Canopus.App.Services;

/// <summary>
/// Instantané des métriques matérielles à un instant donné.
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
    /// Lit l'état actuel des capteurs matériels disponibles.
    /// Nécessite des droits administrateur (voir app.manifest).
    /// </summary>
    HardwareSnapshot GetSnapshot();
}
