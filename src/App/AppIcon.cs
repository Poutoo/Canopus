using Microsoft.Win32;

namespace Canopus.App;

/// <summary>
/// The taskbar and the notification area follow the system theme, not the app's forced
/// light theme: the light-background icon disappears on a dark taskbar.
/// </summary>
public static class AppIcon
{
    public static string TaskbarIconPath => Path.Combine(
        AppContext.BaseDirectory, "Assets", "canopus-icone", "ico",
        TaskbarIsLight() ? "canopus-clair.ico" : "canopus-sombre.ico");

    private static bool TaskbarIsLight()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is 1;
    }
}
