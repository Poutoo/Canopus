namespace Canopus.App.Services;

/// <summary>
/// A process's resource usage at a given instant.
/// </summary>
public record ProcessSnapshot(string Name, double CpuPercent, double MemoryMegabytes);

public interface IProcessMonitorService
{
    /// <summary>
    /// Returns the most CPU-hungry processes since the last call.
    /// The first call returns an empty list: computing %CPU needs a delta between
    /// two measurements (Windows provides no instantaneous value).
    /// </summary>
    IReadOnlyList<ProcessSnapshot> GetTopProcesses(int count = 3);
}
