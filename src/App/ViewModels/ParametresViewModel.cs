using Microsoft.UI.Xaml;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class ParametresViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly IUpdateService _updateService;

    private AppSettings _settings = new();

    public ParametresViewModel()
    {
        _settingsService = new JsonSettingsService();
        _startupService = new WindowsStartupService();
        _updateService = new VelopackUpdateService();

        _versionText = _updateService.GetCurrentVersionText();
        _ = LoadAsync();
    }

    private bool _mousePrecisionEnabled = true;
    public bool MousePrecisionEnabled { get => _mousePrecisionEnabled; private set => SetProperty(ref _mousePrecisionEnabled, value); }

    private bool _launchAtStartupEnabled;
    public bool LaunchAtStartupEnabled { get => _launchAtStartupEnabled; private set => SetProperty(ref _launchAtStartupEnabled, value); }

    private bool _minimizeToTrayEnabled;
    public bool MinimizeToTrayEnabled { get => _minimizeToTrayEnabled; private set => SetProperty(ref _minimizeToTrayEnabled, value); }

    private int _selectedLanguageIndex;
    public int SelectedLanguageIndex { get => _selectedLanguageIndex; private set => SetProperty(ref _selectedLanguageIndex, value); }

    // Restart-to-apply, not live-switching -- see Localization/Strings.cs. Reset to
    // Collapsed on every LoadAsync: a fresh view display has no pending, unsaved change.
    private Visibility _languageRestartVisibility = Visibility.Collapsed;
    public Visibility LanguageRestartVisibility { get => _languageRestartVisibility; private set => SetProperty(ref _languageRestartVisibility, value); }

    private string _versionText;
    public string VersionText { get => _versionText; private set => SetProperty(ref _versionText, value); }

    private string _updateStatusText = string.Empty;
    public string UpdateStatusText { get => _updateStatusText; private set => SetProperty(ref _updateStatusText, value); }

    private bool _canCheckForUpdates = true;
    public bool CanCheckForUpdates { get => _canCheckForUpdates; private set => SetProperty(ref _canCheckForUpdates, value); }

    private bool _isUpdateAvailable;
    private Visibility _installButtonVisibility = Visibility.Collapsed;
    public Visibility InstallButtonVisibility { get => _installButtonVisibility; private set => SetProperty(ref _installButtonVisibility, value); }

    // Re-read on every navigation to this view rather than kept live-in-sync with
    // GameSessionView: the two are never on screen at the same time in the current
    // single-page navigation, so re-reading on display is enough to stay accurate.
    public async Task LoadAsync()
    {
        _settings = await _settingsService.LoadAsync();
        MousePrecisionEnabled = _settings.MousePrecisionTweakEnabled;
        MinimizeToTrayEnabled = _settings.MinimizeToTray;
        LaunchAtStartupEnabled = _startupService.IsEnabled();
        SelectedLanguageIndex = _settings.Language == AppLanguage.En ? 1 : 0;
        LanguageRestartVisibility = Visibility.Collapsed;
    }

    public async Task SetMousePrecisionEnabledAsync(bool enabled)
    {
        if (enabled == MousePrecisionEnabled)
            return;

        MousePrecisionEnabled = enabled;
        _settings = _settings with { MousePrecisionTweakEnabled = enabled };
        await _settingsService.SaveAsync(_settings);
    }

    public void SetLaunchAtStartupEnabled(bool enabled)
    {
        if (enabled == LaunchAtStartupEnabled)
            return;

        _startupService.SetEnabled(enabled);
        LaunchAtStartupEnabled = enabled;
    }

    public async Task SetMinimizeToTrayEnabledAsync(bool enabled)
    {
        if (enabled == MinimizeToTrayEnabled)
            return;

        MinimizeToTrayEnabled = enabled;
        _settings = _settings with { MinimizeToTray = enabled };
        await _settingsService.SaveAsync(_settings);

        // Takes effect immediately, not just on next launch: the window-close handler
        // reads this live off the running App instance rather than re-reading settings.
        if (Application.Current is App app)
            app.MinimizeToTrayEnabled = enabled;
    }

    public async Task SetLanguageAsync(AppLanguage language)
    {
        if (language == _settings.Language)
            return;

        _settings = _settings with { Language = language };
        await _settingsService.SaveAsync(_settings);
        LanguageRestartVisibility = Visibility.Visible;
    }

    public void RestartApp()
    {
        if (Application.Current is App app)
            app.RestartApp();
    }

    public async Task CheckForUpdatesAsync()
    {
        CanCheckForUpdates = false;
        UpdateStatusText = Strings.Get("Parametres.Updates.Checking");
        try
        {
            UpdateCheckResult result = await _updateService.CheckForUpdateAsync();
            _isUpdateAvailable = result.IsUpdateAvailable;
            InstallButtonVisibility = result.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
            UpdateStatusText = result.IsUpdateAvailable
                ? Strings.Format("Parametres.Updates.Available", result.AvailableVersion)
                : Strings.Get("Parametres.Updates.UpToDate");
        }
        finally
        {
            CanCheckForUpdates = true;
        }
    }

    public async Task InstallUpdateAsync()
    {
        if (!_isUpdateAvailable)
            return;

        CanCheckForUpdates = false;
        UpdateStatusText = Strings.Get("Parametres.Updates.Installing");
        await _updateService.DownloadAndApplyUpdateAsync();
    }
}
