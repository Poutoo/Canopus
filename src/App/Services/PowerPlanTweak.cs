using Canopus.App.Models;

namespace Canopus.App.Services;

// The active scheme is captured at session start and restored as-is on revert.
// P/Invoke marshaling lives in PowerSchemeInterop, shared with UsbSelectiveSuspendTweak.
public sealed class PowerPlanTweak : IReversibleTweak
{
    private static readonly Guid HighPerformanceScheme = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

    // Ultimate Performance only shows up in "powercfg /list" once the scheme has
    // been duplicated on the machine (absent by default, confirmed on the test
    // machine). So we try activating it directly and fall back to High
    // Performance if PowerSetActiveScheme fails for lack of a matching scheme.
    private static readonly Guid UltimatePerformanceScheme = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    private const string ActiveSchemeKey = "ActiveSchemeGuid";

    public string Name => "Plan d'alimentation";

    public Task<TweakSnapshot> CaptureAsync()
    {
        Guid active = PowerSchemeInterop.GetActiveScheme() ?? throw new InvalidOperationException("Impossible de lire le plan d'alimentation actif.");
        return Task.FromResult(new TweakSnapshot(Name, new Dictionary<string, object>
        {
            [ActiveSchemeKey] = active.ToString()
        }));
    }

    public Task ApplyAsync()
    {
        if (PowerSchemeInterop.SetActiveScheme(UltimatePerformanceScheme))
            return Task.CompletedTask;

        if (!PowerSchemeInterop.SetActiveScheme(HighPerformanceScheme))
            throw new InvalidOperationException("Impossible d'activer un plan d'alimentation performant.");

        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync()
    {
        Guid? active = PowerSchemeInterop.GetActiveScheme();
        return Task.FromResult(active == HighPerformanceScheme || active == UltimatePerformanceScheme);
    }

    public Task RevertAsync(TweakSnapshot snapshot)
    {
        Guid original = Guid.Parse(snapshot.GetValue<string>(ActiveSchemeKey));
        if (!PowerSchemeInterop.SetActiveScheme(original))
            throw new InvalidOperationException("Impossible de restaurer le plan d'alimentation d'origine.");

        return Task.CompletedTask;
    }
}
