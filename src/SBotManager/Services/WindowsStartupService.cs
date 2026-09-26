using System.Diagnostics;
using System.Security.Principal;

namespace SBotManager.Services;

public sealed class WindowsStartupService
{
    private const string TaskName = "SBotManager Startup";

    public bool IsEnabled()
    {
        return Run("/Query", "/TN", TaskName) == 0;
    }

    public void SetEnabled(bool enabled)
    {
        if (!enabled) { Run("/Delete", "/TN", TaskName, "/F"); return; }
        EnsureElevated();
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Windows startup can only be enabled when i1988 - sBot manager is running from its .exe file.");
        int exitCode = Run("/Create", "/TN", TaskName, "/TR", $"\"{executable}\" --windows-startup", "/SC", "ONLOGON", "/RL", "HIGHEST", "/F");
        if (exitCode != 0) throw new InvalidOperationException("Windows could not create the elevated i1988 - sBot manager startup task. Run i1988 - sBot manager as administrator and try again.");
    }

    private static void EnsureElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Run i1988 - sBot manager as administrator before enabling Windows startup. This lets the startup task launch sBot with the required rights.");
    }

    private static int Run(params string[] arguments)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo("schtasks.exe") { UseShellExecute = false, CreateNoWindow = true } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start(); process.WaitForExit();
        return process.ExitCode;
    }
}
