using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    internal static class EnhancedMapDownloader
    {
        public const string DownloadUrl =
            "https://github.com/andreakarasho/EnhancedMap/releases/download/1.0.0.0/EnhancedMap-release.zip";

        public const string ArchiveFileName = "EnhancedMap-release.zip";
        public const string ExeFileName = "EnhancedMap.exe";

        public static string ProcessName =>
            Path.GetFileNameWithoutExtension(ExeFileName);

        public static string DefaultInstallDirectory =>
            Path.Combine(AppContext.BaseDirectory, "EnhancedMap");

        public static bool IsValidAtPath(string? path) => ResolveExePath(path) != null;

        public static string? ResolveExePath(string? path)
        {
            string trimmed = (path ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(trimmed))
            {
                return FindExe(DefaultInstallDirectory);
            }

            if (File.Exists(trimmed) &&
                Path.GetFileName(trimmed).Equals(ExeFileName, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (Directory.Exists(trimmed))
            {
                return FindExe(trimmed);
            }

            string? parent = Path.GetDirectoryName(trimmed);
            if (!string.IsNullOrEmpty(parent))
            {
                return FindExe(parent);
            }

            return null;
        }

        /// <summary>
        /// Starts Enhanced Map or brings an existing instance to the foreground.
        /// Returns true when a new process was started.
        /// </summary>
        public static bool LaunchOrFocus(string exePath)
        {
            string normalizedExe = Path.GetFullPath(exePath);

            if (TryFindRunningProcess(normalizedExe, out Process? existing) && existing != null)
            {
                TryBringToForeground(existing);
                existing.Dispose();
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = normalizedExe,
                WorkingDirectory = Path.GetDirectoryName(normalizedExe)!,
                UseShellExecute = true
            });

            return true;
        }

        public static bool TryFindRunningProcess(string exePath, out Process? process)
        {
            process = null;
            string normalizedExe = Path.GetFullPath(exePath);
            Process[] candidates = Process.GetProcessesByName(ProcessName);
            Process? nameOnlyMatch = null;

            try
            {
                foreach (Process candidate in candidates)
                {
                    try
                    {
                        string? runningExe = candidate.MainModule?.FileName;
                        if (runningExe != null &&
                            string.Equals(Path.GetFullPath(runningExe), normalizedExe, StringComparison.OrdinalIgnoreCase))
                        {
                            process = candidate;
                            return true;
                        }
                    }
                    catch
                    {
                        nameOnlyMatch ??= candidate;
                    }
                }

                if (nameOnlyMatch != null)
                {
                    process = nameOnlyMatch;
                    return true;
                }

                return false;
            }
            finally
            {
                foreach (Process candidate in candidates)
                {
                    if (!ReferenceEquals(candidate, process))
                    {
                        candidate.Dispose();
                    }
                }
            }
        }

        private static void TryBringToForeground(Process process)
        {
            try
            {
                process.Refresh();
                IntPtr handle = process.MainWindowHandle;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);
            }
            catch
            {
                // foreground activation is best-effort
            }
        }

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static async Task<string> DownloadAndInstallAsync(
            string installDirectory,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(installDirectory);

            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, ArchiveFileName);

            try
            {
                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Connessione a Enhanced Map…", "Connecting to Enhanced Map…")
                });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    DownloadUrl,
                    archivePath,
                    progress,
                    cancellationToken
                ).ConfigureAwait(false);

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Estrazione in corso…", "Extracting…"),
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                ExtractZip(archivePath, installDirectory);

                string? exePath = FindExe(installDirectory);
                if (exePath == null)
                {
                    throw new InvalidDataException(
                        Loc.S(
                            "Installazione completata ma EnhancedMap.exe non è stato trovato.",
                            "Installation completed but EnhancedMap.exe was not found.")
                    );
                }

                progress?.Report(new DownloadProgressReport
                {
                    Status = Loc.S("Installazione completata.", "Installation completed."),
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                return exePath;
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

        private static string? FindExe(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return null;
            }

            string direct = Path.Combine(root, ExeFileName);
            if (File.Exists(direct))
            {
                return direct;
            }

            foreach (string file in Directory.EnumerateFiles(root, ExeFileName, SearchOption.AllDirectories))
            {
                return file;
            }

            return null;
        }

        private static void ExtractZip(string archivePath, string installDirectory)
        {
            if (!IsZipFile(archivePath))
            {
                throw new InvalidDataException(
                    Loc.S("Il file scaricato non è un archivio ZIP valido.", "The downloaded file is not a valid ZIP archive.")
                );
            }

            string stagingDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingDir);

            try
            {
                ZipFile.ExtractToDirectory(archivePath, stagingDir, overwriteFiles: true);

                string sourceDir = stagingDir;
                if (Directory.GetFiles(stagingDir).Length == 0 && Directory.GetDirectories(stagingDir).Length == 1)
                {
                    sourceDir = Directory.GetDirectories(stagingDir)[0];
                }

                CopyDirectory(sourceDir, installDirectory);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(stagingDir))
                    {
                        Directory.Delete(stagingDir, recursive: true);
                    }
                }
                catch
                {
                    // staging cleanup is best-effort
                }
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(sourceDir, file);
                string target = Path.Combine(destinationDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
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
