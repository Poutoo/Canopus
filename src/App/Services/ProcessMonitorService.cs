using System.Diagnostics;

namespace Canopus.App.Services;

/// <summary>
/// Computes per-process %CPU from the delta of <see cref="Process.TotalProcessorTime"/>
/// between two successive calls (Windows exposes no instantaneous per-process %CPU).
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
                // The process can exit between enumeration and reading its
                // properties, or be inaccessible without sufficient rights.
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
