using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class GameSessionViewModel : ViewModelBase
{
    // Honest limits worth surfacing rather than letting "Actif" overclaim what a
    // tweak actually guarantees -- same treatment as the audit's DetailNote.
    private static readonly Dictionary<string, string> TweakNotes = new()
    {
        ["Précision du pointeur"] =
            "Désactive l'accélération souris — les mouvements rapides peuvent sembler moins réactifs, c'est normal et réversible à l'arrêt de la session. "
            + "Peut aussi être repris par un logiciel tiers (souris gaming, pilote fabricant, etc.) — non garanti pendant toute la session.",
        ["Suspension sélective USB"] =
            "Cible le plan d'alimentation actif avant le démarrage de la session. Si le plan d'alimentation bascule vers un plan de performance, ce réglage ne s'applique pas au nouveau plan actif."
    };

    private readonly ISettingsService _settingsService;
    private readonly IReadOnlyList<IReversibleTweak> _allTweaks;

    private GameSessionService? _sessionService;

    public GameSessionViewModel()
    {
        _settingsService = new JsonSettingsService();
        _allTweaks = GameSessionService.CreateDefaultTweaks();

        _tweakStatuses = _allTweaks.Select(t => IdleItem(t.Name)).ToList();
        _ = LoadSettingsAsync();
    }

    private bool _isSessionActive;
    public bool IsSessionActive { get => _isSessionActive; private set => SetProperty(ref _isSessionActive, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    private bool _canToggle = true;
    public bool CanToggle { get => _canToggle; private set => SetProperty(ref _canToggle, value); }

    private string _toggleButtonText = "Démarrer la session";
    public string ToggleButtonText { get => _toggleButtonText; private set => SetProperty(ref _toggleButtonText, value); }

    private IReadOnlyList<TweakStatusDisplayItem> _tweakStatuses;
    public IReadOnlyList<TweakStatusDisplayItem> TweakStatuses { get => _tweakStatuses; private set => SetProperty(ref _tweakStatuses, value); }

    private string _feedbackMessage = "Applique les réglages ci-dessus et les restaure automatiquement à l'arrêt.";
    public string FeedbackMessage { get => _feedbackMessage; private set => SetProperty(ref _feedbackMessage, value); }

    // Excluding a tweak only takes effect on the next session start, so the
    // checkbox is locked while one is already running -- flipping it mid-session
    // wouldn't retroactively change what was already captured/applied.
    private bool _mousePrecisionTweakEnabled = true;
    public bool MousePrecisionTweakEnabled { get => _mousePrecisionTweakEnabled; private set => SetProperty(ref _mousePrecisionTweakEnabled, value); }

    private async Task LoadSettingsAsync()
    {
        AppSettings settings = await _settingsService.LoadAsync();
        MousePrecisionTweakEnabled = settings.MousePrecisionTweakEnabled;
        RefreshIdleStatuses();
    }

    public async Task SetMousePrecisionTweakEnabledAsync(bool enabled)
    {
        if (IsSessionActive || enabled == MousePrecisionTweakEnabled)
            return;

        MousePrecisionTweakEnabled = enabled;
        await _settingsService.SaveAsync(new AppSettings(enabled));
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
                ? ExcludedItem(t.Name)
                : outcomes.FirstOrDefault(o => o.TweakName == t.Name) is { } outcome
                    ? (outcome.Succeeded ? ActiveItem(t.Name) : FailedItem(t.Name, outcome.FailureReason))
                    : FailedItem(t.Name, null))
            .ToList();

        IsSessionActive = true;
        ToggleButtonText = "Arrêter la session";

        int failedCount = outcomes.Count(o => !o.Succeeded);
        int excludedCount = _allTweaks.Count - activeTweaks.Count;
        FeedbackMessage = (failedCount, excludedCount) switch
        {
            (0, 0) => "Session démarrée : les 3 réglages sont actifs.",
            (0, > 0) => $"Session démarrée : {activeTweaks.Count} réglage(s) actif(s), {excludedCount} exclu(s) volontairement.",
            _ => $"Session démarrée avec {failedCount} échec(s) — les autres réglages restent actifs."
        };
    }

    private async Task StopAsync()
    {
        if (_sessionService is not null)
            await _sessionService.StopSessionAsync();

        RefreshIdleStatuses();
        IsSessionActive = false;
        ToggleButtonText = "Démarrer la session";
        FeedbackMessage = "Session arrêtée, réglages d'origine restaurés.";
    }

    private void RefreshIdleStatuses()
    {
        if (IsSessionActive)
            return;

        TweakStatuses = _allTweaks.Select(t => IdleItem(t.Name)).ToList();
    }

    private TweakStatusDisplayItem IdleItem(string name) =>
        Build(name, "Inactif", GetBrush("TextTertiaryBrush"), GetBrush("StatusNeutralBgBrush"));

    private TweakStatusDisplayItem ActiveItem(string name) =>
        Build(name, "Actif", GetBrush("StatusGoodTextBrush"), GetBrush("StatusGoodBgBrush"));

    private TweakStatusDisplayItem FailedItem(string name, string? failureReason) =>
        Build(name, "Échec", GetBrush("StatusBadTextBrush"), GetBrush("StatusBadBgBrush"), failureReason);

    private TweakStatusDisplayItem ExcludedItem(string name) =>
        Build(name, "Exclu", GetBrush("StatusNeutralTextBrush"), GetBrush("StatusNeutralBgBrush"));

    private TweakStatusDisplayItem Build(string name, string statusLabel, Brush statusTextBrush, Brush statusBgBrush, string? noteOverride = null)
    {
        string note = noteOverride ?? TweakNotes.GetValueOrDefault(name, string.Empty);
        bool isMouseTweak = name == "Précision du pointeur";

        return new TweakStatusDisplayItem(name, statusLabel, statusTextBrush, statusBgBrush, note,
            string.IsNullOrEmpty(note) ? Visibility.Collapsed : Visibility.Visible,
            isMouseTweak ? Visibility.Visible : Visibility.Collapsed,
            !MousePrecisionTweakEnabled,
            !IsSessionActive);
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
