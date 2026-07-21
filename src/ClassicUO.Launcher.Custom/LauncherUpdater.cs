using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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
        public bool UsesManifest { get; init; }
        public bool NeedsClientUpdate { get; init; }
        public bool NeedsLauncherUpdate { get; init; }
        public bool NeedsRazorUpdate { get; init; }
        public string? ClientDownloadUrl { get; init; }
        public string? LauncherDownloadUrl { get; init; }
        public string? RazorDownloadUrl { get; init; }
        public string? ClientPackageFileName { get; init; }
        public string? LauncherPackageFileName { get; init; }
        public string? RazorPackageFileName { get; init; }
        public string? ClientRemoteVersion { get; init; }
        public string? LauncherRemoteVersion { get; init; }
        public string? RazorRemoteVersion { get; init; }
        public string LocalClientVersion { get; init; } = "";
        public string LocalLauncherVersion { get; init; } = "";
        public string LocalRazorVersion { get; init; } = "";
        public string? ClientSha256 { get; init; }
        public string? LauncherSha256 { get; init; }
        public string? RazorSha256 { get; init; }
        public long ClientSizeBytes { get; init; }
        public long LauncherSizeBytes { get; init; }
        public long RazorSizeBytes { get; init; }

        public bool HasAnyUpdate => NeedsClientUpdate || NeedsLauncherUpdate || NeedsRazorUpdate;

        public long TotalDownloadBytes =>
            (NeedsClientUpdate ? ClientSizeBytes : 0) +
            (NeedsLauncherUpdate ? LauncherSizeBytes : 0) +
            (NeedsRazorUpdate ? RazorSizeBytes : 0);
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

        public const string RazorVersionMarkerFileName = "uodreams-razor.version";

        public static async Task<UpdateCheckResult?> CheckForUpdatesAsync(
            string? localClientVersion = null,
            CancellationToken cancellationToken = default)
        {
            string apiUrl = $"https://api.github.com/repos/{LauncherManifest.GitHubRepo}/releases?per_page=30";
            using var response = await Http.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            string effectiveClientVersion = ResolveEffectiveClientVersion(localClientVersion);
            string localLauncherVersion = LauncherManifest.RuntimeLauncherVersion;
            string localRazorVersion = ResolveEffectiveRazorVersion();

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

                UpdateManifest? manifest = await TryLoadManifestForReleaseAsync(release, tag, cancellationToken)
                    .ConfigureAwait(false);
                if (manifest != null)
                {
                    UpdateCheckResult? manifestResult = BuildManifestUpdateResult(
                        manifest,
                        effectiveClientVersion,
                        localLauncherVersion,
                        localRazorVersion);
                    if (manifestResult != null)
                    {
                        return manifestResult;
                    }
                }

                UpdateCheckResult? legacyResult = BuildLegacyUpdateResult(
                    release,
                    tag,
                    effectiveClientVersion,
                    localLauncherVersion);
                if (legacyResult != null)
                {
                    return legacyResult;
                }
            }

            return null;
        }

        public static string? TryReadRazorVersionMarker(string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string path = Path.Combine(installRoot, "Assistant", "RazorEnhanced", RazorVersionMarkerFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return NormalizeVersion(File.ReadAllText(path).Trim());
            }
            catch
            {
                return null;
            }
        }

        public static void WriteRazorVersionMarker(string version, string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string razorDir = Path.Combine(installRoot, "Assistant", "RazorEnhanced");
            Directory.CreateDirectory(razorDir);
            File.WriteAllText(
                Path.Combine(razorDir, RazorVersionMarkerFileName),
                NormalizeVersion(version)
            );
        }

        internal static string ResolveEffectiveRazorVersion(string? installRoot = null)
        {
            string? markerVersion = TryReadRazorVersionMarker(installRoot);
            if (!string.IsNullOrWhiteSpace(markerVersion))
            {
                return markerVersion;
            }

            string razorExe = Path.Combine(
                installRoot ?? AppContext.BaseDirectory,
                "Assistant",
                "RazorEnhanced",
                "RazorEnhanced.exe");
            if (File.Exists(razorExe))
            {
                // Pre-marker installs bundled with the launcher zip.
                return "0.0.0";
            }

            return "0.0.0";
        }

        public static async Task ApplyRazorUpdateAsync(
            string downloadUrl,
            string packageFileName,
            string? expectedSha256,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", "razor-update", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, packageFileName);
            string extractDir = Path.Combine(tempDir, "extract");
            Directory.CreateDirectory(extractDir);

            try
            {
                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Download Razor Enhanced…", "Downloading Razor Enhanced…")
                });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    downloadUrl,
                    archivePath,
                    progress,
                    cancellationToken
                ).ConfigureAwait(false);

                VerifyDownloadedSha256(archivePath, expectedSha256);

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Estrazione Razor Enhanced…", "Extracting Razor Enhanced…"),
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                ZipFile.ExtractToDirectory(archivePath, extractDir, overwriteFiles: true);

                string installRoot = AppContext.BaseDirectory;
                string? sourceAssistant = ResolveAssistantSourceRoot(extractDir);
                if (sourceAssistant == null)
                {
                    throw new InvalidDataException(Loc.S(
                        "Pacchetto Razor non valido (cartella Assistant mancante).",
                        "Invalid Razor package (Assistant folder missing)."));
                }

                UpdateAssistantFolder(sourceAssistant, Path.Combine(installRoot, "Assistant"));

                string? razorVersion = ParseVersionFromPackageName(packageFileName);
                if (!string.IsNullOrWhiteSpace(razorVersion))
                {
                    WriteRazorVersionMarker(razorVersion, installRoot);
                }

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Razor Enhanced aggiornato.", "Razor Enhanced updated."),
                    BytesReceived = 1,
                    TotalBytes = 1
                });
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, recursive: true);
                    }
                }
                catch
                {
                    // temp cleanup is best-effort
                }
            }
        }

        internal static void VerifyDownloadedSha256(string filePath, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256))
            {
                return;
            }

            string actual = ComputeSha256Hex(filePath);
            if (!actual.Equals(expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(Loc.S(
                    "Verifica SHA256 fallita: il pacchetto scaricato non corrisponde al manifest.",
                    "SHA256 verification failed: downloaded package does not match the manifest."));
            }
        }

        internal static string ComputeSha256Hex(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            byte[] hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static async Task<UpdateManifest?> TryLoadManifestForReleaseAsync(
            JsonElement release,
            string releaseTag,
            CancellationToken cancellationToken)
        {
            if (!release.TryGetProperty("assets", out JsonElement assetsEl))
            {
                return null;
            }

            string? manifestUrl = null;
            foreach (JsonElement asset in assetsEl.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.Equals(UpdateManifest.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    manifestUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(manifestUrl))
            {
                return null;
            }

            try
            {
                using var response = await Http.GetAsync(manifestUrl, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                UpdateManifest? manifest = UpdateManifest.TryParse(json);
                if (manifest == null || !manifest.IsSupportedForCurrentEdition())
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(manifest.ReleaseTag) &&
                    !manifest.ReleaseTag.Equals(releaseTag, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        private static UpdateCheckResult? BuildManifestUpdateResult(
            UpdateManifest manifest,
            string localClientVersion,
            string localLauncherVersion,
            string localRazorVersion)
        {
            UpdateManifestComponent? launcherComponent = manifest.GetComponent("launcher");
            UpdateManifestComponent? clientComponent = manifest.GetComponent("client");
            UpdateManifestComponent? razorComponent = manifest.GetComponent("razor");

            if (launcherComponent == null && clientComponent == null && razorComponent == null)
            {
                return null;
            }

            string releaseTag = string.IsNullOrWhiteSpace(manifest.ReleaseTag)
                ? $"v{LauncherManifest.LauncherVersion}"
                : manifest.ReleaseTag;

            bool needsLauncher = launcherComponent != null &&
                NeedsComponentUpdate(launcherComponent.Version, localLauncherVersion, true);
            bool needsClient = clientComponent != null &&
                NeedsComponentUpdate(clientComponent.Version, localClientVersion, true);
            bool needsRazor = razorComponent != null &&
                NeedsComponentUpdate(razorComponent.Version, localRazorVersion, true);

            string latestVersion = MaxVersion(
                MaxVersion(
                    launcherComponent?.Version,
                    clientComponent?.Version),
                razorComponent?.Version);

            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                latestVersion = ParseVersionFromTag(releaseTag) ?? localLauncherVersion;
            }

            string releaseNotes = manifest.GetLocalizedNotes();
            if (string.IsNullOrWhiteSpace(releaseNotes))
            {
                releaseNotes = BuildManifestComponentNotes(clientComponent, razorComponent, launcherComponent);
            }

            return new UpdateCheckResult
            {
                LatestVersion = latestVersion,
                ReleaseNotes = releaseNotes,
                UsesManifest = true,
                NeedsClientUpdate = needsClient,
                NeedsLauncherUpdate = needsLauncher,
                NeedsRazorUpdate = needsRazor,
                ClientDownloadUrl = clientComponent?.BuildDownloadUrl(releaseTag),
                LauncherDownloadUrl = launcherComponent?.BuildDownloadUrl(releaseTag),
                RazorDownloadUrl = razorComponent?.BuildDownloadUrl(releaseTag),
                ClientPackageFileName = clientComponent?.Asset,
                LauncherPackageFileName = launcherComponent?.Asset,
                RazorPackageFileName = razorComponent?.Asset,
                ClientRemoteVersion = clientComponent?.Version,
                LauncherRemoteVersion = launcherComponent?.Version,
                RazorRemoteVersion = razorComponent?.Version,
                LocalClientVersion = localClientVersion,
                LocalLauncherVersion = localLauncherVersion,
                LocalRazorVersion = localRazorVersion,
                ClientSha256 = clientComponent?.Sha256,
                LauncherSha256 = launcherComponent?.Sha256,
                RazorSha256 = razorComponent?.Sha256,
                ClientSizeBytes = clientComponent?.SizeBytes ?? 0,
                LauncherSizeBytes = launcherComponent?.SizeBytes ?? 0,
                RazorSizeBytes = razorComponent?.SizeBytes ?? 0
            };
        }

        private static string BuildManifestComponentNotes(
            UpdateManifestComponent? clientComponent,
            UpdateManifestComponent? razorComponent,
            UpdateManifestComponent? launcherComponent)
        {
            var lines = new List<string>();
            foreach (UpdateManifestComponent? component in new[] { clientComponent, razorComponent, launcherComponent })
            {
                if (component == null)
                {
                    continue;
                }

                string note = component.GetLocalizedNotes();
                if (!string.IsNullOrWhiteSpace(note))
                {
                    lines.Add("• " + note);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static UpdateCheckResult? BuildLegacyUpdateResult(
            JsonElement release,
            string tag,
            string effectiveClientVersion,
            string localLauncherVersion)
        {
            string? version = ParseVersionFromTag(tag);
            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            string? clientUrl = null;
            string? launcherUrl = null;
            string? clientFile = null;
            string? launcherFile = null;

            foreach (JsonElement asset in release.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                if (IsClientPackage(name))
                {
                    clientUrl = url;
                    clientFile = name;
                }
                else if (IsLauncherPackage(name))
                {
                    launcherUrl = url;
                    launcherFile = name;
                }
            }

            if (clientUrl == null && launcherUrl == null)
            {
                return null;
            }

            string releaseNotes = release.TryGetProperty("body", out JsonElement bodyEl)
                ? bodyEl.GetString() ?? ""
                : "";

            bool needsClient = NeedsComponentUpdate(version, effectiveClientVersion, clientUrl != null);
            bool needsLauncher = NeedsComponentUpdate(version, localLauncherVersion, launcherUrl != null);

            if (!needsClient &&
                clientUrl != null &&
                CompareVersions(localLauncherVersion, effectiveClientVersion) > 0 &&
                CompareVersions(version, effectiveClientVersion) >= 0)
            {
                needsClient = true;
            }

            if (!needsClient && needsLauncher && clientUrl != null)
            {
                needsClient = true;
            }

            if (needsClient && !needsLauncher && launcherUrl != null)
            {
                needsLauncher = true;
            }

            return new UpdateCheckResult
            {
                LatestVersion = version,
                ReleaseNotes = FormatReleaseNotes(releaseNotes),
                UsesManifest = false,
                NeedsClientUpdate = needsClient,
                NeedsLauncherUpdate = needsLauncher,
                NeedsRazorUpdate = false,
                ClientDownloadUrl = clientUrl,
                LauncherDownloadUrl = launcherUrl,
                ClientPackageFileName = clientFile,
                LauncherPackageFileName = launcherFile,
                LocalClientVersion = effectiveClientVersion,
                LocalLauncherVersion = localLauncherVersion,
                LocalRazorVersion = ResolveEffectiveRazorVersion()
            };
        }

        private static string? ResolveAssistantSourceRoot(string extractDir)
        {
            string direct = Path.Combine(extractDir, "Assistant");
            if (Directory.Exists(direct))
            {
                return direct;
            }

            foreach (string dir in Directory.EnumerateDirectories(extractDir, "Assistant", SearchOption.AllDirectories))
            {
                return dir;
            }

            string razorDirect = Path.Combine(extractDir, "RazorEnhanced");
            if (Directory.Exists(razorDirect))
            {
                return Path.GetDirectoryName(razorDirect);
            }

            foreach (string file in Directory.EnumerateFiles(extractDir, "RazorEnhanced.exe", SearchOption.AllDirectories))
            {
                string? razorDir = Path.GetDirectoryName(file);
                if (razorDir == null)
                {
                    continue;
                }

                string? assistantDir = Path.GetDirectoryName(razorDir);
                if (assistantDir != null && Path.GetFileName(razorDir)
                    .Equals("RazorEnhanced", StringComparison.OrdinalIgnoreCase))
                {
                    return assistantDir;
                }
            }

            return null;
        }

        public static async Task ApplyLauncherUpdateAsync(
            string downloadUrl,
            string packageFileName,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default,
            string? expectedSha256 = null)
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

                VerifyDownloadedSha256(archivePath, expectedSha256);

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
                        "Pacchetto launcher non valido (eseguibile launcher mancante).",
                        "Invalid launcher package (launcher executable missing)."));
                }

                string installRoot = AppContext.BaseDirectory;
                // Prefer Assistant next to the found exe (handles nested zip layouts),
                // then fall back to extract root.
                string? exeDir = Path.GetDirectoryName(newExe);
                string? sourceAssistant = null;
                if (!string.IsNullOrEmpty(exeDir))
                {
                    string besideExe = Path.Combine(exeDir, "Assistant");
                    if (Directory.Exists(besideExe))
                    {
                        sourceAssistant = besideExe;
                    }
                }

                if (sourceAssistant == null)
                {
                    string atRoot = Path.Combine(extractDir, "Assistant");
                    if (Directory.Exists(atRoot))
                    {
                        sourceAssistant = atRoot;
                    }
                }

                if (sourceAssistant != null)
                {
                    // Always overwrite Razor program files (exes/dlls/plugins); profiles guarded inside.
                    UpdateAssistantFolder(sourceAssistant, Path.Combine(installRoot, "Assistant"));
                    string? razorVersion = ParseVersionFromPackageName(packageFileName);
                    if (string.IsNullOrWhiteSpace(razorVersion))
                    {
                        razorVersion = ParseVersionFromPackageName(Path.GetFileName(downloadUrl));
                    }

                    if (!string.IsNullOrWhiteSpace(razorVersion))
                    {
                        WriteRazorVersionMarker(razorVersion, installRoot);
                    }
                }

                string currentExe = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, LauncherExeFileNames[0]);
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

            Match match = Regex.Match(fileName, @"v(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
            return match.Success ? NormalizeVersion(match.Groups[1].Value) : null;
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
            Match match = Regex.Match(tag, @"(?:pvp-v|classic-v|v)(\d+(?:\.\d+)*)$", RegexOptions.IgnoreCase);
            return match.Success ? NormalizeVersion(match.Groups[1].Value) : null;
        }

        private static bool IsClientPackage(string name)
        {
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (LauncherManifest.IsPvpEdition)
            {
                return name.StartsWith("UODreams-PVP-by-lall0ne-Client-v", StringComparison.OrdinalIgnoreCase) ||
                       name.StartsWith($"{LauncherManifest.AssetPrefix}-Client-v", StringComparison.OrdinalIgnoreCase) ||
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
                return name.StartsWith("UODreams-PVP-by-lall0ne-Launcher-v", StringComparison.OrdinalIgnoreCase) ||
                       name.StartsWith($"{LauncherManifest.AssetPrefix}-Launcher-v", StringComparison.OrdinalIgnoreCase) ||
                       (name.StartsWith("UODreams-Launcher-v", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-Classic-", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("UODreams-PVP-", StringComparison.OrdinalIgnoreCase));
            }

            return name.StartsWith($"{LauncherManifest.AssetPrefix}-Launcher-v", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRazorPackage(string name)
        {
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.StartsWith("UODreams-PVP-by-lall0ne-Assistant-Razor-v", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith($"{LauncherManifest.AssetPrefix}-Assistant-Razor-v", StringComparison.OrdinalIgnoreCase);
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
            foreach (string name in LauncherExeFileNames)
            {
                string direct = Path.Combine(root, name);
                if (File.Exists(direct))
                {
                    return direct;
                }

                foreach (string file in Directory.EnumerateFiles(root, name, SearchOption.AllDirectories))
                {
                    return file;
                }
            }

            return null;
        }

#if LAUNCHER_EDITION_ONEUO
        private static readonly string[] LauncherExeFileNames = { "0nE UO Launcher.exe", "UODreams Launcher.exe" };
#else
        private static readonly string[] LauncherExeFileNames = { "UODreams Launcher.exe", "0nE UO Launcher.exe" };
#endif

        /// <summary>
        /// Stock Razor profile name. Never force-replaced by OTA — left as virgin/user-owned.
        /// </summary>
        internal const string StockRazorProfileName = "default";

        /// <summary>
        /// Managed PVP starter profile. Installed from the package only when missing;
        /// never overwritten if the user already has it.
        /// Folder name is case-insensitive on Windows (matches "default pvp").
        /// </summary>
        internal const string BundledPvpProfileName = "Default PVP";

        /// <summary>
        /// Pristine Default PVP profile shipped beside Razor (not under Profiles/) so OTA
        /// can refresh the stock copy used only to repair a corrupted Profiles/Default PVP.
        /// </summary>
        internal const string BundledDefaultProfileFolder = "_bundled_default_pvp";

        internal static string ResolveEffectiveClientVersion(string? storedClientVersion, string? installRoot = null)
        {
            string? markerVersion = ClientRuntimeDownloader.TryReadClientVersionMarker(installRoot);
            if (!string.IsNullOrWhiteSpace(markerVersion))
            {
                return markerVersion;
            }

            string stored = NormalizeVersion(storedClientVersion);
            if (ClientRuntimeDownloader.IsInstalled(installRoot))
            {
                // Pre-marker installs (launcher-only OTA) may report a stale stored version.
                return "0.0.0";
            }

            return string.IsNullOrWhiteSpace(stored) ? "0.0.0" : stored;
        }

        internal static void UpdateAssistantFolder(string sourceRoot, string targetRoot)
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
                if (ShouldPreserveAssistantUserDataOnUpdate(normalized))
                {
                    continue;
                }

                string destination = Path.Combine(targetRoot, relative);
                string? parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, destination, overwrite: true);
            }

            // Preserve existing Profiles/Backup (including Default PVP). Only install missing profiles.
            InstallBundledPvpRazorProfileIfMissing(sourceRoot, targetRoot);
            SyncBundledPvpStock(sourceRoot, targetRoot);
            MergeBundledRazorScripts(sourceRoot, targetRoot);
        }

        /// <summary>
        /// If Profiles/Default PVP GENERAL is missing or not a DataTable JSON array ('['),
        /// replace Profiles/Default PVP and Backup/Default PVP from the pristine
        /// _bundled_default_pvp stock (or from Backup when that copy is still valid).
        /// Stock Profiles/default is never touched.
        /// </summary>
        internal static bool EnsureHealthyDefaultRazorProfile(string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string razorRoot = Path.Combine(installRoot, "Assistant", "RazorEnhanced");
            if (!Directory.Exists(razorRoot))
            {
                return false;
            }

            string profilesPvp = ResolveProfileDirectory(Path.Combine(razorRoot, "Profiles"), BundledPvpProfileName)
                ?? Path.Combine(razorRoot, "Profiles", BundledPvpProfileName);
            string generalPath = Path.Combine(profilesPvp, "RazorEnhanced.settings.GENERAL");
            if (IsValidRazorGeneralSettingsFile(generalPath))
            {
                EnsureBackupPvpFromProfile(razorRoot, profilesPvp);
                return false;
            }

            string stock = Path.Combine(razorRoot, BundledDefaultProfileFolder);
            string backupPvp = ResolveProfileDirectory(Path.Combine(razorRoot, "Backup"), BundledPvpProfileName)
                ?? Path.Combine(razorRoot, "Backup", BundledPvpProfileName);
            string? repairSource = null;
            if (IsValidRazorGeneralSettingsFile(Path.Combine(stock, "RazorEnhanced.settings.GENERAL")))
            {
                repairSource = stock;
            }
            else if (IsValidRazorGeneralSettingsFile(Path.Combine(backupPvp, "RazorEnhanced.settings.GENERAL")))
            {
                repairSource = backupPvp;
            }

            if (repairSource == null)
            {
                return false;
            }

            // Always write canonical folder name "Default PVP".
            string destProfiles = Path.Combine(razorRoot, "Profiles", BundledPvpProfileName);
            string destBackup = Path.Combine(razorRoot, "Backup", BundledPvpProfileName);
            if (!string.Equals(profilesPvp, destProfiles, StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(profilesPvp))
            {
                try { Directory.Delete(profilesPvp, recursive: true); } catch { /* best-effort */ }
            }

            if (!string.Equals(backupPvp, destBackup, StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(backupPvp))
            {
                try { Directory.Delete(backupPvp, recursive: true); } catch { /* best-effort */ }
            }

            ReplaceDirectory(repairSource, destProfiles);
            ReplaceDirectory(repairSource, destBackup);
            return true;
        }

        internal static bool IsBundledPvpProfileName(string? profileName) =>
            !string.IsNullOrWhiteSpace(profileName) &&
            profileName.Equals(BundledPvpProfileName, StringComparison.OrdinalIgnoreCase);

        internal static bool IsValidRazorGeneralSettingsFile(string? generalPath)
        {
            if (string.IsNullOrWhiteSpace(generalPath) || !File.Exists(generalPath))
            {
                return false;
            }

            try
            {
                using var stream = File.OpenRead(generalPath);
                int b;
                while ((b = stream.ReadByte()) >= 0)
                {
                    char c = (char)b;
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    return c == '[';
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        internal static bool ShouldPreserveAssistantUserDataOnUpdate(string normalizedRelativePath)
        {
            if (normalizedRelativePath.StartsWith("RazorEnhanced/_deploy_pending/", StringComparison.OrdinalIgnoreCase) ||
                normalizedRelativePath.Equals("RazorEnhanced/_deploy_pending", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsNonBundledPvpRazorProfilePath(normalizedRelativePath, "Profiles") ||
                IsNonBundledPvpRazorProfilePath(normalizedRelativePath, "Backup"))
            {
                // Preserve stock default and all user profiles (including Profiles/default).
                return true;
            }

            if (IsBundledPvpRazorProfilePath(normalizedRelativePath))
            {
                // Preserve existing Default PVP; only installed if missing (see InstallBundledPvpRazorProfileIfMissing).
                return true;
            }

            if (normalizedRelativePath.StartsWith("RazorEnhanced/Scripts/", StringComparison.OrdinalIgnoreCase) ||
                normalizedRelativePath.Equals("RazorEnhanced/Scripts", StringComparison.OrdinalIgnoreCase))
            {
                // Merged after binary update so bundled starter scripts stay current.
                return true;
            }

            // _bundled_default_pvp stock is always refreshed for repair-only use.
            return false;
        }

        internal static bool IsBundledPvpRazorProfilePath(string normalizedRelativePath)
        {
            foreach (string folder in new[] { "Profiles", "Backup" })
            {
                string prefix = $"RazorEnhanced/{folder}/{BundledPvpProfileName}/";
                if (normalizedRelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedRelativePath.Equals(
                        $"RazorEnhanced/{folder}/{BundledPvpProfileName}",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Back-compat alias used by older call sites / docs.</summary>
        internal static bool IsDefaultRazorProfilePath(string normalizedRelativePath) =>
            IsBundledPvpRazorProfilePath(normalizedRelativePath);

        private static bool IsNonBundledPvpRazorProfilePath(string normalizedRelativePath, string folderName)
        {
            string prefix = $"RazorEnhanced/{folderName}/";
            if (!normalizedRelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string remainder = normalizedRelativePath[prefix.Length..];
            if (remainder.Length == 0)
            {
                return false;
            }

            string profileName = remainder.Split('/')[0];
            return !IsBundledPvpProfileName(profileName);
        }

        /// <summary>
        /// Install Default PVP (+ Backup) from the package only when the profile folder is missing.
        /// Never overwrite an existing user/edited Default PVP.
        /// </summary>
        private static void InstallBundledPvpRazorProfileIfMissing(string sourceRoot, string targetRoot)
        {
            string? sourceProfile = ResolveSourceBundledPvpProfile(sourceRoot);
            if (sourceProfile == null)
            {
                return;
            }

            string profilesRoot = Path.Combine(targetRoot, "RazorEnhanced", "Profiles");
            string backupRoot = Path.Combine(targetRoot, "RazorEnhanced", "Backup");
            string destProfiles = Path.Combine(profilesRoot, BundledPvpProfileName);
            string destBackup = Path.Combine(backupRoot, BundledPvpProfileName);

            bool hasProfiles = ResolveProfileDirectory(profilesRoot, BundledPvpProfileName) != null;
            bool hasBackup = ResolveProfileDirectory(backupRoot, BundledPvpProfileName) != null;

            if (!hasProfiles)
            {
                ReplaceDirectory(sourceProfile, destProfiles);
            }

            if (!hasBackup)
            {
                ReplaceDirectory(sourceProfile, destBackup);
            }
        }

        private static void ReplaceBundledPvpRazorProfile(string sourceRoot, string targetRoot)
        {
            string? sourceProfile = ResolveSourceBundledPvpProfile(sourceRoot);
            if (sourceProfile == null)
            {
                return;
            }

            string destProfiles = Path.Combine(targetRoot, "RazorEnhanced", "Profiles", BundledPvpProfileName);
            string destBackup = Path.Combine(targetRoot, "RazorEnhanced", "Backup", BundledPvpProfileName);

            // Remove any alternate casing (e.g. "default pvp") before writing canonical name.
            RemoveAlternateProfileFolders(
                Path.Combine(targetRoot, "RazorEnhanced", "Profiles"),
                BundledPvpProfileName,
                destProfiles);
            RemoveAlternateProfileFolders(
                Path.Combine(targetRoot, "RazorEnhanced", "Backup"),
                BundledPvpProfileName,
                destBackup);

            ReplaceDirectory(sourceProfile, destProfiles);
            ReplaceDirectory(sourceProfile, destBackup);
        }

        private static void SyncBundledPvpStock(string sourceRoot, string targetRoot)
        {
            string? sourceProfile = ResolveSourceBundledPvpProfile(sourceRoot);
            if (sourceProfile == null)
            {
                return;
            }

            string stockDest = Path.Combine(targetRoot, "RazorEnhanced", BundledDefaultProfileFolder);
            ReplaceDirectory(sourceProfile, stockDest);

            // Drop legacy stock folder from older releases.
            string legacyStock = Path.Combine(targetRoot, "RazorEnhanced", "_bundled_default");
            if (Directory.Exists(legacyStock))
            {
                try { Directory.Delete(legacyStock, recursive: true); } catch { /* best-effort */ }
            }
        }

        private static string? ResolveSourceBundledPvpProfile(string sourceRoot)
        {
            string bundled = Path.Combine(sourceRoot, "RazorEnhanced", BundledDefaultProfileFolder);
            if (IsValidRazorGeneralSettingsFile(Path.Combine(bundled, "RazorEnhanced.settings.GENERAL")))
            {
                return bundled;
            }

            string profilesRoot = Path.Combine(sourceRoot, "RazorEnhanced", "Profiles");
            string? profiles = ResolveProfileDirectory(profilesRoot, BundledPvpProfileName);
            if (profiles != null &&
                IsValidRazorGeneralSettingsFile(Path.Combine(profiles, "RazorEnhanced.settings.GENERAL")))
            {
                return profiles;
            }

            return null;
        }

        private static string? ResolveProfileDirectory(string parentDir, string profileName)
        {
            if (!Directory.Exists(parentDir))
            {
                return null;
            }

            string exact = Path.Combine(parentDir, profileName);
            if (Directory.Exists(exact))
            {
                return exact;
            }

            foreach (string dir in Directory.EnumerateDirectories(parentDir))
            {
                if (Path.GetFileName(dir).Equals(profileName, StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
            }

            return null;
        }

        private static void RemoveAlternateProfileFolders(string parentDir, string profileName, string keepPath)
        {
            if (!Directory.Exists(parentDir))
            {
                return;
            }

            foreach (string dir in Directory.EnumerateDirectories(parentDir))
            {
                if (!Path.GetFileName(dir).Equals(profileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(dir, keepPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
            }
        }

        private static void EnsureBackupPvpFromProfile(string razorRoot, string profilesPvp)
        {
            string backupDir = Path.Combine(razorRoot, "Backup", BundledPvpProfileName);
            string backupGeneral = Path.Combine(backupDir, "RazorEnhanced.settings.GENERAL");
            if (IsValidRazorGeneralSettingsFile(backupGeneral))
            {
                return;
            }

            ReplaceDirectory(profilesPvp, backupDir);
        }

        private static void ReplaceDirectory(string source, string destination)
        {
            if (!Directory.Exists(source))
            {
                return;
            }

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, recursive: true);
            }

            CopyDirectory(source, destination, overwrite: true);
        }

        private static void MergeBundledRazorScripts(string sourceRoot, string targetRoot)
        {
            string sourceScripts = Path.Combine(sourceRoot, "RazorEnhanced", "Scripts");
            string destinationScripts = Path.Combine(targetRoot, "RazorEnhanced", "Scripts");
            if (!Directory.Exists(sourceScripts))
            {
                return;
            }

            CopyDirectory(sourceScripts, destinationScripts, overwrite: true);
        }

        private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(sourceDir, file);
                string target = Path.Combine(destDir, relative);
                string? parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, target, overwrite);
            }
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
                return CompareVersionSegments(left, right);
            }
        }

        private static int CompareVersionSegments(string left, string right)
        {
            string[] leftParts = left.Split('.', StringSplitOptions.RemoveEmptyEntries);
            string[] rightParts = right.Split('.', StringSplitOptions.RemoveEmptyEntries);
            int count = Math.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < count; i++)
            {
                int leftValue = i < leftParts.Length && int.TryParse(leftParts[i], out int parsedLeft)
                    ? parsedLeft
                    : 0;
                int rightValue = i < rightParts.Length && int.TryParse(rightParts[i], out int parsedRight)
                    ? parsedRight
                    : 0;

                if (leftValue != rightValue)
                {
                    return leftValue.CompareTo(rightValue);
                }
            }

            return 0;
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

        internal static string NormalizeVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return "";
            }

            string text = version.Trim().TrimStart('v', 'V');
            while (text.EndsWith('.'))
            {
                text = text[..^1];
            }

            var parts = new List<string>();
            foreach (string segment in text.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment.Length == 0 || !segment.All(char.IsDigit))
                {
                    break;
                }

                parts.Add(segment);
            }

            if (parts.Count == 0)
            {
                return "";
            }

            if (parts.Count > 3)
            {
                parts.RemoveRange(3, parts.Count - 3);
            }

            return string.Join('.', parts);
        }
    }
}
