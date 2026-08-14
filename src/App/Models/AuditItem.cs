namespace Canopus.App.Models;

/// <summary>
/// <see cref="Info"/> est un palier neutre (gris, sans couleur sémantique)
/// pour les informations factuelles qui ne portent pas de jugement.
/// </summary>
public enum AuditStatus { Confirmed, Warning, Problem, Info }

/// <param name="DetailNote">Limite de détection à afficher honnêtement à l'utilisateur.</param>
public record AuditItem(
    string Title,
    AuditStatus Status,
    string StatusLabel,
    string Description,
    string? DetailNote = null
);
