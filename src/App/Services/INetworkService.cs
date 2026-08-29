namespace Canopus.App.Services;

/// <summary>
/// Network measurement at a given instant.
/// Null if the last network request failed (offline, host unreachable, etc.).
/// </summary>
public record NetworkSnapshot(double? LatencyMs, double? JitterMs);

public interface INetworkService
{
    /// <summary>
    /// Performs a latency measurement (ping) and computes jitter against the
    /// previous measurements.
    /// </summary>
    Task<NetworkSnapshot> GetSnapshotAsync();
}
