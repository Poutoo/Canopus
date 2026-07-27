namespace Canopus.App.Services;

/// <summary>
/// Utilisation d'un disque/partition à un instant donné.
/// </summary>
public record DriveSnapshot(string Name, double UsedGigabytes, double TotalGigabytes);

public interface IStorageService
{
    /// <summary>
    /// Liste les disques/partitions fixes prêts, avec leur occupation actuelle.
    /// </summary>
    IReadOnlyList<DriveSnapshot> GetSnapshot();
}
