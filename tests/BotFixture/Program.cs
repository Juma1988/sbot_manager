using System.Runtime.InteropServices;

// Harmless native-control fixture. Never connects to a game or bot.
internal static class Program
{
    [STAThread] static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new Fixture()); }
}
internal sealed class Fixture : Form
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint CreateWindowEx(uint ex, string cls, string title, uint style, int x, int y, int w, int h, nint parent, nint id, nint instance, nint param);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);
    private readonly nint helper;
    private readonly Form preferences = new() { Text = "Mob preferences", Width = 240, Height = 130, ShowInTaskbar = false };
    public Fixture()
    {
        Text = "SBotP test fixture"; Width = 380; Height = 190;
        // Mimic real sBot: a hidden, untitled helper/tooltip top-level window that
        // must NOT be mistaken for the main window by WindowFor.
        helper = CreateWindowEx(0, "tooltips_class32", "", 0x80000000, 0, 0, 0, 0, 0, 0, 0, 0);
        SetWindowPos(helper, new nint(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010); // topmost, no move/size/activate
        Shown += (_, _) => preferences.Show();
        FormClosing += (_, e) =>
        {
            if (File.Exists("refuse-close.flag")) { e.Cancel = true; return; }
            preferences.Close();
        };
    }
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (File.Exists("no-controls.flag")) return;
        CreateWindowEx(0, "Button", "Start training", 0x50000000, 15, 20, 145, 35, Handle, 100, 0, 0);
        CreateWindowEx(0, "Button", "Stop training", 0x50000000, 175, 20, 145, 35, Handle, 101, 0, 0);
        CreateWindowEx(0, "Button", "Start Game!", 0x50000000, 15, 60, 145, 35, Handle, 102, 0, 0);
        CreateWindowEx(0, "Button", "Go clientless", 0x50000000, 15, 100, 145, 35, Handle, 103, 0, 0);
        CreateWindowEx(0, "Button", "Switch to clientless mode when entered game after", 0x50000003, 175, 100, 180, 35, Handle, 104, 0, 0);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0111)
        {
            int id = (int)(m.WParam.ToInt64() & 0xFFFF);
            if (id is 100 or 101 or 102 or 103 or 104) File.AppendAllText("commands.log", id == 100 ? "START\n" : id == 101 ? "STOP\n" : id == 102 ? "GAME\n" : id == 103 ? "CLIENTLESS\n" : "AUTO_CLIENTLESS\n");
        }
        base.WndProc(ref m);
    }
}
