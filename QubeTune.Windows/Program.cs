using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QubeTune;

public sealed class QubeApp : Application
{
    [STAThread]
    public static void Main() => new QubeApp().Run();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new Dashboard();
        MainWindow.Show();
    }
}

public sealed class Dashboard : Window
{
    private readonly TextBlock _status = new();
    private readonly TextBlock _system = new();
    private readonly TextBlock _network = new();
    private readonly TextBlock _wifi = new();
    private readonly TextBlock _latency = new();
    private readonly Button _refresh = new() { Content = "REFRESH DIAGNOSTICS", Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(0, 12, 10, 0) };
    private readonly Button _test = new() { Content = "RUN NETWORK TEST", Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(0, 12, 0, 0) };
    private readonly ComboBox _gamePicker = new() { Width = 230, Padding = new Thickness(8), Margin = new Thickness(0, 8, 12, 8) };
    private readonly TextBlock _gameDetails = new() { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(215, 228, 247)), Margin = new Thickness(0, 8, 0, 10) };
    private readonly Button _openGame = new() { Content = "OPEN GAME PLATFORM", Padding = new Thickness(14, 9, 14, 9), Margin = new Thickness(0, 4, 0, 0) };

    public Dashboard()
    {
        Title = "QUBE TUNE — Windows Diagnostics";
        Width = 1000; Height = 760; MinWidth = 760; MinHeight = 600;
        Background = Brush("#090E18");
        Foreground = Brush("#EAF2FF");
        FontFamily = new FontFamily("Segoe UI");
        var root = new DockPanel { Margin = new Thickness(28) };
        Content = root;

        var header = new StackPanel();
        DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(new TextBlock { Text = "QUBE TUNE", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Brush("#42D9FF") });
        header.Children.Add(new TextBlock { Text = "WINDOWS LOCAL DIAGNOSTICS  /  READ-ONLY MODE", FontSize = 11, Foreground = Brush("#91A5C4"), Margin = new Thickness(0, 3, 0, 8) });
        header.Children.Add(_status);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(_refresh); actions.Children.Add(_test);
        header.Children.Add(actions);
        root.Children.Add(header);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 22, 0, 0) };
        root.Children.Add(scroll);
        var cards = new StackPanel();
        scroll.Content = cards;
        cards.Children.Add(Card("SYSTEM", _system));
        cards.Children.Add(Card("NETWORK ADAPTERS", _network));
        cards.Children.Add(Card("WI-FI DETAILS", _wifi));
        cards.Children.Add(Card("CONNECTIVITY TEST", _latency));
        var gameHub = new StackPanel();
        _gamePicker.ItemsSource = new[] { "Fortnite", "NBA 2K", "Valorant", "Rocket League", "Apex Legends", "Call of Duty", "Minecraft", "Roblox" };
        _gamePicker.SelectedIndex = 0;
        _gamePicker.SelectionChanged += (_, _) => UpdateGameProfile();
        _openGame.Click += (_, _) => OpenGamePlatform();
        gameHub.Children.Add(new TextBlock { Text = "Choose a game to see focused setup tips and open its game platform.", Foreground = Brush("#91A5C4"), TextWrapping = TextWrapping.Wrap });
        gameHub.Children.Add(_gamePicker);
        gameHub.Children.Add(_gameDetails);
        gameHub.Children.Add(_openGame);
        cards.Children.Add(Card("GAME CENTER  /  FORTNITE + NBA 2K", new TextBlock()));
        var gameCard = (Border)cards.Children[cards.Children.Count - 1];
        gameCard.Child = gameHub;
        UpdateGameProfile();
        cards.Children.Add(new TextBlock { Text = "QUBE TUNE only reads system and network information. It does not change Windows settings, route game traffic, or guarantee lower ping/FPS gains.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("#91A5C4"), Margin = new Thickness(2, 14, 2, 0), FontSize = 12 });

        _refresh.Click += async (_, _) => await RefreshAll();
        _test.Click += async (_, _) => await RunNetworkTest();
        Loaded += async (_, _) => await RefreshAll();
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static Border Card(string title, TextBlock body)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brush("#42D9FF"), Margin = new Thickness(0, 0, 0, 10) });
        body.FontSize = 13; body.Foreground = Brush("#D7E4F7"); body.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(body);
        return new Border { Background = Brush("#111B2B"), BorderBrush = Brush("#24344D"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 12), Child = panel };
    }

    private void UpdateGameProfile()
    {
        var game = _gamePicker.SelectedItem?.ToString() ?? "Fortnite";
        _gameDetails.Text = game switch
        {
            "Fortnite" => "FORTNITE COMPETITIVE PROFILE\\n• Check in-game Performance Mode and resolution settings.\\n• Close unnecessary background apps before launching.\\n• Use the network test to check packet loss and connection variation.\\n• Compare frame pacing in a repeatable Creative session; this app does not alter Fortnite files.",
            "NBA 2K" => "NBA 2K PROFILE\\n• Check the game's display mode, resolution and frame-rate options.\\n• Use a wired connection when practical for online play.\\n• Run the network test before online matches; this is not 2K server latency.\\n• Keep graphics drivers and Windows updates current.",
            "Valorant" => "VALORANT PROFILE\\n• Prioritize consistent frame times and a stable refresh-rate setting.\\n• Close heavy background downloads before playing.\\n• Check packet loss and network variation with the diagnostics panel.",
            "Rocket League" => "ROCKET LEAGUE PROFILE\\n• Keep frame rate consistent with your display settings.\\n• Check network stability before competitive sessions.\\n• Avoid running unnecessary overlays and background capture tools.",
            "Apex Legends" => "APEX LEGENDS PROFILE\\n• Review resolution and graphics options in-game.\\n• Monitor CPU load and available memory while playing.\\n• Check connection stability; results here are not Apex server ping.",
            "Call of Duty" => "CALL OF DUTY PROFILE\\n• Review in-game display and graphics settings.\\n• Allow shader compilation to finish before judging performance.\\n• Check network stability before online sessions.",
            "Minecraft" => "MINECRAFT PROFILE\\n• Review render distance and graphics settings in-game.\\n• Keep modpacks and resource packs appropriate for your PC.\\n• Check memory availability before launching large modpacks.",
            _ => "ROBLOX PROFILE\\n• Close unneeded background applications.\\n• Check network stability and use in-game graphics settings appropriate for your PC."
        };
        _openGame.Content = game == "Fortnite" || game == "Rocket League" ? "OPEN EPIC GAMES" :
            game == "NBA 2K" || game == "Apex Legends" || game == "Call of Duty" ? "OPEN STEAM LIBRARY" :
            game == "Valorant" ? "OPEN VALORANT WEBSITE" :
            game == "Minecraft" ? "OPEN MINECRAFT WEBSITE" : "OPEN ROBLOX WEBSITE";
    }

    private void OpenGamePlatform()
    {
        var game = _gamePicker.SelectedItem?.ToString() ?? "Fortnite";
        var target = game switch
        {
            "Fortnite" => "com.epicgames.launcher://apps/Fortnite?action=launch&silent=true",
            "Rocket League" => "com.epicgames.launcher://apps/Sugar?action=launch&silent=true",
            "NBA 2K" or "Apex Legends" or "Call of Duty" => "steam://open/library",
            "Valorant" => "https://playvalorant.com/",
            "Minecraft" => "https://www.minecraft.net/",
            _ => "https://www.roblox.com/"
        };
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); _status.Text = "Opened platform link for " + game + "."; }
        catch (Exception ex) { _status.Text = "Could not open platform link: " + ex.Message; }
    }

    private async Task RefreshAll()
    {
        _refresh.IsEnabled = false;
        _status.Text = "Collecting local diagnostics…";
        try
        {
            var cpuTask = Task.Run(ReadCpuUsage);
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n =>
                {
                    var p = n.GetIPProperties();
                    var ips = p.UnicastAddresses.Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6).Select(a => a.Address.ToString());
                    var gateways = p.GatewayAddresses.Select(g => g.Address.ToString()).Where(x => x != "0.0.0.0");
                    var dns = p.DnsAddresses.Select(d => d.ToString());
                    return $"{n.Name}  [{n.NetworkInterfaceType}]  {n.OperationalStatus}\n  Speed: {(n.Speed > 0 ? $"{n.Speed / 1_000_000} Mbps" : "Unavailable")}\n  IP: {string.Join(", ", ips)}\n  Gateway: {string.Join(", ", gateways)}\n  DNS: {string.Join(", ", dns)}";
                }).ToArray();

            var mem = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() }; GlobalMemoryStatusEx(ref mem);
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows)[..1], StringComparison.OrdinalIgnoreCase));
            var cpu = await cpuTask;
            _system.Text = $"Computer: {Environment.MachineName}\nWindows: {Environment.OSVersion}\nProcessor: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Windows processor"}\nCPU usage (sample): {cpu:F0}%\nMemory: {(mem.TotalPhys - mem.AvailPhys) / 1024d / 1024 / 1024:F1} GB used / {mem.TotalPhys / 1024d / 1024 / 1024:F1} GB total\nSystem drive: {(drive is null ? "Unavailable" : $"{drive.Name} {drive.AvailableFreeSpace / 1024d / 1024 / 1024:F1} GB free / {drive.TotalSize / 1024d / 1024 / 1024:F1} GB")}";
            _network.Text = adapters.Length == 0 ? "No network adapters found." : string.Join("\n\n", adapters);
            _wifi.Text = await ReadWifi();
            _status.Text = $"Updated {DateTime.Now:T}  •  {adapters.Length} adapter(s) detected";
        }
        catch (Exception ex) { _status.Text = "Diagnostics error: " + ex.Message; }
        finally { _refresh.IsEnabled = true; }
    }

    private static async Task<string> ReadWifi()
    {
        try
        {
            var psi = new ProcessStartInfo("netsh", "wlan show interfaces") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is null) return "Wi-Fi information unavailable.";
            var output = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();
            if (p.ExitCode != 0 || string.IsNullOrWhiteSpace(output)) return "No Wi-Fi interface details reported (this is normal for Ethernet-only systems).";
            var lines = output.Split('\n').Select(x => x.Trim()).Where(x => Regex.IsMatch(x, @"^(Name|State|SSID|BSSID|Radio type|Channel|Receive rate|Transmit rate|Signal)\s*:")).ToArray();
            return lines.Length == 0 ? "No connected Wi-Fi interface details found." : string.Join("\n", lines);
        }
        catch { return "Wi-Fi details are unavailable on this Windows configuration."; }
    }

    private async Task RunNetworkTest()
    {
        _test.IsEnabled = false;
        _latency.Text = "Sending 8 ICMP probes to 1.1.1.1…";
        try
        {
            var times = new List<long>(); var lost = 0;
            using var ping = new Ping();
            for (var i = 0; i < 8; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync(IPAddress.Parse("1.1.1.1"), 1800);
                    if (reply.Status == IPStatus.Success) times.Add(reply.RoundtripTime); else lost++;
                }
                catch { lost++; }
            }
            var avg = times.Count == 0 ? 0 : times.Average();
            var jitter = times.Count < 2 ? 0 : times.Zip(times.Skip(1), (a, b) => Math.Abs(a - b)).Average();
            _latency.Text = $"Target: 1.1.1.1 (Cloudflare)\nSuccessful replies: {times.Count}/8\nPacket loss: {lost / 8d * 100:F0}%\nAverage RTT: {(times.Count == 0 ? "No replies" : $"{avg:F1} ms")}\nJitter estimate: {(times.Count < 2 ? "N/A" : $"{jitter:F1} ms")}\n\nThis is a basic ICMP test from this PC, not your game's server ping.";
        }
        catch (Exception ex) { _latency.Text = "Test failed: " + ex.Message; }
        finally { _test.IsEnabled = true; }
    }

    private static async Task<double> ReadCpuUsage()
    {
        GetSystemTimes(out var idle1, out var kernel1, out var user1);
        await Task.Delay(500);
        GetSystemTimes(out var idle2, out var kernel2, out var user2);
        var idle = FileTime(idle2) - FileTime(idle1);
        var total = (FileTime(kernel2) - FileTime(kernel1)) + (FileTime(user2) - FileTime(user1));
        return total <= 0 ? 0 : Math.Clamp((1d - (double)idle / total) * 100, 0, 100);
    }
    private static ulong FileTime(System.Runtime.InteropServices.ComTypes.FILETIME t) => ((ulong)(uint)t.dwHighDateTime << 32) | (uint)t.dwLowDateTime;
    [DllImport("kernel32.dll")] private static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME idle, out System.Runtime.InteropServices.ComTypes.FILETIME kernel, out System.Runtime.InteropServices.ComTypes.FILETIME user);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] private struct MemoryStatus
    {
        public uint Length; public uint MemoryLoad; public ulong TotalPhys; public ulong AvailPhys; public ulong TotalPageFile; public ulong AvailPageFile; public ulong TotalVirtual; public ulong AvailVirtual; public ulong AvailExtendedVirtual;
    }
}