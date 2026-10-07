using System.Text.Json;
using Canopus.App.Localization;
using Canopus.App.Models;

namespace Canopus.App.Services;

public sealed class GameSessionService
{
    private static readonly string DefaultSnapshotFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Canopus", "session-snapshot.json");

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly IReadOnlyList<IReversibleTweak> _tweaks;
    private readonly string _snapshotFilePath;

    // snapshotFilePath only exists so tests never touch the real snapshot file.
    public GameSessionService(IReadOnlyList<IReversibleTweak> tweaks, string? snapshotFilePath = null)
    {
        _tweaks = tweaks;
        _snapshotFilePath = snapshotFilePath ?? DefaultSnapshotFilePath;
    }

    public static IReadOnlyList<IReversibleTweak> CreateDefaultTweaks() =>
    [
        new PowerPlanTweak(),
        new MousePrecisionTweak(),
        new UsbSelectiveSuspendTweak()
    ];

    public async Task<IReadOnlyList<TweakOutcome>> StartSessionAsync()
    {
        var capturedTweaks = new List<IReversibleTweak>();
        var snapshots = new List<TweakSnapshot>();
        var outcomes = new List<TweakOutcome>();

        foreach (IReversibleTweak tweak in _tweaks)
        {
            try
            {
                snapshots.Add(await tweak.CaptureAsync());
                capturedTweaks.Add(tweak);
            }
            catch (Exception ex)
            {
                outcomes.Add(new TweakOutcome(tweak.Name, false, Strings.Format("GameSession.Tweak.CaptureFailedPrefix", ex.Message)));
            }
        }

        await WriteSnapshotFileAsync(_snapshotFilePath, new SessionSnapshotFile(true, snapshots));

        foreach (IReversibleTweak tweak in capturedTweaks)
        {
            try
            {
                await tweak.ApplyAsync();
                bool verified = await tweak.VerifyAsync();
                outcomes.Add(new TweakOutcome(tweak.Name, verified, verified ? null : Strings.Get("GameSession.Tweak.VerifyFailed")));
            }
            catch (Exception ex)
            {
                outcomes.Add(new TweakOutcome(tweak.Name, false, ex.Message));
            }
        }

        return outcomes;
    }

    public async Task StopSessionAsync()
    {
        SessionSnapshotFile? file = await ReadSnapshotFileAsync(_snapshotFilePath);
        if (file is not null)
            await RevertAllAsync(_tweaks, file.Snapshots);

        DeleteSnapshotFile(_snapshotFilePath);
    }

    // Crash safety net: called at app startup before the main window is shown.
    // Builds its own tweak list rather than depending on a ViewModel instance,
    // which doesn't exist yet at this point in startup.
    public static Task RevertStaleSessionIfAnyAsync() =>
        RevertStaleSessionIfAnyAsync(CreateDefaultTweaks, DefaultSnapshotFilePath);

    internal static async Task RevertStaleSessionIfAnyAsync(Func<IReadOnlyList<IReversibleTweak>> createTweaks, string snapshotFilePath)
    {
        if (!File.Exists(snapshotFilePath))
            return;

        SessionSnapshotFile? file = await ReadSnapshotFileAsync(snapshotFilePath);
        if (file is not null)
            await RevertAllAsync(createTweaks(), file.Snapshots);

        DeleteSnapshotFile(snapshotFilePath);
    }

    private static async Task RevertAllAsync(IReadOnlyList<IReversibleTweak> tweaks, IReadOnlyList<TweakSnapshot> snapshots)
    {
        foreach (TweakSnapshot snapshot in snapshots)
        {
            IReversibleTweak? tweak = tweaks.FirstOrDefault(t => t.Name == snapshot.TweakName);
            if (tweak is null)
                continue;

            try
            {
                await tweak.RevertAsync(snapshot);
            }
            catch
            {
                // Best effort: a failing revert must not block the other tweaks
                // from being restored, nor block app startup.
            }
        }
    }

    private static async Task WriteSnapshotFileAsync(string path, SessionSnapshotFile file)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, file, SerializerOptions);
    }

    private static async Task<SessionSnapshotFile?> ReadSnapshotFileAsync(string path)
    {
        if (!File.Exists(path))
            return null;

        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SessionSnapshotFile>(stream, SerializerOptions);
    }

    private static void DeleteSnapshotFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
