using System.Diagnostics;
using System.Text.Json;
using SBotManager.Models;
using SBotManager.Services;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); checks++;
}
void Reject(Action action, string name)
{
    bool rejected = false;
    try { action(); } catch (Exception e) when (e is ArgumentException or InvalidOperationException) { rejected = true; }
    Check(rejected, name);
}
async Task WaitUntilAsync(Func<bool> condition, string name)
{
    for (int i = 0; i < 30; i++)
    {
        if (condition()) return;
        await Task.Delay(100);
    }
    throw new Exception("FAIL: Timed out waiting for " + name);
}
var root = Path.GetFullPath(Path.Combine("artifacts", "checks-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);
var store = new SettingsStore(Path.Combine(root, "settings"));
var fake = new FakeBots();
DateTimeOffset now = DateTimeOffset.UtcNow;
var dashboard = new Dashboard(store, fake, clock: () => now);
Check(dashboard.Accounts.Count == 0 && fake.Opens == 0, "Defaults load no seeded accounts and launch nothing");
var demo = new[] { "AccountA", "AccountB", "AccountC", "AccountD" };
for (int i = 0; i < demo.Length; i++)
{
    dashboard.SaveAccount(null, new(Guid.NewGuid(), demo[i], ""));
    if (i == 0) Check(!File.Exists(store.FilePath + ".bak"), "First save creates settings without overwriting anything");
}
Check(dashboard.Accounts.Count == 4 && fake.Opens == 0, "Test harness adds its own accounts without launching");
await dashboard.RefreshAsync();
Check(dashboard.Accounts.All(a => a.State == BotState.Stopped), "Process snapshots update cards");
var statusCard = new Account(new(Guid.NewGuid(), "StatusCheck", ""));
statusCard.Apply(new(BotState.Running, "Test", CanStartTraining: true));
Check(statusCard.Status == "READY" && statusCard.TrainingActionText == "Start training" && statusCard.PrimaryActionText == "Terminate" && statusCard.CanPrimaryAction, "Running card exposes Terminate as its primary bottom action");
statusCard.Apply(new(BotState.Running, "Test", CanStopTraining: true));
Check(statusCard.Status == "TRAINING" && statusCard.TrainingActionText == "Stop training" && statusCard.PrimaryActionText == "Terminate", "Training card keeps Terminate separate from the training action");
statusCard.Apply(new(BotState.Stopped, "Test"));
Check(statusCard.BulkEnabled && statusCard.CardStatus == "STOPPED" && statusCard.PrimaryActionText == "Launch", "Stopped card remains individually launchable while enabled for bulk actions");
var xar = dashboard.Accounts[0];
Check(!xar.CanStartGame, "Start Game unavailable until bot verified running");
dashboard.SaveAccount(xar, xar.Settings with { ExecutablePath = @"C:\Bots\AccountA\bot.exe" });
Check(new Dashboard(store, fake).Accounts[0].ExecutablePath == @"C:\Bots\AccountA\bot.exe", "Executable path survives reload");
dashboard.SetCloseToTray(false);
Check(!new Dashboard(store, fake).CloseToTray && File.Exists(store.FilePath + ".bak"), "Tray setting and atomic backup persist");
Check(dashboard.AutoHideBots && dashboard.AutoHideClients, "Auto-hide settings default on");
dashboard.SetAutoHideBots(false); dashboard.SetAutoHideClients(false);
Check(!new Dashboard(store, fake).AutoHideBots && !new Dashboard(store, fake).AutoHideClients, "Auto-hide settings persist");
dashboard.SetAutoHideBots(true); dashboard.SetAutoHideClients(true);
  dashboard.SetPreferences(dashboard.CloseToTray, dashboard.AutoHideBots, dashboard.AutoHideClients, true, true, true, StartupLaunchMode.LaunchOneByOne);
  Check(new Dashboard(store, fake).StartWithWindows && new Dashboard(store, fake).LaunchBotsAtWindowsStartup && new Dashboard(store, fake).TerminateSessionBotsOnExit && new Dashboard(store, fake).StartupLaunchMode == StartupLaunchMode.LaunchOneByOne, "Windows startup mode and exit preferences persist");
  dashboard.SetPreferences(dashboard.CloseToTray, dashboard.AutoHideBots, dashboard.AutoHideClients, false, false, false, StartupLaunchMode.LaunchOneByOne, true);
Check(new Dashboard(store, fake).AutoClientlessAfterGame, "Automatic clientless preference persists");
  dashboard.SetPreferences(dashboard.CloseToTray, dashboard.AutoHideBots, dashboard.AutoHideClients, false, false, false);
Check(dashboard.LaunchDelaySeconds == 20, "Launch 1-1 defaults to 20 seconds");
Check(dashboard.StartupLaunchMode == StartupLaunchMode.LaunchOneByOne, "Windows startup defaults to Launch 1-1 mode");
dashboard.SetLaunchDelaySeconds(25);
Check(new Dashboard(store, fake).LaunchDelaySeconds == 25, "Launch 1-1 spacing persists through reload");
Reject(() => dashboard.SetLaunchDelaySeconds(0), "Launch 1-1 spacing rejects values below one second");
Reject(() => dashboard.SaveAccount(null, new(Guid.NewGuid(), "Other", xar.ExecutablePath)), "Duplicate path rejected");
Reject(() => dashboard.SaveAccount(null, new(Guid.NewGuid(), "AccountA", "")), "Duplicate name rejected");
Reject(() => dashboard.SaveAccount(null, new(Guid.NewGuid(), "Bad", "relative.exe")), "Relative executable rejected");
Reject(() => dashboard.SaveAccount(null, new(Guid.NewGuid(), "Bad", @"\\server\bot.exe")), "Network executable rejected");
for (int i = 5; i <= 10; i++) dashboard.SaveAccount(null, new(Guid.NewGuid(), "Character" + i, ""));
Check(dashboard.Accounts.Count == 10 && !dashboard.CanAdd, "Add hidden at ten accounts");
Reject(() => dashboard.SaveAccount(null, new(Guid.NewGuid(), "Eleven", "")), "Model enforces ten-account limit");
await dashboard.RefreshAsync(false);
dashboard.Remove(dashboard.Accounts[^1]);
Check(dashboard.CanAdd && dashboard.Accounts.Count == 9, "Removing entry restores Add");
await dashboard.RefreshAsync(false);
await dashboard.OpenAsync(xar);
Check(fake.Opens == 1 && xar.State == BotState.Running, "Open delegates and verifies process");
Check(dashboard.SessionLaunchedRunning.Single() == xar, "Only a successful Manager launch is tracked for this session");
Check(dashboard.SessionSummary.StartsWith("1 launched this session"), "Session summary distinguishes Manager-launched bots");
        await WaitUntilAsync(() => fake.GameStarts == 1, "automatic Start Game after Open");
Check(xar.WantsRunning, "Open retains running intent while automatic Start Game runs in the background");
await dashboard.GoClientlessAllAsync();
Check(fake.GoClientless == 1, "Go clientless all targets verified running bots");
await WaitUntilAsync(() => xar.Snapshot.WindowVisible == false, "Auto-hide sBot hides the verified bot window after launch");
        await dashboard.SetAllBotsVisibilityAsync(false);
        Check(xar.Snapshot.WindowVisible == false, "Hide all bots hides every verified sBot window");
        await dashboard.SetAllBotsVisibilityAsync(true);
        Check(xar.Snapshot.WindowVisible == true, "Show all bots restores every verified sBot window");
        Check(xar.CanShowHideClient, "Related client visibility is available when a client window is verified");
await dashboard.ShowHideClientAsync(xar);
Check(xar.Snapshot.ClientWindowVisible == false, "Hide client keeps the bot running and hides only its client window");
await dashboard.ShowHideClientAsync(xar);
Check(xar.Snapshot.ClientWindowVisible == true, "Show client restores its related client window");
await dashboard.SetAllClientsVisibilityAsync(false);
Check(xar.Snapshot.ClientWindowVisible == false, "Hide all clients hides every verified related client window");
await dashboard.SetAllClientsVisibilityAsync(true);
Check(xar.Snapshot.ClientWindowVisible == true, "Show all clients restores every verified related client window");
await dashboard.OpenAsync(xar);
Check(fake.Opens == 1, "Duplicate open blocked");

var launchStore = new SettingsStore(Path.Combine(root, "launch-all"));
var launchFake = new FakeBots();
var launchDashboard = new Dashboard(launchStore, launchFake, clock: () => now);
launchDashboard.SetPreferences(launchDashboard.CloseToTray, launchDashboard.AutoHideBots, launchDashboard.AutoHideClients, false, false, false, StartupLaunchMode.LaunchOneByOne, true);
launchDashboard.SaveAccount(null, new(Guid.NewGuid(), "Launch", @"C:\Bots\Launch\bot.exe", RecoveryEnabled: false));
await launchDashboard.RefreshAsync();
var launched = launchDashboard.Accounts.Single();
launchDashboard.SetBulkEnabled(launched, false);
Check(!launched.BulkEnabled && launched.CardStatus == "DISABLED" && new Dashboard(launchStore, launchFake).Accounts.Single().BulkEnabled == false && launched.CanOpen, "Disabled account persists, is visibly disabled, and remains individually launchable");
await launchDashboard.BulkAsync("open");
Check(launched.State == BotState.Stopped && launchFake.Opens == 0, "Launch all skips a disabled account");
await launchDashboard.OpenAsync(launched);
Check(launched.State == BotState.Running && launchFake.Hidden.ContainsKey(launched.Id),
    "Individual Launch keeps a disabled bulk account available and hides its related windows");
await WaitUntilAsync(() => launchFake.GameStarts == 1, "automatic Start Game after Launch all");
Check(launchFake.AutoClientless == 1, "Automatic clientless is enabled before Start Game for Manager launches");
launchDashboard.SetPreferences(launchDashboard.CloseToTray, launchDashboard.AutoHideBots, launchDashboard.AutoHideClients, false, false, false, StartupLaunchMode.LaunchOneByOne, false);
await launchDashboard.SyncAutomaticClientlessForRunningAsync();
Check(launchFake.AutoClientless == 2, "Turning off automatic clientless synchronizes the native setting for running bots");

var clientlessSafetyStore = new SettingsStore(Path.Combine(root, "clientless-safety"));
var clientlessSafetyFake = new FakeBots { ClientlessSyncFails = true };
var clientlessSafetyDashboard = new Dashboard(clientlessSafetyStore, clientlessSafetyFake, clock: () => now, delay: _ => Task.CompletedTask);
clientlessSafetyDashboard.SetPreferences(clientlessSafetyDashboard.CloseToTray, clientlessSafetyDashboard.AutoHideBots, clientlessSafetyDashboard.AutoHideClients, false, false, false, StartupLaunchMode.LaunchOneByOne, true);
clientlessSafetyDashboard.SaveAccount(null, new(Guid.NewGuid(), "Safety", @"C:\Bots\Safety\bot.exe"));
await clientlessSafetyDashboard.RefreshAsync();
await clientlessSafetyDashboard.OpenAsync(clientlessSafetyDashboard.Accounts.Single());
await WaitUntilAsync(() => clientlessSafetyDashboard.Activity.Any(entry => entry.Message.StartsWith("Automatic Start Game blocked:")), "automatic Start Game safety block");
Check(clientlessSafetyFake.GameStarts == 0, "Automatic Start Game fails closed when clientless state cannot be confirmed");

var retryStore = new SettingsStore(Path.Combine(root, "delayed-game-start"));
var retryFake = new FakeBots { GameStartFailClicks = 2 };
var retryDashboard = new Dashboard(retryStore, retryFake, clock: () => now, delay: _ => Task.CompletedTask);
retryDashboard.SaveAccount(null, new(Guid.NewGuid(), "Delayed", @"C:\Bots\Delayed\bot.exe", RecoveryEnabled: false));
await retryDashboard.RefreshAsync();
await retryDashboard.OpenAsync(retryDashboard.Accounts.Single());
await WaitUntilAsync(() => retryFake.GameStarts == 3, "automatic Start Game retries after sBot initially rejects clicks");
Check(retryFake.GameStarts == 3, "Automatic Start Game retries in the background until sBot accepts the click");
await WaitUntilAsync(() => retryFake.ClientHidden.Count == 1, "Auto-hide SRO client hides a verified related client after launch");

var exitStore = new SettingsStore(Path.Combine(root, "tray-exit"));
var exitFake = new FakeBots();
var exitDashboard = new Dashboard(exitStore, exitFake, clock: () => now);
exitDashboard.SaveAccount(null, new(Guid.NewGuid(), "Session", @"C:\Bots\Session\bot.exe"));
exitDashboard.SaveAccount(null, new(Guid.NewGuid(), "Existing", @"C:\Bots\Existing\bot.exe"));
await exitDashboard.RefreshAsync();
var sessionBot = exitDashboard.Accounts[0];
var existingBot = exitDashboard.Accounts[1];
await exitDashboard.OpenAsync(sessionBot);
exitFake.Running.Add(existingBot.Id); await exitDashboard.RefreshAsync();
Check(exitDashboard.SessionLaunchedRunning.Single() == sessionBot, "Bot already running at Manager startup is not session-launched");
exitFake.ForceCloseRequired = true;
var exitFailures = await exitDashboard.CloseSessionLaunchedAsync();
Check(exitFailures.Count == 0 && sessionBot.State == BotState.Stopped && existingBot.State == BotState.Running && exitFake.Terminations == 2,
    "Tray exit closes only the session bot and force-closes it after normal close fails");

Reject(() => dashboard.SaveAccount(xar, xar.Settings with { ExecutablePath = @"C:\Other.exe" }), "Running path cannot change");
Reject(() => dashboard.Remove(xar), "Running account cannot be removed");
await dashboard.TrainingAsync(xar, false);
Check(fake.Stops == 1 && fake.Terminations == 0 && xar.RecoverySuspended && xar.TrainingActionText == "Start training", "Stop training does not terminate, pauses recovery, and changes the card action to Start training");
await dashboard.TrainingAsync(xar, true);
Check(fake.Starts == 1 && !xar.RecoverySuspended && xar.TrainingActionText == "Stop training", "Explicit training start restores desired running intent and changes the card action to Stop training");
dashboard.BeginTermination();
await dashboard.TrainingAsync(xar, true);
Check(fake.Starts == 1 && !dashboard.CanBulk, "Training blocked during termination");
await dashboard.TerminateAsync(xar);
dashboard.EndTermination();
Check(fake.Terminations == 1 && xar.State == BotState.Stopped && !xar.WantsRunning, "Terminate clears recovery intent");

dashboard.SaveAccount(xar, xar.Settings with { RecoveryEnabled = true });
await dashboard.OpenAsync(xar);
fake.FailLaunches = true; fake.Running.Remove(xar.Id);
for (int i = 0; i < 3; i++) { now = now.AddSeconds(61); await dashboard.RefreshAsync(); }
int attempted = fake.Opens;
now = now.AddMinutes(5); await dashboard.RefreshAsync();
Check(xar.RecoveryAttempts == 3 && fake.Opens == attempted, "Recovery stops after three attempts");
fake.FailLaunches = false;
await dashboard.OpenAsync(xar);
Check(xar.RecoveryAttempts == 0, "Explicit Open resets retry budget");
await dashboard.TrainingAsync(xar, false); fake.Running.Remove(xar.Id);
now = now.AddMinutes(2); attempted = fake.Opens; await dashboard.RefreshAsync();
Check(fake.Opens == attempted, "Intentional stop prevents recovery relaunch");
fake.Unknown = true; xar.WantsRunning = true; xar.RecoverySuspended = false;
await dashboard.RefreshAsync();
Check(fake.Opens == attempted && xar.State == BotState.Unknown, "Unknown process state blocks recovery");
fake.Unknown = false;
dashboard.Log("AccountA", "Error", "Test error"); dashboard.Log("AccountB", "Info", "Test info");
Check(dashboard.Activity.Any(e => e.Account == "AccountA" && e.Severity == "Error"), "Activity keeps full event history with severity");
Check(dashboard.Activity.Any(e => e.Account == "AccountB" && e.Severity == "Info"), "Activity keeps events from every account");
for (int i = 0; i < 520; i++) dashboard.Log("MANAGER", "Info", "Bounded event");
Check(dashboard.Activity.Count == 500, "In-memory logs bounded");
File.WriteAllText(store.FilePath, "{bad json");
Check(store.Load().Recovered && store.Load().Settings.Accounts.Count > 0, "Corrupt settings recover from backup");
File.WriteAllText(store.FilePath + ".bak", "{bad backup");
Check(store.Load().Settings.Accounts.Count == 0 && store.Load().Warning is not null, "Double corruption loads no targets and reports warning");
Check(xar.ExpText == "EXP unavailable", "EXP never fabricates a value");

// Notification configuration: defaults, formatting, master switch, persistence.
var defaults = NotificationConfig.Defaults();
Check(defaults.MasterEnabled && defaults.Format(NotificationEvent.RecoveryBudget, "AccountA").Contains("AccountA") && defaults.Format(NotificationEvent.BotStoppedUnexpected, "AccountB").Contains("AccountB"),
    "Notification defaults enabled with account substitution");
Check(new NotificationConfig(MasterEnabled: false).Format(NotificationEvent.RecoveryBudget, "AccountA") == "",
    "Master switch suppresses all notification balloons");
Check(new NotificationConfig(RecoveryBudget: new(false, "message")).Format(NotificationEvent.RecoveryBudget, "AccountA") == "",
    "Disabled rule suppresses that balloon");
Check(new NotificationConfig(RecoveryBudget: new(true, "   ")).Format(NotificationEvent.RecoveryBudget, "AccountA") == "",
    "Blank message suppresses the balloon");
var customNote = new NotificationConfig(MasterEnabled: false, ForceTerminateRequired: new(true, "{Account} needs force"));
dashboard.SetNotifications(customNote);
Check(new Dashboard(store, fake, clock: () => now).Notifications.MasterEnabled == false &&
      new Dashboard(store, fake, clock: () => now).Notifications.For(NotificationEvent.ForceTerminateRequired).Message.Contains("{Account}"),
    "Notification settings persist through reload");

// Notification events: unexpected stop, unverifiable process, retry budget, force-close need.
var fired = new List<(NotificationEvent, string)>();
void OnNotify(NotificationEvent e, string a) { fired.Add((e, a)); }
dashboard.Notify += OnNotify;
dashboard.SetNotifications(NotificationConfig.Defaults());
var xur = dashboard.Accounts[3];
await dashboard.RefreshAsync(); // Earlier Unknown-state check left all accounts Unknown; settle before editing.
dashboard.SaveAccount(xur, xur.Settings with { ExecutablePath = @"C:\Bots\AccountD\bot.exe", RecoveryEnabled = false });
await dashboard.RefreshAsync(); // Path change marks Checking; settle to Stopped before opening.
await dashboard.OpenAsync(xur);
fired.Clear();
fake.Running.Remove(xur.Id); await dashboard.RefreshAsync();
Check(fired.Contains((NotificationEvent.BotStoppedUnexpected, "AccountD")) && xur.State == BotState.Stopped,
    "Unexpected stop fires notification while user wanted it running");
await dashboard.OpenAsync(xur);
fired.Clear();
fake.Unknown = true; await dashboard.RefreshAsync(); fake.Unknown = false;
Check(fired.Contains((NotificationEvent.BotUnknown, "AccountD")) && xur.State == BotState.Unknown,
    "Unverifiable process fires notification while user wanted it running");
await dashboard.TerminateAsync(xur);
fired.Clear();
fake.ForceCloseRequired = true;
var forceResult = await dashboard.TerminateAsync(xur, false);
fake.ForceCloseRequired = false;
Check(forceResult.NeedsForce && fired.Contains((NotificationEvent.ForceTerminateRequired, "AccountD")),
    "Force-close need fires notification without silently killing");
fired.Clear();
fake.ForceCloseRequired = true;
await dashboard.TerminateAsync(xur, true);
fake.ForceCloseRequired = false;
Check(!fired.Contains((NotificationEvent.ForceTerminateRequired, "AccountD")),
    "Explicit force close does not re-announce the need");
dashboard.Notify -= OnNotify;
dashboard.SaveAccount(xur, xur.Settings with { RecoveryEnabled = true });
await dashboard.OpenAsync(xur);
fired.Clear(); dashboard.Notify += OnNotify;
fake.FailLaunches = true; fake.Running.Remove(xur.Id);
for (int i = 0; i < 3; i++) { now = now.AddSeconds(61); await dashboard.RefreshAsync(); }
fake.FailLaunches = false;
Check(fired.Contains((NotificationEvent.RecoveryBudget, "AccountD")) && xur.RecoveryAttempts == 3,
    "Retry-budget exhaustion fires notification");

// Real Win32 integration against our harmless fixture, never a user's bot.
var native = new WindowsBotService();
var fixtureSource = Path.GetFullPath("tests/BotFixture/bin/Debug/net10.0-windows");
var fixtureDir = Path.Combine(root, "fixture"); Directory.CreateDirectory(fixtureDir);
foreach (var file in Directory.GetFiles(fixtureSource)) File.Copy(file, Path.Combine(fixtureDir, Path.GetFileName(file)));
var config = new AccountSettings(Guid.NewGuid(), "Fixture", Path.Combine(fixtureDir, "BotFixture.exe"));
try
{
    Check(native.Inspect(config).State == BotState.Stopped, "Native stopped detection by path");
    Check((await native.OpenAsync(config)).Success, "Native fixture launch and verification");
    var inspect = native.Inspect(config);
    Check(inspect.CanStartTraining && inspect.CanStopTraining && inspect.CanStartGame, "Exact native sBot controls discovered");
    Check(!(await native.OpenAsync(config)).Success, "Native duplicate process launch refused");
    Check((await native.TrainingAsync(config, true)).Success, "Native Start training delivered");
    Check((await native.TrainingAsync(config, false)).Success, "Native Stop training delivered");
    await Task.Delay(200);
    Check(File.ReadAllLines(Path.Combine(fixtureDir, "commands.log")).SequenceEqual(new[] { "START", "STOP" }), "Fixture independently confirms exact button commands");
    Check((await native.StartGameAsync(config)).Success, "Native Start Game command delivered");
    await Task.Delay(200);
    Check(File.ReadAllLines(Path.Combine(fixtureDir, "commands.log")).SequenceEqual(new[] { "START", "STOP", "GAME" }), "Fixture independently confirms Start Game command");
    Check((await native.SetClientlessAfterGameAsync(config, true)).Success, "Native automatic clientless control delivered");
    Check((await native.SetClientlessAfterGameAsync(config, false)).Success, "Native automatic clientless control can be disabled");
    Check((await native.GoClientlessAsync(config)).Success, "Native Go clientless command delivered");
    await Task.Delay(200);
    Check(File.ReadAllLines(Path.Combine(fixtureDir, "commands.log")).SequenceEqual(new[] { "START", "STOP", "GAME", "CLIENTLESS" }), "Fixture independently confirms training, game, and immediate clientless commands");
    Check(native.Inspect(config).State == BotState.Running, "Stop training leaves process running");
    Check((await native.SetVisibilityAsync(config, false)).Success && native.Inspect(config).WindowVisible == false, "Native hide bot window verified and stays running");
    Check((await native.SetVisibilityAsync(config, true)).Success, "Native show bot window restores fixture window");
    Check((await native.TerminateAsync(config, false)).Success && native.Inspect(config).State == BotState.Stopped, "Graceful close verified");
    File.WriteAllText(Path.Combine(fixtureDir, "refuse-close.flag"), "test");
    await native.OpenAsync(config);
    var close = await native.TerminateAsync(config, false);
    Check(close.NeedsForce && native.Inspect(config).State == BotState.Running, "Normal-close timeout requests force rather than silently killing");
    Check((await native.TerminateAsync(config, true)).Success, "Explicit force termination verified");
    for (int i = 0; i < 20 && native.Inspect(config).State != BotState.Stopped; i++) await Task.Delay(100);
    File.Delete(Path.Combine(fixtureDir, "refuse-close.flag"));
    File.WriteAllText(Path.Combine(fixtureDir, "no-controls.flag"), "test");
    var unsupportedLaunch = await native.OpenAsync(config);
    Check(unsupportedLaunch.Success, "Fixture without training controls launched: " + unsupportedLaunch.Message);
    Check(!native.Inspect(config).CanStartTraining && !(await native.TrainingAsync(config, false)).Success, "Unsupported training controls fail closed");
    Check(!native.Inspect(config).CanStartGame && !(await native.StartGameAsync(config)).Success, "Unsupported Start Game fails closed");
    Check(native.Inspect(config).State == BotState.Running, "Unavailable Start Game never terminates process");
    await native.TerminateAsync(config, true);
}
finally
{
    if (native.Inspect(config).State == BotState.Running) await native.TerminateAsync(config, true);
}
Console.WriteLine($"\n{checks} checks passed. Isolated evidence directory: {root}");

sealed class FakeBots : IBotService
{
    public HashSet<Guid> Running = [];
    public Dictionary<Guid, bool> Hidden = [];
    public Dictionary<Guid, bool> ClientHidden = [];
    public int Opens, Starts, Stops, GameStarts, AutoClientless, GoClientless, Terminations;
    public bool FailLaunches, Unknown, ForceCloseRequired, ClientlessSyncFails;
    public HashSet<Guid> Training = [];
    public int GameControlReadyAfter = 0; // Inspect calls before Start Game control verifies (-1 = never)
    public int GameStartFailClicks; // Remaining Start Game clicks that fail even though the control verified
    private int inspects;
    public ProcessSnapshot Inspect(AccountSettings a) { inspects++;
        bool canGame = Running.Contains(a.Id) && GameControlReadyAfter >= 0 && inspects >= GameControlReadyAfter;
        return Unknown ? new(BotState.Unknown, "Test access failure") :
            Running.Contains(a.Id) ? new(BotState.Running, "Test process", 1, !Training.Contains(a.Id), Training.Contains(a.Id), canGame, WindowVisible: !Hidden.ContainsKey(a.Id), ClientWindowVisible: !ClientHidden.ContainsKey(a.Id)) : new(BotState.Stopped, "Test stopped");
    }
    public Task<BotResult> OpenAsync(AccountSettings a) { Opens++; if (!FailLaunches) { Running.Add(a.Id); Training.Add(a.Id); Hidden.Remove(a.Id); ClientHidden.Remove(a.Id); } return Task.FromResult(new BotResult(!FailLaunches, "Test open")); }
    public Task<BotResult> TrainingAsync(AccountSettings a, bool start) { if (start) { Starts++; Training.Add(a.Id); } else { Stops++; Training.Remove(a.Id); } return Task.FromResult(new BotResult(true, "Test training")); }
    public Task<BotResult> StartGameAsync(AccountSettings a) { GameStarts++;
        return Task.FromResult(new BotResult(Running.Contains(a.Id) && GameStartFailClicks-- <= 0, "Test start game")); }
    public Task<BotResult> SetClientlessAfterGameAsync(AccountSettings a, bool enabled) { AutoClientless++; return Task.FromResult(new BotResult(Running.Contains(a.Id) && !ClientlessSyncFails, "Test automatic clientless")); }
    public Task<BotResult> GoClientlessAsync(AccountSettings a) { GoClientless++; return Task.FromResult(new BotResult(Running.Contains(a.Id), "Test go clientless")); }
    public Task<BotResult> SetVisibilityAsync(AccountSettings a, bool visible) { if (visible) Hidden.Remove(a.Id); else Hidden[a.Id] = true; return Task.FromResult(new BotResult(true, "Test visibility")); }
    public Task<BotResult> SetRelatedClientVisibilityAsync(AccountSettings a, bool visible) { if (visible) ClientHidden.Remove(a.Id); else ClientHidden[a.Id] = true; return Task.FromResult(new BotResult(true, "Test client visibility")); }
    public Task<BotResult> TerminateAsync(AccountSettings a, bool force)
    {
        Terminations++;
        if (ForceCloseRequired && !force) return Task.FromResult(new BotResult(false, "Did not close", NeedsForce: true));
        Running.Remove(a.Id); Training.Remove(a.Id); Hidden.Remove(a.Id); ClientHidden.Remove(a.Id);
        return Task.FromResult(new BotResult(true, "Test terminate"));
    }
}
