using Canopus.App.ViewModels;

namespace Canopus.App.Models;

/// <summary>
/// One "Mémoire et disques" row. Mutable on purpose: the dashboard refresh updates rows in
/// place, so the bound bar animates instead of being recreated every tick.
/// </summary>
public sealed class UsageDisplayItem(string key) : ViewModelBase
{
    public string Key { get; } = key;

    private string _label = string.Empty;
    public string Label { get => _label; set => SetProperty(ref _label, value); }

    private string _valueText = string.Empty;
    public string ValueText { get => _valueText; set => SetProperty(ref _valueText, value); }

    private double _fraction;
    public double Fraction { get => _fraction; set => SetProperty(ref _fraction, value); }
}
