using System.Runtime.InteropServices;
using Canopus.App.Models;

namespace Canopus.App.Services;

// SPI_SETMOUSE expects a 3-int array: thresholds 1/2 in pixels (left unchanged
// here) then the acceleration flag 0/1 -- that last index is what corresponds to
// the "Enhance pointer precision" checkbox. Verified on a real machine: with only
// SPIF_SENDCHANGE, the value changes for the session (read back via SPI_GETMOUSE)
// but HKCU\Control Panel\Mouse\MouseSpeed never updates -- hence the combined
// SPIF_UPDATEINIFILE | SPIF_SENDCHANGE flag below, without which Windows Settings
// would stay out of sync with the actually active state.
public sealed class MousePrecisionTweak : IReversibleTweak
{
    private const uint SPI_GETMOUSE = 0x0003;
    private const uint SPI_SETMOUSE = 0x0004;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    private const string Threshold1Key = "Threshold1";
    private const string Threshold2Key = "Threshold2";
    private const string AccelerationKey = "Acceleration";

    public string Name => "Précision du pointeur";

    public Task<TweakSnapshot> CaptureAsync()
    {
        int[] current = GetMouseParams();
        return Task.FromResult(new TweakSnapshot(Name, new Dictionary<string, object>
        {
            [Threshold1Key] = current[0],
            [Threshold2Key] = current[1],
            [AccelerationKey] = current[2]
        }));
    }

    public Task ApplyAsync()
    {
        int[] current = GetMouseParams();
        SetMouseParams([current[0], current[1], 0]);
        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync() => Task.FromResult(GetMouseParams()[2] == 0);

    public Task RevertAsync(TweakSnapshot snapshot)
    {
        SetMouseParams([
            snapshot.GetValue<int>(Threshold1Key),
            snapshot.GetValue<int>(Threshold2Key),
            snapshot.GetValue<int>(AccelerationKey)
        ]);
        return Task.CompletedTask;
    }

    private static int[] GetMouseParams()
    {
        int[] values = new int[3];
        if (!SystemParametersInfo(SPI_GETMOUSE, 0, values, 0))
            throw new InvalidOperationException("Lecture des paramètres souris impossible.");

        return values;
    }

    private static void SetMouseParams(int[] values)
    {
        if (!SystemParametersInfo(SPI_SETMOUSE, 0, values, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
            throw new InvalidOperationException("Écriture des paramètres souris impossible.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);
}
