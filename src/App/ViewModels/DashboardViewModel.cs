using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Canopus.App.Localization;
using Canopus.App.Models;
using Canopus.App.Services;

namespace Canopus.App.ViewModels;

/// <summary>
/// Alimente le dashboard avec des données réelles, rafraîchies périodiquement
/// via un <see cref="DispatcherTimer"/> (voir <see cref="TickIntervalSeconds"/>).
/// Les seuils de classification (température) sont des valeurs raisonnables
/// par défaut, pas des seuils produit validés.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private const double TickIntervalSeconds = 1.5;
    private const string RamRowKey = "RAM";

    private enum StatusTier { Normal, Warn, Bad }

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

        NetworkFootnote = Strings.Format("Dashboard.Network.Footnote",
            PingNetworkService.TargetHost, Formats.Number(TickIntervalSeconds, 1));

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

        ApplyProcessors(hardware);
        ApplyMemory(hardware, drives);
        ApplyNetwork(network);
        ApplyProcesses(processes);
    }

    // ------------------------------------------------------------------
    // Élément principal : processeur et carte graphique
    // ------------------------------------------------------------------

    public ProcessorDisplay Cpu { get; } = new();
    public ProcessorDisplay Gpu { get; } = new();

    private void ApplyProcessors(HardwareSnapshot hardware)
    {
        Cpu.Apply(hardware.CpuTemperatureCelsius, hardware.CpuLoadPercent, hardware.CpuName,
            hardware.CpuFrequencyMhz is double cpuMhz ? Formats.Gigahertz(cpuMhz) : string.Empty);
        Gpu.Apply(hardware.GpuTemperatureCelsius, hardware.GpuLoadPercent, hardware.GpuName,
            hardware.GpuFrequencyMhz is double gpuMhz ? Formats.Megahertz(gpuMhz) : string.Empty);
    }

    // ------------------------------------------------------------------
    // Mémoire et disques
    // ------------------------------------------------------------------

    public ObservableCollection<UsageDisplayItem> MemoryRows { get; } = [];

    private void ApplyMemory(HardwareSnapshot hardware, IReadOnlyList<DriveSnapshot> drives)
    {
        var rows = new List<(string Key, string Label, string Value, double Fraction)>();

        if (hardware.MemoryUsedGigabytes is double usedGb && hardware.MemoryAvailableGigabytes is double availableGb)
        {
            double totalGb = usedGb + availableGb;
            rows.Add((RamRowKey, Strings.Get("Dashboard.Memory.Ram"), Formats.UsedOfTotal(usedGb, totalGb),
                totalGb > 0 ? usedGb / totalGb : 0));
        }
        else if (hardware.MemoryUsedPercent is double memPercent)
        {
            rows.Add((RamRowKey, Strings.Get("Dashboard.Memory.Ram"), Formats.Percent(memPercent), memPercent / 100));
        }

        foreach (DriveSnapshot drive in drives)
        {
            string label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? drive.Name
                : Strings.Format("Dashboard.Memory.Drive", drive.Name, drive.VolumeLabel);
            rows.Add((drive.Name, label, Formats.UsedOfTotal(drive.UsedGigabytes, drive.TotalGigabytes),
                drive.TotalGigabytes > 0 ? drive.UsedGigabytes / drive.TotalGigabytes : 0));
        }

        // Rows are updated in place (matched by key) so that bars animate rather than
        // being recreated on every tick.
        for (int i = MemoryRows.Count - 1; i >= 0; i--)
        {
            if (rows.All(r => r.Key != MemoryRows[i].Key))
                MemoryRows.RemoveAt(i);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var (key, label, value, fraction) = rows[i];
            UsageDisplayItem? item = MemoryRows.FirstOrDefault(r => r.Key == key);
            if (item is null)
            {
                item = new UsageDisplayItem(key);
                MemoryRows.Insert(Math.Min(i, MemoryRows.Count), item);
            }

            item.Label = label;
            item.ValueText = value;
            item.Fraction = Math.Clamp(fraction, 0, 1);
        }
    }

    // ------------------------------------------------------------------
    // Réseau
    // ------------------------------------------------------------------

    private string _latencyValue = Formats.Missing;
    public string LatencyValue { get => _latencyValue; private set => SetProperty(ref _latencyValue, value); }

    private string _jitterText = string.Empty;
    public string JitterText { get => _jitterText; private set => SetProperty(ref _jitterText, value); }

    public string NetworkFootnote { get; }

    private void ApplyNetwork(NetworkSnapshot network)
    {
        LatencyValue = network.LatencyMs is double latency ? Formats.Number(latency, 0) : Formats.Missing;
        JitterText = Strings.Format("Dashboard.Network.Jitter",
            network.JitterMs is double jitter ? Formats.Milliseconds(jitter, 1) : Formats.Missing);
    }

    // ------------------------------------------------------------------
    // Processus
    // ------------------------------------------------------------------

    public ObservableCollection<ProcessDisplayItem> TopProcesses { get; } = [];

    private void ApplyProcesses(IReadOnlyList<ProcessSnapshot> processes)
    {
        var items = processes
            .Select(p => new ProcessDisplayItem(p.Name, Formats.Percent(p.CpuPercent, 1), Formats.Memory(p.MemoryMegabytes)))
            .ToList();

        while (TopProcesses.Count > items.Count)
            TopProcesses.RemoveAt(TopProcesses.Count - 1);

        for (int i = 0; i < items.Count; i++)
        {
            if (i >= TopProcesses.Count)
                TopProcesses.Add(items[i]);
            else if (TopProcesses[i] != items[i])
                TopProcesses[i] = items[i];
        }
    }

    // ------------------------------------------------------------------
    // Résumé de l'audit (niveau 0)
    // ------------------------------------------------------------------

    private string _auditSummaryText = Strings.Get("Dashboard.AuditSummary.Loading");
    public string AuditSummaryText { get => _auditSummaryText; private set => SetProperty(ref _auditSummaryText, value); }

    private Brush _auditSummaryBrush = GetBrush("TextPrimaryBrush");
    public Brush AuditSummaryBrush { get => _auditSummaryBrush; private set => SetProperty(ref _auditSummaryBrush, value); }

    private Visibility _auditSourceVisibility = Visibility.Collapsed;
    public Visibility AuditSourceVisibility { get => _auditSourceVisibility; private set => SetProperty(ref _auditSourceVisibility, value); }

    public async Task RefreshAuditSummaryAsync()
    {
        IReadOnlyList<AuditItem> items = await _auditService.GetOrRunAuditAsync();
        int toCheck = items.Count(i => i.Status is AuditStatus.Warning or AuditStatus.Problem);

        AuditSummaryText = AuditSummary.Text(toCheck);
        AuditSummaryBrush = GetBrush(toCheck == 0 ? "TextPrimaryBrush" : "StatusWarnTextBrush");
        AuditSourceVisibility = Visibility.Visible;
    }

    private static Brush GetBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];

    public void Dispose() => _timer.Stop();

    /// <summary>One column of the main card: temperature, status, frequency, load and model name.</summary>
    public sealed class ProcessorDisplay : ViewModelBase
    {
        private string _temperature = Formats.Missing;
        public string Temperature { get => _temperature; private set => SetProperty(ref _temperature, value); }

        private string _statusText = string.Empty;
        public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

        private Brush _statusBrush = GetBrush("StatusNeutralTextBrush");
        public Brush StatusBrush { get => _statusBrush; private set => SetProperty(ref _statusBrush, value); }

        private string _frequencyText = string.Empty;
        public string FrequencyText { get => _frequencyText; private set => SetProperty(ref _frequencyText, value); }

        private string _loadText = Formats.Missing;
        public string LoadText { get => _loadText; private set => SetProperty(ref _loadText, value); }

        private double _loadFraction;
        public double LoadFraction { get => _loadFraction; private set => SetProperty(ref _loadFraction, value); }

        private string _name = string.Empty;
        public string Name { get => _name; private set => SetProperty(ref _name, value); }

        public void Apply(double? celsius, double? loadPercent, string? name, string frequencyText)
        {
            Temperature = celsius is double c ? Formats.Number(c, 0) : Formats.Missing;
            FrequencyText = frequencyText;
            Name = name ?? string.Empty;
            LoadText = loadPercent is double load ? Formats.Percent(load) : Formats.Missing;
            LoadFraction = loadPercent is double fraction ? Math.Clamp(fraction / 100, 0, 1) : 0;

            if (celsius is not double value)
            {
                StatusText = string.Empty;
                return;
            }

            StatusTier tier = value >= 90 ? StatusTier.Bad : value >= 75 ? StatusTier.Warn : StatusTier.Normal;
            StatusText = Strings.Get(tier switch
            {
                StatusTier.Bad => "Dashboard.Status.Critical",
                StatusTier.Warn => "Dashboard.Status.High",
                _ => "Dashboard.Status.Normal"
            });
            StatusBrush = GetBrush(tier switch
            {
                StatusTier.Bad => "StatusBadTextBrush",
                StatusTier.Warn => "StatusWarnTextBrush",
                _ => "StatusNeutralTextBrush"
            });
        }
    }
}
