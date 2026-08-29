using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Canopus.App.Models;
using Canopus.App.ViewModels;

namespace Canopus.App.Views;

public sealed partial class ParametresView : UserControl
{
    public ParametresViewModel ViewModel { get; }

    public ParametresView()
    {
        InitializeComponent();
        ViewModel = new ParametresViewModel();
    }

    // MainWindow reuses this same instance across sidebar navigations rather than
    // recreating it, so settings changed elsewhere (e.g. the mouse-tweak checkbox on
    // GameSessionView) need a re-read here on each visit -- see ParametresViewModel.LoadAsync.
    public void OnNavigatedTo() => _ = ViewModel.LoadAsync();

    private void OnMousePrecisionToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
            _ = ViewModel.SetMousePrecisionEnabledAsync(toggle.IsOn);
    }

    private void OnLaunchAtStartupToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
            ViewModel.SetLaunchAtStartupEnabled(toggle.IsOn);
    }

    private void OnMinimizeToTrayToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
            _ = ViewModel.SetMinimizeToTrayEnabledAsync(toggle.IsOn);
    }

    private async void OnCheckForUpdatesClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CheckForUpdatesAsync();

    private async void OnInstallUpdateClick(object sender, RoutedEventArgs e) =>
        await ViewModel.InstallUpdateAsync();

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
            _ = ViewModel.SetLanguageAsync(comboBox.SelectedIndex == 1 ? AppLanguage.En : AppLanguage.Fr);
    }

    private void OnRestartNowClick(object sender, RoutedEventArgs e) => ViewModel.RestartApp();
}
