namespace Canopus.App.Services;

/// <summary>
/// Résultat d'une vérification de mise à jour.
/// </summary>
public record UpdateCheckResult(bool IsUpdateAvailable, string? AvailableVersion);

public interface IUpdateService
{
    /// <summary>
    /// Vérifie si une nouvelle version est disponible sur le flux de mise à jour.
    /// N'applique jamais la mise à jour automatiquement.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdateAsync();

    /// <summary>
    /// Télécharge puis applique la mise à jour détectée par le dernier
    /// <see cref="CheckForUpdateAsync"/>, et redémarre l'application.
    /// Ne fait rien si aucune mise à jour n'a été détectée au préalable.
    /// </summary>
    Task DownloadAndApplyUpdateAsync();

    /// <summary>
    /// Version actuellement installée, telle que packagée par Velopack (<c>vpk pack --packVersion</c>).
    /// Distincte de la version d'assembly .NET, qui n'est pas renseignée dans le csproj.
    /// </summary>
    string GetCurrentVersionText();
}
