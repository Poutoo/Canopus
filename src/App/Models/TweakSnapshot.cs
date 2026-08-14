using System.Text.Json;

namespace Canopus.App.Models;

public record TweakSnapshot(string TweakName, IReadOnlyDictionary<string, object> Values);

public static class TweakSnapshotExtensions
{
    // After a round-trip through System.Text.Json (disk persistence), dictionary
    // values arrive as JsonElement rather than their original CLR type, so callers
    // must go through this helper instead of casting Values[key] directly.
    public static T GetValue<T>(this TweakSnapshot snapshot, string key)
    {
        object raw = snapshot.Values[key];
        if (raw is T typed)
            return typed;

        if (raw is JsonElement element)
            return element.Deserialize<T>()!;

        return (T)Convert.ChangeType(raw, typeof(T));
    }
}
