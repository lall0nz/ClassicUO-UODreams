using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    internal static class ClientRuntimeDownloader
    {
        public const string ClientVersionMarkerFileName = "uodreams-client.version";

        private static readonly string[] RazorUserDataFolders = { "Profiles", "Scripts", "Backup" };

        /// <summary>
        /// Relative to install root (zip root). Must match Clear-UserClientData in package-release.ps1.
        /// </summary>
        private static readonly string[] PreservedClientDirectoryPaths =
        {
            "Client/Data/Profiles",
            "Client/Data/Client/JournalLogs",
            "Client/Logs",
            "Client/Bootstrap/Data/Profiles",
            "Client/Bootstrap/Data/Client/JournalLogs",
            "Client/Bootstrap/Logs",
        };

        private static readonly string[] PreservedClientFilePaths =
        {
            "Client/settings.json",
            "Client/Bootstrap/settings.json",
        };

        private static readonly string[] PreservedClientUserMarkerPrefixes =
        {
            "Client/Data/Client/",
            "Client/Bootstrap/Data/Client/",
        };

        private static readonly string[] PreservedAssistantDirectoryPaths =
        {
            "Assistant/RazorEnhanced/Profiles",
            "Assistant/RazorEnhanced/Scripts",
            "Assistant/RazorEnhanced/Backup",
        };

        public static string ClientDir =>
            Path.Combine(AppContext.BaseDirectory, "Client");

        public static string? TryReadClientVersionMarker(string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string path = Path.Combine(installRoot, "Client", ClientVersionMarkerFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return LauncherUpdater.NormalizeVersion(File.ReadAllText(path).Trim());
            }
            catch
            {
                return null;
            }
        }

        public static void WriteClientVersionMarker(string version, string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string clientDir = Path.Combine(installRoot, "Client");
            Directory.CreateDirectory(clientDir);
            File.WriteAllText(
                Path.Combine(clientDir, ClientVersionMarkerFileName),
                LauncherUpdater.NormalizeVersion(version)
            );
        }

        public static string BootstrapDir =>
            Path.Combine(ClientDir, "Bootstrap");

        /// <summary>
        /// Dust765-style layout: ClassicUO.exe + native modded cuo.dll in Client root.
        /// Supports mods and Razor Enhanced in the same session.
        /// </summary>
        public static bool HasUnifiedNativeClient()
        {
            string exe = Path.Combine(ClientDir, "ClassicUO.exe");
            string nativeCuo = Path.Combine(ClientDir, "cuo.dll");
            return File.Exists(exe) && IsNativeCuoDll(nativeCuo);
        }

        public static string? TryGetUnifiedNativeClientExe()
        {
            if (!HasUnifiedNativeClient())
            {
                return null;
            }

            return Path.Combine(ClientDir, "ClassicUO.exe");
        }

        public static string? TryGetLegacyBootstrapClientExe()
        {
            string exe = Path.Combine(BootstrapDir, "ClassicUO.exe");
            string nativeCuo = Path.Combine(BootstrapDir, "cuo.dll");
            if (File.Exists(exe) && IsNativeCuoDll(nativeCuo))
            {
                return exe;
            }

            return null;
        }

        public static string PluginsDir
        {
            get
            {
                string unified = Path.Combine(ClientDir, "Data", "Plugins");
                if (Directory.Exists(unified))
                {
                    return unified;
                }

                return Path.Combine(BootstrapDir, "Data", "Plugins");
            }
        }

        public static bool IsNativeCuoDll(string cuoDllPath)
        {
            if (!File.Exists(cuoDllPath))
            {
                return false;
            }

            try
            {
                AssemblyName.GetAssemblyName(cuoDllPath);
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsInstalled(string? installRoot = null)
        {
            installRoot ??= AppContext.BaseDirectory;
            string clientDir = Path.Combine(installRoot, "Client");
            string bootstrapDir = Path.Combine(clientDir, "Bootstrap");

            string unifiedExe = Path.Combine(clientDir, "ClassicUO.exe");
            string unifiedCuo = Path.Combine(clientDir, "cuo.dll");
            if (File.Exists(unifiedExe) && IsNativeCuoDll(unifiedCuo))
            {
                return true;
            }

            if (!LauncherManifest.IsPvpEdition)
            {
                string cuoExe = Path.Combine(clientDir, "cuo.exe");
                string classicExe = Path.Combine(clientDir, "ClassicUO.exe");
                return File.Exists(cuoExe) || File.Exists(classicExe);
            }

            string modded = Path.Combine(clientDir, "cuo-modded.exe");
            string bootstrap = Path.Combine(bootstrapDir, "ClassicUO.exe");
            string nativeCuo = Path.Combine(bootstrapDir, "cuo.dll");

            return File.Exists(modded) && File.Exists(bootstrap) && File.Exists(nativeCuo);
        }

        public static async Task DownloadAndInstallAsync(
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default,
            string? packageUrl = null,
            string? packageFileName = null,
            string? expectedSha256 = null)
        {
            string installRoot = AppContext.BaseDirectory;
            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archiveName = packageFileName ?? LauncherManifest.ClientPackageFileName;
            string archivePath = Path.Combine(tempDir, archiveName);
            string downloadUrl = packageUrl ?? LauncherManifest.ClientPackageUrl;

            try
            {
                progress?.Report(new DownloadProgressReport
                {
                    Status = "Download componenti ClassicUO UODreams…"
                });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    downloadUrl,
                    archivePath,
                    progress,
                    cancellationToken
                ).ConfigureAwait(false);

                LauncherUpdater.VerifyDownloadedSha256(archivePath, expectedSha256);

                progress?.Report(new DownloadProgressReport
                {
                    Status = "Estrazione in corso…",
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                if (!IsZipFile(archivePath))
                {
                    throw new InvalidDataException(
                        "Il pacchetto scaricato non è valido. Riprova più tardi o contatta il supporto."
                    );
                }

                ExtractClientPackagePreservingUserData(archivePath, installRoot);

                if (!IsInstalled())
                {
                    string message = LauncherManifest.IsPvpEdition
                        ? "Installazione incompleta: ClassicUO.exe + cuo.dll nativo o cuo-modded.exe + Bootstrap non trovati nel pacchetto."
                        : "Installazione incompleta: ClassicUO.exe + cuo.dll ufficiale non trovati nel pacchetto.";
                    throw new InvalidDataException(message);
                }

                string installedVersion = LauncherUpdater.ParseVersionFromPackageName(archiveName)
                    ?? LauncherUpdater.NormalizeVersion(LauncherManifest.ClientRuntimeVersion);
                WriteClientVersionMarker(installedVersion, installRoot);

                progress?.Report(new DownloadProgressReport
                {
                    Status = "Installazione completata.",
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

        public static string? DetectRazorEnhancedPath()
        {
            string? fromAssistant = AssistantPaths.DetectRazorInstallDirectory();
            if (fromAssistant != null)
            {
                return fromAssistant;
            }

            return DetectRazorEnhancedPathInPlugins();
        }

        internal static string? DetectRazorEnhancedPathInPlugins()
        {
            string pluginsDir = PluginsDir;

            if (!Directory.Exists(pluginsDir))
            {
                return null;
            }

            string flatExe = Path.Combine(pluginsDir, "RazorEnhanced.exe");
            if (File.Exists(flatExe))
            {
                return pluginsDir;
            }

            foreach (string dir in Directory.EnumerateDirectories(pluginsDir))
            {
                if (dir.Contains("RazorEnhanced", StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
            }

            return null;
        }

        public static string? DetectOrionLauncherExe(string? installRoot = null)
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            foreach (string root in CandidatePaths(
                installRoot,
                Path.Combine(AssistantPaths.LauncherAssistantRoot, "Orion"),
                AssistantPaths.LauncherAssistantRoot,
                @"c:\Orion Launcher",
                Path.Combine(programFiles, "Orion Launcher"),
                Path.Combine(programFilesX86, "Orion Launcher")))
            {
                foreach (string name in new[] { "OrionLauncher64.exe", "Orion Launcher64.exe" })
                {
                    string exe = Path.Combine(root, name);
                    if (File.Exists(exe))
                    {
                        return exe;
                    }
                }
            }

            return null;
        }

        public static string? DetectOrionInstallRoot(string? installRoot = null)
        {
            string? exe = DetectOrionLauncherExe(installRoot);
            return exe == null ? null : Path.GetDirectoryName(exe);
        }

        public static string? DetectOrionExe(string? installRoot = null) => DetectOrionLauncherExe(installRoot);

        public static string? DetectOrionAssistantDll(string? installRoot = null) => DetectOrionLauncherExe(installRoot);

        public static string? DetectUOSteamExe(string? installRoot = null)
        {
            foreach (string root in CandidatePaths(
                installRoot,
                Path.Combine(AssistantPaths.LauncherAssistantRoot, "UOSteam"),
                AssistantPaths.LauncherAssistantRoot,
                @"c:\Program Files (x86)\UOS"))
            {
                string exe = Path.Combine(root, "UOS.exe");
                if (File.Exists(exe))
                {
                    return exe;
                }
            }

            return null;
        }

        public static string? DetectUOSteamDll(string? installRoot = null) => DetectUOSteamExe(installRoot);

        private static IEnumerable<string> CandidatePaths(string? preferred, params string[] defaults)
        {
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                yield return preferred.Trim().Trim('"');
            }

            foreach (string path in defaults)
            {
                yield return path;
            }
        }

        private static bool IsZipFile(string path)
        {
            using var fs = File.OpenRead(path);

            if (fs.Length < 4)
            {
                return false;
            }

            Span<byte> header = stackalloc byte[4];
            fs.Read(header);
            return header[0] == 0x50 && header[1] == 0x4B;
        }

        private static void ExtractClientPackagePreservingUserData(string archivePath, string installRoot)
        {
            using ZipArchive archive = ZipFile.OpenRead(archivePath);

            // Never wipe existing Default PVP — preserve like any other profile.
            // Package only installs it when missing (launcher UpdateAssistantFolder).

            List<(string Target, string Backup)> preserved = BackupUserData(installRoot);

            try
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (ShouldSkipZipEntry(entry.FullName))
                    {
                        continue;
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(installRoot, entry.FullName));

                    if (!destinationPath.StartsWith(Path.GetFullPath(installRoot), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    string? parent = Path.GetDirectoryName(destinationPath);

                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            finally
            {
                RestoreUserData(preserved);
            }
        }

        private static bool ArchiveContainsBundledPvpRazor(ZipArchive archive)
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (IsBundledPvpRazorZipEntry(NormalizeZipEntryPath(entry.FullName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DeleteBundledPvpRazorProfiles(string installRoot)
        {
            foreach (string razorRoot in FindRazorRoots(installRoot))
            {
                foreach (string folder in new[] { "Profiles", "Backup" })
                {
                    string parent = Path.Combine(razorRoot, folder);
                    if (!Directory.Exists(parent))
                    {
                        continue;
                    }

                    foreach (string dir in Directory.EnumerateDirectories(parent))
                    {
                        if (!LauncherUpdater.IsBundledPvpProfileName(Path.GetFileName(dir)))
                        {
                            continue;
                        }

                        try
                        {
                            Directory.Delete(dir, recursive: true);
                        }
                        catch
                        {
                            // Best-effort; launcher OTA / startup repair will reapply Default PVP.
                        }
                    }
                }
            }
        }

        private static bool ShouldSkipZipEntry(string entryPath)
        {
            return IsRazorUserDataZipEntry(entryPath) || IsClientUserDataZipEntry(entryPath);
        }

        private static bool IsClientUserDataZipEntry(string entryPath)
        {
            string normalized = NormalizeZipEntryPath(entryPath);

            foreach (string preservedDir in PreservedClientDirectoryPaths)
            {
                if (IsZipEntryUnderPath(normalized, preservedDir))
                {
                    return true;
                }
            }

            foreach (string preservedFile in PreservedClientFilePaths)
            {
                if (normalized.Equals(preservedFile, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (normalized.EndsWith(".usr", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string prefix in PreservedClientUserMarkerPrefixes)
                {
                    if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string NormalizeZipEntryPath(string entryPath)
        {
            return entryPath.Replace('\\', '/').TrimStart('/');
        }

        private static bool IsZipEntryUnderPath(string normalizedEntryPath, string normalizedPrefix)
        {
            if (normalizedEntryPath.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedEntryPath.StartsWith(
                normalizedPrefix + "/",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static bool IsRazorUserDataZipEntry(string entryPath)
        {
            string normalized = NormalizeZipEntryPath(entryPath);

            // Preserve all Razor profiles including Default PVP. Stock _bundled_default_pvp
            // is still allowed through so repair source stays current.
            if (IsBundledPvpRazorZipEntry(normalized))
            {
                // Only the pristine stock folder comes from the package; live Profiles/Backup stay preserved.
                string stock = LauncherUpdater.BundledDefaultProfileFolder;
                if (IsZipEntryUnderPath(normalized, $"Assistant/RazorEnhanced/{stock}") ||
                    normalized.Equals($"Assistant/RazorEnhanced/{stock}", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }

            foreach (string preservedDir in PreservedAssistantDirectoryPaths)
            {
                if (IsZipEntryUnderPath(normalized, preservedDir))
                {
                    return true;
                }
            }

            const string marker = "/Data/Plugins/";
            int pluginsIdx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (pluginsIdx < 0)
            {
                return false;
            }

            string afterPlugins = normalized[(pluginsIdx + marker.Length)..];
            string[] parts = afterPlugins.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return false;
            }

            if (IsRazorUserDataFolder(parts[0]))
            {
                return true;
            }

            if (parts.Length >= 2 &&
                parts[0].Contains("RazorEnhanced", StringComparison.OrdinalIgnoreCase) &&
                IsRazorUserDataFolder(parts[1]))
            {
                return true;
            }

            return false;
        }

        private static bool IsBundledPvpRazorZipEntry(string normalizedEntryPath)
        {
            string pvpName = LauncherUpdater.BundledPvpProfileName;
            string stock = LauncherUpdater.BundledDefaultProfileFolder;

            string[] prefixes =
            {
                $"Assistant/RazorEnhanced/Profiles/{pvpName}",
                $"Assistant/RazorEnhanced/Backup/{pvpName}",
                $"Assistant/RazorEnhanced/{stock}"
            };

            foreach (string prefix in prefixes)
            {
                if (IsZipEntryUnderPath(normalizedEntryPath, prefix) ||
                    normalizedEntryPath.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRazorUserDataFolder(string name)
        {
            foreach (string folder in RazorUserDataFolders)
            {
                if (name.Equals(folder, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<(string Target, string Backup)> BackupUserData(string installRoot)
        {
            var preserved = new List<(string Target, string Backup)>();
            string tempRoot = Path.Combine(
                Path.GetTempPath(),
                "UODreamsLauncher",
                "client-preserve",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(tempRoot);

            int backupIndex = 0;

            foreach (string razorRoot in FindRazorRoots(installRoot))
            {
                BackupRazorUserDataFolder(razorRoot, "Profiles", tempRoot, ref backupIndex, preserved);
                BackupRazorUserDataFolder(razorRoot, "Backup", tempRoot, ref backupIndex, preserved);
                BackupDirectory(
                    Path.Combine(razorRoot, "Scripts"),
                    tempRoot,
                    ref backupIndex,
                    preserved
                );
            }

            foreach (string relativeDir in PreservedAssistantDirectoryPaths)
            {
                string absoluteDir = Path.Combine(
                    installRoot,
                    relativeDir.Replace('/', Path.DirectorySeparatorChar)
                );
                if (relativeDir.EndsWith("/Profiles", StringComparison.OrdinalIgnoreCase) ||
                    relativeDir.EndsWith("/Backup", StringComparison.OrdinalIgnoreCase))
                {
                    BackupRazorUserDataFolder(
                        Path.GetDirectoryName(absoluteDir) ?? absoluteDir,
                        Path.GetFileName(absoluteDir),
                        tempRoot,
                        ref backupIndex,
                        preserved
                    );
                    continue;
                }

                BackupDirectory(absoluteDir, tempRoot, ref backupIndex, preserved);
            }

            foreach (string relativeDir in PreservedClientDirectoryPaths)
            {
                BackupDirectory(
                    Path.Combine(installRoot, relativeDir.Replace('/', Path.DirectorySeparatorChar)),
                    tempRoot,
                    ref backupIndex,
                    preserved
                );
            }

            foreach (string relativeFile in PreservedClientFilePaths)
            {
                BackupFile(
                    Path.Combine(installRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar)),
                    tempRoot,
                    ref backupIndex,
                    preserved
                );
            }

            foreach (string markerPrefix in PreservedClientUserMarkerPrefixes)
            {
                string markerDir = Path.Combine(
                    installRoot,
                    markerPrefix.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar)
                );

                if (!Directory.Exists(markerDir))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(markerDir, "*.usr"))
                {
                    BackupFile(file, tempRoot, ref backupIndex, preserved);
                }
            }

            return preserved;
        }

        private static void BackupRazorUserDataFolder(
            string razorRoot,
            string folderName,
            string tempRoot,
            ref int backupIndex,
            List<(string Target, string Backup)> preserved)
        {
            string source = Path.Combine(razorRoot, folderName);
            if (!Directory.Exists(source))
            {
                return;
            }

            foreach (string entry in Directory.EnumerateFileSystemEntries(source))
            {
                if (Directory.Exists(entry))
                {
                    BackupDirectory(entry, tempRoot, ref backupIndex, preserved);
                    continue;
                }

                BackupFile(entry, tempRoot, ref backupIndex, preserved);
            }
        }

        private static void BackupDirectory(
            string source,
            string tempRoot,
            ref int backupIndex,
            List<(string Target, string Backup)> preserved)
        {
            if (!Directory.Exists(source))
            {
                return;
            }

            string backup = Path.Combine(tempRoot, $"{backupIndex:D3}_dir");
            CopyDirectory(source, backup);
            preserved.Add((source, backup));
            backupIndex++;
        }

        private static void BackupFile(
            string source,
            string tempRoot,
            ref int backupIndex,
            List<(string Target, string Backup)> preserved)
        {
            if (!File.Exists(source))
            {
                return;
            }

            string backup = Path.Combine(tempRoot, $"{backupIndex:D3}_file_{Path.GetFileName(source)}");
            string? parent = Path.GetDirectoryName(backup);

            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Copy(source, backup, overwrite: true);
            preserved.Add((source, backup));
            backupIndex++;
        }

        private static void RestoreUserData(List<(string Target, string Backup)> preserved)
        {
            foreach ((string target, string backup) in preserved)
            {
                if (Directory.Exists(backup))
                {
                    CopyDirectory(backup, target, overwrite: true);
                    continue;
                }

                if (!File.Exists(backup))
                {
                    continue;
                }

                string? parent = Path.GetDirectoryName(target);

                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(backup, target, overwrite: true);
            }
        }

        private static IEnumerable<string> FindRazorRoots(string installRoot)
        {
            string assistantRazor = Path.Combine(installRoot, "Assistant", "RazorEnhanced");
            if (Directory.Exists(assistantRazor) &&
                File.Exists(Path.Combine(assistantRazor, "RazorEnhanced.exe")))
            {
                yield return assistantRazor;
            }

            string clientDir = Path.Combine(installRoot, "Client");
            string[] pluginsDirs =
            {
                Path.Combine(clientDir, "Data", "Plugins"),
                Path.Combine(clientDir, "Bootstrap", "Data", "Plugins")
            };

            foreach (string pluginsDir in pluginsDirs)
            {
                if (!Directory.Exists(pluginsDir))
                {
                    continue;
                }

                if (File.Exists(Path.Combine(pluginsDir, "RazorEnhanced.exe")))
                {
                    yield return pluginsDir;
                }

                foreach (string dir in Directory.EnumerateDirectories(pluginsDir))
                {
                    if (dir.Contains("RazorEnhanced", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return dir;
                    }
                }
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
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
    }
}
