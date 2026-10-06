using Canopus.App.Localization;

namespace Canopus.App.ViewModels;

internal static class AuditSummary
{
    public static string Text(int toCheck) => toCheck switch
    {
        0 => Strings.Get("Audit.Summary.None"),
        1 => Strings.Get("Audit.Summary.Singular"),
        _ => Strings.Format("Audit.Summary.Plural", toCheck)
    };
}
