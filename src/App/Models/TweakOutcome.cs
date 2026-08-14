namespace Canopus.App.Models;

public record TweakOutcome(string TweakName, bool Succeeded, string? FailureReason = null);
