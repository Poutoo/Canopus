namespace Canopus.App.Services;

/// <summary>
/// Implémentation basée sur <see cref="DriveInfo"/> — pas besoin de
/// LibreHardwareMonitor pour de la simple occupation disque.
/// </summary>
public sealed class DriveInfoStorageService : IStorageService
{
    public IReadOnlyList<DriveSnapshot> GetSnapshot()
    {
        var result = new List<DriveSnapshot>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                continue;

            const double bytesPerGigabyte = 1024d * 1024d * 1024d;
            double totalGb = drive.TotalSize / bytesPerGigabyte;
            double freeGb = drive.TotalFreeSpace / bytesPerGigabyte;

            result.Add(new DriveSnapshot(drive.Name.TrimEnd('\\'), totalGb - freeGb, totalGb));
        }

        return result;
    }
}
