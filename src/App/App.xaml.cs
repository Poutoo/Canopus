using Microsoft.UI.Xaml;
using Canopus.App.Services;
using Canopus.App.Views;

namespace Canopus.App;

public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Shared instance so the dashboard CTA counter and the audit screen read the
    /// same cache instead of each triggering its own WMI sweep.
    /// </summary>
    public static IAuditService AuditService { get; } = new WindowsAuditService();

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // If a session-snapshot.json is still on disk, the app crashed mid-session
        // last run: revert those tweaks before anything else runs.
        await GameSessionService.RevertStaleSessionIfAnyAsync();

        _window = new MainWindow();
        _window.Activate();
    }
}
