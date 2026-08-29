using Velopack;
using Velopack.Sources;
using Canopus.App.Localization;

namespace Canopus.App.Services;

/// <summary>
/// Velopack-based implementation, with GitHub Releases as the update feed.
/// Keeps the last detected update in memory so it can be applied on an
/// explicit user request (see <see cref="DownloadAndApplyUpdateAsync"/>).
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private const string RepoUrl = "https://github.com/Poutoo/Canopus";

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateService()
    {
        _updateManager = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        // IsInstalled is false in dev (dotnet run / F5): there is then no
        // Velopack install to check an update against.
        if (!_updateManager.IsInstalled)
            return new UpdateCheckResult(false, null);

        try
        {
            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
        }
        catch
        {
            // Network check against GitHub: a failure (offline, rate limit, etc.)
            // must not stop the user from continuing to use the app.
            _pendingUpdate = null;
        }

        return new UpdateCheckResult(
            _pendingUpdate is not null,
            _pendingUpdate?.TargetFullRelease.Version.ToString());
    }

    public async Task DownloadAndApplyUpdateAsync()
    {
        if (_pendingUpdate is null)
            return;

        await _updateManager.DownloadUpdatesAsync(_pendingUpdate);
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    public string GetCurrentVersionText() =>
        _updateManager.IsInstalled
            ? _updateManager.CurrentVersion?.ToString() ?? Strings.Get("Update.CurrentVersion.Unknown")
            : Strings.Get("Update.CurrentVersion.NotInstalled");
}
