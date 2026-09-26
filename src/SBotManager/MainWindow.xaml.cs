using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SBotManager.Models;
using SBotManager.Services;
using Forms = System.Windows.Forms;

namespace SBotManager;

public partial class MainWindow : Window
{
    private readonly Dashboard dashboard;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly Forms.NotifyIcon tray;
    private readonly Forms.ContextMenuStrip trayMenu;
    private readonly Forms.ToolStripMenuItem liveBotsMenu;
    private readonly DispatcherTimer launchDelayLongPressTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private bool launchDelayLongPressHandled;
    private bool exiting, terminating;
    public MainWindow()
    {
        dashboard = new(new SettingsStore(App.DataDirectory), new WindowsBotService(), new ActivityStore(Path.Combine(App.DataDirectory, "Logs")));
        InitializeComponent(); DataContext = dashboard;
        tray = new Forms.NotifyIcon { Icon = LoadAppIcon(), Text = "i1988 - sBot manager", Visible = true };
        trayMenu = new Forms.ContextMenuStrip();
        liveBotsMenu = new Forms.ToolStripMenuItem("Live bots");
        trayMenu.Items.Add("Show manager", null, (_, _) => Dispatcher.Invoke(Restore));
        trayMenu.Items.Add(liveBotsMenu);
        trayMenu.Items.Add(new Forms.ToolStripSeparator());
        trayMenu.Items.Add("Exit manager…", null, (_, _) => Dispatcher.Invoke(ExitFromTray));
        trayMenu.Opened += TrayMenu_Opened;
        tray.ContextMenuStrip = trayMenu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(Restore);
        dashboard.Notify += (eventKind, account) =>
        {
            var message = dashboard.Notifications.Format(eventKind, account);
            if (message.Length == 0) return;
            var icon = eventKind == NotificationEvent.CloseToTray ? Forms.ToolTipIcon.Info : Forms.ToolTipIcon.Warning;
            tray.ShowBalloonTip(4000, "i1988 - sBot manager", message, icon);
        };
        timer.Tick += async (_, _) => await RefreshSafely();
        launchDelayLongPressTimer.Tick += LaunchDelayLongPressTimer_Tick;
        Loaded += async (_, _) =>
        {
            await RefreshSafely();
            if (App.LaunchedAtWindowsStartup && dashboard.LaunchBotsAtWindowsStartup)
                await dashboard.BulkAsync(dashboard.StartupLaunchMode == StartupLaunchMode.LaunchOneByOne ? "open-sequential" : "open");
            timer.Start();
        };
        Closing += OnClosing;
    }
    private static System.Drawing.Icon LoadAppIcon()
    {
        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;
        return stream is null ? System.Drawing.SystemIcons.Application : new System.Drawing.Icon(stream);
    }
    private async Task RefreshSafely()
    {
        try { await dashboard.RefreshAsync(!terminating); }
        catch (Exception e) { dashboard.Log("MANAGER", "Error", "Monitoring failed: " + e.Message); }
    }
    private static Account? Target(object sender) => (sender as Button)?.Tag as Account;
    private async void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (Target(sender) is not { } account) return;
        if (!account.PrimaryActionTerminates) { await dashboard.OpenAsync(account); return; }

        var result = await dashboard.TerminateAsync(account);
        if (!result.Success) await dashboard.TerminateAsync(account, true);
    }
    private void Avatar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || (sender as FrameworkElement)?.Tag is not Account account) return;
        dashboard.SetBulkEnabled(account, !account.BulkEnabled);
        e.Handled = true;
    }
    private async void ShowHide_Click(object sender, RoutedEventArgs e) { if (Target(sender) is { } a) await dashboard.ShowHideAsync(a); }
    private async void ShowHideClient_Click(object sender, RoutedEventArgs e) { if (Target(sender) is { } a) await dashboard.ShowHideClientAsync(a); }
    private async void TrainingStart_Click(object sender, RoutedEventArgs e) { if (Target(sender) is { } a) await dashboard.TrainingAsync(a, true); }
    private async void TrainingToggle_Click(object sender, RoutedEventArgs e) { if (Target(sender) is { } a) await dashboard.TrainingAsync(a, a.StartsTraining); }
    private async void OpenAll_Click(object sender, RoutedEventArgs e)
    {
        await dashboard.BulkAsync("open");
    }
    private async void OpenSequential_Click(object sender, RoutedEventArgs e)
    {
        if (launchDelayLongPressHandled) { launchDelayLongPressHandled = false; return; }
        await dashboard.BulkAsync("open-sequential");
    }
    private void OpenSequential_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (dashboard.CanBulk) launchDelayLongPressTimer.Start();
    }
    private void OpenSequential_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        launchDelayLongPressTimer.Stop();
    }
    private void LaunchDelayLongPressTimer_Tick(object? sender, EventArgs e)
    {
        launchDelayLongPressTimer.Stop();
        launchDelayLongPressHandled = true;
        EditLaunchDelay();
    }
    private void EditLaunchDelay()
    {
        var seconds = new TextBox { Text = dashboard.LaunchDelaySeconds.ToString(), MinWidth = 120, Margin = new Thickness(0, 6, 0, 0) };
        var error = new TextBlock { Foreground = System.Windows.Media.Brushes.IndianRed, Margin = new Thickness(0, 8, 0, 0) };
        var dialog = new Window
        {
            Owner = this, Title = "Launch 1-1 spacing", WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize, SizeToContent = SizeToContent.WidthAndHeight, ShowInTaskbar = false
        };
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Seconds between each bot launch (1–300)" });
        panel.Children.Add(seconds); panel.Children.Add(error);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 78 };
        var save = new Button { Content = "Save", IsDefault = true, MinWidth = 78, Margin = new Thickness(8, 0, 0, 0) };
        save.Click += (_, _) =>
        {
            if (!int.TryParse(seconds.Text, out var value) || value is < 1 or > 300) { error.Text = "Enter a whole number from 1 to 300."; return; }
            dashboard.SetLaunchDelaySeconds(value); dialog.DialogResult = true;
        };
        actions.Children.Add(cancel); actions.Children.Add(save); panel.Children.Add(actions);
        dialog.Content = panel; dialog.Loaded += (_, _) => { seconds.Focus(); seconds.SelectAll(); };
        dialog.ShowDialog();
    }
    private async void StartAll_Click(object sender, RoutedEventArgs e)
    {
        await dashboard.BulkAsync("start");
    }
    private async void StopAll_Click(object sender, RoutedEventArgs e)
    {
        dashboard.SuspendRecovery();
        foreach (var a in dashboard.Accounts.Where(a => a.BulkEnabled).ToArray()) { await WaitForIdle(a); await dashboard.TrainingAsync(a, false); }
    }
    private async void ShowAllBots_Click(object sender, RoutedEventArgs e) => await dashboard.SetAllBotsVisibilityAsync(true);
    private async void HideAllBots_Click(object sender, RoutedEventArgs e) => await dashboard.SetAllBotsVisibilityAsync(false);
    private async void ShowAllClients_Click(object sender, RoutedEventArgs e) => await dashboard.SetAllClientsVisibilityAsync(true);
    private async void HideAllClients_Click(object sender, RoutedEventArgs e) => await dashboard.SetAllClientsVisibilityAsync(false);
    private async void GoClientlessAll_Click(object sender, RoutedEventArgs e) => await dashboard.GoClientlessAllAsync();
    private string Names() => string.Join(", ", dashboard.Accounts.Select(a => a.Name));
    private async void TerminateAll_Click(object sender, RoutedEventArgs e)
    {
        if (terminating) return;
        terminating = true; dashboard.BeginTermination();
        try
        {
            foreach (var a in dashboard.Accounts.Where(a => a.BulkEnabled).ToArray())
            {
                await WaitForIdle(a);
                var result = await dashboard.TerminateAsync(a);
                if (!result.Success)
                    await dashboard.TerminateAsync(a, true);
            }
        }
        finally { terminating = false; dashboard.EndTermination(); }
    }
    private static async Task WaitForIdle(Account account)
    {
        for (int i = 0; i < 50 && account.Busy; i++) await Task.Delay(200);
    }
    private async void Configure_Click(object sender, RoutedEventArgs e)
    {
        if (Target(sender) is { } a) { new AccountSettingsWindow(dashboard, a) { Owner = this }.ShowDialog(); await RefreshSafely(); }
    }
    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (dashboard.CanAdd) { new AccountSettingsWindow(dashboard, null) { Owner = this }.ShowDialog(); await RefreshSafely(); }
    }
    private string FilteredLogs() => string.Join(Environment.NewLine, dashboard.Activity.Reverse().Select(ActivityStore.Format));
    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(FilteredLogs()); }
        catch (System.Runtime.InteropServices.COMException) { MessageBox.Show(this, "Clipboard is busy. Try again."); }
    }
    private void ExportLog_Click(object sender, RoutedEventArgs e)
    {
        var picker = new SaveFileDialog { FileName = "farm-activity.log", Filter = "Log file (*.log)|*.log", OverwritePrompt = true };
        if (picker.ShowDialog(this) != true) return;
        try { File.WriteAllText(picker.FileName, FilteredLogs()); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { MessageBox.Show(this, error.Message, "Export failed"); }
    }
    private void LogEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if ((sender as FrameworkElement)?.DataContext is ActivityEntry entry)
        {
            try { Clipboard.SetText(ActivityStore.Format(entry)); }
            catch (System.Runtime.InteropServices.COMException) { MessageBox.Show(this, "Clipboard is busy. Try again."); }
        }
    }
    private void ClearLog_Click(object sender, RoutedEventArgs e) => dashboard.Activity.Clear();
    private void Restore() { Show(); WindowState = WindowState.Normal; Activate(); }
    private async void Exit_Click(object sender, RoutedEventArgs e) => await ExitManagerAsync();
    private async void TrayMenu_Opened(object? sender, EventArgs e)
    {
        await RefreshSafely();
        liveBotsMenu.DropDownItems.Clear();
        var live = dashboard.SessionLaunchedRunning;
        if (live.Count == 0)
        {
            liveBotsMenu.DropDownItems.Add(new Forms.ToolStripMenuItem("No session-launched bots running") { Enabled = false });
            return;
        }
        var showAll = new Forms.ToolStripMenuItem("Show all session bots");
        showAll.Click += async (_, _) => await dashboard.ShowSessionBotsAsync();
        liveBotsMenu.DropDownItems.Add(showAll);
        liveBotsMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        foreach (var account in live)
        {
            var item = new Forms.ToolStripMenuItem(account.Name + " · " + account.Status) { Enabled = account.CanShowHide, Tag = account };
            item.Click += LiveBot_Click;
            liveBotsMenu.DropDownItems.Add(item);
        }
    }
    private async void LiveBot_Click(object? sender, EventArgs e)
    {
        if ((sender as Forms.ToolStripMenuItem)?.Tag is Account account) await dashboard.ShowBotAsync(account);
    }
    private async void ExitFromTray() => await ExitManagerAsync();
    private async Task ExitManagerAsync()
    {
        if (exiting || terminating) return;
        IReadOnlyList<string> failures = [];
        if (dashboard.TerminateSessionBotsOnExit)
        {
            terminating = true; dashboard.BeginTermination();
            try { failures = await dashboard.CloseSessionLaunchedAsync(); }
            finally { terminating = false; dashboard.EndTermination(); }
        }
        else
        {
            // Leave bots running, but restore every verified sBot window before
            // Manager stops controlling them. Client windows are intentionally not touched.
            await dashboard.RestoreVerifiedBotWindowsAsync();
        }
        if (failures.Count > 0)
            tray.ShowBalloonTip(5000, "i1988 - sBot manager", "Could not close: " + string.Join("; ", failures), Forms.ToolTipIcon.Warning);
        exiting = true; Close();
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!exiting && dashboard.CloseToTray)
        {
            e.Cancel = true; Hide();
            var message = dashboard.Notifications.Format(NotificationEvent.CloseToTray, "");
            if (message.Length > 0) tray.ShowBalloonTip(2500, "i1988 - sBot manager", message, Forms.ToolTipIcon.Info);
            return;
        }
        if (!exiting)
        {
            e.Cancel = true;
            await ExitManagerAsync();
            return;
        }
        dashboard.Shutdown(); timer.Stop(); tray.Visible = false; tray.Dispose();
    }
    private void Guide_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this,
        "Open bot launches the configured executable; sBot may auto-login/train according to its own settings.\n\n" +
        "Training actions only send an exact accessible native button command. A delivered command is NOT proof of resulting game state. Unsupported controls remain disabled.\n\n" +
        "Stop training never kills sBot. Terminate attempts normal close, then asks before force termination. Both pause manager recovery.\n\n" +
        "Recovery is opt-in and only follows an observed/opened process. Missing/ambiguous process access blocks recovery. Up to 3 retries; 60 seconds of stable running resets the budget.\n\n" +
        "Game, training and EXP telemetry are not connected. No values are invented.\n\n" +
        "Settings: " + App.DataDirectory + "\nLogs: 14-day retention, 2 MB daily rotation. No credentials stored.\n\nClose to tray keeps monitoring active; Exit manager stops monitoring without terminating bots.",
        "Help and safety", MessageBoxButton.OK, MessageBoxImage.Information);
    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow(dashboard) { Owner = this }.ShowDialog();
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
}
