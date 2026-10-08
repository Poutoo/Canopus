using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.Tests;

internal sealed class FakeTweak(string name) : IReversibleTweak
{
    public string Name { get; } = name;
    public string DisplayName => Name;

    public bool FailCapture { get; init; }
    public bool FailApply { get; init; }
    public bool FailRevert { get; init; }
    public bool VerifyResult { get; init; } = true;

    public bool Applied { get; private set; }
    public TweakSnapshot? RevertedWith { get; private set; }

    public Task<TweakSnapshot> CaptureAsync() => FailCapture
        ? throw new InvalidOperationException("capture boom")
        : Task.FromResult(new TweakSnapshot(Name, new Dictionary<string, object> { ["Original"] = $"{Name}-before" }));

    public Task ApplyAsync()
    {
        if (FailApply)
            throw new InvalidOperationException("apply boom");
        Applied = true;
        return Task.CompletedTask;
    }

    public Task<bool> VerifyAsync() => Task.FromResult(VerifyResult);

    public Task RevertAsync(TweakSnapshot snapshot)
    {
        if (FailRevert)
            throw new InvalidOperationException("revert boom");
        RevertedWith = snapshot;
        return Task.CompletedTask;
    }
}
