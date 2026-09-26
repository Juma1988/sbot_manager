using System.IO;
using System.Windows;
using Microsoft.Win32;
using SBotManager.Models;

namespace SBotManager;

public partial class AccountSettingsWindow : Window
{
    private readonly Dashboard dashboard;
    private readonly Account? account;
    public AccountSettingsWindow(Dashboard dashboard, Account? account)
    {
        InitializeComponent(); this.dashboard = dashboard; this.account = account;
        Heading.Text = account is null ? "Add character" : $"Settings · {account.Name}";
        NameInput.Text = account?.Name ?? ""; PathInput.Text = account?.ExecutablePath ?? "";
        RecoveryInput.IsChecked = account?.RecoveryEnabled ?? false;
        RemoveButton.Visibility = account is null ? Visibility.Collapsed : Visibility.Visible;
        RemoveButton.IsEnabled = account?.CanRemove == true;
        ValidatePath();
    }
    private void ValidatePath() => Validation.Text = string.IsNullOrEmpty(PathInput.Text) ? "Not configured" : File.Exists(PathInput.Text) ? "Ready · executable exists" : "File missing";
    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = "Choose the character's sBot executable", Filter = "Windows executable (*.exe)|*.exe", CheckFileExists = true, Multiselect = false };
        var folder = Path.GetDirectoryName(PathInput.Text);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder)) picker.InitialDirectory = folder;
        if (picker.ShowDialog(this) == true) { PathInput.Text = picker.FileName; ValidatePath(); }
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { if (PathInput.Text.Length > 0) Clipboard.SetText(PathInput.Text); }
        catch (System.Runtime.InteropServices.COMException) { ErrorText.Text = "Clipboard is busy. Try again."; }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            dashboard.SaveAccount(account, new(account?.Id ?? Guid.NewGuid(), NameInput.Text.Trim(), PathInput.Text.Trim(), RecoveryInput.IsChecked == true));
            DialogResult = true;
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException)
        { ErrorText.Text = error.Message; }
    }
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (account is null || MessageBox.Show(this, $"Remove {account.Name} from the manager? No bot files will be deleted.", "Remove account", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { dashboard.Remove(account); DialogResult = true; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException) { ErrorText.Text = error.Message; }
    }
}
