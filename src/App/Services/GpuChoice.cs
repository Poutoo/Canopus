namespace Canopus.App.Services;

/// <summary>
/// A laptop or a Ryzen with an integrated GPU exposes two adapters. The dedicated one is
/// the one with the most dedicated memory; an adapter whose memory is unknown loses to any
/// that reports it, and the first one wins a tie.
/// </summary>
public static class GpuChoice
{
    public static int PickDedicated(IReadOnlyList<double?> dedicatedMemoryMegabytes)
    {
        int best = 0;
        for (int i = 1; i < dedicatedMemoryMegabytes.Count; i++)
        {
            if (dedicatedMemoryMegabytes[i] > (dedicatedMemoryMegabytes[best] ?? double.MinValue))
                best = i;
        }
        return best;
    }
}
