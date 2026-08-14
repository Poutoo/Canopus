namespace Canopus.App.Services;

/// <summary>
/// Mesure réseau à un instant donné.
/// Nulle si la dernière requête réseau a échoué (hors ligne, hôte injoignable, etc.).
/// </summary>
public record NetworkSnapshot(double? LatencyMs, double? JitterMs);

public interface INetworkService
{
    /// <summary>
    /// Effectue une mesure de latence (ping) et calcule la gigue par rapport
    /// aux mesures précédentes.
    /// </summary>
    Task<NetworkSnapshot> GetSnapshotAsync();
}
