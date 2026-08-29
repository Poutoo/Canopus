using Velopack;
using Velopack.Sources;

namespace Canopus.App.Services;

/// <summary>
/// Implémentation basée sur Velopack, avec GitHub Releases comme flux de mise à jour.
/// Garde en mémoire la dernière mise à jour détectée pour pouvoir l'appliquer
/// sur demande explicite de l'utilisateur (voir <see cref="DownloadAndApplyUpdateAsync"/>).
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
        // IsInstalled est faux en dev (dotnet run / F5) : il n'y a alors pas
        // d'installation Velopack contre laquelle vérifier une mise à jour.
        if (!_updateManager.IsInstalled)
            return new UpdateCheckResult(false, null);

        try
        {
            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
        }
        catch
        {
            // Vérification réseau vers GitHub : un échec (hors ligne, rate limit, etc.)
            // ne doit pas empêcher l'utilisateur de continuer à utiliser l'app.
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
            ? _updateManager.CurrentVersion?.ToString() ?? "version inconnue"
            : "développement (non installé)";
}
