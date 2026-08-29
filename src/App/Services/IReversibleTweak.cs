using Canopus.App.Models;

namespace Canopus.App.Services;

public interface IReversibleTweak
{
    /// <summary>
    /// Stable internal identifier -- used for TweakNotes lookups, snapshot matching
    /// (<see cref="TweakSnapshot.TweakName"/>) and crash-recovery persistence. Not localized:
    /// changing it with the display language would break matching a snapshot written in a
    /// different language than the one active when the app restarts after a crash.
    /// </summary>
    string Name { get; }

    /// <summary>Localized text shown to the user (card title). See <see cref="Name"/> for the stable identifier used internally.</summary>
    string DisplayName { get; }

    Task<TweakSnapshot> CaptureAsync();
    Task ApplyAsync();
    Task<bool> VerifyAsync();
    Task RevertAsync(TweakSnapshot snapshot);
}
