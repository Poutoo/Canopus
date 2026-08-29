using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using H.NotifyIcon;
using Canopus.App.Models;
using Canopus.App.Services;
using Canopus.App.Views;

namespace Canopus.App;

public partial class App : Application
{
    private readonly ISettingsService _settingsService = new JsonSettingsService();

    private Window? _window;
    private TaskbarIcon? _trayIcon;

    // Distinguishes a real quit (tray "Quitter", or a click on X while the setting is off)
    // from a close that should just hide the window instead -- see OnWindowClosed.
    private bool _handleWindowClosed = true;

    /// <summary>
    /// Shared instance so the dashboard CTA counter and the audit screen read the
    /// same cache instead of each triggering its own WMI sweep.
    /// </summary>
    public static IAuditService AuditService { get; } = new WindowsAuditService();

    // Set from ParametresViewModel when the toggle changes, so it takes effect on the very
    // next window close without needing a settings re-read from disk at that point.
    public bool MinimizeToTrayEnabled { get; set; }

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // If a session-snapshot.json is still on disk, the app crashed mid-session
        // last run: revert those tweaks before anything else runs.
        await GameSessionService.RevertStaleSessionIfAnyAsync();

        AppSettings settings = await _settingsService.LoadAsync();
        MinimizeToTrayEnabled = settings.MinimizeToTray;

        _window = new MainWindow();
        _window.Closed += OnWindowClosed;

        // Application.xaml can't wire Click="..." attributes (XamlCompiler WMC1005), so the
        // context menu's XamlUICommand resources are hooked up here instead.
        var openCommand = (XamlUICommand)Resources["TrayOpenCommand"];
        openCommand.ExecuteRequested += (_, _) => ShowMainWindow();

        var exitCommand = (XamlUICommand)Resources["TrayExitCommand"];
        exitCommand.ExecuteRequested += OnTrayExitRequested;

        // ForceCreate is required in unpackaged apps -- without it the Win32 tray icon
        // handle isn't reliably created before the first interaction with it.
        _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);
        _trayIcon.ForceCreate();

        _window.Activate();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (!_handleWindowClosed || !MinimizeToTrayEnabled)
            return;

        args.Handled = true;
        _window?.Hide();
    }

    private void OnTrayExitRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        _handleWindowClosed = false;
        _trayIcon?.Dispose();
        _window?.Close();

        // https://github.com/HavenDV/H.NotifyIcon/issues/66 -- defensive fallback, our
        // _window should never actually be null here since OnLaunched always creates one.
        if (_window is null)
            Environment.Exit(0);
    }

    private void ShowMainWindow()
    {
        _window?.Show();
        _window?.Activate();
    }

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
