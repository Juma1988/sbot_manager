using System.IO;

namespace SBotManager.Models;

public enum BotState { NotConfigured, MissingFile, Checking, Stopped, Running, Unknown }
public sealed record AccountSettings(Guid Id, string Name, string ExecutablePath, bool RecoveryEnabled = false, bool BulkEnabled = true);
public sealed record ProcessSnapshot(BotState State, string Detail, int? ProcessId = null,
    bool CanStartTraining = false, bool CanStopTraining = false, bool CanStartGame = false, string Game = "Unknown",
    string Training = "Unknown", bool? WindowVisible = null, bool? ClientWindowVisible = null);

public sealed class Account(AccountSettings settings) : Bindable
{
    private AccountSettings config = settings;
    private ProcessSnapshot snapshot = new(BotState.Checking, "Checking configured instance…");
    private bool busy;
    private string lastRecoveryEvent = "No recovery events this session";
    public AccountSettings Settings => config;
    public Guid Id => config.Id;
    public string Name => config.Name;
    public string Initial => Name.Length >= 2 ? Name[..2].ToUpperInvariant() : Name.ToUpperInvariant();
    public string ExecutablePath => config.ExecutablePath;
    public string Executable => string.IsNullOrEmpty(ExecutablePath) ? "Choose a bot executable" : Path.GetFileName(ExecutablePath);
    public bool RecoveryEnabled => config.RecoveryEnabled;
    public bool BulkEnabled => config.BulkEnabled;
    public string BulkAvailabilityText => BulkEnabled ? "ENABLED" : "DISABLED";
    public string BulkAvailabilityHelp => BulkEnabled
        ? "Double-click the avatar to exclude this character from bulk actions"
        : "Double-click the avatar to include this character in bulk actions";
    public ProcessSnapshot Snapshot => snapshot;
    public BotState State => snapshot.State;
    public bool Busy { get => busy; set { busy = value; NotifyAll(); } }
    public bool WantsRunning { get; set; }
    public bool RecoverySuspended { get; set; }
    public int RecoveryAttempts { get; set; }
    public DateTimeOffset RetryAt { get; set; }
    public DateTimeOffset? StableSince { get; set; }
    public string Status => Busy ? "WORKING" : State switch
    {
        BotState.NotConfigured => "NOT CONFIGURED", BotState.MissingFile => "FILE MISSING",
        BotState.Checking => "CHECKING", BotState.Running => snapshot.CanStopTraining ? "TRAINING" : snapshot.CanStartTraining ? "READY" : "RUNNING",
        BotState.Stopped => "STOPPED", _ => "UNKNOWN"
    };
    public string StatusColor => State == BotState.Running && snapshot.CanStopTraining ? "#8EE1BE" :
        State == BotState.Running && snapshot.CanStartTraining ? "#BFC8ED" :
        State is BotState.Unknown or BotState.MissingFile ? "#F4CB7B" : "#A6B2C6";
    public string CardStatus => BulkEnabled ? Status : "DISABLED";
    public string CardStatusColor => BulkEnabled ? StatusColor : "#98A4B8";
    public string Detail => snapshot.Detail;
    public string GameStatus => snapshot.CanStartGame ? "Game control verified" : "Game state not verified";
    public string TrainingStatus => snapshot.CanStopTraining ? "Stop control verified" : snapshot.CanStartTraining ? "Start control verified" : "Training state not verified";
    public string ExpText => "EXP unavailable";
    public string ExpHelp => "No verified EXP source is connected. The bar is not a zero-percent reading.";
    public string RecoveryText => !RecoveryEnabled ? "Recovery off" : RecoverySuspended ? "Recovery paused" : RecoveryAttempts >= 3 ? "Needs attention · retry limit" : $"Recovery on · {RecoveryAttempts}/3 retries";
    public string LastRecoveryEvent => lastRecoveryEvent;
    public bool WindowVisible => snapshot.WindowVisible != false;
    public bool CanOpen => !Busy && State == BotState.Stopped;
    public bool CanTerminate => !Busy && State == BotState.Running;
    public bool CanPrimaryAction => CanOpen || CanTerminate;
    public bool PrimaryActionTerminates => State == BotState.Running;
    public string PrimaryActionText => PrimaryActionTerminates ? "Terminate" : "Launch";
    public string PrimaryActionHelp => PrimaryActionTerminates
        ? "Terminate this bot; Stop training leaves it open"
        : "Open this bot's sBot executable";
    public bool CanShowHide => !Busy && State == BotState.Running && snapshot.WindowVisible is not null;
    public string ShowHideText => snapshot.WindowVisible == false ? "Show bot" : "Hide bot";
    public bool CanShowHideClient => !Busy && State == BotState.Running && snapshot.ClientWindowVisible is not null;
    public string ShowHideClientText => snapshot.ClientWindowVisible == false ? "Show client" : "Hide client";
    public bool CanStartTraining => !Busy && State == BotState.Running && snapshot.CanStartTraining;
    public bool CanStopTraining => !Busy && State == BotState.Running && snapshot.CanStopTraining;
    public bool CanChangeTraining => CanStartTraining || CanStopTraining;
    public bool StartsTraining => CanStartTraining && !CanStopTraining;
    public string TrainingActionText => CanStopTraining ? "Stop training" : CanStartTraining ? "Start training" : "Training unavailable";
    public string TrainingActionHelp => CanStopTraining ? "Stop training only; sBot stays open" :
        CanStartTraining ? "Start training in the running sBot" : "Training controls are not currently available";
    public bool CanStartGame => !Busy && State == BotState.Running && snapshot.CanStartGame;
    public bool CanRemove => !Busy && State is BotState.Stopped or BotState.NotConfigured or BotState.MissingFile;

    public void Apply(ProcessSnapshot value) { snapshot = value; NotifyAll(); }
    public void RecordRecoveryEvent(DateTimeOffset when, string message) { lastRecoveryEvent = $"{when.ToLocalTime():HH:mm:ss} · {message}"; NotifyAll(); }
    public void Configure(AccountSettings value) { config = value; NotifyAll(); }
    public void NotifyAll() => Changed(string.Empty);
}
