using System.Text.Json;
using Canopus.App.Models;

namespace Canopus.App.Services;

// filePath only exists so tests never touch the real settings file.
public sealed class JsonSettingsService(string? filePath = null) : ISettingsService
{
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Canopus", "settings.json");

    private readonly string _filePath = filePath ?? DefaultFilePath;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        await using FileStream stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions);
    }
}
