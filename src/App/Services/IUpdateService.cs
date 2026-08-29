namespace Canopus.App.Services;

/// <summary>
/// Result of an update check.
/// </summary>
public record UpdateCheckResult(bool IsUpdateAvailable, string? AvailableVersion);

public interface IUpdateService
{
    /// <summary>
    /// Checks whether a newer version is available on the update feed.
    /// Never applies the update automatically.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdateAsync();

    /// <summary>
    /// Downloads then applies the update detected by the last
    /// <see cref="CheckForUpdateAsync"/>, and restarts the application.
    /// Does nothing if no update was detected beforehand.
    /// </summary>
    Task DownloadAndApplyUpdateAsync();

    /// <summary>
    /// Currently installed version, as packaged by Velopack (<c>vpk pack --packVersion</c>).
    /// Distinct from the .NET assembly version, which is not set in the csproj.
    /// </summary>
    string GetCurrentVersionText();
}
