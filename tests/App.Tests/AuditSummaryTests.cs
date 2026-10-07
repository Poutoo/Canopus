using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.ViewModels;

namespace Canopus.App.Tests;

public sealed class AuditSummaryTests
{
    [Theory]
    [InlineData(0, "Aucun réglage à vérifier")]
    [InlineData(1, "1 réglage à vérifier")]
    [InlineData(3, "3 réglages à vérifier")]
    public void Summary_agrees_with_the_count(int toCheck, string expected)
    {
        Strings.Initialize(AppLanguage.Fr);
        Assert.Equal(expected, AuditSummary.Text(toCheck));
    }
}
