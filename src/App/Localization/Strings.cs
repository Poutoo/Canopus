using Canopus.App.Models;

namespace Canopus.App.Localization;

// Restart-to-apply, not live-switching: initialized once from App.OnLaunched before any XAML
// is constructed (see LocExtension for the static-XAML side, and App.xaml.cs for the ordering
// this depends on). No runtime rebinding, no attached properties -- deliberately, see the plan.
public static class Strings
{
    private static IReadOnlyDictionary<string, string> _values = new Dictionary<string, string>();

    public static void Initialize(AppLanguage language) => _values = LocalizationLoader.Load(language);

    // Missing key -> the key itself, not an empty string: a raw key on screen is an obvious,
    // traceable bug; a blank label would silently pass unnoticed.
    public static string Get(string key) => _values.TryGetValue(key, out string? value) ? value : key;

    public static string Format(string key, params object?[] args) => string.Format(Get(key), args);
}
