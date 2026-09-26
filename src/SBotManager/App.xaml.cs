using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using SBotManager.Services;

namespace SBotManager;

public partial class App : Application
{
    private Mutex? instance;
    public static string DataDirectory { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SBotManager");
    public static bool LaunchedAtWindowsStartup { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        int flag = Array.IndexOf(e.Args, "--data-dir");
        if (flag >= 0 && flag + 1 < e.Args.Length) DataDirectory = Path.GetFullPath(e.Args[flag + 1]);
        else if (File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")))
        {
            DataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
            var store = new SettingsStore(DataDirectory);
            if (!File.Exists(store.FilePath)) store.Save(SettingsStore.Defaults());
        }
        LaunchedAtWindowsStartup = Array.Exists(e.Args, argument => string.Equals(argument, "--windows-startup", StringComparison.Ordinal));
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(DataDirectory.ToUpperInvariant())))[..20];
        instance = new Mutex(true, @"Local\SBotManager-" + key, out bool created);
        if (!created)
        {
            MessageBox.Show("i1988 - sBot manager is already running. Check its system-tray icon.", "i1988 - sBot manager");
            instance.Dispose(); instance = null; Shutdown(); return;
        }
        MainWindow = new MainWindow(); MainWindow.Show();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        instance?.ReleaseMutex(); instance?.Dispose(); base.OnExit(e);
    }
}
