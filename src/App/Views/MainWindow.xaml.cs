using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Canopus.App.Localization;
using Canopus.App.Services;

namespace Canopus.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly IUpdateService _updateService = new VelopackUpdateService();

    public MainWindow()
    {
        InitializeComponent();
        Title = Strings.Get("App.Name");
        ConfigureBackdrop();
        ConfigureTitleBar();
        Activated += OnActivated;
        _ = CheckForUpdatesAsync();
    }

    private void ConfigureBackdrop()
    {
        if (CanopusAcrylicBackdrop.IsSupported)
            SystemBackdrop = new CanopusAcrylicBackdrop(this);
        else
            RootGrid.Background = Resource<Brush>("BackdropFallbackBrush");
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindowTitleBar titleBar = AppWindow.TitleBar;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // The theme is forced to Light, so every caption button state is set by hand:
        // otherwise Windows draws white glyphs whenever the system theme is dark.
        Color transparent = Resource<Color>("TransparentColor");
        Color foreground = Resource<Color>("TitleBarButtonForegroundColor");
        titleBar.ButtonBackgroundColor = transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonHoverBackgroundColor = Resource<Color>("TitleBarButtonHoverBackgroundColor");
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = Resource<Color>("TitleBarButtonPressedBackgroundColor");
        titleBar.ButtonPressedForegroundColor = foreground;
        titleBar.ButtonInactiveBackgroundColor = transparent;
        titleBar.ButtonInactiveForegroundColor = Resource<Color>("TitleBarButtonInactiveForegroundColor");
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        bool isActive = args.WindowActivationState != WindowActivationState.Deactivated;
        AppTitleContent.Opacity = isActive ? 1 : Resource<double>("InactiveTitleOpacity");
    }

    private static T Resource<T>(string key) => (T)Application.Current.Resources[key];

    private enum Page { Dashboard, Audit, GameSession, Parametres }

    // Audit and game session are secondary screens reached from dashboard CTAs, not
    // sidebar destinations. Parametres is the one sidebar destination actually wired up
    // so far -- Historique/Documentation don't have a screen yet and fall back to Dashboard.
    private void OnNavigationRequested(object sender, string destination) =>
        ShowPage(destination == "Parametres" ? Page.Parametres : Page.Dashboard);

    private void OnAuditRequested(object sender, EventArgs e) => ShowPage(Page.Audit);

    private void OnGameSessionRequested(object sender, EventArgs e) => ShowPage(Page.GameSession);

    private void ShowPage(Page page)
    {
        DashboardPage.Visibility = page == Page.Dashboard ? Visibility.Visible : Visibility.Collapsed;
        AuditPage.Visibility = page == Page.Audit ? Visibility.Visible : Visibility.Collapsed;
        GameSessionPage.Visibility = page == Page.GameSession ? Visibility.Visible : Visibility.Collapsed;
        ParametresPage.Visibility = page == Page.Parametres ? Visibility.Visible : Visibility.Collapsed;

        if (page == Page.Audit)
            _ = AuditPage.ViewModel.RefreshAsync();
        else if (page == Page.Dashboard)
            _ = DashboardPage.ViewModel.RefreshAuditSummaryAsync();
        else if (page == Page.Parametres)
            ParametresPage.OnNavigatedTo();
        else if (page == Page.GameSession)
            GameSessionPage.OnNavigatedTo();
    }

    // Flux de mise à jour minimal, temporaire : juste de quoi prouver que
    // check -> dialogue -> install fonctionne bout en bout. L'habillage
    // visuel définitif viendra dans une itération séparée.
    private async Task CheckForUpdatesAsync()
    {
        var result = await _updateService.CheckForUpdateAsync();
        if (!result.IsUpdateAvailable)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = Strings.Get("UpdateDialog.Title"),
            Content = Strings.Format("UpdateDialog.Content", result.AvailableVersion),
            PrimaryButtonText = Strings.Get("UpdateDialog.Install"),
            CloseButtonText = Strings.Get("UpdateDialog.Later")
        };

        var choice = await dialog.ShowAsync();
        if (choice == ContentDialogResult.Primary)
        {
            await _updateService.DownloadAndApplyUpdateAsync();
        }
    }
}
