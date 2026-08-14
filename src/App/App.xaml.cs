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

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
