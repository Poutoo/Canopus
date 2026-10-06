using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Canopus.App.Animations;
using Canopus.App.Localization;
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
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        Motion.EnableTranslation(LanguageThumb);
        ApplyLanguageSegment(animate: false);
    }

    // MainWindow reuses this same instance across sidebar navigations rather than
    // recreating it, so settings changed elsewhere (e.g. the mouse-tweak checkbox on
    // GameSessionView) need a re-read here on each visit -- see ParametresViewModel.LoadAsync.
    public void OnNavigatedTo() => _ = ViewModel.LoadAsync();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ParametresViewModel.SelectedLanguageIndex))
            ApplyLanguageSegment(animate: true);
    }

    private void ApplyLanguageSegment(bool animate)
    {
        bool isEnglish = ViewModel.SelectedLanguageIndex == 1;
        var active = (Style)Application.Current.Resources["SegmentLabelActiveStyle"];
        var idle = (Style)Application.Current.Resources["SegmentLabelStyle"];

        FrenchLabel.Style = isEnglish ? idle : active;
        EnglishLabel.Style = isEnglish ? active : idle;
        AutomationProperties.SetItemStatus(FrenchSegment, isEnglish ? string.Empty : Strings.Get("Parametres.Language.Selected"));
        AutomationProperties.SetItemStatus(EnglishSegment, isEnglish ? Strings.Get("Parametres.Language.Selected") : string.Empty);

        float x = isEnglish ? (float)FrenchSegment.Width : 0f;
        TimeSpan duration = animate ? Motion.Duration("MotionDurationSegment") : TimeSpan.Zero;
        Motion.AnimateScalar(LanguageThumb, "Translation.X", null, x, duration, TimeSpan.Zero, "MotionEasingStandard");
    }

    private void OnLanguageSegmentClick(object sender, RoutedEventArgs e) =>
        _ = ViewModel.SetLanguageAsync(ReferenceEquals(sender, EnglishSegment) ? AppLanguage.En : AppLanguage.Fr);

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

    private void OnRestartNowClick(object sender, RoutedEventArgs e) => ViewModel.RestartApp();
}
