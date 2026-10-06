namespace Canopus.App.Localization;

/// <summary>Numbers and units, formatted in the interface language.</summary>
public static class Formats
{
    private const double GigabytesPerTerabyte = 1024;
    private const double MegabytesPerGigabyte = 1024;

    public static string Missing => Strings.Get("Value.Missing");

    public static string Number(double value, int decimals) =>
        value.ToString("N" + decimals, Strings.Culture);

    public static string Percent(double value, int decimals = 0) =>
        Strings.Format("Format.Percent", Number(value, decimals));

    public static string Milliseconds(double value, int decimals = 0) =>
        Strings.Format("Format.Milliseconds", Number(value, decimals));

    public static string Gigahertz(double megahertz) =>
        Strings.Format("Format.Gigahertz", Number(megahertz / 1000, 1));

    public static string Megahertz(double megahertz) =>
        Strings.Format("Format.Megahertz", Number(megahertz, 0));

    public static string Memory(double megabytes) => megabytes >= MegabytesPerGigabyte
        ? Strings.Format("Format.SizeValue", Number(megabytes / MegabytesPerGigabyte, 1), Strings.Get("Unit.Gigabytes"))
        : Strings.Format("Format.SizeValue", Number(megabytes, 0), Strings.Get("Unit.Megabytes"));

    /// <summary>"11,4 sur 32 Go", "612 Go sur 1 To", "1,3 sur 2 To".</summary>
    public static string UsedOfTotal(double usedGigabytes, double totalGigabytes)
    {
        (string used, string usedUnit) = Capacity(usedGigabytes);
        (string total, string totalUnit) = Capacity(totalGigabytes);

        return usedUnit == totalUnit
            ? Strings.Format("Format.UsedOfTotal.SameUnit", used, total, totalUnit)
            : Strings.Format("Format.UsedOfTotal.MixedUnits", used, usedUnit, total, totalUnit);
    }

    private static (string Value, string Unit) Capacity(double gigabytes)
    {
        if (gigabytes >= GigabytesPerTerabyte)
            return (Compact(gigabytes / GigabytesPerTerabyte), Strings.Get("Unit.Terabytes"));

        return (gigabytes >= 100 ? Number(gigabytes, 0) : Compact(gigabytes), Strings.Get("Unit.Gigabytes"));
    }

    // One decimal, dropped when it is zero: "2 To" rather than "2,0 To".
    private static string Compact(double value) =>
        Math.Abs(value - Math.Round(value)) < 0.05 ? Number(Math.Round(value), 0) : Number(value, 1);
}
