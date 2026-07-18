using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

            string effectiveClientVersion = ResolveEffectiveClientVersion(localClientVersion);

            bool needsClient = NeedsComponentUpdate(bestVersion, effectiveClientVersion, clientUrl != null);
            bool needsLauncher = NeedsComponentUpdate(bestVersion, LauncherManifest.RuntimeLauncherVersion, launcherUrl != null);

            if (!needsClient &&
                clientUrl != null &&
                CompareVersions(LauncherManifest.RuntimeLauncherVersion, effectiveClientVersion) > 0 &&
                CompareVersions(bestVersion, effectiveClientVersion) >= 0)
            {
                // Launcher was updated in-place but client files/settings lag behind the same release.
                needsClient = true;
            }

            if (!needsClient && needsLauncher && clientUrl != null)
            {
                // Always refresh client binaries when the launcher updates to a new release.
                needsClient = true;
            }

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
                    UpdateAssistantFolder(sourceAssistant, Path.Combine(installRoot, "Assistant"));
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
