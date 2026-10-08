using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.Tests;

public sealed class JsonSettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "canopus-tests-" + Guid.NewGuid());
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Defaults_are_returned_when_nothing_was_saved()
    {
        AppSettings settings = await new JsonSettingsService(SettingsPath).LoadAsync();
        Assert.Equal(new AppSettings(), settings);
    }

    [Fact]
    public async Task Saved_settings_are_read_back()
    {
        var service = new JsonSettingsService(SettingsPath);
        var saved = new AppSettings(MousePrecisionTweakEnabled: false, MinimizeToTray: true, Language: AppLanguage.En);

        await service.SaveAsync(saved);

        Assert.Equal(saved, await new JsonSettingsService(SettingsPath).LoadAsync());
    }
}
