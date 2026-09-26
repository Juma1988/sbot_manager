using System.IO;
using System.Text;
using SBotManager.Models;

namespace SBotManager.Services;

public sealed class ActivityStore(string directory)
{
    private DateOnly? lastPruned;
    public void Append(ActivityEntry entry)
    {
        Directory.CreateDirectory(directory);
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today != lastPruned)
        {
            foreach (var path in Directory.EnumerateFiles(directory, "activity-*.log"))
                if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-14)) File.Delete(path);
            lastPruned = today;
        }
        var file = Path.Combine(directory, $"activity-{today:yyyy-MM-dd}.log");
        if (File.Exists(file) && new FileInfo(file).Length >= 2 * 1024 * 1024)
            File.Move(file, file + ".previous.log", true);
        File.AppendAllText(file, Format(entry) + Environment.NewLine, Encoding.UTF8);
    }
    public static string Format(ActivityEntry e) => $"{e.Timestamp:yyyy-MM-dd HH:mm:ss} [{e.Severity}] [{e.Account}] {e.Message.Replace('\r', ' ').Replace('\n', ' ')}";
}
