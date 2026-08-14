using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Canopus.App.ViewModels;

namespace Canopus.App.Views;

public sealed partial class GameSessionView : UserControl
{
    public GameSessionViewModel ViewModel { get; }

    public GameSessionView()
    {
        InitializeComponent();
        ViewModel = new GameSessionViewModel();
    }

    private async void OnToggleSessionClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ToggleSessionAsync();

    private async void OnMousePrecisionExcludeToggled(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            await ViewModel.SetMousePrecisionTweakEnabledAsync(checkBox.IsChecked != true);
    }
}
