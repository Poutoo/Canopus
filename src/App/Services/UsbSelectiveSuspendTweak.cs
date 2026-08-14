using System.Runtime.InteropServices;
using Canopus.App.Models;

namespace Canopus.App.Services;

// Subgroup/setting GUIDs confirmed via "powercfg /q" on the test machine
// (standard Windows identifiers, not machine-specific):
//   Subgroup "USB settings"                2a737441-1930-4402-8d77-b2bebba308a3
//   Setting "USB selective suspend setting" 48e6b7a6-50f5-4782-a5d4-53bb8f07e226
//   0 = Disabled, 1 = Enabled
//
// Written via PowerWriteACValueIndex (powrprof.dll) rather than shelling out to
// powercfg. Undocumented quirk verified on a real machine: the write only takes
// effect (visible via PowerReadACValueIndex and "powercfg /q") for the scheme
// Windows is currently enforcing after a PowerSetActiveScheme call on that same
// scheme -- see WriteAcValue for why that reapply must be conditional.
//
// The scheme GUID is resolved and pinned here during CaptureAsync (not re-read
// as "current scheme" on every call) because GameSessionService captures every
// tweak before applying any of them -- PowerPlanTweak can change the active
// scheme after this capture, so targeting "the active scheme" at apply/verify
// time would hit the wrong one.
//
// Known v1 scope limit: because of that same capture-before-any-apply ordering,
// this tweak has no way to know in advance which scheme PowerPlanTweak will
// land on, so it always targets the scheme that was active *before* the session
// started. If PowerPlanTweak successfully switches to a different scheme, USB
// selective suspend is only guaranteed disabled on the original scheme, not on
// the one actually active during the session -- surfaced to the user via
// GameSessionViewModel's tweak note rather than silently overclaiming.
public sealed class UsbSelectiveSuspendTweak : IReversibleTweak
{
    private static readonly Guid SubgroupGuid = new("2a737441-1930-4402-8d77-b2bebba308a3");
    private static readonly Guid SettingGuid = new("48e6b7a6-50f5-4782-a5d4-53bb8f07e226");

    private const string SchemeGuidKey = "SchemeGuid";
    private const string OriginalValueKey = "OriginalAcValue";

    private Guid? _schemeGuid;

    public string Name => "Suspension sélective USB";

    public Task<TweakSnapshot> CaptureAsync()
    {
        Guid scheme = GetActiveScheme() ?? throw new InvalidOperationException("Impossible de lire le plan d'alimentation actif.");
        _schemeGuid = scheme;

        uint originalValue = ReadAcValue(scheme);

        return Task.FromResult(new TweakSnapshot(Name, new Dictionary<string, object>
        {
            [SchemeGuidKey] = scheme.ToString(),
            [OriginalValueKey] = (int)originalValue
        }));
    }

    public Task ApplyAsync()
    {
        Guid scheme = _schemeGuid ?? throw new InvalidOperationException("CaptureAsync doit être appelé avant ApplyAsync.");
        WriteAcValue(scheme, disabled: true);
        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync()
    {
        if (_schemeGuid is not Guid scheme)
            return Task.FromResult(false);

        return Task.FromResult(ReadAcValue(scheme) == 0);
    }

    public Task RevertAsync(TweakSnapshot snapshot)
    {
        Guid scheme = Guid.Parse(snapshot.GetValue<string>(SchemeGuidKey));
        int originalValue = snapshot.GetValue<int>(OriginalValueKey);
        WriteAcValue(scheme, disabled: originalValue == 0);
        return Task.CompletedTask;
    }

    private static void WriteAcValue(Guid scheme, bool disabled)
    {
        Guid subgroup = SubgroupGuid;
        Guid setting = SettingGuid;
        if (PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, disabled ? 0u : 1u) != 0)
            throw new InvalidOperationException("Écriture du réglage USB impossible.");

        // Must be conditional (see the reapply quirk noted on PowerWriteACValueIndex
        // above): reapplying unconditionally here previously clobbered PowerPlanTweak's
        // scheme switch, since this tweak applies right after it using the scheme
        // captured *before* the power plan changed. Only reapply when `scheme` is the
        // one actually active, otherwise this would incorrectly switch the system to it.
        Guid current = GetActiveScheme() ?? Guid.Empty;
        if (current == scheme && PowerSetActiveScheme(IntPtr.Zero, ref scheme) != 0)
            throw new InvalidOperationException("Impossible de réappliquer le plan d'alimentation après l'écriture du réglage USB.");
    }

    private static uint ReadAcValue(Guid scheme)
    {
        Guid subgroup = SubgroupGuid;
        Guid setting = SettingGuid;
        if (PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, out uint value) != 0)
            throw new InvalidOperationException("Lecture du réglage USB impossible.");

        return value;
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

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint acValueIndex);

    // Undocumented Win32 quirk, confirmed by live testing (2026-08-15): a write here
    // only takes effect -- readable back via PowerReadACValueIndex, visible in
    // "powercfg /q" -- once PowerSetActiveScheme is reapplied on that same scheme
    // afterward. The value otherwise sits in the registry unused. Applies to
    // PowerWriteDCValueIndex too, and to any future tweak in this codebase that
    // writes a power sub-setting, not just this one -- see WriteAcValue below for
    // why that reapply must be conditional on the target actually being active.
    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, uint acValueIndex);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
