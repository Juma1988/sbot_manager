using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using SBotManager.Services;

namespace SBotManager.Models;

public sealed record ActivityEntry(DateTimeOffset Timestamp, string Account, string Severity, string Message)
{
    public string Time => Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public string Color => Severity == "Error" ? "#F1A6A6" : Severity == "Warning" ? "#F4CB7B" : "#8EE1BE";
}

public sealed class AddCard { }

public sealed class Dashboard : Bindable
{
    private readonly SettingsStore store;
    private readonly IBotService bots;
    private readonly ActivityStore? logs;
    private readonly Func<DateTimeOffset> now;
    private readonly Func<TimeSpan, Task> delay;
    private readonly object activityGate = new();
    private readonly HashSet<Guid> launchedThisSession = [];
    private readonly HashSet<Guid> automaticGameStarts = [], automaticClientHides = [];
    private bool refreshing, bulkBusy, shuttingDown, terminationMode;
    private int launchGeneration;
    public bool CloseToTray { get; private set; }
    public bool AutoHideBots { get; private set; }
    public bool AutoHideClients { get; private set; }
    public bool StartWithWindows { get; private set; }
    public bool LaunchBotsAtWindowsStartup { get; private set; }
    public StartupLaunchMode StartupLaunchMode { get; private set; }
    public bool AutoClientlessAfterGame { get; private set; }
    public bool TerminateSessionBotsOnExit { get; private set; }
    public int LaunchDelaySeconds { get; private set; }
    public string LaunchDelayToolTip => $"Change one-by-one spacing (currently {LaunchDelaySeconds} seconds)";
    public string Notice { get; private set; } = "Live process controls · training requires accessible sBot controls · EXP unavailable";
    public ObservableCollection<Account> Accounts { get; } = [];
    public ObservableCollection<object> Cards { get; } = [];
    public ObservableCollection<ActivityEntry> Activity { get; } = [];
    public IReadOnlyList<Account> SessionLaunchedRunning => Accounts.Where(a => launchedThisSession.Contains(a.Id) && a.State == BotState.Running).ToList();
    public string SessionSummary => $"{SessionLaunchedRunning.Count} launched this session · {Accounts.Count(a => a.State == BotState.Running && !launchedThisSession.Contains(a.Id))} pre-existing";
    public bool CanAdd => Accounts.Count < 10 && CanBulk;
    public bool CanBulk => !bulkBusy && !shuttingDown && !terminationMode;
    public string Summary => $"{Accounts.Count}/10 accounts   ·   {Accounts.Count(a => a.State == BotState.Running)} running   ·   {Accounts.Count(a => a.State == BotState.Stopped)} stopped   ·   {Accounts.Count(a => a.RecoveryEnabled && !a.RecoverySuspended)} recovery enabled";
    public event Action<NotificationEvent, string>? Notify;
    public NotificationConfig Notifications { get; private set; }

    public Dashboard(SettingsStore store, IBotService bots, ActivityStore? logs = null, Func<DateTimeOffset>? clock = null,
        Func<TimeSpan, Task>? delay = null)
    {
        this.store = store; this.bots = bots; this.logs = logs; now = clock ?? (() => DateTimeOffset.UtcNow); this.delay = delay ?? Task.Delay;
        var loaded = store.Load();
        CloseToTray = loaded.Settings.CloseToTray;
        AutoHideBots = loaded.Settings.AutoHideBots;
        AutoHideClients = loaded.Settings.AutoHideClients;
        StartWithWindows = loaded.Settings.StartWithWindows;
        LaunchBotsAtWindowsStartup = loaded.Settings.LaunchBotsAtWindowsStartup;
        StartupLaunchMode = loaded.Settings.StartupLaunchMode;
        AutoClientlessAfterGame = loaded.Settings.AutoClientlessAfterGame;
        TerminateSessionBotsOnExit = loaded.Settings.TerminateSessionBotsOnExit;
        LaunchDelaySeconds = Math.Clamp(loaded.Settings.LaunchDelaySeconds, 1, 300);
        Notifications = loaded.Settings.Notifications ?? NotificationConfig.Defaults();
        foreach (var config in loaded.Settings.Accounts) Attach(new Account(config));
        SyncCards();
        Log("MANAGER", "Info", "Manager opened. No bots or training started automatically.");
        if (loaded.Warning is not null) { Notice = loaded.Warning; Log("MANAGER", "Warning", loaded.Warning); }
    }

    public void SetNotifications(NotificationConfig value)
    {
        Notifications = value;
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        Changed(nameof(Notifications));
    }
    public void SetPreferences(bool closeToTray, bool autoHideBots, bool autoHideClients, bool startWithWindows, bool launchBotsAtWindowsStartup, bool terminateSessionBotsOnExit = false, StartupLaunchMode startupLaunchMode = StartupLaunchMode.LaunchOneByOne, bool autoClientlessAfterGame = false)
    {
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), closeToTray, Notifications, LaunchDelaySeconds, autoHideBots, autoHideClients, startWithWindows, launchBotsAtWindowsStartup, terminateSessionBotsOnExit, startupLaunchMode, autoClientlessAfterGame));
        CloseToTray = closeToTray;
        AutoHideBots = autoHideBots;
        AutoHideClients = autoHideClients;
        StartWithWindows = startWithWindows;
        LaunchBotsAtWindowsStartup = launchBotsAtWindowsStartup;
        TerminateSessionBotsOnExit = terminateSessionBotsOnExit;
        StartupLaunchMode = startupLaunchMode;
        AutoClientlessAfterGame = autoClientlessAfterGame;
        Changed(nameof(CloseToTray)); Changed(nameof(AutoHideBots)); Changed(nameof(AutoHideClients)); Changed(nameof(StartWithWindows)); Changed(nameof(LaunchBotsAtWindowsStartup)); Changed(nameof(TerminateSessionBotsOnExit)); Changed(nameof(StartupLaunchMode)); Changed(nameof(AutoClientlessAfterGame));
    }

    private void Attach(Account account)
    {
        Accounts.Add(account);
        account.PropertyChanged += (_, _) => { Changed(nameof(Summary)); Changed(nameof(SessionSummary)); };
        SyncCards();
    }

    private void SyncCards()
    {
        Cards.Clear();
        foreach (var account in Accounts) Cards.Add(account);
        // A fresh instance per sync: WPF's ItemContainerGenerator can silently skip
        // regenerating a container when the same item instance is re-added after a Reset.
        Cards.Add(new AddCard());
        Changed(nameof(Cards));
    }

    public void SaveAccount(Account? existing, AccountSettings config)
    {
        if (!CanBulk) throw new InvalidOperationException("Wait for the active operation to finish.");
        if (existing?.Busy == true) throw new InvalidOperationException("This account is busy.");
        if (existing is not null && existing.State is not (BotState.Stopped or BotState.NotConfigured or BotState.MissingFile) &&
            !string.Equals(existing.ExecutablePath, config.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Terminate this bot and verify it is stopped before changing its executable.");
        var list = Accounts.Select(a => a.Id == existing?.Id ? config : a.Settings).ToList();
        if (existing is null) list.Add(config);
        store.Save(new(1, list, CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame)); // Commit to disk before changing live state.
        if (existing is null) Attach(new(config));
        else
        {
            bool pathChanged = !string.Equals(existing.ExecutablePath, config.ExecutablePath, StringComparison.OrdinalIgnoreCase);
            existing.Configure(config);
            if (pathChanged) { existing.WantsRunning = false; existing.Apply(new(BotState.Checking, "Checking new executable…")); }
        }
        Changed(string.Empty);
        Log(config.Name, "Info", "Account settings saved locally. No executable was launched.");
    }

    public void Remove(Account account)
    {
        if (!CanBulk || !account.CanRemove) throw new InvalidOperationException("Only a verified stopped or unconfigured account can be removed.");
        store.Save(new(1, Accounts.Where(a => a != account).Select(a => a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        Accounts.Remove(account); SyncCards(); Changed(string.Empty);
        Log(account.Name, "Info", "Removed manager entry. No bot files were deleted.");
    }

    public void SetBulkEnabled(Account account, bool enabled)
    {
        if (!Accounts.Contains(account) || !CanBulk || account.BulkEnabled == enabled) return;
        var updated = account.Settings with { BulkEnabled = enabled };
        store.Save(new(1, Accounts.Select(a => a == account ? updated : a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        account.Configure(updated);
        Log(account.Name, "Info", enabled ? "Included in bulk actions." : "Excluded from bulk actions. Individual controls remain available.");
    }

    public void SetCloseToTray(bool value)
    {
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), value, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        CloseToTray = value; Changed(nameof(CloseToTray));
    }
    public void SetAutoHideBots(bool value)
    {
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, value, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        AutoHideBots = value; Changed(nameof(AutoHideBots));
    }
    public void SetAutoHideClients(bool value)
    {
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, value, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        AutoHideClients = value; Changed(nameof(AutoHideClients));
    }
    public void SetLaunchDelaySeconds(int value)
    {
        if (value is < 1 or > 300) throw new ArgumentOutOfRangeException(nameof(value), "Launch spacing must be 1–300 seconds.");
        LaunchDelaySeconds = value;
        store.Save(new(1, Accounts.Select(a => a.Settings).ToList(), CloseToTray, Notifications, LaunchDelaySeconds, AutoHideBots, AutoHideClients, StartWithWindows, LaunchBotsAtWindowsStartup, TerminateSessionBotsOnExit, StartupLaunchMode, AutoClientlessAfterGame));
        Changed(nameof(LaunchDelaySeconds)); Changed(nameof(LaunchDelayToolTip));
    }

    public async Task RefreshAsync(bool allowRecovery = true)
    {
        if (refreshing || shuttingDown) return;
        refreshing = true;
        try
        {
            foreach (var account in Accounts.ToArray())
            {
                if (account.Busy) continue;
                var config = account.Settings;
                var result = await Task.Run(() => bots.Inspect(config));
                if (shuttingDown || account.Busy || config != account.Settings || !Accounts.Contains(account)) continue;
                var previous = account.State;
                account.Apply(result);
                if (previous != result.State) Log(account.Name, result.State == BotState.Unknown ? "Warning" : "Info", $"Bot: {account.Status}. {result.Detail}");
                if (result.State == BotState.Running)
                {
                    if (previous == BotState.Stopped && account.RecoveryAttempts > 0)
                        account.RecordRecoveryEvent(now(), $"Running after recovery attempt {account.RecoveryAttempts}/3");
                    if (!account.RecoverySuspended) account.WantsRunning = true;
                    account.StableSince ??= now();
                    if (now() - account.StableSince >= TimeSpan.FromSeconds(60)) account.RecoveryAttempts = 0;
                }
                else account.StableSince = null;
                if (account.WantsRunning && previous == BotState.Running)
                {
                    if (result.State is BotState.Stopped or BotState.NotConfigured or BotState.MissingFile)
                    {
                        account.RecordRecoveryEvent(now(), "Stopped unexpectedly");
                        Notify?.Invoke(NotificationEvent.BotStoppedUnexpected, account.Name);
                    }
                    else if (result.State == BotState.Unknown)
                        Notify?.Invoke(NotificationEvent.BotUnknown, account.Name);
                }
                if (allowRecovery && CanBulk && result.State == BotState.Stopped && account.RecoveryEnabled && account.WantsRunning && !account.RecoverySuspended && account.RecoveryAttempts < 3 && now() >= account.RetryAt)
                {
                    account.RecoveryAttempts++;
                    account.RetryAt = now().AddSeconds(account.RecoveryAttempts switch { 1 => 10, 2 => 30, _ => 60 });
                    account.RecordRecoveryEvent(now(), $"Recovery attempt {account.RecoveryAttempts}/3 started");
                    Log(account.Name, "Warning", $"Recovery attempt {account.RecoveryAttempts}/3. sBot's own auto-login/training settings may take effect.");
                    await OpenAsync(account, true);
                    if (account.RecoveryAttempts >= 3)
                    {
                        account.RecordRecoveryEvent(now(), "Recovery stopped: retry limit reached");
                        Log(account.Name, "Warning", "Automatic retry budget used. Explicit Open bot resets the budget; 60 seconds of stable running also resets it.");
                        Notify?.Invoke(NotificationEvent.RecoveryBudget, account.Name);
                    }
                }
                account.NotifyAll();
            }
        }
        finally { refreshing = false; }
    }

    private async Task<BotResult> RunAsync(Account a, Func<Task<BotResult>> operation, bool termination = false)
    {
        if (a.Busy || shuttingDown || (terminationMode && !termination)) return new(false, "Account busy, terminating, or manager exiting.");
        a.Busy = true;
        try
        {
            var result = await operation();
            Log(a.Name, result.Success ? "Info" : "Warning", result.Message);
            a.Apply(await Task.Run(() => bots.Inspect(a.Settings)));
            return result;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Log(a.Name, "Error", e.Message); return new(false, e.Message);
        }
        finally { a.Busy = false; }
    }

    public async Task<BotResult> OpenAsync(Account a, bool recovery = false)
    {
        if (!a.CanOpen || shuttingDown || terminationMode) return new(false, "Open unavailable until the account is verified stopped and no termination is active.");
        a.WantsRunning = true;
        if (!recovery) { a.RecoveryAttempts = 0; a.RecoverySuspended = false; }
        var launch = await RunAsync(a, () => bots.OpenAsync(a.Settings));
        if (launch.Success)
        {
            launchedThisSession.Add(a.Id); Changed(nameof(SessionSummary));
            QueueAutomaticGameStart(a);
            if (AutoHideBots && a.CanShowHide) await RunAsync(a, () => bots.SetVisibilityAsync(a.Settings, false));
            if (AutoHideClients) QueueAutomaticClientHide(a);
        }
        return launch;
    }

    // sBot may expose Start Game shortly after its process is running. Keep that
    // readiness wait off the bulk-launch path so one late control never delays
    // launching the next configured bot.
    private void QueueAutomaticGameStart(Account account)
    {
        if (!automaticGameStarts.Add(account.Id)) return;
        _ = StartGameWhenReadyAsync(account);
    }

    private async Task StartGameWhenReadyAsync(Account account)
    {
        try
        {
            var timeout = Stopwatch.StartNew();
            string? lastFailure = null;
            while (timeout.Elapsed < TimeSpan.FromSeconds(60))
            {
                if (shuttingDown || terminationMode || !account.WantsRunning || !Accounts.Contains(account)) return;
                var snapshot = await Task.Run(() => bots.Inspect(account.Settings));
                if (shuttingDown || terminationMode || !account.WantsRunning || !Accounts.Contains(account)) return;
                if (snapshot.State != BotState.Running) return;
                if (snapshot.CanStartGame)
                {
                    var clientless = await bots.SetClientlessAfterGameAsync(account.Settings, AutoClientlessAfterGame);
                    if (!clientless.Success) Log(account.Name, "Warning", clientless.Message);
                    var result = await bots.StartGameAsync(account.Settings);
                    if (result.Success)
                    {
                        Log(account.Name, "Info", result.Message);
                        return;
                    }
                    lastFailure = result.Message;
                }
                await delay(TimeSpan.FromSeconds(2));
            }
            Log(account.Name, "Warning", "Automatic Start Game did not complete within 60 seconds. " +
                (lastFailure ?? "The Start Game control was not available.") + " Open the bot window and start it manually.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Log(account.Name, "Warning", "Automatic Start Game failed: " + e.Message);
        }
        finally { automaticGameStarts.Remove(account.Id); }
    }
    private void QueueAutomaticClientHide(Account account)
    {
        if (!automaticClientHides.Add(account.Id)) return;
        _ = HideClientWhenReadyAsync(account);
    }
    private async Task HideClientWhenReadyAsync(Account account)
    {
        try
        {
            await delay(TimeSpan.FromSeconds(3));
            if (shuttingDown || terminationMode || !Accounts.Contains(account) || !AutoHideClients) return;
            account.Apply(await Task.Run(() => bots.Inspect(account.Settings)));
            if (account.CanShowHideClient) await RunAsync(account, () => bots.SetRelatedClientVisibilityAsync(account.Settings, false));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Log(account.Name, "Warning", "Automatic client hide failed: " + e.Message);
        }
        finally { automaticClientHides.Remove(account.Id); }
    }
    public async Task<BotResult> TrainingAsync(Account a, bool start)
    {
        if (terminationMode || shuttingDown) return new BotResult(false, "Training blocked during termination or exit.");
        if (!start)
        {
            a.RecoverySuspended = true; a.WantsRunning = false; a.NotifyAll();
        }
        if (start ? !a.CanStartTraining : !a.CanStopTraining)
        {
            Log(a.Name, "Warning", "Training command unavailable: accessible control not verified. No fallback to termination.");
            return new BotResult(false, "Training control unavailable.");
        }
        if (start) { a.RecoverySuspended = false; a.WantsRunning = true; }
        var result = await RunAsync(a, () => bots.TrainingAsync(a.Settings, start));
        if (result.Success) await RefreshTrainingControlAsync(a, expectStartControl: !start);
        return result;
    }
    private async Task RefreshTrainingControlAsync(Account account, bool expectStartControl)
    {
        // sBot can update its Start/Stop button after accepting BM_CLICK. Poll only
        // the verified native state briefly so the card action reflects the result.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            account.Apply(await Task.Run(() => bots.Inspect(account.Settings)));
            if (expectStartControl ? account.CanStartTraining : account.CanStopTraining) return;
            await delay(TimeSpan.FromMilliseconds(200));
        }
        Log(account.Name, "Warning", "Training command was delivered, but sBot did not expose the updated Start/Stop control within 2 seconds.");
    }
    public Task<BotResult> ShowHideAsync(Account a)
    {
        if (!a.CanShowHide) return Task.FromResult(new BotResult(false, "Window command unavailable until the bot is verified running."));
        bool visible = a.Snapshot.WindowVisible != false;
        return RunAsync(a, () => bots.SetVisibilityAsync(a.Settings, !visible));
    }
    public Task<BotResult> ShowBotAsync(Account a)
    {
        if (!a.CanShowHide) return Task.FromResult(new BotResult(false, "Bot window command unavailable until the real sBot main window is verified."));
        return RunAsync(a, () => bots.SetVisibilityAsync(a.Settings, true));
    }
    public async Task ShowSessionBotsAsync()
    {
        foreach (var account in SessionLaunchedRunning.Where(a => a.CanShowHide)) await ShowBotAsync(account);
    }
    public async Task SetAllBotsVisibilityAsync(bool visible)
    {
        if (!CanBulk) return;
        foreach (var account in Accounts.Where(a => a.BulkEnabled && a.CanShowHide).ToArray())
            await RunAsync(account, () => bots.SetVisibilityAsync(account.Settings, visible));
    }
    public Task<BotResult> ShowHideClientAsync(Account a)
    {
        if (!a.CanShowHideClient) return Task.FromResult(new BotResult(false, "Client window command unavailable until a related Silkroad window is verified."));
        bool visible = a.Snapshot.ClientWindowVisible != false;
        return RunAsync(a, () => bots.SetRelatedClientVisibilityAsync(a.Settings, !visible));
    }
    public async Task SetAllClientsVisibilityAsync(bool visible)
    {
        if (!CanBulk) return;
        foreach (var account in Accounts.Where(a => a.BulkEnabled && a.CanShowHideClient).ToArray())
            await RunAsync(account, () => bots.SetRelatedClientVisibilityAsync(account.Settings, visible));
    }
    public async Task SyncAutomaticClientlessForRunningAsync()
    {
        if (!CanBulk) return;
        foreach (var account in Accounts.Where(a => a.State == BotState.Running).ToArray())
            await RunAsync(account, () => bots.SetClientlessAfterGameAsync(account.Settings, AutoClientlessAfterGame));
    }
    public async Task GoClientlessAllAsync()
    {
        if (!CanBulk) return;
        foreach (var account in Accounts.Where(a => a.BulkEnabled && a.State == BotState.Running).ToArray())
            await RunAsync(account, () => bots.GoClientlessAsync(account.Settings));
    }
    public async Task<BotResult> TerminateAsync(Account a, bool force = false)
    {
        a.RecoverySuspended = true; a.WantsRunning = false; a.NotifyAll();
        var result = await RunAsync(a, () => bots.TerminateAsync(a.Settings, force), true);
        if (result.NeedsForce) Notify?.Invoke(NotificationEvent.ForceTerminateRequired, a.Name);
        return result;
    }
    public async Task<IReadOnlyList<string>> CloseSessionLaunchedAsync()
    {
        var failures = new List<string>();
        foreach (var account in SessionLaunchedRunning)
        {
            var result = await TerminateAsync(account);
            if (!result.Success) result = await TerminateAsync(account, true);
            if (!result.Success) failures.Add(account.Name + ": " + result.Message);
        }
        return failures;
    }
    public async Task BulkAsync(string action)
    {
        if (!CanBulk) return;
        int generation = ++launchGeneration;
        bulkBusy = true; Changed(string.Empty);
        try
        {
            var launched = new List<Account>();
            bool sequential = action == "open-sequential";
            bool opening = action is "open" or "open-sequential";
            var targets = opening ? Accounts.Where(a => a.BulkEnabled && a.CanOpen).ToArray() : Accounts.Where(a => a.BulkEnabled).ToArray();
            for (int i = 0; i < targets.Length; i++)
            {
                var a = targets[i];
                if (shuttingDown || generation != launchGeneration) break;
                if (opening)
                {
                    var result = await OpenAsync(a);
                    if (result.Success || a.State == BotState.Running) launched.Add(a);
                    if (sequential && result.Success)
                    {
                        if (i < targets.Length - 1) await Task.Delay(TimeSpan.FromSeconds(LaunchDelaySeconds));
                    }
                    else if (!sequential && i < targets.Length - 1) await Task.Delay(1000);
                }
                else await TrainingAsync(a, action == "start");
            }
        }
        finally { bulkBusy = false; Changed(string.Empty); }
    }
    public void SuspendRecovery()
    {
        ++launchGeneration;
        foreach (var a in Accounts) { a.RecoverySuspended = true; a.WantsRunning = false; a.NotifyAll(); }
    }
    public void Shutdown() { shuttingDown = true; SuspendRecovery(); }
    public void BeginTermination() { terminationMode = true; SuspendRecovery(); Changed(string.Empty); }
    public void EndTermination() { terminationMode = false; Changed(string.Empty); }

    public void Log(string account, string severity, string message)
    {
        var entry = new ActivityEntry(now(), account, severity, message);
        lock (activityGate)
        {
            Activity.Insert(0, entry);
            while (Activity.Count > 500) Activity.RemoveAt(Activity.Count - 1);
        }
        try { logs?.Append(entry); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { Notice = "Log files could not be written; events remain available in this session."; Changed(nameof(Notice)); }
    }
}
