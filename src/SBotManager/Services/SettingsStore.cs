using System.IO;
using System.Text.Json;
using SBotManager.Models;

namespace SBotManager.Services;

public enum StartupLaunchMode { LaunchAll, LaunchOneByOne }
public sealed record ManagerSettings(int Version, List<AccountSettings> Accounts, bool CloseToTray = true, NotificationConfig? Notifications = null, int LaunchDelaySeconds = 20, bool AutoHideBots = true, bool AutoHideClients = true, bool StartWithWindows = false, bool LaunchBotsAtWindowsStartup = false, bool TerminateSessionBotsOnExit = false, StartupLaunchMode StartupLaunchMode = StartupLaunchMode.LaunchOneByOne, bool AutoClientlessAfterGame = false);
public sealed record SettingsLoad(ManagerSettings Settings, string? Warning = null, bool Recovered = false);

public sealed class SettingsStore(string directory)
{
    public string DirectoryPath { get; } = directory;
    public string FilePath => Path.Combine(DirectoryPath, "settings.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static ManagerSettings Defaults() => new(1, [], Notifications: NotificationConfig.Defaults());

    public SettingsLoad Load()
    {
        if (!File.Exists(FilePath)) return new(Defaults());
        try { return new(Read(FilePath)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            try { return new(Read(FilePath + ".bak"), "Settings could not be read. Loaded the previous backup; review settings before operating bots.", true); }
            catch (Exception backup) when (backup is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
            {
                return new(new(1, []), "Settings and backup are unavailable or invalid. No bots were loaded. Files have not been overwritten.", true);
            }
        }
    }

    private static ManagerSettings Read(string path)
    {
        var result = JsonSerializer.Deserialize<ManagerSettings>(File.ReadAllText(path), Json) ?? throw new JsonException("Empty settings.");
        Validate(result);
        return result;
    }

    public static void Validate(ManagerSettings settings)
    {
        if (settings.Version != 1 || settings.Accounts is null || settings.Accounts.Count > 10 || settings.LaunchDelaySeconds is < 1 or > 300 || !Enum.IsDefined(settings.StartupLaunchMode)) throw new ArgumentException("Unsupported settings, launch spacing, startup mode, or more than 10 accounts.");
        var ids = new HashSet<Guid>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in settings.Accounts)
        {
            if (a is null || a.Id == Guid.Empty || !ids.Add(a.Id) || string.IsNullOrWhiteSpace(a.Name) || a.Name.Length > 24 || a.Name.Any(char.IsControl) || !names.Add(a.Name.Trim()))
                throw new ArgumentException("Use unique character names (1–24 characters) and account identifiers.");
            if (a.ExecutablePath is null) throw new ArgumentException("Invalid executable path.");
            if (a.ExecutablePath.Length == 0) continue;
            if (!Path.IsPathFullyQualified(a.ExecutablePath) || !Path.GetExtension(a.ExecutablePath).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                a.ExecutablePath.StartsWith(@"\\", StringComparison.Ordinal) || !paths.Add(Path.GetFullPath(a.ExecutablePath)))
                throw new ArgumentException("Choose a unique local .exe path for each account. Network/device paths are not supported.");
        }
    }

    public void Save(ManagerSettings settings)
    {
        Validate(settings);
        Directory.CreateDirectory(DirectoryPath);
        var temporary = FilePath + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Json));
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
            else File.Move(temporary, FilePath);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
