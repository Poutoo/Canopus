namespace Canopus.App.Models;

public record AppSettings(bool MousePrecisionTweakEnabled = true, bool MinimizeToTray = false, AppLanguage Language = AppLanguage.Fr);
