using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class AuditViewModel : ViewModelBase
{
    private readonly IAuditService _auditService;

    public AuditViewModel(IAuditService auditService)
    {
        _auditService = auditService;

        // Cache-or-run-once: the dashboard already triggers the first audit in the
        // background at startup, and AuditView is constructed eagerly (declared in
        // MainWindow.xaml) even while hidden -- forcing a fresh run here too would
        // sweep WMI twice concurrently for no reason.
        _ = LoadAsync(forceRefresh: false);
    }

    /// <summary>Warnings and problems: the main element of the screen.</summary>
    private IReadOnlyList<AuditDisplayItem> _attentionItems = [];
    public IReadOnlyList<AuditDisplayItem> AttentionItems { get => _attentionItems; private set => SetProperty(ref _attentionItems, value); }

    private IReadOnlyList<AuditDisplayItem> _confirmedItems = [];
    public IReadOnlyList<AuditDisplayItem> ConfirmedItems { get => _confirmedItems; private set => SetProperty(ref _confirmedItems, value); }

    private IReadOnlyList<AuditDisplayItem> _infoItems = [];
    public IReadOnlyList<AuditDisplayItem> InfoItems { get => _infoItems; private set => SetProperty(ref _infoItems, value); }

    private string _summaryText = Strings.Get("Audit.Running");
    public string SummaryText { get => _summaryText; private set => SetProperty(ref _summaryText, value); }

    private string _subtitleText = Strings.Get("Audit.Running");
    public string SubtitleText { get => _subtitleText; private set => SetProperty(ref _subtitleText, value); }

    private bool _canRerun;
    public bool CanRerun { get => _canRerun; private set => SetProperty(ref _canRerun, value); }

    private Visibility _infoVisibility = Visibility.Collapsed;
    public Visibility InfoVisibility { get => _infoVisibility; private set => SetProperty(ref _infoVisibility, value); }

    // Forces a fresh detection pass -- used when the user explicitly navigates to
    // this screen, so they always see current data rather than a stale cache.
    public Task RefreshAsync() => LoadAsync(forceRefresh: true);

    private async Task LoadAsync(bool forceRefresh)
    {
        CanRerun = false;
        SubtitleText = Strings.Get("Audit.Running");

        IReadOnlyList<AuditItem> items = forceRefresh
            ? await _auditService.RunAuditAsync()
            : await _auditService.GetOrRunAuditAsync();

        AttentionItems = items.Where(i => i.Status is AuditStatus.Warning or AuditStatus.Problem).Select(ToDisplayItem).ToList();
        ConfirmedItems = items.Where(i => i.Status == AuditStatus.Confirmed).Select(ToDisplayItem).ToList();
        InfoItems = items.Where(i => i.Status == AuditStatus.Info).Select(ToDisplayItem).ToList();
        InfoVisibility = InfoItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        SummaryText = AuditSummary.Text(AttentionItems.Count);
        SubtitleText = Strings.Format("Audit.Subtitle", items.Count);
        CanRerun = true;
    }

    private static AuditDisplayItem ToDisplayItem(AuditItem item) => new(
        item.Title,
        item.StatusLabel,
        GetBrush(item.Status switch
        {
            AuditStatus.Warning => "StatusWarnTextBrush",
            AuditStatus.Problem => "StatusBadTextBrush",
            AuditStatus.Info => "StatusInfoTextBrush",
            _ => "StatusNeutralTextBrush"
        }),
        GetBrush(item.Status switch
        {
            AuditStatus.Warning => "StatusWarnBgBrush",
            AuditStatus.Problem => "StatusBadBgBrush",
            _ => "TransparentBrush"
        }),
        item.Description,
        item.DetailNote ?? string.Empty,
        string.IsNullOrWhiteSpace(item.DetailNote) ? Visibility.Collapsed : Visibility.Visible);

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
