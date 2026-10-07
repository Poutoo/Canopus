using System.Text.Json;
using System.Text.RegularExpressions;
using Canopus.App.Localization;
using Canopus.App.Models;

namespace Canopus.App.Tests;

public sealed partial class LocalizationTests
{
    private static Dictionary<string, string> Load(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(RepoPaths.AppSource, "Resources", "Strings", $"{language}.json")))!;

    [Fact]
    public void French_and_English_have_the_same_keys()
    {
        var fr = Load("fr").Keys.ToHashSet();
        var en = Load("en").Keys.ToHashSet();

        Assert.Empty(fr.Except(en));
        Assert.Empty(en.Except(fr));
    }

    [Fact]
    public void Every_translation_uses_the_same_placeholders_in_both_languages()
    {
        var fr = Load("fr");
        var en = Load("en");

        var mismatches = fr.Keys
            .Where(key => !Placeholders(fr[key]).SetEquals(Placeholders(en[key])))
            .ToList();

        Assert.Empty(mismatches);
    }

    [Fact]
    public void No_translation_is_empty()
    {
        Assert.Empty(Load("fr").Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key));
        Assert.Empty(Load("en").Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key));
    }

    [Fact]
    public void Every_key_used_in_the_code_exists()
    {
        var keys = Load("fr").Keys.ToHashSet();
        var used = Directory
            .EnumerateFiles(RepoPaths.AppSource, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => UsedKey().Matches(File.ReadAllText(f)).Select(m => m.Groups["key"].Value))
            .ToHashSet();

        Assert.NotEmpty(used);
        Assert.Empty(used.Except(keys));
    }

    [Fact]
    public void Embedded_strings_load_for_both_languages()
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal("Tableau de bord", Strings.Get("Nav.Dashboard"));

        Strings.Initialize(AppLanguage.En);
        Assert.Equal("Dashboard", Strings.Get("Nav.Dashboard"));
    }

    [Fact]
    public void A_missing_key_is_shown_as_is()
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal("Does.Not.Exist", Strings.Get("Does.Not.Exist"));
    }

    private static HashSet<string> Placeholders(string value) =>
        Placeholder().Matches(value).Select(m => m.Groups[1].Value).ToHashSet();

    [GeneratedRegex(@"\{(\d+)(?::[^}]*)?\}")]
    private static partial Regex Placeholder();

    // Strings.Get("...") / Strings.Format("...") in C#, {loc:Loc Key=...} in XAML.
    [GeneratedRegex(@"Strings\.(?:Get|Format)\(""(?<key>[^""]+)""|Key=(?<key>[A-Za-z0-9.]+)")]
    private static partial Regex UsedKey();
}
