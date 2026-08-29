using Microsoft.UI.Xaml;

namespace Canopus.App.Models;

/// <summary>
/// Display-ready representation of a disk/partition, with the fill-bar split
/// already computed (two star-sized <see cref="GridLength"/> columns: no XAML
/// converter needed).
/// </summary>
public sealed record DriveDisplayItem(
    string Name,
    string UsageText,
    GridLength FilledColumnWidth,
    GridLength EmptyColumnWidth);
