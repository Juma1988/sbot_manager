namespace SBotManager.Models;

public enum NotificationEvent { RecoveryBudget, CloseToTray, BotStoppedUnexpected, BotUnknown, ForceTerminateRequired }

public sealed record NotificationRule(bool Enabled = true, string Message = "");

public sealed record NotificationConfig(
    bool MasterEnabled = true,
    NotificationRule? RecoveryBudget = null,
    NotificationRule? CloseToTray = null,
    NotificationRule? BotStoppedUnexpected = null,
    NotificationRule? BotUnknown = null,
    NotificationRule? ForceTerminateRequired = null)
{
    public static NotificationConfig Defaults() => new(
        MasterEnabled: true,
        RecoveryBudget: new(true, "{Account}: recovery retry budget used. Check the bot."),
        CloseToTray: new(true, "Still running in the tray. Monitoring continues."),
        BotStoppedUnexpected: new(true, "{Account}: bot stopped unexpectedly while you wanted it running. Check the bot."),
        BotUnknown: new(true, "{Account}: bot could not be verified. Check process permission or version."),
        ForceTerminateRequired: new(true, "{Account}: bot did not close normally. Force termination may be required."));

    public NotificationRule For(NotificationEvent e) => e switch
    {
        NotificationEvent.RecoveryBudget => RecoveryBudget ?? Defaults().RecoveryBudget!,
        NotificationEvent.CloseToTray => CloseToTray ?? Defaults().CloseToTray!,
        NotificationEvent.BotStoppedUnexpected => BotStoppedUnexpected ?? Defaults().BotStoppedUnexpected!,
        NotificationEvent.BotUnknown => BotUnknown ?? Defaults().BotUnknown!,
        NotificationEvent.ForceTerminateRequired => ForceTerminateRequired ?? Defaults().ForceTerminateRequired!,
        _ => new()
    };

    public string Format(NotificationEvent e, string account)
    {
        if (!MasterEnabled) return "";
        var rule = For(e);
        if (!rule.Enabled || string.IsNullOrWhiteSpace(rule.Message)) return "";
        return rule.Message.Replace("{Account}", account);
    }
}