using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Canopus.App.Services;

namespace Canopus.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly IUpdateService _updateService = new VelopackUpdateService();

    public MainWindow()
    {
        InitializeComponent();
        _ = CheckForUpdatesAsync();
    }

    private enum Page { Dashboard, Audit, GameSession }

    // Audit and game session are secondary screens reached from dashboard CTAs,
    // not sidebar destinations, so any sidebar navigation brings the dashboard back.
    private void OnNavigationRequested(object sender, string destination) => ShowPage(Page.Dashboard);

    private void OnAuditRequested(object sender, EventArgs e) => ShowPage(Page.Audit);

    private void OnGameSessionRequested(object sender, EventArgs e) => ShowPage(Page.GameSession);

    private void ShowPage(Page page)
    {
        DashboardPage.Visibility = page == Page.Dashboard ? Visibility.Visible : Visibility.Collapsed;
        AuditPage.Visibility = page == Page.Audit ? Visibility.Visible : Visibility.Collapsed;
        GameSessionPage.Visibility = page == Page.GameSession ? Visibility.Visible : Visibility.Collapsed;

        if (page == Page.Audit)
            _ = AuditPage.ViewModel.RefreshAsync();
        else if (page == Page.Dashboard)
            _ = DashboardPage.ViewModel.RefreshAuditSummaryAsync();
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
            Title = "Mise à jour disponible",
            Content = $"Une mise à jour est disponible (version {result.AvailableVersion}).",
            PrimaryButtonText = "Installer",
            CloseButtonText = "Plus tard"
        };

        var choice = await dialog.ShowAsync();
        if (choice == ContentDialogResult.Primary)
        {
            await _updateService.DownloadAndApplyUpdateAsync();
        }
    }
}
