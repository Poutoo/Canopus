using System.Diagnostics;

namespace Canopus.App.Services;

/// <summary>
/// Calcule le %CPU par process à partir de l'écart de <see cref="Process.TotalProcessorTime"/>
/// entre deux appels successifs (Windows n'expose pas de %CPU instantané par process).
/// </summary>
public sealed class ProcessMonitorService : IProcessMonitorService
{
    private Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp)> _previousSamples = new();

    public IReadOnlyList<ProcessSnapshot> GetTopProcesses(int count = 3)
    {
        DateTime now = DateTime.UtcNow;
        int processorCount = Environment.ProcessorCount;
        var currentSamples = new Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp)>();
        var results = new List<ProcessSnapshot>();

        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                TimeSpan cpuTime = process.TotalProcessorTime;
                currentSamples[process.Id] = (cpuTime, now);

                if (_previousSamples.TryGetValue(process.Id, out var previous))
                {
                    double elapsedWallMs = (now - previous.Timestamp).TotalMilliseconds;
                    double elapsedCpuMs = (cpuTime - previous.CpuTime).TotalMilliseconds;

                    if (elapsedWallMs > 0)
                    {
                        double cpuPercent = elapsedCpuMs / elapsedWallMs / processorCount * 100.0;
                        double memoryMb = process.WorkingSet64 / 1024d / 1024d;
                        results.Add(new ProcessSnapshot(process.ProcessName, cpuPercent, memoryMb));
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Le process peut se terminer entre l'énumération et la lecture de
                // ses propriétés, ou être inaccessible sans droits suffisants.
            }
            finally
            {
                process.Dispose();
            }
        }

        _previousSamples = currentSamples;

        return results.OrderByDescending(p => p.CpuPercent).Take(count).ToList();
    }
}
