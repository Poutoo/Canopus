using Canopus.App.Models;

namespace Canopus.App.Services;

public interface IAuditService
{
    /// <summary>
    /// Exécute la détection de tous les leviers couverts. Chaque levier est isolé :
    /// l'échec de l'un produit un item explicite plutôt qu'un verdict par défaut.
    /// </summary>
    Task<IReadOnlyList<AuditItem>> RunAuditAsync();
}
