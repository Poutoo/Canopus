using Microsoft.UI.Xaml.Markup;

namespace Canopus.App.Localization;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    protected override object ProvideValue() => Strings.Get(Key);
}
