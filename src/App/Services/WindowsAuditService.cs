using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Canopus.App.Localization;
using Canopus.App.Models;

namespace Canopus.App.Services;

public sealed class WindowsAuditService : IAuditService
{
    private static readonly Guid BalancedScheme = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid HighPerformanceScheme = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid PowerSaverScheme = new("a1841308-3541-4fab-bc81-f71556f20b4a");
    private static readonly Guid UltimatePerformanceScheme = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    // Windows 11's Power Mode slider only exists when the classic scheme is
    // Balanced; it layers an overlay on top that PowerGetActiveScheme never sees.
    // Confirmed on a real machine: classic scheme stayed Balanced after switching
    // the slider to "Best performance", only the overlay GUID changed.
    private static readonly Guid OverlayBestPerformance = new("ded574b5-45a0-4f42-8737-46345c09c238");
    private static readonly Guid OverlayBetterBattery = new("961cc777-2547-4f9d-8174-7d86181b8a7a");

    private static readonly Dictionary<string, string> KnownOverlays = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Discord"] = "Discord",
        ["DiscordPTB"] = "Discord (PTB)",
        ["DiscordCanary"] = "Discord (Canary)",
        ["NVIDIA Overlay"] = "NVIDIA Overlay",
        ["NVIDIA Share"] = "NVIDIA Overlay",
        ["GeForceExperience"] = "NVIDIA GeForce Experience",
        ["MSIAfterburner"] = "MSI Afterburner",
        ["RTSS"] = "RivaTuner Statistics Server",
        ["RTSSHooksLoader64"] = "RivaTuner Statistics Server",
        ["GameOverlayUI"] = "Overlay Steam",
        ["Overwolf"] = "Overwolf",
        ["obs64"] = "OBS Studio"
    };

    // Holds the in-flight or last-completed run, not just its result: this makes
    // GetOrRunAuditAsync single-flight, so two callers racing at startup (the
    // dashboard's background trigger and AuditViewModel's constructor) await the
    // same sweep instead of each starting their own.
    private Task<IReadOnlyList<AuditItem>>? _auditTask;

    public Task<IReadOnlyList<AuditItem>> RunAuditAsync()
    {
        Task<IReadOnlyList<AuditItem>> task = Task.Run<IReadOnlyList<AuditItem>>(() =>
        [
            DetectPowerPlan(),
            DetectGameMode(),
            DetectMemoryProfile(),
            DetectOverlays(),
            DetectDefenderExclusions(),
            ReadGpuDriverInfo()
        ]);

        _auditTask = task;
        return task;
    }

    public Task<IReadOnlyList<AuditItem>> GetOrRunAuditAsync() => _auditTask ?? RunAuditAsync();

    private static AuditItem DetectPowerPlan()
    {
        string title = Strings.Get("Audit.PowerPlan.Title");
        IntPtr schemeGuidPtr = IntPtr.Zero;

        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out schemeGuidPtr) != 0 || schemeGuidPtr == IntPtr.Zero)
                return ReadFailed(title, Strings.Get("Audit.PowerPlan.ReadFailed"));

            Guid active = Marshal.PtrToStructure<Guid>(schemeGuidPtr);

            // The displayed name is localized ("Utilisation normale" for Balanced in French),
            // hence classifying by GUID rather than by name.
            string name = ReadPowerSchemeFriendlyName(active) ?? active.ToString();

            if (active == HighPerformanceScheme || active == UltimatePerformanceScheme)
                return new AuditItem(title, AuditStatus.Confirmed, Strings.Get("Audit.Status.Optimal"),
                    Strings.Format("Audit.PowerPlan.HighPerf", name));

            if (active == PowerSaverScheme)
                return new AuditItem(title, AuditStatus.Problem, Strings.Get("Audit.Status.Problem"),
                    Strings.Format("Audit.PowerPlan.PowerSaver", name));

            if (active == BalancedScheme)
                return ClassifyBalancedScheme(title, name);

            return new AuditItem(title, AuditStatus.Info, Strings.Get("Audit.Status.Info"),
                Strings.Format("Audit.PowerPlan.Custom", name),
                Strings.Get("Audit.PowerPlan.CustomNote"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.PowerPlan.ReadException", ex.GetType().Name));
        }
        finally
        {
            if (schemeGuidPtr != IntPtr.Zero)
                LocalFree(schemeGuidPtr);
        }
    }

    private static AuditItem ClassifyBalancedScheme(string title, string schemeName)
    {
        if (PowerGetEffectiveOverlayScheme(out Guid overlay) != 0)
            return new AuditItem(title, AuditStatus.Warning, Strings.Get("Audit.Status.ToCheck"),
                Strings.Format("Audit.PowerPlan.Balanced.Default", schemeName));

        if (overlay == OverlayBestPerformance)
            return new AuditItem(title, AuditStatus.Confirmed, Strings.Get("Audit.Status.Optimal"),
                Strings.Format("Audit.PowerPlan.Balanced.BestPerf", schemeName));

        if (overlay == OverlayBetterBattery)
            return new AuditItem(title, AuditStatus.Problem, Strings.Get("Audit.Status.Problem"),
                Strings.Format("Audit.PowerPlan.Balanced.BetterBattery", schemeName));

        return new AuditItem(title, AuditStatus.Warning, Strings.Get("Audit.Status.ToCheck"),
            Strings.Format("Audit.PowerPlan.Balanced.Default", schemeName));
    }

    private static string? ReadPowerSchemeFriendlyName(Guid scheme)
    {
        uint bufferSize = 1024;
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            return PowerReadFriendlyName(IntPtr.Zero, ref scheme, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize) == 0
                ? Marshal.PtrToStringUni(buffer)
                : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static AuditItem DetectGameMode()
    {
        string title = Strings.Get("Audit.GameMode.Title");

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\GameBar");
            if (key is null)
                return ReadFailed(title, Strings.Get("Audit.GameMode.KeyMissing"));

            // Both values coexist on Windows 11: AutoGameModeEnabled holds the Game Mode
            // state, AllowAutoGameMode the global permission.
            int? enabled = key.GetValue("AutoGameModeEnabled") as int?
                        ?? key.GetValue("AllowAutoGameMode") as int?;

            if (enabled is null)
                return ReadFailed(title, Strings.Get("Audit.GameMode.ValueMissing"));

            return enabled != 0
                ? new AuditItem(title, AuditStatus.Confirmed, Strings.Get("Audit.Status.Enabled"),
                    Strings.Get("Audit.GameMode.Enabled"))
                : new AuditItem(title, AuditStatus.Warning, Strings.Get("Audit.Status.Disabled"),
                    Strings.Get("Audit.GameMode.Disabled"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.GameMode.ReadException", ex.GetType().Name));
        }
    }

    private static AuditItem DetectMemoryProfile()
    {
        string title = Strings.Get("Audit.MemoryProfile.Title");

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT BankLabel, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory");

            var modules = new List<(uint Nominal, uint Configured)>();
            foreach (ManagementObject module in searcher.Get().Cast<ManagementObject>())
            {
                using (module)
                {
                    if (module["Speed"] is uint nominal && module["ConfiguredClockSpeed"] is uint configured
                        && nominal > 0 && configured > 0)
                    {
                        modules.Add((nominal, configured));
                    }
                }
            }

            if (modules.Count == 0)
                return ReadFailed(title, Strings.Get("Audit.MemoryProfile.NoModules"));

            uint nominalMax = modules.Max(m => m.Nominal);
            uint configuredMin = modules.Min(m => m.Configured);

            // 5% tolerance: WMI values are often rounded (5999 vs 6000).
            if (configuredMin < nominalMax * 0.95)
            {
                return new AuditItem(title, AuditStatus.Warning, Strings.Get("Audit.Status.ToCheck"),
                    Strings.Format("Audit.MemoryProfile.NotActive", configuredMin, nominalMax),
                    Strings.Get("Audit.MemoryProfile.MethodNote"));
            }

            return new AuditItem(title, AuditStatus.Confirmed, Strings.Get("Audit.Status.Optimal"),
                Strings.Format("Audit.MemoryProfile.Active", configuredMin, modules.Count),
                Strings.Get("Audit.MemoryProfile.MethodNote"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.MemoryProfile.ReadException", ex.GetType().Name));
        }
    }

    private static AuditItem DetectOverlays()
    {
        string title = Strings.Get("Audit.Overlays.Title");

        try
        {
            var detected = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    if (KnownOverlays.TryGetValue(process.ProcessName, out string? friendlyName))
                        detected.Add(friendlyName);
                }
            }

            if (detected.Count == 0)
                return new AuditItem(title, AuditStatus.Confirmed, Strings.Get("Audit.Status.NoneDetected"),
                    Strings.Get("Audit.Overlays.None"), Strings.Get("Audit.Overlays.CoverageNote"));

            return new AuditItem(title, AuditStatus.Warning, Strings.Get("Audit.Status.OverlayActive"),
                Strings.Format("Audit.Overlays.Detected", detected.Count, string.Join(", ", detected)),
                Strings.Get("Audit.Overlays.CoverageNote"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.Overlays.EnumException", ex.GetType().Name));
        }
    }

    private static AuditItem DetectDefenderExclusions()
    {
        string title = Strings.Get("Audit.Defender.Title");

        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\Defender", "SELECT ExclusionPath FROM MSFT_MpPreference");

            foreach (ManagementObject preference in searcher.Get().Cast<ManagementObject>())
            {
                using (preference)
                {
                    if (preference["ExclusionPath"] is not string[] paths)
                        continue;

                    // Without elevation, Defender returns not an empty list but a sentinel
                    // entry "N/A: Must be an administrator to view exclusions": counting it
                    // would show "1 exclusion configured", which would be wrong.
                    if (paths.Any(p => p.Contains("Must be an administrator", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new AuditItem(title, AuditStatus.Info, Strings.Get("Audit.Status.Info"),
                            Strings.Get("Audit.Defender.RequiresAdmin"),
                            Strings.Get("Audit.Defender.ScopeNote"));
                    }

                    return new AuditItem(title, AuditStatus.Info, Strings.Get("Audit.Status.Info"),
                        Strings.Format("Audit.Defender.Configured", paths.Length), Strings.Get("Audit.Defender.ScopeNote"));
                }
            }

            return new AuditItem(title, AuditStatus.Info, Strings.Get("Audit.Status.Info"),
                Strings.Get("Audit.Defender.None"), Strings.Get("Audit.Defender.ScopeNote"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.Defender.ReadException", ex.GetType().Name));
        }
    }

    private static AuditItem ReadGpuDriverInfo()
    {
        string title = Strings.Get("Audit.GpuDriver.Title");

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DriverVersion, DriverDate FROM Win32_VideoController");

            var lines = new List<string>();
            foreach (ManagementObject controller in searcher.Get().Cast<ManagementObject>())
            {
                using (controller)
                {
                    string name = controller["Name"] as string ?? Strings.Get("Audit.GpuDriver.UnknownGpu");
                    string version = controller["DriverVersion"] as string ?? Strings.Get("Audit.GpuDriver.UnknownVersion");
                    lines.Add(Strings.Format("Audit.GpuDriver.Line", name, version, FormatDriverDate(controller["DriverDate"] as string)));
                }
            }

            if (lines.Count == 0)
                return ReadFailed(title, Strings.Get("Audit.GpuDriver.NoController"));

            return new AuditItem(title, AuditStatus.Info, Strings.Get("Audit.Status.Info"),
                string.Join(Environment.NewLine, lines),
                Strings.Get("Audit.GpuDriver.Note"));
        }
        catch (Exception ex)
        {
            return ReadFailed(title, Strings.Format("Audit.GpuDriver.ReadException", ex.GetType().Name));
        }
    }

    private static string FormatDriverDate(string? cimDate)
    {
        if (string.IsNullOrWhiteSpace(cimDate))
            return string.Empty;

        try
        {
            return Strings.Format("Audit.GpuDriver.InstalledOn", ManagementDateTimeConverter.ToDateTime(cimDate));
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            return string.Empty;
        }
    }

    private static AuditItem ReadFailed(string title, string reason) =>
        new(title, AuditStatus.Info, Strings.Get("Audit.Status.NotChecked"), reason,
            Strings.Get("Audit.ReadFailed.Note"));

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetEffectiveOverlayScheme(out Guid effectiveOverlayGuid);

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    private static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid,
        IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
