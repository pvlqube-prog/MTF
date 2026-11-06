using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace QubeTune;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Dashboard());
    }
}

public sealed class Dashboard : Form
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MemoryStatus
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatus>();
        public uint MemoryLoad;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus status);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

    private readonly Label cpuValue = Metric("—"), ramValue = Metric("—"), diskValue = Metric("—"), netValue = Metric("—");
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.LightSkyBlue, Text = "READY • Local diagnostics" };
    private readonly ComboBox gamePicker = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(13, 19, 31), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private long lastIdle, lastKernel, lastUser;
    private string selectedGame = "Fortnite";

    public Dashboard()
    {
        Text = "QUBE TUNE • Gaming Performance";
        MinimumSize = new Size(900, 650);
        Size = new Size(1120, 760);
        BackColor = Color.FromArgb(8, 13, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        BuildUi();
        timer.Tick += (_, _) => RefreshMetrics();
        timer.Start();
        RefreshMetrics();
        AddLog("QUBE TUNE started. Monitoring is read-only; no system settings were changed.");
    }

    private static Label Metric(string value) => new() { Text = value, AutoSize = true, Font = new Font("Segoe UI Semibold", 23, FontStyle.Bold), ForeColor = Color.FromArgb(70, 220, 255) };

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(24), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill };
        var brand = new Label { Text = "QUBE TUNE", Font = new Font("Segoe UI Black", 25, FontStyle.Bold), ForeColor = Color.FromArgb(70, 220, 255), AutoSize = true, Location = new Point(0, 0) };
        var sub = new Label { Text = "GAMING PERFORMANCE CENTER  /  WINDOWS DIAGNOSTICS", ForeColor = Color.Silver, AutoSize = true, Location = new Point(3, 48) };
        header.Controls.Add(brand); header.Controls.Add(sub); header.Controls.Add(status);
        status.Anchor = AnchorStyles.Top | AnchorStyles.Right; status.Location = new Point(720, 20);
        root.Controls.Add(header, 0, 0);

        var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.FromArgb(15, 24, 40), Padding = new Padding(12) };
        for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddMetric(metrics, "CPU LOAD", cpuValue, 0); AddMetric(metrics, "MEMORY", ramValue, 1); AddMetric(metrics, "SYSTEM DRIVE", diskValue, 2); AddMetric(metrics, "NETWORK", netValue, 3);
        root.Controls.Add(metrics, 0, 1);

        var gaming = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 16, 0, 8) };
        gaming.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); gaming.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        var gamePanel = PanelCard("GAME CENTER");
        var pickerLabel = new Label { Text = "SELECT YOUR GAME", ForeColor = Color.Silver, AutoSize = true, Location = new Point(18, 48) };
        gamePicker.Items.AddRange(new object[] { "Fortnite", "NBA 2K", "Valorant", "Rocket League", "Apex Legends", "Call of Duty" });
        gamePicker.SelectedIndex = 0; gamePicker.Location = new Point(18, 76);
        gamePicker.SelectedIndexChanged += (_, _) => { selectedGame = gamePicker.SelectedItem?.ToString() ?? "Fortnite"; UpdateGameInfo(); };
        var gameInfo = new Label { Name = "gameInfo", Text = "Competitive profile • live PC diagnostics • safe setup checklist", ForeColor = Color.Gainsboro, AutoSize = true, Location = new Point(18, 116) };
        gamePanel.Controls.Add(pickerLabel); gamePanel.Controls.Add(gamePicker); gamePanel.Controls.Add(gameInfo);
        var actions = PanelCard("QUICK ACTIONS");
        var ping = MakeButton("RUN NETWORK TEST", (_, _) => _ = RunNetworkTest());
        var refresh = MakeButton("REFRESH PC STATUS", (_, _) => RefreshMetrics());
        var launch = MakeButton("OPEN GAME PLATFORM", (_, _) => OpenGamePlatform());
        ping.Location = new Point(16, 45); refresh.Location = new Point(16, 88); launch.Location = new Point(16, 131);
        actions.Controls.Add(ping); actions.Controls.Add(refresh); actions.Controls.Add(launch);
        gaming.Controls.Add(gamePanel, 0, 0); gaming.Controls.Add(actions, 1, 0);
        root.Controls.Add(gaming, 0, 2);

        var logPanel = PanelCard("DIAGNOSTICS & GAME NOTES");
        logPanel.Padding = new Padding(12);
        logPanel.Controls.Add(log);
        root.Controls.Add(logPanel, 0, 3);
        UpdateGameInfo();
    }

    private static void AddMetric(TableLayoutPanel panel, string title, Label value, int column)
    {
        var box = new Panel { Dock = DockStyle.Fill };
        box.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Color.Silver, Location = new Point(8, 8) });
        value.Location = new Point(8, 38); box.Controls.Add(value); panel.Controls.Add(box, column, 0);
    }

    private static Panel PanelCard(string title)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 24, 40), Margin = new Padding(6) };
        p.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = Color.FromArgb(70, 220, 255), Location = new Point(16, 12) });
        return p;
    }

    private static Button MakeButton(string text, EventHandler click)
    {
        var b = new Button { Text = text, Width = 250, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(24, 49, 73), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft };
        b.FlatAppearance.BorderColor = Color.FromArgb(40, 150, 190); b.Click += click; return b;
    }

    private void UpdateGameInfo()
    {
        var info = Controls.Find("gameInfo", true).FirstOrDefault() as Label;
        if (info == null) return;
        info.Text = selectedGame switch
        {
            "Fortnite" => "Fortnite • Check frame pacing, CPU/GPU load, network stability and background apps.",
            "NBA 2K" => "NBA 2K • Monitor system load and connection stability; use in-game graphics settings.",
            "Valorant" => "Valorant • Focus on stable frame times, low background load and packet loss checks.",
            "Rocket League" => "Rocket League • Track CPU load and network jitter for consistent matches.",
            _ => selectedGame + " • Monitor PC load and connection quality while you play."
        };
        AddLog("Selected game profile: " + selectedGame + ". Profiles provide guidance; they do not alter game files or settings.");
    }

    private void RefreshMetrics()
    {
        if (GetSystemTimes(out long idle, out long kernel, out long user))
        {
            if (lastKernel != 0)
            {
                long total = (kernel - lastKernel) + (user - lastUser);
                long busy = total - (idle - lastIdle);
                cpuValue.Text = total > 0 ? Math.Clamp(100.0 * busy / total, 0, 100).ToString("0") + "%" : "—";
            }
            lastIdle = idle; lastKernel = kernel; lastUser = user;
        }
        var memory = new MemoryStatus();
        if (GlobalMemoryStatusEx(memory)) ramValue.Text = $"{memory.MemoryLoad}%";
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
            diskValue.Text = drive.IsReady ? $"{(100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize):0}%" : "—";
        }
        catch { diskValue.Text = "—"; }
        var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        netValue.Text = nic == null ? "OFFLINE" : nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "WI-FI" : "ONLINE";
    }

    private async Task RunNetworkTest()
    {
        status.Text = "TESTING • network";
        AddLog("Starting 8-ping test to Cloudflare DNS (1.1.1.1). This is not a game-server ping.");
        var values = new List<long>(); int lost = 0;
        using var ping = new Ping();
        for (int i = 0; i < 8; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync("1.1.1.1", 1800);
                if (reply.Status == IPStatus.Success) values.Add(reply.RoundtripTime); else lost++;
            }
            catch { lost++; }
        }
        if (values.Count == 0) AddLog("Network test failed: no replies. Check your connection.");
        else
        {
            double avg = values.Average();
            double jitter = values.Count > 1 ? values.Zip(values.Skip(1), (a, b) => Math.Abs(a - b)).Average() : 0;
            AddLog($"Network test: avg {avg:0} ms • variation {jitter:0.0} ms • packet loss {lost}/8 ({lost * 12.5:0.0}%).");
        }
        status.Text = "READY • Local diagnostics";
    }

    private void OpenGamePlatform()
    {
        string url = selectedGame switch
        {
            "Fortnite" => "com.epicgames.launcher://apps/Fortnite?action=launch&silent=true",
            "NBA 2K" => "steam://open/library",
            "Valorant" => "https://playvalorant.com/",
            "Rocket League" => "com.epicgames.launcher://apps/Sugar?action=launch&silent=true",
            "Apex Legends" => "steam://open/library",
            "Call of Duty" => "steam://open/library",
            _ => "https://www.epicgames.com/"
        };
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); AddLog("Opened the relevant game platform link for " + selectedGame + "."); }
        catch (Exception ex) { AddLog("Could not open platform link: " + ex.Message); }
    }

    private void AddLog(string message)
    {
        if (log.IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => AddLog(message)); return; }
        log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    protected override void OnFormClosed(FormClosedEventArgs e) { timer.Stop(); timer.Dispose(); base.OnFormClosed(e); }
}
