using Canopus.App.Services;

namespace Canopus.App.Tests;

public sealed class GpuChoiceTests
{
    [Fact]
    public void Dedicated_card_wins_over_the_integrated_one()
    {
        Assert.Equal(1, GpuChoice.PickDedicated([512, 16368]));
        Assert.Equal(0, GpuChoice.PickDedicated([16368, 512]));
    }

    [Fact]
    public void Unknown_memory_loses_to_a_known_one()
    {
        Assert.Equal(1, GpuChoice.PickDedicated([null, 512]));
    }

    [Fact]
    public void First_adapter_wins_a_tie_or_when_nothing_is_known()
    {
        Assert.Equal(0, GpuChoice.PickDedicated([512, 512]));
        Assert.Equal(0, GpuChoice.PickDedicated([null, null]));
        Assert.Equal(0, GpuChoice.PickDedicated([8192]));
    }
}
