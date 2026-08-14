namespace Canopus.App.Models;

// The file's existence already signals an active session, but IsActive is kept
// explicit so the file stays readable if inspected by hand.
public record SessionSnapshotFile(bool IsActive, IReadOnlyList<TweakSnapshot> Snapshots);
