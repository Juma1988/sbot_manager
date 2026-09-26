using System.Windows;
using System.IO;
using SBotManager.Models;
using SBotManager.Services;

namespace SBotManager;

public partial class SettingsWindow : Window
{
    private readonly Dashboard dashboard;
    private readonly WindowsStartupService windowsStartup = new();
    public SettingsWindow(Dashboard dashboard)
    {
        this.dashboard = dashboard;
        InitializeComponent();
        Load(dashboard);
    }

    private void Load(Dashboard dashboard)
    {
        CloseToTrayPreference.IsChecked = dashboard.CloseToTray;
        AutoHideBotsPreference.IsChecked = dashboard.AutoHideBots;
        AutoHideClientsPreference.IsChecked = dashboard.AutoHideClients;
        StartWithWindowsPreference.IsChecked = dashboard.StartWithWindows && windowsStartup.IsEnabled();
        LaunchBotsAtWindowsStartupPreference.IsChecked = dashboard.LaunchBotsAtWindowsStartup;
        StartupLaunchModePreference.SelectedIndex = dashboard.StartupLaunchMode == StartupLaunchMode.LaunchOneByOne ? 1 : 0;
        AutoClientlessAfterGamePreference.IsChecked = dashboard.AutoClientlessAfterGame;
        TerminateSessionBotsOnExitPreference.IsChecked = dashboard.TerminateSessionBotsOnExit;
        var notifications = dashboard.Notifications;
        NotificationsMasterPreference.IsChecked = notifications.MasterEnabled;
        RecoveryBudgetNotificationPreference.IsChecked = notifications.For(NotificationEvent.RecoveryBudget).Enabled;
        BotStoppedNotificationPreference.IsChecked = notifications.For(NotificationEvent.BotStoppedUnexpected).Enabled;
        BotUnknownNotificationPreference.IsChecked = notifications.For(NotificationEvent.BotUnknown).Enabled;
        ForceTerminationNotificationPreference.IsChecked = notifications.For(NotificationEvent.ForceTerminateRequired).Enabled;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool startWithWindows = StartWithWindowsPreference.IsChecked == true;
            windowsStartup.SetEnabled(startWithWindows);
            dashboard.SetPreferences(CloseToTrayPreference.IsChecked == true, AutoHideBotsPreference.IsChecked == true,
                AutoHideClientsPreference.IsChecked == true, startWithWindows, LaunchBotsAtWindowsStartupPreference.IsChecked == true,
                TerminateSessionBotsOnExitPreference.IsChecked == true,
                StartupLaunchModePreference.SelectedIndex == 1 ? StartupLaunchMode.LaunchOneByOne : StartupLaunchMode.LaunchAll,
                AutoClientlessAfterGamePreference.IsChecked == true);
            await dashboard.SyncAutomaticClientlessForRunningAsync();
            var notifications = dashboard.Notifications;
            dashboard.SetNotifications(notifications with
            {
                MasterEnabled = NotificationsMasterPreference.IsChecked == true,
                RecoveryBudget = notifications.For(NotificationEvent.RecoveryBudget) with { Enabled = RecoveryBudgetNotificationPreference.IsChecked == true },
                BotStoppedUnexpected = notifications.For(NotificationEvent.BotStoppedUnexpected) with { Enabled = BotStoppedNotificationPreference.IsChecked == true },
                BotUnknown = notifications.For(NotificationEvent.BotUnknown) with { Enabled = BotUnknownNotificationPreference.IsChecked == true },
                ForceTerminateRequired = notifications.For(NotificationEvent.ForceTerminateRequired) with { Enabled = ForceTerminationNotificationPreference.IsChecked == true }
            });
            DialogResult = true;
            Close();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, error.Message, "Settings not saved");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
