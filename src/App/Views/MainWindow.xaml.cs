using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Canopus.App.Animations;
using Canopus.App.Localization;
using Canopus.App.Services;

namespace Canopus.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly IUpdateService _updateService = new VelopackUpdateService();

    private AppPage _currentPage = AppPage.Dashboard;
    private int _navigationVersion;

    public MainWindow()
    {
        InitializeComponent();
        Title = Strings.Get("App.Name");
        ConfigureBackdrop();
        ConfigureTitleBar();
        Motion.AttachWindow(AppWindow);
        Activated += OnActivated;
        RootGrid.Loaded += (_, _) => Entrance.Play(DashboardPage);
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

    private void OnNavigationRequested(object sender, AppPage page) => ShowPage(page);

    private void OnAuditRequested(object sender, EventArgs e) => ShowPage(AppPage.Audit);

    private void OnGameSessionRequested(object sender, EventArgs e) => ShowPage(AppPage.GameSession);

    // Old page fades out, then the new one enters in a cascade. A newer navigation
    // started during the fade-out supersedes this one.
    private async void ShowPage(AppPage page)
    {
        NavSidebar.SetActivePage(page);

        if (page != _currentPage)
        {
            int version = ++_navigationVersion;
            await Motion.AnimateScalarAsync(PageHost, "Opacity", 0f, Motion.Duration("MotionDurationPageExit"), "MotionEasingExit");
            if (version != _navigationVersion)
                return;

            _currentPage = page;
            DashboardPage.Visibility = page == AppPage.Dashboard ? Visibility.Visible : Visibility.Collapsed;
            AuditPage.Visibility = page == AppPage.Audit ? Visibility.Visible : Visibility.Collapsed;
            GameSessionPage.Visibility = page == AppPage.GameSession ? Visibility.Visible : Visibility.Collapsed;
            ParametresPage.Visibility = page == AppPage.Parametres ? Visibility.Visible : Visibility.Collapsed;

            FrameworkElement target = PageFor(page);
            target.UpdateLayout();
            Motion.AnimateScalar(PageHost, "Opacity", null, 1f, TimeSpan.Zero, TimeSpan.Zero, "MotionEasingEnter");
            Entrance.Play(target);
        }

        if (page == AppPage.Audit)
            _ = AuditPage.ViewModel.RefreshAsync();
        else if (page == AppPage.Dashboard)
            _ = DashboardPage.ViewModel.RefreshAuditSummaryAsync();
        else if (page == AppPage.Parametres)
            ParametresPage.OnNavigatedTo();
        else if (page == AppPage.GameSession)
            GameSessionPage.OnNavigatedTo();
    }

    private FrameworkElement PageFor(AppPage page) => page switch
    {
        AppPage.Audit => AuditPage,
        AppPage.GameSession => GameSessionPage,
        AppPage.Parametres => ParametresPage,
        _ => DashboardPage
    };

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
