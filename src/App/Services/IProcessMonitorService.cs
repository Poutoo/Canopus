namespace Canopus.App.Services;

/// <summary>
/// Consommation d'un process à un instant donné.
/// </summary>
public record ProcessSnapshot(string Name, double CpuPercent, double MemoryMegabytes);

public interface IProcessMonitorService
{
    /// <summary>
    /// Retourne les processus les plus consommateurs de CPU depuis le dernier appel.
    /// Le premier appel retourne une liste vide : le calcul du %CPU nécessite un
    /// écart entre deux mesures (pas de valeur instantanée fournie par Windows).
    /// </summary>
    IReadOnlyList<ProcessSnapshot> GetTopProcesses(int count = 3);
}
