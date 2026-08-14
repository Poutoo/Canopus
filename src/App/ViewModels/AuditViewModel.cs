using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

public sealed class AuditViewModel : INotifyPropertyChanged
{
    private readonly IAuditService _auditService;

    public AuditViewModel(IAuditService auditService)
    {
        _auditService = auditService;
        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private IReadOnlyList<AuditDisplayItem> _items = [];
    public IReadOnlyList<AuditDisplayItem> Items { get => _items; private set => SetProperty(ref _items, value); }

    private bool _isRunning;
    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

    private Visibility _runningVisibility = Visibility.Visible;
    public Visibility RunningVisibility { get => _runningVisibility; private set => SetProperty(ref _runningVisibility, value); }

    public async Task RefreshAsync()
    {
        IsRunning = true;
        RunningVisibility = Visibility.Visible;

        IReadOnlyList<AuditItem> items = await _auditService.RunAuditAsync();
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

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
