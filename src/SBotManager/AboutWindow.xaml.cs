using System.Diagnostics;
using System.Windows;

namespace SBotManager;

public partial class AboutWindow : Window
{
    public AboutWindow() => InitializeComponent();

    private void Kofi_Click(object sender, RoutedEventArgs e) => Open(AppInfo.KofiUrl);
    private void InstaPay_Click(object sender, RoutedEventArgs e) => Open(AppInfo.InstaPayUrl);
    private void Contact_Click(object sender, RoutedEventArgs e) => Open("mailto:" + AppInfo.SupportEmail);
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { MessageBox.Show(this, error.Message, "Could not open link"); }
    }
}
