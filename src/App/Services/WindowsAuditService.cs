using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Canopus.App.Models;

namespace Canopus.App.Services;

public sealed class WindowsAuditService : IAuditService
{
    private static readonly Guid BalancedScheme = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid HighPerformanceScheme = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid PowerSaverScheme = new("a1841308-3541-4fab-bc81-f71556f20b4a");
    private static readonly Guid UltimatePerformanceScheme = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    private const string OverlayCoverageNote =
        "Détection basée sur une liste connue, un overlay non répertorié peut passer inaperçu.";

    private const string DefenderScopeNote =
        "Vérification générique Windows Defender — les antivirus tiers ne sont pas couverts, "
        + "et aucun jeu spécifique n'est encore ciblé.";

    private const string MemoryMethodNote =
        "Détection par comparaison WMI ConfiguredClockSpeed / Speed. De nombreuses cartes mères "
        + "renseignent la même valeur dans les deux champs : l'absence d'écart ne prouve donc pas "
        + "que le profil est actif, elle indique seulement qu'aucun bridage n'est visible ici. "
        + "À confirmer dans le BIOS en cas de doute.";

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

    private IReadOnlyList<AuditItem>? _lastResult;

    public async Task<IReadOnlyList<AuditItem>> RunAuditAsync()
    {
        _lastResult = await Task.Run<IReadOnlyList<AuditItem>>(() =>
        [
            DetectPowerPlan(),
            DetectGameMode(),
            DetectMemoryProfile(),
            DetectOverlays(),
            DetectDefenderExclusions(),
            ReadGpuDriverInfo()
        ]);

        return _lastResult;
    }

    public Task<IReadOnlyList<AuditItem>> GetOrRunAuditAsync() =>
        _lastResult is not null ? Task.FromResult(_lastResult) : RunAuditAsync();

    private static AuditItem DetectPowerPlan()
    {
        const string title = "Plan d'alimentation";
        IntPtr schemeGuidPtr = IntPtr.Zero;

        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out schemeGuidPtr) != 0 || schemeGuidPtr == IntPtr.Zero)
                return ReadFailed(title, "Impossible de lire le plan d'alimentation actif.");

            Guid active = Marshal.PtrToStructure<Guid>(schemeGuidPtr);

            // Le nom affiché est localisé ("Utilisation normale" pour Balanced en français),
            // d'où la classification par GUID et non par nom.
            string name = ReadPowerSchemeFriendlyName(active) ?? active.ToString();

            if (active == HighPerformanceScheme || active == UltimatePerformanceScheme)
                return new AuditItem(title, AuditStatus.Confirmed, "Optimal",
                    $"Plan actif : {name}. Le CPU n'est pas bridé par la gestion d'énergie.");

            if (active == PowerSaverScheme)
                return new AuditItem(title, AuditStatus.Problem, "Problème",
                    $"Plan actif : {name}. L'économie d'énergie limite fortement les performances CPU.");

            if (active == BalancedScheme)
                return new AuditItem(title, AuditStatus.Warning, "À vérifier",
                    $"Plan actif : {name}. Un plan équilibré peut brider le CPU en jeu.");

            return new AuditItem(title, AuditStatus.Info, "Informatif",
                $"Plan actif : {name}.",
                "Plan personnalisé ou constructeur, non reconnu parmi les plans Windows standard — impossible de le classer automatiquement.");
        }
        catch (Exception ex)
        {
            return ReadFailed(title, $"Lecture impossible ({ex.GetType().Name}).");
        }
        finally
        {
            if (schemeGuidPtr != IntPtr.Zero)
                LocalFree(schemeGuidPtr);
        }
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
        const string title = "Mode Jeu Windows";

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\GameBar");
            if (key is null)
                return ReadFailed(title, @"Clé de registre HKCU\SOFTWARE\Microsoft\GameBar introuvable.");

            // Les deux valeurs coexistent sur Windows 11 : AutoGameModeEnabled porte l'état
            // du Mode Jeu, AllowAutoGameMode l'autorisation globale.
            int? enabled = key.GetValue("AutoGameModeEnabled") as int?
                        ?? key.GetValue("AllowAutoGameMode") as int?;

            if (enabled is null)
                return ReadFailed(title, "Ni AutoGameModeEnabled ni AllowAutoGameMode n'existent sous cette clé.");

            return enabled != 0
                ? new AuditItem(title, AuditStatus.Confirmed, "Activé",
                    "Le Mode Jeu Windows est activé.")
                : new AuditItem(title, AuditStatus.Warning, "Désactivé",
                    "Le Mode Jeu Windows est désactivé. Il priorise les ressources vers le jeu au premier plan.");
        }
        catch (Exception ex)
        {
            return ReadFailed(title, $"Lecture impossible ({ex.GetType().Name}).");
        }
    }

    private static AuditItem DetectMemoryProfile()
    {
        const string title = "Profil mémoire XMP / EXPO";

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
                return ReadFailed(title, "Aucune barrette mémoire exploitable retournée par WMI.");

            uint nominalMax = modules.Max(m => m.Nominal);
            uint configuredMin = modules.Min(m => m.Configured);

            // Tolérance de 5 % : les valeurs WMI sont souvent arrondies (5999 vs 6000).
            if (configuredMin < nominalMax * 0.95)
            {
                return new AuditItem(title, AuditStatus.Warning, "À vérifier",
                    $"Mémoire cadencée à {configuredMin} MT/s alors que les barrettes annoncent {nominalMax} MT/s — "
                    + "le profil XMP/EXPO ne semble pas activé.",
                    MemoryMethodNote);
            }

            return new AuditItem(title, AuditStatus.Confirmed, "Optimal",
                $"Mémoire cadencée à {configuredMin} MT/s, conforme à la fréquence annoncée par les barrettes "
                + $"({modules.Count} barrette(s) détectée(s)).",
                MemoryMethodNote);
        }
        catch (Exception ex)
        {
            return ReadFailed(title, $"Lecture WMI impossible ({ex.GetType().Name}).");
        }
    }

    private static AuditItem DetectOverlays()
    {
        const string title = "Overlays actifs";

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
                return new AuditItem(title, AuditStatus.Confirmed, "Aucun détecté",
                    "Aucun overlay connu en cours d'exécution.", OverlayCoverageNote);

            return new AuditItem(title, AuditStatus.Warning, "Overlay actif",
                $"{detected.Count} overlay(s) en cours d'exécution : {string.Join(", ", detected)}. "
                + "Un overlay s'injecte dans le jeu et peut coûter quelques FPS.",
                OverlayCoverageNote);
        }
        catch (Exception ex)
        {
            return ReadFailed(title, $"Énumération des process impossible ({ex.GetType().Name}).");
        }
    }

    private static AuditItem DetectDefenderExclusions()
    {
        const string title = "Exclusions antivirus";

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

                    // Sans élévation, Defender ne renvoie pas une liste vide mais une entrée
                    // sentinelle "N/A: Must be an administrator to view exclusions" : la compter
                    // afficherait "1 exclusion configurée", ce qui serait faux.
                    if (paths.Any(p => p.Contains("Must be an administrator", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new AuditItem(title, AuditStatus.Info, "Informatif",
                            "Lecture des exclusions impossible : droits administrateur requis.",
                            DefenderScopeNote);
                    }

                    return new AuditItem(title, AuditStatus.Info, "Informatif",
                        $"{paths.Length} exclusion(s) configurée(s).", DefenderScopeNote);
                }
            }

            return new AuditItem(title, AuditStatus.Info, "Informatif",
                "Aucune exclusion configurée.", DefenderScopeNote);
        }
        catch (Exception ex)
        {
            return ReadFailed(title,
                $"Lecture WMI Defender impossible ({ex.GetType().Name}) — Defender est peut-être "
                + "désactivé ou remplacé par un antivirus tiers.");
        }
    }

    private static AuditItem ReadGpuDriverInfo()
    {
        const string title = "Version du driver GPU";

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DriverVersion, DriverDate FROM Win32_VideoController");

            var lines = new List<string>();
            foreach (ManagementObject controller in searcher.Get().Cast<ManagementObject>())
            {
                using (controller)
                {
                    string name = controller["Name"] as string ?? "GPU inconnu";
                    string version = controller["DriverVersion"] as string ?? "version inconnue";
                    lines.Add($"{name} — Version {version}{FormatDriverDate(controller["DriverDate"] as string)}");
                }
            }

            if (lines.Count == 0)
                return ReadFailed(title, "Aucun contrôleur vidéo retourné par WMI.");

            return new AuditItem(title, AuditStatus.Info, "Informatif",
                string.Join(Environment.NewLine, lines),
                "Information factuelle uniquement : aucune comparaison avec la dernière version publiée par le constructeur n'est effectuée.");
        }
        catch (Exception ex)
        {
            return ReadFailed(title, $"Lecture WMI impossible ({ex.GetType().Name}).");
        }
    }

    private static string FormatDriverDate(string? cimDate)
    {
        if (string.IsNullOrWhiteSpace(cimDate))
            return string.Empty;

        try
        {
            return $", installé le {ManagementDateTimeConverter.ToDateTime(cimDate):dd/MM/yyyy}";
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            return string.Empty;
        }
    }

    private static AuditItem ReadFailed(string title, string reason) =>
        new(title, AuditStatus.Info, "Non vérifié", reason,
            "Ce levier n'a pas pu être évalué : le résultat ci-dessus n'est pas un verdict.");

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    private static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid,
        IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
