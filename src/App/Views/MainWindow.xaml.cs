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

    // Audit is a secondary screen reached from the dashboard CTA, not a sidebar
    // destination, so any sidebar navigation brings the dashboard back.
    private void OnNavigationRequested(object sender, string destination) => ShowAudit(false);

    private void OnAuditRequested(object sender, EventArgs e) => ShowAudit(true);

    private void ShowAudit(bool showAudit)
    {
        AuditPage.Visibility = showAudit ? Visibility.Visible : Visibility.Collapsed;
        DashboardPage.Visibility = showAudit ? Visibility.Collapsed : Visibility.Visible;

        if (showAudit)
            _ = AuditPage.ViewModel.RefreshAsync();
        else
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
