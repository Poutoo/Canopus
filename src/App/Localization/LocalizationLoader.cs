using System.Reflection;
using System.Text.Json;
using Canopus.App.Models;

namespace Canopus.App.Localization;

internal static class LocalizationLoader
{
    public static IReadOnlyDictionary<string, string> Load(AppLanguage language)
    {
        // Embedded resource names derive from RootNamespace (csproj), not the C# namespace
        // used in code -- App.csproj sets <RootNamespace>Canopus</RootNamespace>, so these
        // are "Canopus.Resources...", not "Canopus.App.Resources...". Confirmed against the
        // actual published assembly's GetManifestResourceNames(), not just assumed.
        string resourceName = language switch
        {
            AppLanguage.En => "Canopus.Resources.Strings.en.json",
            _ => "Canopus.Resources.Strings.fr.json"
        };

        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
    }
}
