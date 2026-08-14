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
            "Peut être repris par un logiciel tiers (souris gaming, pilote fabricant, etc.) — non garanti pendant toute la session.",
        ["Suspension sélective USB"] =
            "Cible le plan d'alimentation actif avant le démarrage de la session. Si le plan d'alimentation bascule vers un plan de performance, ce réglage ne s'applique pas au nouveau plan actif."
    };

    private readonly GameSessionService _sessionService;
    private readonly IReadOnlyList<IReversibleTweak> _tweaks;

    public GameSessionViewModel()
    {
        _tweaks = GameSessionService.CreateDefaultTweaks();
        _sessionService = new GameSessionService(_tweaks);

        _tweakStatuses = _tweaks.Select(t => IdleItem(t.Name)).ToList();
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
        IReadOnlyList<TweakOutcome> outcomes = await _sessionService.StartSessionAsync();

        TweakStatuses = _tweaks
            .Select(t => outcomes.FirstOrDefault(o => o.TweakName == t.Name) is { } outcome
                ? (outcome.Succeeded ? ActiveItem(t.Name) : FailedItem(t.Name, outcome.FailureReason))
                : FailedItem(t.Name, null))
            .ToList();

        IsSessionActive = true;
        ToggleButtonText = "Arrêter la session";

        int failedCount = outcomes.Count(o => !o.Succeeded);
        FeedbackMessage = failedCount == 0
            ? "Session démarrée : les 3 réglages sont actifs."
            : $"Session démarrée avec {failedCount} échec(s) — les autres réglages restent actifs.";
    }

    private async Task StopAsync()
    {
        await _sessionService.StopSessionAsync();

        TweakStatuses = _tweaks.Select(t => IdleItem(t.Name)).ToList();
        IsSessionActive = false;
        ToggleButtonText = "Démarrer la session";
        FeedbackMessage = "Session arrêtée, réglages d'origine restaurés.";
    }

    private static TweakStatusDisplayItem IdleItem(string name) =>
        Build(name, "Inactif", GetBrush("TextTertiaryBrush"), GetBrush("StatusNeutralBgBrush"));

    private static TweakStatusDisplayItem ActiveItem(string name) =>
        Build(name, "Actif", GetBrush("StatusGoodTextBrush"), GetBrush("StatusGoodBgBrush"));

    private static TweakStatusDisplayItem FailedItem(string name, string? failureReason) =>
        Build(name, "Échec", GetBrush("StatusBadTextBrush"), GetBrush("StatusBadBgBrush"), failureReason);

    private static TweakStatusDisplayItem Build(string name, string statusLabel, Brush statusTextBrush, Brush statusBgBrush, string? noteOverride = null)
    {
        string note = noteOverride ?? TweakNotes.GetValueOrDefault(name, string.Empty);
        return new TweakStatusDisplayItem(name, statusLabel, statusTextBrush, statusBgBrush, note,
            string.IsNullOrEmpty(note) ? Visibility.Collapsed : Visibility.Visible);
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
