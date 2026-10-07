using System.Text.Json;
using Canopus.App.Models;

namespace Canopus.App.Tests;

public sealed class TweakSnapshotTests
{
    [Fact]
    public void Values_survive_a_json_round_trip()
    {
        var original = new TweakSnapshot("Tweak", new Dictionary<string, object>
        {
            ["Text"] = "abc",
            ["Number"] = 42,
            ["Flag"] = true
        });

        var restored = JsonSerializer.Deserialize<TweakSnapshot>(JsonSerializer.Serialize(original))!;

        Assert.Equal("abc", restored.GetValue<string>("Text"));
        Assert.Equal(42, restored.GetValue<int>("Number"));
        Assert.True(restored.GetValue<bool>("Flag"));
    }

    [Fact]
    public void Values_are_read_directly_before_serialization()
    {
        var snapshot = new TweakSnapshot("Tweak", new Dictionary<string, object> { ["Number"] = 42 });
        Assert.Equal(42, snapshot.GetValue<int>("Number"));
    }
}
