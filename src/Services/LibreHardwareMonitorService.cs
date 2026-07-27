using LibreHardwareMonitor.Hardware;

namespace PCOptimizer.App.Services;

/// <summary>
/// Implémentation basée sur LibreHardwareMonitorLib.
/// Reprend la logique validée par le spike technique (voir docs/spike-notes.md),
/// étendue pour couvrir GPU et ventilateurs en plus du CPU.
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
            IsMotherboardEnabled = true // nécessaire pour la plupart des capteurs de ventilateurs
        };
        _computer.Open();
    }

    public HardwareSnapshot GetSnapshot()
    {
        double? cpuTemp = null, gpuTemp = null, cpuLoad = null, gpuLoad = null, fanRpm = null;

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

        return new HardwareSnapshot(cpuTemp, gpuTemp, cpuLoad, gpuLoad, fanRpm);
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
