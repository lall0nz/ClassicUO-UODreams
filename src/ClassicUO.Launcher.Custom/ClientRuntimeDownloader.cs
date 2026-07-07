using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    internal static class ClientRuntimeDownloader
    {
        public static string ClientDir =>
            Path.Combine(AppContext.BaseDirectory, "Client");

        public static string BootstrapDir =>
            Path.Combine(ClientDir, "Bootstrap");

        public static bool IsInstalled()
        {
            string modded = Path.Combine(ClientDir, "cuo-modded.exe");
            string bootstrap = Path.Combine(BootstrapDir, "ClassicUO.exe");
            string nativeCuo = Path.Combine(BootstrapDir, "cuo.dll");

            return File.Exists(modded) && File.Exists(bootstrap) && File.Exists(nativeCuo);
        }

        public static async Task DownloadAndInstallAsync(
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            string installRoot = AppContext.BaseDirectory;
            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, LauncherManifest.ClientPackageFileName);

            try
            {
                progress?.Report(new DownloadProgressReport
                {
                    Status = "Download componenti ClassicUO UODreams…"
                });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    LauncherManifest.ClientPackageUrl,
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

                ZipFile.ExtractToDirectory(archivePath, installRoot, overwriteFiles: true);

                if (!IsInstalled())
                {
                    throw new InvalidDataException(
                        "Installazione incompleta: cuo-modded.exe o Bootstrap non trovati nel pacchetto."
                    );
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
            string pluginsDir = Path.Combine(BootstrapDir, "Data", "Plugins");

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
    }
}
