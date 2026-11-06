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