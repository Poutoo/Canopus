using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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

    private IReadOnlyList<AuditDisplayItem> _items = [];
    public IReadOnlyList<AuditDisplayItem> Items { get => _items; private set => SetProperty(ref _items, value); }

    private bool _isRunning;
    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

    private Visibility _runningVisibility = Visibility.Visible;
    public Visibility RunningVisibility { get => _runningVisibility; private set => SetProperty(ref _runningVisibility, value); }

    // Forces a fresh detection pass -- used when the user explicitly navigates to
    // this screen, so they always see current data rather than a stale cache.
    public Task RefreshAsync() => LoadAsync(forceRefresh: true);

    private async Task LoadAsync(bool forceRefresh)
    {
        IsRunning = true;
        RunningVisibility = Visibility.Visible;

        IReadOnlyList<AuditItem> items = forceRefresh
            ? await _auditService.RunAuditAsync()
            : await _auditService.GetOrRunAuditAsync();
        Items = items.Select(ToDisplayItem).ToList();

        IsRunning = false;
        RunningVisibility = Visibility.Collapsed;
    }

    private static AuditDisplayItem ToDisplayItem(AuditItem item) => new(
        item.Title,
        item.StatusLabel,
        GetBrush(item.Status switch
        {
            AuditStatus.Confirmed => "StatusGoodTextBrush",
            AuditStatus.Warning => "StatusWarnTextBrush",
            AuditStatus.Problem => "StatusBadTextBrush",
            _ => "StatusNeutralTextBrush"
        }),
        GetBrush(item.Status switch
        {
            AuditStatus.Confirmed => "StatusGoodBgBrush",
            AuditStatus.Warning => "StatusWarnBgBrush",
            AuditStatus.Problem => "StatusBadBgBrush",
            _ => "StatusNeutralBgBrush"
        }),
        item.Description,
        item.DetailNote ?? string.Empty,
        string.IsNullOrWhiteSpace(item.DetailNote) ? Visibility.Collapsed : Visibility.Visible);

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
