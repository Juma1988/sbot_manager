using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SBotManager.Models;

namespace SBotManager.Services;

// Path + PID + creation time are rechecked before every action. Never target by title alone.
public sealed class WindowsBotService : IBotService
{
    private sealed record Identity(int Pid, long Started, string Path);
    private sealed record Match(ProcessSnapshot Snapshot, Identity? Identity);

    public ProcessSnapshot Inspect(AccountSettings account) => Find(account).Snapshot;

    private static Match Find(AccountSettings account)
    {
        if (string.IsNullOrWhiteSpace(account.ExecutablePath)) return new(new(BotState.NotConfigured, "Choose an executable using the gear."), null);
        var matches = new List<Identity>();
        bool unknown = false;
        try
        {
            string expected = Path.GetFullPath(account.ExecutablePath);
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(expected)))
            {
                using (p)
                {
                    try
                    {
                        var path = NativeWindows.ExecutablePath(p.Id);
                        if (path is null) { if (!p.HasExited) unknown = true; continue; }
                        if (string.Equals(Path.GetFullPath(path), expected, StringComparison.OrdinalIgnoreCase))
                            matches.Add(new(p.Id, p.StartTime.ToUniversalTime().Ticks, path));
                    }
                    catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { unknown = true; }
                }
            }
            if (unknown || matches.Count > 1)
                return new(new(BotState.Unknown, unknown ? "Cannot verify a matching process. Check permissions; launch/recovery blocked." : "Multiple processes use this path. Resolve duplicates before controlling them."), null);
            if (matches.Count == 0)
                return new(File.Exists(expected) ? new(BotState.Stopped, "Executable ready. Opening sBot may activate its own auto-login/training settings.") : new(BotState.MissingFile, "Configured executable not found. Choose the current file."), null);
            var match = matches[0];
            var window = NativeWindows.WindowFor(match.Pid);
            bool start = window != 0 && NativeWindows.TrainingButton(window, match.Pid, true) != 0;
            bool stop = window != 0 && NativeWindows.TrainingButton(window, match.Pid, false) != 0;
            bool game = window != 0 && NativeWindows.GameButton(window, match.Pid) != 0;
            bool? visible = window != 0 ? NativeWindows.IsWindowVisible(window) : null;
            bool? clientVisible = NativeWindows.ClientWindowVisibility(match.Pid);
            return new(new(BotState.Running, start || stop || game ? "Native sBot control found; commands are not proof of game state." : "Process verified. Training controls not accessible; check sBot permissions/version.", match.Pid, start, stop, CanStartGame: game, WindowVisible: visible, ClientWindowVisible: clientVisible), match);
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or ArgumentException or IOException or UnauthorizedAccessException)
        { return new(new(BotState.Unknown, "Process inspection failed: " + e.Message), null); }
    }

    private static Process Verified(Identity identity)
    {
        var p = Process.GetProcessById(identity.Pid);
        try
        {
            if (p.StartTime.ToUniversalTime().Ticks != identity.Started ||
                !string.Equals(NativeWindows.ExecutablePath(p.Id), identity.Path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Process identity changed. Action cancelled.");
            return p;
        }
        catch { p.Dispose(); throw; }
    }

    public Task<BotResult> OpenAsync(AccountSettings account) => Task.Run(async () =>
    {
        var found = Find(account);
        if (found.Snapshot.State != BotState.Stopped) return new BotResult(false, "Launch blocked: " + found.Snapshot.Detail);
        try
        {
            using var launched = Process.Start(new ProcessStartInfo(account.ExecutablePath)
            {
                WorkingDirectory = Path.GetDirectoryName(account.ExecutablePath)!, UseShellExecute = false
            });
            if (launched is null) return new BotResult(false, "Windows did not return a launched process.");
            await Task.Delay(1200);
            var after = Find(account);
            return after.Snapshot.State == BotState.Running
                ? new BotResult(true, "sBot process launched and path verified. Game/training state remains unknown.")
                : new BotResult(false, "Launch requested, but a unique running instance was not verified. " + after.Snapshot.Detail);
        }
        catch (System.ComponentModel.Win32Exception e) when (e.NativeErrorCode == 740)
        {
            // ERROR_ELEVATION_REQUIRED: this sBot build asks Windows for administrator rights.
            return new BotResult(false, "sBot requires administrator rights. Close i1988 - sBot manager and start it as administrator (right-click → Run as administrator), then open this bot again.");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            return new BotResult(false, $"Could not start '{account.ExecutablePath}': {e.Message}");
        }
    });

    public Task<BotResult> TrainingAsync(AccountSettings account, bool start) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Training command blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        var window = NativeWindows.WindowFor(p.Id);
        var button = NativeWindows.TrainingButton(window, p.Id, start);
        if (button == 0) return new BotResult(false, "Training button unavailable. No coordinate clicks or termination fallback used.");
        // Revalidate identity and control ownership immediately before sending a bounded native message.
        using var again = Verified(found.Identity);
        if (!NativeWindows.OwnedBy(button, p.Id) || !NativeWindows.IsWindowEnabled(button)) return new BotResult(false, "Training control changed. Action cancelled.");
        var sent = NativeWindows.SendMessageTimeout(button, 0x00F5, 0, 0, 0x0002, 1500, out _) != 0; // BM_CLICK
        return sent
            ? new BotResult(true, $"{(start ? "Start" : "Stop")} training command sent. Result not independently confirmed by sBot telemetry.")
            : new BotResult(false, "Training command could not be delivered (permissions or timeout). Actual training state is unknown.");
    });

    public Task<BotResult> StartGameAsync(AccountSettings account) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Start Game blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        var window = NativeWindows.WindowFor(p.Id);
        var button = NativeWindows.GameButton(window, p.Id);
        if (button == 0) return new BotResult(false, "Start Game control unavailable. No coordinate clicks or fallback used.");
        // Revalidate identity and control ownership immediately before sending a bounded native message.
        using var again = Verified(found.Identity);
        if (!NativeWindows.OwnedBy(button, p.Id) || !NativeWindows.IsWindowEnabled(button)) return new BotResult(false, "Start Game control changed. Action cancelled.");
        var sent = NativeWindows.SendMessageTimeout(button, 0x00F5, 0, 0, 0x0002, 1500, out _) != 0; // BM_CLICK
        return sent
            ? new BotResult(true, "Start Game command sent to sBot. Client launch is not independently confirmed by sBot telemetry.")
            : new BotResult(false, "Start Game command could not be delivered (permissions or timeout). No further action taken.");
    });

    public Task<BotResult> SetClientlessAfterGameAsync(AccountSettings account, bool enabled) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Clientless setting blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        var button = NativeWindows.ClientlessAfterGameButton(NativeWindows.WindowFor(p.Id), p.Id);
        if (button == 0) return new BotResult(false, "Automatic clientless control unavailable. No fallback used.");
        using var again = Verified(found.Identity);
        if (!NativeWindows.OwnedBy(button, p.Id) || !NativeWindows.IsWindowEnabled(button)) return new BotResult(false, "Automatic clientless control changed. Action cancelled.");
        // BM_SETCHECK writes an explicit state. Unlike a toggle click, it cannot
        // accidentally invert an already-correct checkbox when its prior state is unknown.
        if (NativeWindows.SendMessageTimeout(button, 0x00F1, enabled ? 1 : 0, 0, 0x0002, 1500, out _) == 0)
            return new BotResult(false, "Automatic clientless state could not be set. Run Manager at the same elevation as sBot.");
        // Some sBot builds accept BM_SETCHECK but do not return BM_GETCHECK
        // across their privilege boundary. A delivered explicit set is enough to
        // continue Launch; a readable mismatch is still a definite failure.
        if (NativeWindows.SendMessageTimeout(button, 0x00F0, 0, 0, 0x0002, 1500, out var checkedState) == 0)
            return new BotResult(true, "Automatic clientless state was set; sBot did not provide a readable confirmation.");
        if ((checkedState != 0) != enabled)
            return new BotResult(false, "Automatic clientless state could not be confirmed. Run Manager at the same elevation as sBot.");
        return new BotResult(true, enabled
            ? "Enabled sBot automatic clientless after game entry; its existing delay was preserved."
            : "Disabled sBot automatic clientless after game entry; its existing delay was preserved.");
    });

    public Task<BotResult> GoClientlessAsync(AccountSettings account) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Go clientless blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        var button = NativeWindows.GoClientlessButton(NativeWindows.WindowFor(p.Id), p.Id);
        if (button == 0) return new BotResult(false, "Go clientless control unavailable. No fallback used.");
        using var again = Verified(found.Identity);
        if (!NativeWindows.OwnedBy(button, p.Id) || !NativeWindows.IsWindowEnabled(button)) return new BotResult(false, "Go clientless control changed. Action cancelled.");
        var sent = NativeWindows.SendMessageTimeout(button, 0x00F5, 0, 0, 0x0002, 1500, out _) != 0;
        return sent ? new BotResult(true, "Go clientless command sent to sBot. Result is not independently confirmed by sBot telemetry.")
                    : new BotResult(false, "Go clientless command could not be delivered. No further action taken.");
    });

    public Task<BotResult> SetVisibilityAsync(AccountSettings account, bool visible) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Window command blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        var window = NativeWindows.WindowFor(p.Id);
        if (window == 0) return new BotResult(false, "No top-level bot window found.");
        if (visible)
        {
            NativeWindows.ShowWindowAsync(window, 9);
            bool foreground = NativeWindows.SetForegroundWindow(window);
            return new BotResult(true, foreground ? "Bot window restored and foreground requested." : "Bot window restored; Windows did not grant foreground focus.");
        }
        NativeWindows.ShowWindowAsync(window, 0);
        for (int i = 0; i < 20 && NativeWindows.IsWindowVisible(window); i++) Thread.Sleep(100);
        bool actuallyHidden = !NativeWindows.IsWindowVisible(window);
        return new BotResult(actuallyHidden, actuallyHidden ? "Bot window hidden; the process keeps running." : "Windows did not hide the bot window. Verify and try again.");
    });

    public Task<BotResult> SetRelatedClientVisibilityAsync(AccountSettings account, bool visible) => Task.Run(() =>
    {
        var found = Find(account);
        if (found.Identity is null) return new BotResult(false, "Client window command blocked: " + found.Snapshot.Detail);
        int changed = NativeWindows.SetClientWindowsVisibility(found.Identity.Pid, visible);
        return changed > 0
            ? new BotResult(true, visible ? $"Restored {changed} related Silkroad client window(s)." : $"Hidden {changed} related Silkroad client window(s).")
            : new BotResult(false, "No related Silkroad client window was found.");
    });

    public Task<BotResult> TerminateAsync(AccountSettings account, bool force) => Task.Run(async () =>
    {
        var found = Find(account);
        if (found.Snapshot.State == BotState.Stopped) return new BotResult(true, "Bot is already stopped.");
        if (found.Identity is null) return new BotResult(false, "Termination blocked: " + found.Snapshot.Detail);
        using var p = Verified(found.Identity);
        if (force) p.Kill(entireProcessTree: false);
        else
        {
            var window = NativeWindows.WindowFor(p.Id);
            if (window == 0 || !NativeWindows.PostMessage(window, 0x0010, 0, 0))
                return new BotResult(false, "Normal close unavailable. Force termination requires confirmation.", true);
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(force ? 4 : 6));
        try { await p.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { return new BotResult(false, "sBot did not exit in time. It may be showing a confirmation dialog.", !force); }
        return new BotResult(true, force ? "Verified sBot process force-terminated. No process-tree kill used." : "Verified sBot process closed normally.");
    });
}

internal static class NativeWindows
{
    private delegate bool EnumCallback(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, EnumCallback callback, nint parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint handle, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint handle, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint handle, uint command);
    [DllImport("user32.dll")] internal static extern bool IsWindowEnabled(nint handle);
    [DllImport("user32.dll")] internal static extern bool ShowWindowAsync(nint handle, int command);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool PostMessage(nint handle, uint message, nint wparam, nint lparam);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SendMessageTimeout(nint handle, uint message, nint wparam, nint lparam, uint flags, uint timeout, out nint result);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint OpenProcess(uint access, bool inherit, int processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageName(nint process, uint flags, StringBuilder path, ref uint size);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32First(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32Next(nint snapshot, ref ProcessEntry entry);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size, Usage, ProcessId;
        public nint DefaultHeapId;
        public uint ModuleId, Threads, ParentProcessId;
        public int Priority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Executable;
    }

    internal static string? ExecutablePath(int pid)
    {
        var handle = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
        if (handle == 0) return null;
        try { uint size = 32768; var buffer = new StringBuilder((int)size); return QueryFullProcessImageName(handle, 0, buffer, ref size) ? buffer.ToString() : null; }
        finally { CloseHandle(handle); }
    }
    internal static bool OwnedBy(nint window, int pid) { GetWindowThreadProcessId(window, out uint owner); return owner == pid; }
    private static HashSet<int> ProcessTreePids(int rootPid)
    {
        const uint ProcessSnapshot = 0x00000002;
        var snapshot = CreateToolhelp32Snapshot(ProcessSnapshot, 0);
        var processTree = new HashSet<int> { rootPid };
        if (snapshot == -1) return processTree;
        try
        {
            var processes = new List<(int Id, int Parent)>();
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            if (Process32First(snapshot, ref entry))
            {
                do
                {
                    processes.Add(((int)entry.ProcessId, (int)entry.ParentProcessId));
                    entry.Size = (uint)Marshal.SizeOf<ProcessEntry>();
                } while (Process32Next(snapshot, ref entry));
            }
            bool added;
            do
            {
                added = false;
                foreach (var process in processes.Where(p => processTree.Contains(p.Parent))) added |= processTree.Add(process.Id);
            } while (added);
            return processTree;
        }
        finally { CloseHandle(snapshot); }
    }
    private static int SetWindowsVisibility(IReadOnlySet<int> pids, bool visible)
    {
        int changed = 0;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint owner);
            if (pids.Contains((int)owner) && (visible || IsWindowVisible(h)) && ShowWindowAsync(h, visible ? 9 : 0)) changed++;
            return true;
        }, 0);
        return changed;
    }
    internal static bool? ClientWindowVisibility(int rootPid)
    {
        var clients = ProcessTreePids(rootPid);
        clients.Remove(rootPid);
        bool found = false, visible = false;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint owner);
            if (clients.Contains((int)owner)) { found = true; visible |= IsWindowVisible(h); }
            return true;
        }, 0);
        return found ? visible : null;
    }
    internal static int SetClientWindowsVisibility(int rootPid, bool visible)
    {
        var clients = ProcessTreePids(rootPid);
        clients.Remove(rootPid);
        return SetWindowsVisibility(clients, visible);
    }
    internal static nint WindowFor(int pid)
    {
        // sBot keeps helper windows and can show top-level dialogs (for example,
        // "Mob preferences"). Prefer its titled SBotP main form over such dialogs.
        nint visibleMain = 0, main = 0;
        EnumWindows((h, _) =>
        {
            if (!OwnedBy(h, pid) || GetWindow(h, 4) != 0) return true;
            var text = new StringBuilder(256); GetWindowText(h, text, text.Capacity);
            if (text.Length == 0) return true; // tooltips/helpers have no title
            bool isMain = text.ToString().Contains("SBotP", StringComparison.OrdinalIgnoreCase);
            if (isMain)
            {
                if (main == 0) main = h;
                if (IsWindowVisible(h)) visibleMain = h;
                return true;
            }
            return true;
        }, 0);
        return visibleMain != 0 ? visibleMain : main;
    }
    internal static nint TrainingButton(nint window, int pid, bool start)
    {
        if (window == 0) return 0;
        nint found = 0; int matches = 0;
        EnumChildWindows(window, (h, _) =>
        {
            var cls = new StringBuilder(256); GetClassName(h, cls, cls.Capacity);
            if (!cls.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase) || !OwnedBy(h, pid) || !IsWindowEnabled(h)) return true;
            var text = new StringBuilder(256); GetWindowText(h, text, text.Capacity);
            string label = text.ToString().Replace("&", "").Trim();
            if (label.Equals(start ? "Start training" : "Stop training", StringComparison.OrdinalIgnoreCase)) { found = h; matches++; }
            return true;
        }, 0);
        return matches == 1 ? found : 0;
    }
    internal static nint GameButton(nint window, int pid)
    {
        if (window == 0) return 0;
        nint found = 0; int matches = 0;
        EnumChildWindows(window, (h, _) =>
        {
            var cls = new StringBuilder(256); GetClassName(h, cls, cls.Capacity);
            if (!cls.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase) || !OwnedBy(h, pid) || !IsWindowEnabled(h)) return true;
            var text = new StringBuilder(256); GetWindowText(h, text, text.Capacity);
            string label = text.ToString().Replace("&", "").Replace(" ", "").Trim();
            // Accepts "Start Game!", "StartGame", "Start game &run" — never "Start training".
            if (label.StartsWith("StartGame", StringComparison.OrdinalIgnoreCase)) { found = h; matches++; }
            return true;
        }, 0);
        return matches == 1 ? found : 0;
    }
    internal static nint GoClientlessButton(nint window, int pid) => ExactButton(window, pid, "Go clientless");
    internal static nint ClientlessAfterGameButton(nint window, int pid) => ExactButton(window, pid, "Switch to clientless mode when entered game after");
    private static nint ExactButton(nint window, int pid, string expected)
    {
        if (window == 0) return 0;
        nint found = 0; int matches = 0;
        EnumChildWindows(window, (h, _) =>
        {
            var cls = new StringBuilder(256); GetClassName(h, cls, cls.Capacity);
            if (!cls.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase) || !OwnedBy(h, pid) || !IsWindowEnabled(h)) return true;
            var text = new StringBuilder(256); GetWindowText(h, text, text.Capacity);
            if (text.ToString().Replace("&", "").Trim().Equals(expected, StringComparison.OrdinalIgnoreCase)) { found = h; matches++; }
            return true;
        }, 0);
        return matches == 1 ? found : 0;
    }
}
