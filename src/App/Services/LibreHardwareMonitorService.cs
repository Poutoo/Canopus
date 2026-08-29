using LibreHardwareMonitor.Hardware;

namespace Canopus.App.Services;

/// <summary>
/// LibreHardwareMonitorLib-based implementation.
/// Reuses the logic validated by the technical spike (see docs/spike-notes.md),
/// extended to cover GPU, fans, frequencies and RAM.
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
            IsMotherboardEnabled = true // required for most fan sensors
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
                        // "Bus Speed" is also a Clock sensor but does not reflect the
                        // core frequency (~100 MHz): exclude it explicitly.
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
                    // "Total Memory" = physical RAM. There is also a "Virtual Memory"
                    // hardware (page file) with the same sensor names ("Memory",
                    // "Memory Used", "Memory Available"): ignore it here.
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
                    // Fan sensors are often exposed through the motherboard's
                    // sub-hardware (SuperIO) rather than directly here.
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
