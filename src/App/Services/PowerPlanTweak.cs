using System.Runtime.InteropServices;
using Canopus.App.Models;

namespace Canopus.App.Services;

// The active scheme is captured at session start and restored as-is on revert.
// Read side mirrors WindowsAuditService's mechanism (PowerGetActiveScheme +
// LocalFree); PowerSetActiveScheme is added here for the write side.
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
        Guid active = GetActiveScheme() ?? throw new InvalidOperationException("Impossible de lire le plan d'alimentation actif.");
        return Task.FromResult(new TweakSnapshot(Name, new Dictionary<string, object>
        {
            [ActiveSchemeKey] = active.ToString()
        }));
    }

    public Task ApplyAsync()
    {
        Guid ultimate = UltimatePerformanceScheme;
        if (PowerSetActiveScheme(IntPtr.Zero, ref ultimate) == 0)
            return Task.CompletedTask;

        Guid highPerformance = HighPerformanceScheme;
        if (PowerSetActiveScheme(IntPtr.Zero, ref highPerformance) != 0)
            throw new InvalidOperationException("Impossible d'activer un plan d'alimentation performant.");

        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync()
    {
        Guid? active = GetActiveScheme();
        return Task.FromResult(active == HighPerformanceScheme || active == UltimatePerformanceScheme);
    }

    public Task RevertAsync(TweakSnapshot snapshot)
    {
        Guid original = Guid.Parse(snapshot.GetValue<string>(ActiveSchemeKey));
        if (PowerSetActiveScheme(IntPtr.Zero, ref original) != 0)
            throw new InvalidOperationException("Impossible de restaurer le plan d'alimentation d'origine.");

        return Task.CompletedTask;
    }

    private static Guid? GetActiveScheme()
    {
        IntPtr schemeGuidPtr = IntPtr.Zero;
        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out schemeGuidPtr) != 0 || schemeGuidPtr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStructure<Guid>(schemeGuidPtr);
        }
        finally
        {
            if (schemeGuidPtr != IntPtr.Zero)
                LocalFree(schemeGuidPtr);
        }
    }

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
