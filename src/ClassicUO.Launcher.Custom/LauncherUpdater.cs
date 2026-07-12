using System;
using System.Collections.Generic;
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
            string? latestTag = null;
            string? releaseNotes = null;
            string? clientUrl = null;
            string? launcherUrl = null;
            string? clientFile = null;
            string? launcherFile = null;
            string bestVersion = "";

            string apiUrl = $"https://api.github.com/repos/{LauncherManifest.GitHubRepo}/releases?per_page=30";
            using var response = await Http.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (JsonElement release in doc.RootElement.EnumerateArray())
            {
                if (release.TryGetProperty("draft", out JsonElement draftEl) && draftEl.GetBoolean())
                {
                    continue;
                }

                if (release.TryGetProperty("prerelease", out JsonElement preEl) && preEl.GetBoolean())
                {
                    continue;
                }

                string tag = release.GetProperty("tag_name").GetString() ?? "";
                if (!MatchesEditionTag(tag))
                {
                    continue;
                }

                string? version = ParseVersionFromTag(tag);
                if (string.IsNullOrWhiteSpace(version))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(bestVersion) &&
                    CompareVersions(version, bestVersion) <= 0)
                {
                    continue;
                }

                string? candidateClientUrl = null;
                string? candidateLauncherUrl = null;
                string? candidateClientFile = null;
                string? candidateLauncherFile = null;

                foreach (JsonElement asset in release.GetProperty("assets").EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                    if (IsClientPackage(name))
                    {
                        candidateClientUrl = url;
                        candidateClientFile = name;
                    }
                    else if (IsLauncherPackage(name))
                    {
                        candidateLauncherUrl = url;
                        candidateLauncherFile = name;
                    }
                }

                if (candidateClientUrl == null && candidateLauncherUrl == null)
                {
                    continue;
                }

                bestVersion = version;
                latestTag = tag;
                releaseNotes = release.TryGetProperty("body", out JsonElement bodyEl)
                    ? bodyEl.GetString() ?? ""
                    : "";
                clientUrl = candidateClientUrl;
                launcherUrl = candidateLauncherUrl;
                clientFile = candidateClientFile;
                launcherFile = candidateLauncherFile;
            }

            if (string.IsNullOrWhiteSpace(bestVersion) || latestTag == null)
            {
                return null;
            }

            string effectiveClientVersion = string.IsNullOrWhiteSpace(localClientVersion)
                ? LauncherManifest.ClientRuntimeVersion
                : localClientVersion;

            bool needsClient = NeedsComponentUpdate(bestVersion, effectiveClientVersion, clientUrl != null);
            bool needsLauncher = NeedsComponentUpdate(bestVersion, LauncherManifest.RuntimeLauncherVersion, launcherUrl != null);

            return new UpdateCheckResult
            {
                LatestVersion = bestVersion,
                ReleaseNotes = FormatReleaseNotes(releaseNotes ?? ""),
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

                string installRoot = AppContext.BaseDirectory;
                string sourceAssistant = Path.Combine(extractDir, "Assistant");
                if (Directory.Exists(sourceAssistant))
                {
                    MergeAssistantFolder(sourceAssistant, Path.Combine(installRoot, "Assistant"));
                }

                string currentExe = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, "UODreams Launcher.exe");
                string stagingExe = currentExe + ".new";
                string logFile = Path.Combine(tempDir, "launcher-update.log");
                int pid = Environment.ProcessId;

                string updaterScript = Path.Combine(tempDir, "apply-update.cmd");
                File.WriteAllText(updaterScript, $"""
                    @echo off
                    setlocal
                    set "PID={pid}"
                    set "SRC={newExe}"
                    set "DST={currentExe}"
                    set "STG={stagingExe}"
                    set "LOG={logFile}"
                    echo [%date% %time%] Waiting for launcher PID %PID%>>"%LOG%"
                    :waitloop
                    tasklist /FI "PID eq %PID%" 2>nul | find "%PID%" >nul
                    if %errorlevel%==0 (
                      timeout /t 1 /nobreak >nul
                      goto waitloop
                    )
                    if exist "%STG%" del /F /Q "%STG%" >>"%LOG%" 2>&1
                    echo [%date% %time%] Copying update to staging>>"%LOG%"
                    copy /Y "%SRC%" "%STG%" >>"%LOG%" 2>&1
                    if errorlevel 1 (
                      timeout /t 2 /nobreak >nul
                      copy /Y "%SRC%" "%STG%" >>"%LOG%" 2>&1
                    )
                    if errorlevel 1 goto copyfailed
                    echo [%date% %time%] Replacing launcher>>"%LOG%"
                    move /Y "%STG%" "%DST%" >>"%LOG%" 2>&1
                    if errorlevel 1 (
                      copy /Y "%STG%" "%DST%" >>"%LOG%" 2>&1
                    )
                    if errorlevel 1 goto copyfailed
                    echo [%date% %time%] Restarting launcher>>"%LOG%"
                    start "" "%DST%"
                    del "%~f0"
                    exit /b 0
                    :copyfailed
                    echo [%date% %time%] Launcher update failed>>"%LOG%"
                    exit /b 1
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

        private static bool MatchesEditionTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            if (LauncherManifest.IsPvpEdition)
            {
                if (tag.StartsWith("pvp-v", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Legacy single-channel releases (v1.0.x) were the modded/PVP line.
                return tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
                       !tag.StartsWith("classic-", StringComparison.OrdinalIgnoreCase);
            }

            return tag.StartsWith("classic-v", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ParseVersionFromTag(string tag)
        {
            Match match = Regex.Match(tag, @"(?:pvp-v|classic-v|v)([\d.]+)$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static bool IsClientPackage(string name)
        {
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (LauncherManifest.IsPvpEdition)
            {
                return name.StartsWith($"{LauncherManifest.AssetPrefix}-Client-v", StringComparison.OrdinalIgnoreCase) ||
                       (name.StartsWith("UODreams-Client-v", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-Classic-", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-PVP-", StringComparison.OrdinalIgnoreCase));
            }

            return name.StartsWith($"{LauncherManifest.AssetPrefix}-Client-v", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLauncherPackage(string name)
        {
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (LauncherManifest.IsPvpEdition)
            {
                return name.StartsWith($"{LauncherManifest.AssetPrefix}-Launcher-v", StringComparison.OrdinalIgnoreCase) ||
                       (name.StartsWith("UODreams-Launcher-v", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-Classic-", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-PVP-", StringComparison.OrdinalIgnoreCase));
            }

            return name.StartsWith($"{LauncherManifest.AssetPrefix}-Launcher-v", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatReleaseNotes(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }

            bool inNovita = false;
            var sb = new StringBuilder();
            foreach (string rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                {
                    if (inNovita && sb.Length > 0 && !sb.ToString().EndsWith("\n\n", StringComparison.Ordinal))
                    {
                        sb.AppendLine();
                    }

                    continue;
                }

                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    if (!inNovita)
                    {
                        if (line.StartsWith("### Novit", StringComparison.OrdinalIgnoreCase))
                        {
                            inNovita = true;
                        }

                        continue;
                    }

                    if (line.StartsWith("### Install", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("### Server", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    continue;
                }

                if (!inNovita)
                {
                    continue;
                }

                string item = line.TrimStart('-', '*', ' ', '\t');
                item = item.Replace("**", "");
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

        private static void MergeAssistantFolder(string sourceRoot, string targetRoot)
        {
            if (!Directory.Exists(sourceRoot))
            {
                return;
            }

            Directory.CreateDirectory(targetRoot);

            foreach (string file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(sourceRoot, file);
                string normalized = relative.Replace('\\', '/');
                if (IsAssistantUserDataRelativePath(normalized))
                {
                    continue;
                }

                string destination = Path.Combine(targetRoot, relative);
                if (File.Exists(destination))
                {
                    continue;
                }

                string? parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, destination, overwrite: false);
            }
        }

        private static bool IsAssistantUserDataRelativePath(string normalizedRelativePath)
        {
            foreach (string folder in new[] { "Profiles", "Scripts", "Backup", "_deploy_pending" })
            {
                if (normalizedRelativePath.StartsWith($"RazorEnhanced/{folder}/", StringComparison.OrdinalIgnoreCase) ||
                    normalizedRelativePath.Equals($"RazorEnhanced/{folder}", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static int CompareVersions(string? left, string? right)
        {
            left = NormalizeVersion(left);
            right = NormalizeVersion(right);

            if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(left))
            {
                return -1;
            }

            if (string.IsNullOrWhiteSpace(right))
            {
                return 1;
            }

            try
            {
                return Version.Parse(left).CompareTo(Version.Parse(right));
            }
            catch
            {
                return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        internal static string MaxVersion(string? left, string? right)
        {
            left = NormalizeVersion(left);
            right = NormalizeVersion(right);

            if (string.IsNullOrWhiteSpace(left))
            {
                return right ?? "";
            }

            if (string.IsNullOrWhiteSpace(right))
            {
                return left;
            }

            return CompareVersions(left, right) >= 0 ? left : right;
        }

        private static bool NeedsComponentUpdate(string remoteLatest, string localVersion, bool packageAvailable)
        {
            if (!packageAvailable)
            {
                return false;
            }

            // Only offer an update when GitHub is strictly newer than local; equal or local-ahead means up to date.
            return CompareVersions(remoteLatest, localVersion) > 0;
        }

        private static string NormalizeVersion(string? version)
        {
            return (version ?? "").Trim().TrimStart('v', 'V');
        }
    }
}
