using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Canopus.App.Services;
using Canopus.App.ViewModels;

namespace Canopus.App.Views;

public sealed partial class DashboardView : UserControl
{
    private readonly LibreHardwareMonitorService _hardwareMonitorService;

    public DashboardViewModel ViewModel { get; }

    public event EventHandler? AuditRequested;
    public event EventHandler? GameSessionRequested;

    public DashboardView()
    {
        InitializeComponent();

        _hardwareMonitorService = new LibreHardwareMonitorService();
        ViewModel = new DashboardViewModel(
            _hardwareMonitorService,
            new DriveInfoStorageService(),
            new PingNetworkService(),
            new ProcessMonitorService(),
            App.AuditService);

        Unloaded += (_, _) =>
        {
            ViewModel.Dispose();
            _hardwareMonitorService.Dispose();
        };
    }

    private void OnAuditDetailClick(object sender, RoutedEventArgs e) =>
        AuditRequested?.Invoke(this, EventArgs.Empty);

    private void OnGameSessionDetailClick(object sender, RoutedEventArgs e) =>
        GameSessionRequested?.Invoke(this, EventArgs.Empty);
}
