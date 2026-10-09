using Microsoft.UI.Xaml.Controls;
using Canopus.App.Localization;

namespace Canopus.App.Views;

public sealed partial class UpdateDialog : ContentDialog
{
    public UpdateDialog(string? version)
    {
        InitializeComponent();
        MessageText.Text = Strings.Format("UpdateDialog.Content", version);
    }
}
