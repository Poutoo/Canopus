using System.Text.Json;
using Canopus.App.Models;

namespace Canopus.App.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Canopus", "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        await using FileStream stream = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(FilePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions);
    }
}
