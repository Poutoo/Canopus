using Canopus.App.Localization;
using Canopus.App.Models;

namespace Canopus.App.Tests;

public sealed class FormatsTests
{
    [Theory]
    [InlineData(AppLanguage.Fr, "38 %")]
    [InlineData(AppLanguage.En, "38%")]
    public void Percent_follows_the_language(AppLanguage language, string expected)
    {
        Strings.Initialize(language);
        Assert.Equal(expected, Formats.Percent(38));
    }

    [Fact]
    public void Decimals_use_the_language_separator()
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal("28,4 %", Formats.Percent(28.4, 1));
        Assert.Equal("2,1 ms", Formats.Milliseconds(2.1, 1));
        Assert.Equal("4,8 GHz", Formats.Gigahertz(4800));

        Strings.Initialize(AppLanguage.En);
        Assert.Equal("4.8 GHz", Formats.Gigahertz(4800));
    }

    [Fact]
    public void Megahertz_groups_thousands()
    {
        Strings.Initialize(AppLanguage.Fr);
        string separator = Strings.Culture.NumberFormat.NumberGroupSeparator;
        Assert.Equal($"2{separator}610 MHz", Formats.Megahertz(2610));
    }

    [Theory]
    [InlineData(530, "530 Mo")]
    [InlineData(6963, "6,8 Go")]
    public void Process_memory_switches_to_gigabytes(double megabytes, string expected)
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal(expected, Formats.Memory(megabytes));
    }

    [Theory]
    [InlineData(11.4, 32, "11,4 sur 32 Go")]
    [InlineData(612, 1024, "612 Go sur 1 To")]
    [InlineData(1331.2, 2048, "1,3 sur 2 To")]
    public void Used_of_total_states_the_unit_once_when_shared(double used, double total, string expected)
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal(expected, Formats.UsedOfTotal(used, total));
    }

    [Fact]
    public void Used_of_total_in_english()
    {
        Strings.Initialize(AppLanguage.En);
        Assert.Equal("612 GB of 1 TB", Formats.UsedOfTotal(612, 1024));
    }

    [Fact]
    public void Missing_value_comes_from_the_translations()
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal("—", Formats.Missing);
    }
}
