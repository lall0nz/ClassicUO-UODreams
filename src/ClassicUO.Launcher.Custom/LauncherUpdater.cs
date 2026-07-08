using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    internal sealed class UpdateCheckResult
    {
        public string LatestVersion { get; init; } = "";
        public string ReleaseNotes { get; init; } = "";
        public bool NeedsClientUpdate { get; init; }
        public bool NeedsLauncherUpdate { get; init; }
        public string? ClientDownloadUrl { get; init; }
        public string? LauncherDownloadUrl { get; init; }
        public string? ClientPackageFileName { get; init; }
        public string? LauncherPackageFileName { get; init; }

        public bool HasAnyUpdate => NeedsClientUpdate || NeedsLauncherUpdate;
    }

    internal static class LauncherUpdater
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        static LauncherUpdater()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("UODreamsLauncher");
        }

        public static async Task<UpdateCheckResult?> CheckForUpdatesAsync(
            string? localClientVersion = null,
            CancellationToken cancellationToken = default)
        {
            string apiUrl = $"https://api.github.com/repos/{LauncherManifest.GitHubRepo}/releases/latest";
            using var response = await Http.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            string latest = tag.TrimStart('v', 'V');
            if (string.IsNullOrWhiteSpace(latest))
            {
                return null;
            }

            string body = doc.RootElement.TryGetProperty("body", out JsonElement bodyEl)
                ? bodyEl.GetString() ?? ""
                : "";

            string? clientUrl = null;
            string? launcherUrl = null;
            string? clientFile = null;
            string? launcherFile = null;

            foreach (JsonElement asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                if (name.StartsWith("UODreams-Client-v", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    clientUrl = url;
                    clientFile = name;
                }
                else if (name.StartsWith("UODreams-Launcher-v", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    launcherUrl = url;
                    launcherFile = name;
                }
            }

            string effectiveClientVersion = string.IsNullOrWhiteSpace(localClientVersion)
                ? LauncherManifest.ClientRuntimeVersion
                : localClientVersion;

            bool needsClient = IsRemoteNewer(latest, effectiveClientVersion) && clientUrl != null;
            bool needsLauncher = IsRemoteNewer(latest, LauncherManifest.LauncherVersion) && launcherUrl != null;

            return new UpdateCheckResult
            {
                LatestVersion = latest,
                ReleaseNotes = FormatReleaseNotes(body),
                NeedsClientUpdate = needsClient,
                NeedsLauncherUpdate = needsLauncher,
                ClientDownloadUrl = clientUrl,
                LauncherDownloadUrl = launcherUrl,
                ClientPackageFileName = clientFile,
                LauncherPackageFileName = launcherFile
            };
        }

        public static async Task ApplyLauncherUpdateAsync(
            string downloadUrl,
            string packageFileName,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", "launcher-update", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, packageFileName);
            string extractDir = Path.Combine(tempDir, "extract");
            Directory.CreateDirectory(extractDir);

            try
            {
                progress?.Report(new DownloadProgressReport { Status = Loc.S("Download launcher…", "Downloading launcher…") });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    downloadUrl,
                    archivePath,
                    progress,
                    cancellationToken
                ).ConfigureAwait(false);

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Estrazione launcher…", "Extracting launcher…"),
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                ZipFile.ExtractToDirectory(archivePath, extractDir, overwriteFiles: true);

                string? newExe = FindLauncherExe(extractDir);
                if (newExe == null)
                {
                    throw new InvalidDataException(Loc.S(
                        "Pacchetto launcher non valido (UODreams Launcher.exe mancante).",
                        "Invalid launcher package (UODreams Launcher.exe missing)."));
                }

                string currentExe = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, "UODreams Launcher.exe");
                int pid = Environment.ProcessId;

                string updaterScript = Path.Combine(tempDir, "apply-update.cmd");
                File.WriteAllText(updaterScript, $"""
                    @echo off
                    setlocal
                    set "PID={pid}"
                    set "SRC={newExe}"
                    set "DST={currentExe}"
                    :waitloop
                    tasklist /FI "PID eq %PID%" 2>nul | find "%PID%" >nul
                    if %errorlevel%==0 (
                      timeout /t 1 /nobreak >nul
                      goto waitloop
                    )
                    copy /Y "%SRC%" "%DST%" >nul
                    if errorlevel 1 (
                      timeout /t 2 /nobreak >nul
                      copy /Y "%SRC%" "%DST%" >nul
                    )
                    start "" "%DST%"
                    del "%~f0"
                    """);

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterScript,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Riavvio launcher…", "Restarting launcher…"),
                    BytesReceived = 1,
                    TotalBytes = 1
                });
            }
            finally
            {
                // temp cleanup is best-effort; updater script deletes itself
            }
        }

        public static string? ParseVersionFromPackageName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            Match match = Regex.Match(fileName, @"v([\d.]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string FormatReleaseNotes(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (string rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                {
                    if (sb.Length > 0 && !sb.ToString().EndsWith("\n\n", StringComparison.Ordinal))
                    {
                        sb.AppendLine();
                    }

                    continue;
                }

                if (line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("### Install", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("### Server", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                string item = line.TrimStart('-', '*', ' ', '\t');
                if (item.Length == 0)
                {
                    continue;
                }

                sb.AppendLine(item.StartsWith('•') ? item : "• " + item);
            }

            return sb.ToString().Trim();
        }

        private static string? FindLauncherExe(string root)
        {
            string direct = Path.Combine(root, "UODreams Launcher.exe");
            if (File.Exists(direct))
            {
                return direct;
            }

            foreach (string file in Directory.EnumerateFiles(root, "UODreams Launcher.exe", SearchOption.AllDirectories))
            {
                return file;
            }

            return null;
        }

        private static bool IsRemoteNewer(string remote, string local)
        {
            try
            {
                remote = remote.Trim().TrimStart('v', 'V');
                local = local.Trim().TrimStart('v', 'V');
                return Version.Parse(remote) > Version.Parse(local);
            }
            catch
            {
                return !string.Equals(remote, local, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
