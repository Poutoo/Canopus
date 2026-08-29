using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

/// <summary>
/// Alimente le dashboard avec des données réelles, rafraîchies périodiquement
/// via un <see cref="DispatcherTimer"/> (voir <see cref="TickIntervalSeconds"/>).
/// Les seuils de classification (température, RAM, latence, gigue) sont des
/// valeurs raisonnables par défaut, pas des seuils produit validés.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private const double TickIntervalSeconds = 1.5;

    // Échelle du thermomètre : 0-100°C (proche de la plage de throttling
    // ~90-100°C mentionnée pour les cartes de température).
    private const double ThermometerMaxCelsius = 100.0;

    private enum StatusTier { Good, Warn, Bad }

    private readonly IHardwareMonitorService _hardwareMonitorService;
    private readonly IStorageService _storageService;
    private readonly INetworkService _networkService;
    private readonly IProcessMonitorService _processMonitorService;
    private readonly IAuditService _auditService;
    private readonly DispatcherTimer _timer;

    public DashboardViewModel(
        IHardwareMonitorService hardwareMonitorService,
        IStorageService storageService,
        INetworkService networkService,
        IProcessMonitorService processMonitorService,
        IAuditService auditService)
    {
        _hardwareMonitorService = hardwareMonitorService;
        _storageService = storageService;
        _networkService = networkService;
        _processMonitorService = processMonitorService;
        _auditService = auditService;

        _auditIconBackgroundBrush = GetBrush("StatusNeutralBgBrush");
        _auditIconForegroundBrush = GetBrush("StatusNeutralTextBrush");

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(TickIntervalSeconds) };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        // Refresh once immediately instead of waiting for the first tick.
        _ = RefreshAsync();

        // The audit is far heavier than the sensors, so it runs once in the
        // background rather than on every timer tick.
        _ = RefreshAuditSummaryAsync();
    }

    private async Task RefreshAsync()
    {
        HardwareSnapshot hardware = _hardwareMonitorService.GetSnapshot();
        IReadOnlyList<DriveSnapshot> drives = _storageService.GetSnapshot();
        NetworkSnapshot network = await _networkService.GetSnapshotAsync();
        IReadOnlyList<ProcessSnapshot> processes = _processMonitorService.GetTopProcesses();

        ApplyTemperatures(hardware);
        ApplySystemLoad(hardware);
        ApplyDrives(drives);
        ApplyNetwork(network);
        ApplyProcesses(processes);
    }

    // ------------------------------------------------------------------
    // Card Températures
    // ------------------------------------------------------------------

    private string _cpuTemperatureText = "—";
    public string CpuTemperatureText { get => _cpuTemperatureText; private set => SetProperty(ref _cpuTemperatureText, value); }

    private Brush? _cpuTemperatureBrush;
    public Brush? CpuTemperatureBrush { get => _cpuTemperatureBrush; private set => SetProperty(ref _cpuTemperatureBrush, value); }

    private string _cpuFrequencyText = string.Empty;
    public string CpuFrequencyText { get => _cpuFrequencyText; private set => SetProperty(ref _cpuFrequencyText, value); }

    private Visibility _cpuFrequencyVisibility = Visibility.Collapsed;
    public Visibility CpuFrequencyVisibility { get => _cpuFrequencyVisibility; private set => SetProperty(ref _cpuFrequencyVisibility, value); }

    private string _cpuStatusLabel = string.Empty;
    public string CpuStatusLabel { get => _cpuStatusLabel; private set => SetProperty(ref _cpuStatusLabel, value); }

    private Visibility _cpuStatusVisibility = Visibility.Collapsed;
    public Visibility CpuStatusVisibility { get => _cpuStatusVisibility; private set => SetProperty(ref _cpuStatusVisibility, value); }

    private Brush? _cpuStatusTextBrush;
    public Brush? CpuStatusTextBrush { get => _cpuStatusTextBrush; private set => SetProperty(ref _cpuStatusTextBrush, value); }

    private Brush? _cpuStatusBgBrush;
    public Brush? CpuStatusBgBrush { get => _cpuStatusBgBrush; private set => SetProperty(ref _cpuStatusBgBrush, value); }

    private Brush? _cpuThermometerBrush;
    public Brush? CpuThermometerBrush { get => _cpuThermometerBrush; private set => SetProperty(ref _cpuThermometerBrush, value); }

    private GridLength _cpuThermometerFilledRow = new(0, GridUnitType.Star);
    public GridLength CpuThermometerFilledRow { get => _cpuThermometerFilledRow; private set => SetProperty(ref _cpuThermometerFilledRow, value); }

    private GridLength _cpuThermometerEmptyRow = new(1, GridUnitType.Star);
    public GridLength CpuThermometerEmptyRow { get => _cpuThermometerEmptyRow; private set => SetProperty(ref _cpuThermometerEmptyRow, value); }

    private string _gpuTemperatureText = "—";
    public string GpuTemperatureText { get => _gpuTemperatureText; private set => SetProperty(ref _gpuTemperatureText, value); }

    private Brush? _gpuTemperatureBrush;
    public Brush? GpuTemperatureBrush { get => _gpuTemperatureBrush; private set => SetProperty(ref _gpuTemperatureBrush, value); }

    private string _gpuFrequencyText = string.Empty;
    public string GpuFrequencyText { get => _gpuFrequencyText; private set => SetProperty(ref _gpuFrequencyText, value); }

    private Visibility _gpuFrequencyVisibility = Visibility.Collapsed;
    public Visibility GpuFrequencyVisibility { get => _gpuFrequencyVisibility; private set => SetProperty(ref _gpuFrequencyVisibility, value); }

    private string _gpuStatusLabel = string.Empty;
    public string GpuStatusLabel { get => _gpuStatusLabel; private set => SetProperty(ref _gpuStatusLabel, value); }

    private Visibility _gpuStatusVisibility = Visibility.Collapsed;
    public Visibility GpuStatusVisibility { get => _gpuStatusVisibility; private set => SetProperty(ref _gpuStatusVisibility, value); }

    private Brush? _gpuStatusTextBrush;
    public Brush? GpuStatusTextBrush { get => _gpuStatusTextBrush; private set => SetProperty(ref _gpuStatusTextBrush, value); }

    private Brush? _gpuStatusBgBrush;
    public Brush? GpuStatusBgBrush { get => _gpuStatusBgBrush; private set => SetProperty(ref _gpuStatusBgBrush, value); }

    private Brush? _gpuThermometerBrush;
    public Brush? GpuThermometerBrush { get => _gpuThermometerBrush; private set => SetProperty(ref _gpuThermometerBrush, value); }

    private GridLength _gpuThermometerFilledRow = new(0, GridUnitType.Star);
    public GridLength GpuThermometerFilledRow { get => _gpuThermometerFilledRow; private set => SetProperty(ref _gpuThermometerFilledRow, value); }

    private GridLength _gpuThermometerEmptyRow = new(1, GridUnitType.Star);
    public GridLength GpuThermometerEmptyRow { get => _gpuThermometerEmptyRow; private set => SetProperty(ref _gpuThermometerEmptyRow, value); }

    private void ApplyTemperatures(HardwareSnapshot hardware)
    {
        (CpuTemperatureText, CpuTemperatureBrush, CpuStatusLabel, CpuStatusVisibility,
            CpuStatusTextBrush, CpuStatusBgBrush, CpuThermometerBrush, CpuThermometerFilledRow, CpuThermometerEmptyRow) =
            BuildTemperatureDisplay(hardware.CpuTemperatureCelsius);

        (CpuFrequencyText, CpuFrequencyVisibility) = BuildFrequencyDisplay(hardware.CpuFrequencyMhz);

        (GpuTemperatureText, GpuTemperatureBrush, GpuStatusLabel, GpuStatusVisibility,
            GpuStatusTextBrush, GpuStatusBgBrush, GpuThermometerBrush, GpuThermometerFilledRow, GpuThermometerEmptyRow) =
            BuildTemperatureDisplay(hardware.GpuTemperatureCelsius);

        (GpuFrequencyText, GpuFrequencyVisibility) = BuildFrequencyDisplay(hardware.GpuFrequencyMhz);
    }

    private static (string text, Brush brush, string statusLabel, Visibility statusVisibility,
        Brush statusTextBrush, Brush statusBgBrush, Brush thermBrush, GridLength filledRow, GridLength emptyRow)
        BuildTemperatureDisplay(double? celsius)
    {
        if (celsius is null)
        {
            var (filled, empty) = ComputeFillRatio(0);
            return ("—", GetBrush("TextDisabledBrush"), string.Empty, Visibility.Collapsed,
                GetBrush("TextDisabledBrush"), GetBrush("TextDisabledBrush"), GetBrush("TextDisabledBrush"), filled, empty);
        }

        StatusTier tier = celsius >= 90 ? StatusTier.Bad : celsius >= 75 ? StatusTier.Warn : StatusTier.Good;
        string label = tier switch
        {
            StatusTier.Bad => Strings.Get("Dashboard.Status.Critical"),
            StatusTier.Warn => Strings.Get("Dashboard.Status.High"),
            _ => Strings.Get("Dashboard.Status.Stable")
        };
        var (filledRow, emptyRow) = ComputeFillRatio(celsius.Value / ThermometerMaxCelsius);

        return (
            $"{celsius:F0}°C",
            GetStatusTextBrush(tier),
            label,
            Visibility.Visible,
            GetStatusTextBrush(tier),
            GetStatusBgBrush(tier),
            GetStatusMidBrush(tier),
            filledRow,
            emptyRow);
    }

    private static (string text, Visibility visibility) BuildFrequencyDisplay(double? megahertz)
    {
        if (megahertz is null)
            return (string.Empty, Visibility.Collapsed);

        return ($"{megahertz.Value / 1000.0:F1} GHz", Visibility.Visible);
    }

    // ------------------------------------------------------------------
    // Card Charge système
    // ------------------------------------------------------------------

    private string _ramText = "—";
    public string RamText { get => _ramText; private set => SetProperty(ref _ramText, value); }

    private Brush? _ramBrush;
    public Brush? RamBrush { get => _ramBrush; private set => SetProperty(ref _ramBrush, value); }

    private string _cpuLoadText = "—";
    public string CpuLoadText { get => _cpuLoadText; private set => SetProperty(ref _cpuLoadText, value); }

    private GridLength _cpuLoadFilledColumn = new(0, GridUnitType.Star);
    public GridLength CpuLoadFilledColumn { get => _cpuLoadFilledColumn; private set => SetProperty(ref _cpuLoadFilledColumn, value); }

    private GridLength _cpuLoadEmptyColumn = new(1, GridUnitType.Star);
    public GridLength CpuLoadEmptyColumn { get => _cpuLoadEmptyColumn; private set => SetProperty(ref _cpuLoadEmptyColumn, value); }

    private string _gpuLoadText = "—";
    public string GpuLoadText { get => _gpuLoadText; private set => SetProperty(ref _gpuLoadText, value); }

    private GridLength _gpuLoadFilledColumn = new(0, GridUnitType.Star);
    public GridLength GpuLoadFilledColumn { get => _gpuLoadFilledColumn; private set => SetProperty(ref _gpuLoadFilledColumn, value); }

    private GridLength _gpuLoadEmptyColumn = new(1, GridUnitType.Star);
    public GridLength GpuLoadEmptyColumn { get => _gpuLoadEmptyColumn; private set => SetProperty(ref _gpuLoadEmptyColumn, value); }

    private void ApplySystemLoad(HardwareSnapshot hardware)
    {
        if (hardware.MemoryUsedPercent is double memPercent)
        {
            StatusTier tier = memPercent >= 90 ? StatusTier.Bad : memPercent >= 70 ? StatusTier.Warn : StatusTier.Good;
            RamBrush = GetStatusTextBrush(tier);
            RamText = hardware.MemoryAvailableGigabytes is double availableGb && hardware.MemoryUsedGigabytes is double usedGb
                ? $"{usedGb:F1} / {usedGb + availableGb:F1} Go"
                : $"{memPercent:F0} %";
        }
        else
        {
            RamBrush = GetBrush("TextDisabledBrush");
            RamText = "—";
        }

        (CpuLoadText, CpuLoadFilledColumn, CpuLoadEmptyColumn) = BuildLoadDisplay(hardware.CpuLoadPercent);
        (GpuLoadText, GpuLoadFilledColumn, GpuLoadEmptyColumn) = BuildLoadDisplay(hardware.GpuLoadPercent);
    }

    private static (string text, GridLength filled, GridLength empty) BuildLoadDisplay(double? percent)
    {
        if (percent is null)
        {
            var (filled, empty) = ComputeFillRatio(0);
            return ("—", filled, empty);
        }

        var (filledCol, emptyCol) = ComputeFillRatio(percent.Value / 100.0);
        return ($"{percent:F0} %", filledCol, emptyCol);
    }

    // ------------------------------------------------------------------
    // Card Stockage
    // ------------------------------------------------------------------

    private IReadOnlyList<DriveDisplayItem> _drives = [];
    public IReadOnlyList<DriveDisplayItem> Drives { get => _drives; private set => SetProperty(ref _drives, value); }

    private void ApplyDrives(IReadOnlyList<DriveSnapshot> drives)
    {
        Drives = drives
            .Select(d =>
            {
                var (filled, empty) = ComputeFillRatio(d.TotalGigabytes > 0 ? d.UsedGigabytes / d.TotalGigabytes : 0);
                return new DriveDisplayItem(d.Name, $"{d.UsedGigabytes:F0} / {d.TotalGigabytes:F0} Go", filled, empty);
            })
            .ToList();
    }

    // ------------------------------------------------------------------
    // Card Réseau
    // ------------------------------------------------------------------

    private string _latencyText = "—";
    public string LatencyText { get => _latencyText; private set => SetProperty(ref _latencyText, value); }

    private string _jitterText = "—";
    public string JitterText { get => _jitterText; private set => SetProperty(ref _jitterText, value); }

    private Brush? _latencyBar1Brush, _latencyBar2Brush, _latencyBar3Brush, _latencyBar4Brush, _latencyBar5Brush;
    public Brush? LatencyBar1Brush { get => _latencyBar1Brush; private set => SetProperty(ref _latencyBar1Brush, value); }
    public Brush? LatencyBar2Brush { get => _latencyBar2Brush; private set => SetProperty(ref _latencyBar2Brush, value); }
    public Brush? LatencyBar3Brush { get => _latencyBar3Brush; private set => SetProperty(ref _latencyBar3Brush, value); }
    public Brush? LatencyBar4Brush { get => _latencyBar4Brush; private set => SetProperty(ref _latencyBar4Brush, value); }
    public Brush? LatencyBar5Brush { get => _latencyBar5Brush; private set => SetProperty(ref _latencyBar5Brush, value); }

    private Brush? _jitterBar1Brush, _jitterBar2Brush, _jitterBar3Brush, _jitterBar4Brush, _jitterBar5Brush;
    public Brush? JitterBar1Brush { get => _jitterBar1Brush; private set => SetProperty(ref _jitterBar1Brush, value); }
    public Brush? JitterBar2Brush { get => _jitterBar2Brush; private set => SetProperty(ref _jitterBar2Brush, value); }
    public Brush? JitterBar3Brush { get => _jitterBar3Brush; private set => SetProperty(ref _jitterBar3Brush, value); }
    public Brush? JitterBar4Brush { get => _jitterBar4Brush; private set => SetProperty(ref _jitterBar4Brush, value); }
    public Brush? JitterBar5Brush { get => _jitterBar5Brush; private set => SetProperty(ref _jitterBar5Brush, value); }

    private void ApplyNetwork(NetworkSnapshot network)
    {
        LatencyText = network.LatencyMs is double latency ? $"{latency:F0} ms" : "—";
        JitterText = network.JitterMs is double jitter ? $"{jitter:F1} ms" : "—";

        // Seuils approximatifs (pas de spec produit validée) : latence et gigue
        // "correctes" pour une connexion domestique typique.
        int latencyBars = network.LatencyMs switch
        {
            null => 0,
            <= 20 => 5,
            <= 50 => 4,
            <= 80 => 3,
            <= 150 => 2,
            _ => 1
        };
        StatusTier latencyTier = network.LatencyMs switch
        {
            null => StatusTier.Good,
            <= 50 => StatusTier.Good,
            <= 100 => StatusTier.Warn,
            _ => StatusTier.Bad
        };

        int jitterBars = network.JitterMs switch
        {
            null => 0,
            <= 5 => 5,
            <= 15 => 4,
            <= 30 => 3,
            <= 50 => 2,
            _ => 1
        };
        StatusTier jitterTier = network.JitterMs switch
        {
            null => StatusTier.Good,
            <= 15 => StatusTier.Good,
            <= 30 => StatusTier.Warn,
            _ => StatusTier.Bad
        };

        Brush litLatency = GetStatusMidBrush(latencyTier);
        Brush unlit = GetBrush("TextDisabledBrush");
        LatencyBar1Brush = 1 <= latencyBars ? litLatency : unlit;
        LatencyBar2Brush = 2 <= latencyBars ? litLatency : unlit;
        LatencyBar3Brush = 3 <= latencyBars ? litLatency : unlit;
        LatencyBar4Brush = 4 <= latencyBars ? litLatency : unlit;
        LatencyBar5Brush = 5 <= latencyBars ? litLatency : unlit;

        Brush litJitter = GetStatusMidBrush(jitterTier);
        JitterBar1Brush = 1 <= jitterBars ? litJitter : unlit;
        JitterBar2Brush = 2 <= jitterBars ? litJitter : unlit;
        JitterBar3Brush = 3 <= jitterBars ? litJitter : unlit;
        JitterBar4Brush = 4 <= jitterBars ? litJitter : unlit;
        JitterBar5Brush = 5 <= jitterBars ? litJitter : unlit;
    }

    // ------------------------------------------------------------------
    // Card Top processus
    // ------------------------------------------------------------------

    private IReadOnlyList<ProcessDisplayItem> _topProcesses = [];
    public IReadOnlyList<ProcessDisplayItem> TopProcesses { get => _topProcesses; private set => SetProperty(ref _topProcesses, value); }

    private void ApplyProcesses(IReadOnlyList<ProcessSnapshot> processes)
    {
        TopProcesses = processes
            .Select(p => new ProcessDisplayItem(p.Name, $"{p.CpuPercent:F0} %", $"{p.MemoryMegabytes:F0} Mo"))
            .ToList();
    }

    // ------------------------------------------------------------------
    // Audit CTA card
    // ------------------------------------------------------------------

    private const string WarningGlyph = "\uE7BA";
    private const string OkGlyph = "\uE73E";

    private string _auditSummaryText = Strings.Get("Dashboard.AuditSummary.Loading");
    public string AuditSummaryText { get => _auditSummaryText; private set => SetProperty(ref _auditSummaryText, value); }

    private string _auditIconGlyph = WarningGlyph;
    public string AuditIconGlyph { get => _auditIconGlyph; private set => SetProperty(ref _auditIconGlyph, value); }

    private Brush? _auditIconBackgroundBrush;
    public Brush? AuditIconBackgroundBrush { get => _auditIconBackgroundBrush; private set => SetProperty(ref _auditIconBackgroundBrush, value); }

    private Brush? _auditIconForegroundBrush;
    public Brush? AuditIconForegroundBrush { get => _auditIconForegroundBrush; private set => SetProperty(ref _auditIconForegroundBrush, value); }

    public async Task RefreshAuditSummaryAsync()
    {
        IReadOnlyList<AuditItem> items = await _auditService.GetOrRunAuditAsync();
        int toCheck = items.Count(i => i.Status is AuditStatus.Warning or AuditStatus.Problem);

        if (toCheck == 0)
        {
            AuditSummaryText = Strings.Get("Dashboard.AuditSummary.AllGood");
            AuditIconGlyph = OkGlyph;
            AuditIconBackgroundBrush = GetBrush("StatusGoodBgBrush");
            AuditIconForegroundBrush = GetBrush("StatusGoodTextBrush");
            return;
        }

        AuditSummaryText = toCheck == 1
            ? Strings.Get("Dashboard.AuditSummary.Singular")
            : Strings.Format("Dashboard.AuditSummary.Plural", toCheck);
        AuditIconGlyph = WarningGlyph;
        AuditIconBackgroundBrush = GetBrush("StatusWarnBgBrush");
        AuditIconForegroundBrush = GetBrush("StatusWarnTextBrush");
    }

    // ------------------------------------------------------------------
    // Utilitaires partagés
    // ------------------------------------------------------------------

    private static (GridLength filled, GridLength empty) ComputeFillRatio(double fraction)
    {
        fraction = Math.Clamp(fraction, 0.0, 1.0);
        return (new GridLength(fraction, GridUnitType.Star), new GridLength(1 - fraction, GridUnitType.Star));
    }

    private static Brush GetStatusTextBrush(StatusTier tier) => GetBrush(tier switch
    {
        StatusTier.Bad => "StatusBadTextBrush",
        StatusTier.Warn => "StatusWarnTextBrush",
        _ => "StatusGoodTextBrush"
    });

    private static Brush GetStatusMidBrush(StatusTier tier) => GetBrush(tier switch
    {
        StatusTier.Bad => "StatusBadMidBrush",
        StatusTier.Warn => "StatusWarnMidBrush",
        _ => "StatusGoodMidBrush"
    });

    private static Brush GetStatusBgBrush(StatusTier tier) => GetBrush(tier switch
    {
        StatusTier.Bad => "StatusBadBgBrush",
        StatusTier.Warn => "StatusWarnBgBrush",
        _ => "StatusGoodBgBrush"
    });

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];

    public void Dispose() => _timer.Stop();
}
