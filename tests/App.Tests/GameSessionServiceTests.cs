using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.Tests;

public sealed class GameSessionServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "canopus-tests-" + Guid.NewGuid());
    private string SnapshotPath => Path.Combine(_directory, "session-snapshot.json");

    public GameSessionServiceTests() => Strings.Initialize(AppLanguage.Fr);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Start_applies_and_verifies_every_tweak_then_keeps_a_snapshot()
    {
        var tweaks = new[] { new FakeTweak("A"), new FakeTweak("B") };

        var outcomes = await new GameSessionService(tweaks, SnapshotPath).StartSessionAsync();

        Assert.All(outcomes, o => Assert.True(o.Succeeded));
        Assert.All(tweaks, t => Assert.True(t.Applied));
        Assert.True(File.Exists(SnapshotPath));
    }

    [Fact]
    public async Task A_tweak_that_cannot_be_captured_is_never_applied()
    {
        var failing = new FakeTweak("A") { FailCapture = true };
        var ok = new FakeTweak("B");

        var outcomes = await new GameSessionService([failing, ok], SnapshotPath).StartSessionAsync();

        Assert.False(failing.Applied);
        Assert.True(ok.Applied);
        TweakOutcome outcome = Assert.Single(outcomes, o => o.TweakName == "A");
        Assert.False(outcome.Succeeded);
        Assert.Equal(Strings.Format("GameSession.Tweak.CaptureFailedPrefix", "capture boom"), outcome.FailureReason);
    }

    [Fact]
    public async Task A_failed_verification_is_reported_as_a_failure()
    {
        var outcomes = await new GameSessionService([new FakeTweak("A") { VerifyResult = false }], SnapshotPath).StartSessionAsync();

        TweakOutcome outcome = Assert.Single(outcomes);
        Assert.False(outcome.Succeeded);
        Assert.Equal(Strings.Get("GameSession.Tweak.VerifyFailed"), outcome.FailureReason);
    }

    [Fact]
    public async Task A_failing_apply_does_not_stop_the_other_tweaks()
    {
        var failing = new FakeTweak("A") { FailApply = true };
        var ok = new FakeTweak("B");

        var outcomes = await new GameSessionService([failing, ok], SnapshotPath).StartSessionAsync();

        Assert.True(ok.Applied);
        Assert.False(outcomes.Single(o => o.TweakName == "A").Succeeded);
        Assert.True(outcomes.Single(o => o.TweakName == "B").Succeeded);
    }

    [Fact]
    public async Task Stop_reverts_with_the_captured_values_and_deletes_the_snapshot()
    {
        var tweak = new FakeTweak("A");
        var service = new GameSessionService([tweak], SnapshotPath);

        await service.StartSessionAsync();
        await service.StopSessionAsync();

        Assert.NotNull(tweak.RevertedWith);
        Assert.Equal("A-before", tweak.RevertedWith!.GetValue<string>("Original"));
        Assert.False(File.Exists(SnapshotPath));
    }

    [Fact]
    public async Task A_failing_revert_does_not_block_the_others()
    {
        var failing = new FakeTweak("A") { FailRevert = true };
        var ok = new FakeTweak("B");
        var service = new GameSessionService([failing, ok], SnapshotPath);

        await service.StartSessionAsync();
        await service.StopSessionAsync();

        Assert.NotNull(ok.RevertedWith);
        Assert.False(File.Exists(SnapshotPath));
    }

    [Fact]
    public async Task Startup_without_a_leftover_snapshot_does_nothing()
    {
        bool tweaksCreated = false;

        await GameSessionService.RevertStaleSessionIfAnyAsync(() =>
        {
            tweaksCreated = true;
            return [];
        }, SnapshotPath);

        Assert.False(tweaksCreated);
    }

    [Fact]
    public async Task Startup_after_a_crash_reverts_the_leftover_session()
    {
        await new GameSessionService([new FakeTweak("A")], SnapshotPath).StartSessionAsync();

        var afterRestart = new FakeTweak("A");
        await GameSessionService.RevertStaleSessionIfAnyAsync(() => [afterRestart], SnapshotPath);

        Assert.Equal("A-before", afterRestart.RevertedWith!.GetValue<string>("Original"));
        Assert.False(File.Exists(SnapshotPath));
    }
}
