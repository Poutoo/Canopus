using LibreHardwareMonitor.Hardware;

namespace Canopus.App.Services;

/// <summary>
/// Implémentation basée sur LibreHardwareMonitorLib.
/// Reprend la logique validée par le spike technique (voir docs/spike-notes.md),
/// étendue pour couvrir GPU, ventilateurs, fréquences et RAM.
/// </summary>
public sealed class LibreHardwareMonitorService : IHardwareMonitorService, IDisposable
{
    private readonly Computer _computer;

    public LibreHardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true // nécessaire pour la plupart des capteurs de ventilateurs
        };
        _computer.Open();
    }

    public HardwareSnapshot GetSnapshot()
    {
        double? cpuTemp = null, gpuTemp = null, cpuLoad = null, gpuLoad = null, fanRpm = null;
        double? cpuFreq = null, gpuFreq = null;
        double? memPercent = null, memUsedGb = null, memAvailableGb = null;

        foreach (IHardware hardware in _computer.Hardware)
        {
            hardware.Update();

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            cpuTemp ??= sensor.Value;
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            cpuLoad ??= sensor.Value;
                        // "Bus Speed" est aussi un capteur Clock mais ne reflète pas la
                        // fréquence du cœur (~100 MHz) : à exclure explicitement.
                        if (sensor.SensorType == SensorType.Clock && sensor.Value.HasValue
                            && !sensor.Name.Contains("Bus Speed", StringComparison.OrdinalIgnoreCase))
                            cpuFreq ??= sensor.Value;
                    }
                    break;

                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            gpuTemp ??= sensor.Value;
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            gpuLoad ??= sensor.Value;
                        if (sensor.SensorType == SensorType.Clock && sensor.Value.HasValue)
                            gpuFreq ??= sensor.Value;
                    }
                    break;

                case HardwareType.Memory:
                    // "Total Memory" = RAM physique. Il existe aussi un hardware
                    // "Virtual Memory" (fichier d'échange) avec les mêmes noms de
                    // capteurs ("Memory", "Memory Used", "Memory Available") : à ignorer ici.
                    if (hardware.Name != "Total Memory")
                        break;

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            memPercent ??= sensor.Value;
                        if (sensor.SensorType == SensorType.Data && sensor.Value.HasValue)
                        {
                            if (sensor.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                                memUsedGb ??= sensor.Value;
                            else if (sensor.Name.Contains("Available", StringComparison.OrdinalIgnoreCase))
                                memAvailableGb ??= sensor.Value;
                        }
                    }
                    break;

                case HardwareType.Motherboard:
                    // Les capteurs de ventilateurs sont souvent exposés via les sous-matériels
                    // (SuperIO) de la carte mère plutôt que directement ici.
                    foreach (IHardware sub in hardware.SubHardware)
                    {
                        sub.Update();
                        foreach (ISensor sensor in sub.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                                fanRpm ??= sensor.Value;
                        }
                    }
                    break;
            }
        }

        return new HardwareSnapshot(
            cpuTemp, gpuTemp, cpuLoad, gpuLoad, fanRpm,
            cpuFreq, gpuFreq,
            memPercent, memUsedGb, memAvailableGb);
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
