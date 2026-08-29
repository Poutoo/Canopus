using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class GameSessionViewModel : ViewModelBase
{
    // Honest limits worth surfacing rather than letting "Actif" overclaim what a
    // tweak actually guarantees -- same treatment as the audit's DetailNote.
    // Keyed by IReversibleTweak.Name (stable identifier, not localized) -> translation key.
    private static readonly Dictionary<string, string> TweakNoteKeys = new()
    {
        ["Précision du pointeur"] = "GameSession.Notes.MousePrecision",
        ["Suspension sélective USB"] = "GameSession.Notes.UsbSuspend"
    };

    private readonly ISettingsService _settingsService;
    private readonly IReadOnlyList<IReversibleTweak> _allTweaks;

    private GameSessionService? _sessionService;
    private AppSettings _settings = new();

    public GameSessionViewModel()
    {
        _settingsService = new JsonSettingsService();
        _allTweaks = GameSessionService.CreateDefaultTweaks();

        _tweakStatuses = _allTweaks.Select(IdleItem).ToList();
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
    public IReadOnlyList<TweakStatusDisplayItem> TweakStatuses { get => _tweakStatuses; private set => SetProperty(ref _tweakStatuses, value); }

    private string _feedbackMessage = Strings.Get("GameSession.DefaultFeedback");
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }

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

        TweakStatuses = _allTweaks
            .Select(t => !activeTweaks.Contains(t)
                ? ExcludedItem(t)
                : outcomes.FirstOrDefault(o => o.TweakName == t.Name) is { } outcome
                    ? (outcome.Succeeded ? ActiveItem(t) : FailedItem(t, outcome.FailureReason))
                    : FailedItem(t, null))
            .ToList();

        IsSessionActive = true;
        ToggleButtonText = Strings.Get("GameSession.StopButton");

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

        RefreshIdleStatuses();
        IsSessionActive = false;
        ToggleButtonText = Strings.Get("GameSession.StartButton");
        FeedbackMessage = Strings.Get("GameSession.Feedback.Stopped");
    }

    private void RefreshIdleStatuses()
    {
        if (IsSessionActive)
            return;

        TweakStatuses = _allTweaks.Select(IdleItem).ToList();
    }

    private TweakStatusDisplayItem IdleItem(IReversibleTweak tweak) =>
        Build(tweak, Strings.Get("GameSession.Status.Idle"), GetBrush("TextTertiaryBrush"), GetBrush("StatusNeutralBgBrush"));

    private TweakStatusDisplayItem ActiveItem(IReversibleTweak tweak) =>
        Build(tweak, Strings.Get("GameSession.Status.Active"), GetBrush("StatusGoodTextBrush"), GetBrush("StatusGoodBgBrush"));

    private TweakStatusDisplayItem FailedItem(IReversibleTweak tweak, string? failureReason) =>
        Build(tweak, Strings.Get("GameSession.Status.Failed"), GetBrush("StatusBadTextBrush"), GetBrush("StatusBadBgBrush"), failureReason);

    private TweakStatusDisplayItem ExcludedItem(IReversibleTweak tweak) =>
        Build(tweak, Strings.Get("GameSession.Status.Excluded"), GetBrush("StatusNeutralTextBrush"), GetBrush("StatusNeutralBgBrush"));

    private TweakStatusDisplayItem Build(IReversibleTweak tweak, string statusLabel, Brush statusTextBrush, Brush statusBgBrush, string? noteOverride = null)
    {
        string note = noteOverride
            ?? (TweakNoteKeys.TryGetValue(tweak.Name, out string? noteKey) ? Strings.Get(noteKey) : string.Empty);
        bool isMouseTweak = tweak is MousePrecisionTweak;

        return new TweakStatusDisplayItem(tweak.DisplayName, statusLabel, statusTextBrush, statusBgBrush, note,
            string.IsNullOrEmpty(note) ? Visibility.Collapsed : Visibility.Visible,
            isMouseTweak ? Visibility.Visible : Visibility.Collapsed,
            MousePrecisionTweakEnabled,
            !IsSessionActive);
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
