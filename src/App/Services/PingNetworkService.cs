using System.Net.NetworkInformation;

namespace Canopus.App.Services;

/// <summary>
/// Measures latency by pinging a fixed public host and derives jitter
/// (mean of absolute deltas between successive pings) over a sliding window.
/// </summary>
public sealed class PingNetworkService : INetworkService, IDisposable
{
    private const string TargetHost = "1.1.1.1";
    private const int TimeoutMs = 1000;
    private const int HistorySize = 5;

    private readonly Ping _ping = new();
    private readonly Queue<double> _recentLatenciesMs = new();

    public async Task<NetworkSnapshot> GetSnapshotAsync()
    {
        double? latency = null;

        try
        {
            PingReply reply = await _ping.SendPingAsync(TargetHost, TimeoutMs);
            if (reply.Status == IPStatus.Success)
                latency = reply.RoundtripTime;
        }
        catch (PingException)
        {
            // Offline, DNS/host unreachable, etc.: no measurement for this tick.
        }

        if (latency is null)
            return new NetworkSnapshot(null, null);

        _recentLatenciesMs.Enqueue(latency.Value);
        while (_recentLatenciesMs.Count > HistorySize)
            _recentLatenciesMs.Dequeue();

        double? jitter = null;
        if (_recentLatenciesMs.Count >= 2)
        {
            double[] samples = [.. _recentLatenciesMs];
            double sumOfDeltas = 0;
            for (int i = 1; i < samples.Length; i++)
                sumOfDeltas += Math.Abs(samples[i] - samples[i - 1]);
            jitter = sumOfDeltas / (samples.Length - 1);
        }

        return new NetworkSnapshot(latency, jitter);
    }

    public void Dispose() => _ping.Dispose();
}
