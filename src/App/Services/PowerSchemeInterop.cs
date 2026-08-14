using System.Runtime.InteropServices;

namespace Canopus.App.Services;

// Shared by every tweak that reads/writes the active power scheme (PowerPlanTweak,
// UsbSelectiveSuspendTweak), so the marshaling and P/Invoke declarations live in one
// place instead of duplicated per tweak.
internal static class PowerSchemeInterop
{
    public static Guid? GetActiveScheme()
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

    public static bool SetActiveScheme(Guid scheme) => PowerSetActiveScheme(IntPtr.Zero, ref scheme) == 0;

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
