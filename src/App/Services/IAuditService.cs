using Canopus.App.Models;

namespace Canopus.App.Services;

public interface IAuditService
{
    /// <summary>
    /// Re-runs detection for every covered check. Each check is isolated: a failing
    /// one yields an explicit item rather than a misleading default verdict.
    /// </summary>
    Task<IReadOnlyList<AuditItem>> RunAuditAsync();

    /// <summary>
    /// Returns the last known result, running an audit only if none has run yet.
    /// For consumers that just need a summary (the dashboard counter) without
    /// forcing another WMI sweep.
    /// </summary>
    Task<IReadOnlyList<AuditItem>> GetOrRunAuditAsync();
}
