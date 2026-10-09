using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class GameSessionViewModel : ViewModelBase
{
    // Keyed by IReversibleTweak.Name (stable identifier, not localized) -> translation key.
    private static readonly Dictionary<string, string> TweakDescriptionKeys = new()
    {
        ["Plan d'alimentation"] = "GameSession.Tweaks.PowerPlan",
        ["Précision du pointeur"] = "GameSession.Tweaks.MousePrecision",
        ["Suspension sélective USB"] = "GameSession.Tweaks.UsbSuspend"
    };

    private readonly ISettingsService _settingsService;
    private readonly IReadOnlyList<IReversibleTweak> _allTweaks;

    private GameSessionService? _sessionService;
    private AppSettings _settings = new();

    public GameSessionViewModel()
    {
        _settingsService = new JsonSettingsService();
        _allTweaks = GameSessionService.CreateDefaultTweaks();

        _tweakStatuses = BuildIdleItems();
        _heroSubtitle = IdleSubtitle();
        _feedbackBrush = GetBrush("TextTertiaryBrush");
        _ = LoadSettingsAsync();
    }

    private bool _isSessionActive;
    public bool IsSessionActive { get => _isSessionActive; private set => SetProperty(ref _isSessionActive, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    private bool _canToggle = true;
    public bool CanToggle { get => _canToggle; private set => SetProperty(ref _canToggle, value); }

    private string _toggleButtonText = Strings.Get("GameSession.StartButton");
    public string ToggleButtonText { get => _toggleButtonText; private set => SetProperty(ref _toggleButtonText, value); }

    private IReadOnlyList<TweakStatusDisplayItem> _tweakStatuses;
    public IReadOnlyList<TweakStatusDisplayItem> TweakStatuses { get => _tweakStatuses; private set => SetItems(ref _tweakStatuses, value); }

    private string _feedbackMessage = Strings.Get("GameSession.DefaultFeedback");
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }

    private Brush _feedbackBrush;
    public Brush FeedbackBrush { get => _feedbackBrush; private set => SetProperty(ref _feedbackBrush, value); }

    private string _heroTitle = Strings.Get("GameSession.Hero.IdleTitle");
    public string HeroTitle { get => _heroTitle; private set => SetProperty(ref _heroTitle, value); }

    private string _heroSubtitle;
    public string HeroSubtitle { get => _heroSubtitle; private set => SetProperty(ref _heroSubtitle, value); }

    // Excluding a tweak only takes effect on the next session start, so the
    // checkbox is locked while one is already running -- flipping it mid-session
    // wouldn't retroactively change what was already captured/applied.
    private bool _mousePrecisionTweakEnabled = true;
    public bool MousePrecisionTweakEnabled { get => _mousePrecisionTweakEnabled; private set => SetProperty(ref _mousePrecisionTweakEnabled, value); }

    // Public so MainWindow can re-trigger it each time this view is navigated to -- this
    // view instance is never recreated, so without this a change made on ParametresView
    // (same ISettingsService-backed value) wouldn't show up here until an app restart.
    public async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.LoadAsync();
        MousePrecisionTweakEnabled = _settings.MousePrecisionTweakEnabled;
        RefreshIdleStatuses();
    }

    public async Task SetMousePrecisionTweakEnabledAsync(bool enabled)
    {
        if (IsSessionActive || enabled == MousePrecisionTweakEnabled)
            return;

        MousePrecisionTweakEnabled = enabled;
        // `with`, not `new AppSettings(enabled)` -- a positional constructor call would silently
        // reset every other setting (e.g. MinimizeToTray) back to its default on each toggle here.
        _settings = _settings with { MousePrecisionTweakEnabled = enabled };
        await _settingsService.SaveAsync(_settings);
        RefreshIdleStatuses();
    }

    public async Task ToggleSessionAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        CanToggle = false;
        try
        {
            if (IsSessionActive)
                await StopAsync();
            else
                await StartAsync();
        }
        finally
        {
            IsBusy = false;
            CanToggle = true;
        }
    }

    private async Task StartAsync()
    {
        IReadOnlyList<IReversibleTweak> activeTweaks = MousePrecisionTweakEnabled
            ? _allTweaks
            : _allTweaks.Where(t => t is not MousePrecisionTweak).ToList();

        _sessionService = new GameSessionService(activeTweaks);
        IReadOnlyList<TweakOutcome> outcomes = await _sessionService.StartSessionAsync();

        IsSessionActive = true;
        TweakStatuses = _allTweaks
            .Select((t, i) => !activeTweaks.Contains(t)
                ? ExcludedItem(t, i)
                : outcomes.FirstOrDefault(o => o.TweakName == t.Name) is { } outcome
                    ? (outcome.Succeeded ? ActiveItem(t, i) : FailedItem(t, i, outcome.FailureReason))
                    : FailedItem(t, i, null))
            .ToList();

        ToggleButtonText = Strings.Get("GameSession.StopButton");
        HeroTitle = Strings.Get("GameSession.Hero.ActiveTitle");
        HeroSubtitle = Strings.Get("GameSession.Hero.ActiveSubtitle");
        FeedbackBrush = GetBrush("TextPrimaryBrush");

        int failedCount = outcomes.Count(o => !o.Succeeded);
        int excludedCount = _allTweaks.Count - activeTweaks.Count;
        FeedbackMessage = (failedCount, excludedCount) switch
        {
            (0, 0) => Strings.Get("GameSession.Feedback.AllActive"),
            (0, > 0) => Strings.Format("GameSession.Feedback.SomeExcluded", activeTweaks.Count, excludedCount),
            _ => Strings.Format("GameSession.Feedback.SomeFailed", failedCount)
        };
    }

    private async Task StopAsync()
    {
        if (_sessionService is not null)
            await _sessionService.StopSessionAsync();

        IsSessionActive = false;
        RefreshIdleStatuses();
        ToggleButtonText = Strings.Get("GameSession.StartButton");
        HeroTitle = Strings.Get("GameSession.Hero.IdleTitle");
        FeedbackMessage = Strings.Get("GameSession.Feedback.Stopped");
        FeedbackBrush = GetBrush("TextPrimaryBrush");
    }

    private void RefreshIdleStatuses()
    {
        if (IsSessionActive)
            return;

        TweakStatuses = BuildIdleItems();
        HeroSubtitle = IdleSubtitle();
    }

    private List<TweakStatusDisplayItem> BuildIdleItems() =>
        _allTweaks
            .Select((t, i) => t is MousePrecisionTweak && !MousePrecisionTweakEnabled ? ExcludedItem(t, i) : IdleItem(t, i))
            .ToList();

    private string IdleSubtitle() =>
        Strings.Format("GameSession.Hero.IdleSubtitle", _allTweaks.Count - (MousePrecisionTweakEnabled ? 0 : 1));

    private TweakStatusDisplayItem IdleItem(IReversibleTweak tweak, int index) =>
        Build(tweak, index, "GameSession.Status.Idle", "StatusIdleTextBrush", "TransparentBrush");

    private TweakStatusDisplayItem ActiveItem(IReversibleTweak tweak, int index) =>
        Build(tweak, index, "GameSession.Status.Active", "StatusActiveTextBrush", "StatusActiveBgBrush");

    private TweakStatusDisplayItem FailedItem(IReversibleTweak tweak, int index, string? failureReason) =>
        Build(tweak, index, "GameSession.Status.Failed", "StatusBadTextBrush", "StatusBadBgBrush",
            failureReason ?? Strings.Get("GameSession.Feedback.FailedFallback"));

    private TweakStatusDisplayItem ExcludedItem(IReversibleTweak tweak, int index) =>
        Build(tweak, index, "GameSession.Status.Excluded", "StatusExcludedTextBrush", "StatusExcludedBgBrush");

    private TweakStatusDisplayItem Build(IReversibleTweak tweak, int index, string statusKey,
        string statusTextBrushKey, string statusBgBrushKey, string? failureReason = null)
    {
        string description = TweakDescriptionKeys.TryGetValue(tweak.Name, out string? key) ? Strings.Get(key) : string.Empty;
        bool isMouseTweak = tweak is MousePrecisionTweak;

        return new TweakStatusDisplayItem(
            tweak.DisplayName,
            description,
            Strings.Get(statusKey),
            GetBrush(statusTextBrushKey),
            GetBrush(statusBgBrushKey),
            failureReason ?? string.Empty,
            failureReason is null ? Visibility.Collapsed : Visibility.Visible,
            index == 0 ? new Thickness(0) : new Thickness(0, 1, 0, 0),
            isMouseTweak ? Visibility.Visible : Visibility.Collapsed,
            !MousePrecisionTweakEnabled,
            !IsSessionActive);
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
