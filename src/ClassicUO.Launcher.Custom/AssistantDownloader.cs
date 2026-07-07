using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    internal static class AssistantDownloader
    {
        private sealed class AssistantSource
        {
            public required string DisplayName { get; init; }
            public required string DownloadUrl { get; init; }
            public required string ArchiveFileName { get; init; }
            public required bool IsInstaller { get; init; }
            public required string[] PluginCandidates { get; init; }
        }

        private static readonly AssistantSource ClassicAssist = new()
        {
            DisplayName = "ClassicAssist",
            DownloadUrl = "https://github.com/Reetus/ClassicAssist/releases/download/5.0.1/ClassicAssist.zip",
            ArchiveFileName = "ClassicAssist.zip",
            IsInstaller = false,
            PluginCandidates = new[] { "ClassicAssist.dll" }
        };

        private static readonly AssistantSource RazorEnhanced = new()
        {
            DisplayName = "Razor Enhanced",
            DownloadUrl = "https://github.com/UltimaTools/RazorEnhanced/releases/download/v1.0.0.13/RazorEnhanced-1.0.0.13.zip",
            ArchiveFileName = "RazorEnhanced.zip",
            IsInstaller = false,
            PluginCandidates = new[] { "RazorEnhanced.exe", "RazorEnhanced.dll" }
        };

        private static readonly AssistantSource Orion = new()
        {
            DisplayName = "Orion",
            DownloadUrl = "http://orionuo.online/Updates5152/OrionLauncher/x64/Orion%20Launcher64_2.0.0.0.exe",
            ArchiveFileName = "OrionLauncher64.exe",
            IsInstaller = true,
            PluginCandidates = new[] { "OA\\OrionAssistant64.dll", "OrionAssistant64.dll" }
        };

        private static readonly AssistantSource UOSteam = new()
        {
            DisplayName = "UOSteam",
            DownloadUrl = "https://razorenhanced.net/download/UOS_Latest.exe",
            ArchiveFileName = "UOS_Latest.exe",
            IsInstaller = true,
            PluginCandidates = new[] { "UOS.dll" }
        };

        public static bool SupportsDownload(string assistant) => TryGetSource(assistant, out _);

        public static string GetDefaultInstallDirectory(string assistant)
        {
            if (assistant == "Razor Enhanced")
            {
                string pluginsDir = Path.Combine(ClientRuntimeDownloader.BootstrapDir, "Data", "Plugins");
                return Path.Combine(pluginsDir, "RazorEnhanced");
            }

            return Path.Combine(AppContext.BaseDirectory, "Assistants", SanitizeFolderName(assistant));
        }

        public static bool IsInstalled(string assistant, string? path = null)
        {
            return ResolvePluginPath(assistant, path) != null;
        }

        public static string? ResolvePluginPath(string assistant, string? path = null)
        {
            if (!TryGetSource(assistant, out AssistantSource? source) || source is null)
            {
                return null;
            }

            string trimmed = (path ?? "").Trim().Trim('"');
            if (!string.IsNullOrEmpty(trimmed))
            {
                string? resolved = ResolveInPath(trimmed, source.PluginCandidates);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            string? fromDefault = FindPlugin(GetDefaultInstallDirectory(assistant), source.PluginCandidates);
            if (fromDefault != null)
            {
                return fromDefault;
            }

            return assistant switch
            {
                "Razor Enhanced" => FindPlugin(ClientRuntimeDownloader.DetectRazorEnhancedPath() ?? "", source.PluginCandidates),
                "Orion" => ClientRuntimeDownloader.DetectOrionAssistantDll(),
                "UOSteam" => ClientRuntimeDownloader.DetectUOSteamDll(),
                _ => null
            };
        }

        public static async Task<string> DownloadAndInstallAsync(
            string assistant,
            string installDirectory,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetSource(assistant, out AssistantSource? source) || source is null)
            {
                throw new InvalidOperationException($"Download non supportato per {assistant}.");
            }

            Directory.CreateDirectory(installDirectory);

            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, source.ArchiveFileName);

            try
            {
                progress?.Report(new DownloadProgressReport
                {
                    Status = $"Connessione a {source.DisplayName}…"
                });

                await UoClientDownloader.DownloadFileFromUrlAsync(
                    source.DownloadUrl,
                    archivePath,
                    progress,
                    cancellationToken
                ).ConfigureAwait(false);

                if (source.IsInstaller)
                {
                    progress?.Report(new DownloadProgressReport
                    {
                        Status = "Installazione in corso…",
                        BytesReceived = 1,
                        TotalBytes = 1
                    });

                    await RunSilentInstallerAsync(archivePath, installDirectory, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    progress?.Report(new DownloadProgressReport
                    {
                        Status = "Estrazione in corso…",
                        BytesReceived = 1,
                        TotalBytes = 1
                    });

                    ExtractZip(archivePath, installDirectory);
                }

                string? pluginPath = FindPlugin(installDirectory, source.PluginCandidates);
                if (pluginPath == null)
                {
                    throw new InvalidDataException(
                        $"Installazione completata ma il plugin di {source.DisplayName} non è stato trovato in {installDirectory}."
                    );
                }

                progress?.Report(new DownloadProgressReport
                {
                    Status = "Installazione completata.",
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                return installDirectory;
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

        private static bool TryGetSource(string assistant, out AssistantSource? source)
        {
            source = assistant switch
            {
                "ClassicAssist" => ClassicAssist,
                "Razor Enhanced" => RazorEnhanced,
                "Orion" => Orion,
                "UOSteam" => UOSteam,
                _ => null
            };

            return source != null;
        }

        private static string? ResolveInPath(string path, string[] candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }

            if (Directory.Exists(path))
            {
                return FindPlugin(path, candidates);
            }

            return null;
        }

        private static string? FindPlugin(string root, string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return null;
            }

            foreach (string name in candidates)
            {
                string candidate = Path.Combine(root, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                foreach (string name in candidates)
                {
                    if (fileName.Equals(Path.GetFileName(name), StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }
            }

            return null;
        }

        private static void ExtractZip(string archivePath, string installDirectory)
        {
            if (!IsZipFile(archivePath))
            {
                throw new InvalidDataException("Il file scaricato non è un archivio ZIP valido.");
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

        private static async Task RunSilentInstallerAsync(
            string installerPath,
            string installDirectory,
            CancellationToken cancellationToken)
        {
            string arguments =
                $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{installDirectory.TrimEnd('\\')}\"";

            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Impossibile avviare l'installer.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"L'installer è terminato con codice {process.ExitCode}. Prova l'installazione manuale."
                );
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

        private static string SanitizeFolderName(string name) =>
            name.Replace(" ", "", StringComparison.Ordinal);
    }
}
