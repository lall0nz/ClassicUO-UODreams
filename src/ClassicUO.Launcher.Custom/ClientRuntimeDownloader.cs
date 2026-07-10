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
        public static string ClientDir =>
            Path.Combine(AppContext.BaseDirectory, "Client");

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

        public static bool IsInstalled()
        {
            if (HasUnifiedNativeClient())
            {
                return true;
            }

            if (!LauncherManifest.IsPvpEdition)
            {
                return false;
            }

            string modded = Path.Combine(ClientDir, "cuo-modded.exe");
            string bootstrap = Path.Combine(BootstrapDir, "ClassicUO.exe");
            string nativeCuo = Path.Combine(BootstrapDir, "cuo.dll");

            return File.Exists(modded) && File.Exists(bootstrap) && File.Exists(nativeCuo);
        }

        public static async Task DownloadAndInstallAsync(
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default,
            string? packageUrl = null,
            string? packageFileName = null)
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
            string pluginsDir = PluginsDir;

            if (!Directory.Exists(pluginsDir))
            {
                return null;
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
            foreach (string root in CandidatePaths(installRoot, @"c:\Program Files (x86)\UOS"))
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
            List<(string Target, string Backup)> preserved = BackupUserData(installRoot);

            try
            {
                using ZipArchive archive = ZipFile.OpenRead(archivePath);

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
                foreach (string folder in RazorUserDataFolders)
                {
                    BackupDirectory(
                        Path.Combine(razorRoot, folder),
                        tempRoot,
                        ref backupIndex,
                        preserved
                    );
                }
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
