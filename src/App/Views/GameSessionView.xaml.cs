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

    // Re-read the mouse-tweak setting on every navigation to this view -- see
    // ParametresView.OnNavigatedTo for the same pattern on the other side.
    public void OnNavigatedTo() => _ = ViewModel.LoadSettingsAsync();

    private async void OnToggleSessionClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ToggleSessionAsync();

    private async void OnMousePrecisionToggled(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            await ViewModel.SetMousePrecisionTweakEnabledAsync(checkBox.IsChecked == true);
    }
}
