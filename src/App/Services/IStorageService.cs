namespace Canopus.App.Services;

/// <summary>
/// A disk/partition's usage at a given instant.
/// </summary>
public record DriveSnapshot(string Name, double UsedGigabytes, double TotalGigabytes);

public interface IStorageService
{
    /// <summary>
    /// Lists the ready fixed disks/partitions, with their current usage.
    /// </summary>
    IReadOnlyList<DriveSnapshot> GetSnapshot();
}
