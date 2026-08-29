namespace Canopus.App.Models;

/// <summary>
/// <see cref="Info"/> is a neutral tier (grey, no semantic color) for factual
/// information that carries no judgment.
/// </summary>
public enum AuditStatus { Confirmed, Warning, Problem, Info }

/// <param name="DetailNote">Detection limit to surface honestly to the user.</param>
public record AuditItem(
    string Title,
    AuditStatus Status,
    string StatusLabel,
    string Description,
    string? DetailNote = null
);
