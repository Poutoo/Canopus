using System.Diagnostics;

namespace Canopus.App.Services;

// A registry Run-key entry was the first attempt here, but app.manifest requires admin
// (LibreHardwareMonitorLib needs it to read sensors -- see IHardwareMonitorService, and
// that's what feeds the Dashboard's live temps/load, not a leftover from the spike).
// Windows does not silently elevate a Run-key launch, so that approach left a UAC prompt
// at every logon -- defeating the point of "starts without friction".
//
// A scheduled task with RunLevel=Highest is the standard way an always-admin app
// autostarts without a UAC prompt: Task Scheduler is trusted to launch it elevated
// directly, no consent dialog. Creating/deleting the task itself doesn't need a fresh
// elevation prompt either, since Canopus is already running elevated (same manifest) by
// the time the user can reach this toggle -- schtasks.exe just inherits that token.
//
// /SC ONLOGON + /RU <current user>, no /RP: "run only when user is logged on" for the
// user's own account doesn't need a stored password, unlike "run whether logged on or
// not". Implemented via schtasks.exe rather than the Task Scheduler COM API to avoid
// pulling in an extra NuGet dependency for something this narrow.
public sealed class WindowsStartupService : IStartupService
{
    private const string TaskName = "Canopus";

    public bool IsEnabled() => RunSchtasks("/Query", "/TN", TaskName) == 0;

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            RunSchtasks("/Delete", "/TN", TaskName, "/F");
            return;
        }

        string exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Impossible de déterminer le chemin de l'exécutable.");
        string currentUser = $"{Environment.UserDomainName}\\{Environment.UserName}";

        int exitCode = RunSchtasks(
            "/Create", "/TN", TaskName,
            "/TR", $"\"{exePath}\"",
            "/SC", "ONLOGON",
            "/RU", currentUser,
            "/RL", "HIGHEST",
            "/F");

        if (exitCode != 0)
            throw new InvalidOperationException("Impossible de créer la tâche planifiée de démarrage.");
    }

    private static int RunSchtasks(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("schtasks.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using Process? process = Process.Start(startInfo);
        process?.WaitForExit();
        return process?.ExitCode ?? -1;
    }
}
