using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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

    // Language the interface is currently displayed in: a change only applies on restart.
    private AppLanguage? _launchLanguage;

    public ParametresViewModel()
    {
        _settingsService = new JsonSettingsService();
        _startupService = new WindowsStartupService();
        _updateService = new VelopackUpdateService();

        _versionText = Strings.Format("Parametres.Updates.Version", _updateService.GetCurrentVersionText());
        _updateStatusBrush = GetBrush("TextTertiaryBrush");
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

    // Restart-to-apply, not live-switching -- see Localization/Strings.cs.
    private Visibility _languageRestartVisibility = Visibility.Collapsed;
    public Visibility LanguageRestartVisibility { get => _languageRestartVisibility; private set => SetProperty(ref _languageRestartVisibility, value); }

    private string _versionText;
    public string VersionText { get => _versionText; private set => SetProperty(ref _versionText, value); }

    private string _updateStatusText = string.Empty;
    public string UpdateStatusText { get => _updateStatusText; private set => SetProperty(ref _updateStatusText, value); }

    private Brush _updateStatusBrush;
    public Brush UpdateStatusBrush { get => _updateStatusBrush; private set => SetProperty(ref _updateStatusBrush, value); }

    private Visibility _updateStatusVisibility = Visibility.Collapsed;
    public Visibility UpdateStatusVisibility { get => _updateStatusVisibility; private set => SetProperty(ref _updateStatusVisibility, value); }

    private Visibility _checkButtonVisibility = Visibility.Visible;
    public Visibility CheckButtonVisibility { get => _checkButtonVisibility; private set => SetProperty(ref _checkButtonVisibility, value); }

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
        _launchLanguage ??= _settings.Language;
        ApplyLanguage(_settings.Language);
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
        ApplyLanguage(language);
    }

    private void ApplyLanguage(AppLanguage language)
    {
        SelectedLanguageIndex = language == AppLanguage.En ? 1 : 0;
        LanguageRestartVisibility = language != _launchLanguage ? Visibility.Visible : Visibility.Collapsed;
    }

    public void RestartApp()
    {
        if (Application.Current is App app)
            app.RestartApp();
    }

    public async Task CheckForUpdatesAsync()
    {
        CanCheckForUpdates = false;
        SetUpdateStatus(Strings.Get("Parametres.Updates.Checking"), "TextTertiaryBrush");
        try
        {
            UpdateCheckResult result = await _updateService.CheckForUpdateAsync();
            _isUpdateAvailable = result.IsUpdateAvailable;
            InstallButtonVisibility = result.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
            CheckButtonVisibility = result.IsUpdateAvailable ? Visibility.Collapsed : Visibility.Visible;
            if (result.IsUpdateAvailable)
                SetUpdateStatus(Strings.Format("Parametres.Updates.Available", result.AvailableVersion), "TextPrimaryBrush");
            else
                SetUpdateStatus(Strings.Get("Parametres.Updates.UpToDate"), "TextTertiaryBrush");
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
        InstallButtonVisibility = Visibility.Collapsed;
        SetUpdateStatus(Strings.Get("Parametres.Updates.Installing"), "TextPrimaryBrush");
        await _updateService.DownloadAndApplyUpdateAsync();
    }

    private void SetUpdateStatus(string text, string brushKey)
    {
        UpdateStatusText = text;
        UpdateStatusBrush = GetBrush(brushKey);
        UpdateStatusVisibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
