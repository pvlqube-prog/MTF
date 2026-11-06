# QUBE TUNE

QUBE TUNE is a Windows desktop companion for local PC and network diagnostics.

## First Windows milestone

- CPU usage sample using Windows system-time counters
- Installed memory usage
- System drive capacity/free space
- Network adapter status, link speed, IP addresses, default gateways, and DNS servers
- Wi-Fi interface details from Windows `netsh` (when available)
- Eight ICMP probes to Cloudflare DNS with average round-trip time, a simple jitter estimate, and packet-loss percentage
- Dark QUBE TUNE dashboard

## Build

Requires Windows and the .NET 8 SDK:

```powershell
dotnet publish QubeTune.Windows/QubeTune.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Run `publish/QubeTune.exe`.

## Automatic build

GitHub Actions runs on pushes to `main` and on manual dispatch. Open the repository's **Actions** tab, select **Build QUBE TUNE for Windows**, and download the `QUBE-TUNE-Windows-x64` artifact after the workflow succeeds. The artifact is a ZIP containing the self-contained app files.

## Scope and limitations

This milestone is read-only. It does not change Windows settings, edit the registry, disable security features, overclock hardware, manipulate game traffic, or promise FPS/ping improvements. ICMP results to 1.1.1.1 are a basic connectivity sample and are not the same as latency to a game server. GPU temperature and live in-game FPS are not included in this first build.

QUBE TUNE is an independent project and is not affiliated with Microsoft, Cloudflare, or game publishers.