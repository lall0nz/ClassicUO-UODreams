using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Launcher.Custom
{
    public sealed class DownloadProgressReport
    {
        public long BytesReceived { get; init; }
        public long? TotalBytes { get; init; }
        public double BytesPerSecond { get; init; }
        public string Status { get; init; } = "";
    }

    public static class UoClientDownloader
    {
        public const string DefaultFileId = "1aqWCNRRz2QV5U3Sy47dO35VQ2GEjy3tF";

        public static string BuildDownloadUrl(string fileId = DefaultFileId) =>
            $"https://drive.usercontent.google.com/download?id={fileId}&export=download";

        public static async Task<string> DownloadAndExtractAsync(
            string extractDirectory,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UODreamsLauncher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string archivePath = Path.Combine(tempDir, "uodreams-client.zip");

            try
            {
                progress?.Report(new DownloadProgressReport { Status = "Connessione a Google Drive…" });

                await DownloadGoogleDriveFileAsync(
                    DefaultFileId,
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

                Directory.CreateDirectory(extractDirectory);

                if (IsZipFile(archivePath))
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);
                }
                else if (LooksLikeUltimaOnlineFolder(archivePath))
                {
                    // single-file fallback (should not happen)
                    throw new InvalidDataException("Il download non è un archivio ZIP valido.");
                }
                else
                {
                    throw new InvalidDataException("Formato file non riconosciuto. Atteso un archivio ZIP.");
                }

                string? uoRoot = FindUltimaOnlineRoot(extractDirectory);
                if (uoRoot == null)
                    throw new InvalidDataException("Estrazione completata ma tiledata.mul non trovato nell'archivio.");

                progress?.Report(new DownloadProgressReport
                {
                    Status = "Download completato.",
                    BytesReceived = 1,
                    TotalBytes = 1
                });

                return uoRoot;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // temp cleanup is best-effort
                }
            }
        }

        public static async Task DownloadFileFromUrlAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var client = CreateHttpClient(handler);

            using var response = await client
                .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Il server ha restituito una pagina web invece del file. Verifica l'URL di download."
                );
            }

            await SaveResponseToFileAsync(response, destinationPath, progress, cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task DownloadGoogleDriveFileAsync(
            string fileId,
            string destinationPath,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var client = CreateHttpClient(handler);

            IReadOnlyList<string> downloadUrls = new[]
            {
                BuildDownloadUrl(fileId),
                $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm=t",
                $"https://drive.google.com/uc?export=download&id={fileId}&confirm=t"
            };

            Exception? lastError = null;

            foreach (string initialUrl in downloadUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await TryDownloadFromUrlAsync(client, initialUrl, fileId, destinationPath, progress, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex;
                }
            }

            throw lastError ?? new InvalidDataException(
                "Impossibile scaricare il client da Google Drive. Prova a scaricarlo manualmente e seleziona la cartella."
            );
        }

        private static HttpClient CreateHttpClient(HttpClientHandler handler)
        {
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromHours(4) };
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
            );
            return client;
        }

        private static async Task<bool> TryDownloadFromUrlAsync(
            HttpClient client,
            string url,
            string fileId,
            string destinationPath,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken)
        {
            string downloadUrl = await ResolveDirectDownloadUrlAsync(client, url, fileId, cancellationToken)
                .ConfigureAwait(false);

            using var response = await client
                .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                string html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                string? bypassUrl = BuildBypassUrl(fileId, html);

                if (bypassUrl == null || string.Equals(bypassUrl, downloadUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                using var retry = await client
                    .GetAsync(bypassUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                retry.EnsureSuccessStatusCode();

                contentType = retry.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                await SaveResponseToFileAsync(retry, destinationPath, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SaveResponseToFileAsync(response, destinationPath, progress, cancellationToken).ConfigureAwait(false);
            }

            if (!IsZipFile(destinationPath))
            {
                throw new InvalidDataException(
                    "Il file scaricato non è un archivio ZIP valido. Google Drive potrebbe aver bloccato il download automatico."
                );
            }

            return true;
        }

        private static async Task SaveResponseToFileAsync(
            HttpResponseMessage response,
            string destinationPath,
            IProgress<DownloadProgressReport>? progress,
            CancellationToken cancellationToken)
        {
            long? totalBytes = response.Content.Headers.ContentLength;

            await using Stream input = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            await using FileStream output = new(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 128,
                useAsync: true
            );

            byte[] buffer = new byte[1024 * 128];
            long received = 0;
            var speedWatch = System.Diagnostics.Stopwatch.StartNew();
            long lastReportBytes = 0;
            var lastReportAt = speedWatch.Elapsed;

            while (true)
            {
                int read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                received += read;

                var now = speedWatch.Elapsed;
                if ((now - lastReportAt).TotalMilliseconds >= 200)
                {
                    double seconds = Math.Max((now - lastReportAt).TotalSeconds, 0.001);
                    double bytesPerSecond = (received - lastReportBytes) / seconds;
                    lastReportBytes = received;
                    lastReportAt = now;

                    progress?.Report(new DownloadProgressReport
                    {
                        BytesReceived = received,
                        TotalBytes = totalBytes,
                        BytesPerSecond = bytesPerSecond,
                        Status = "Download in corso…"
                    });
                }
            }

            progress?.Report(new DownloadProgressReport
            {
                BytesReceived = received,
                TotalBytes = totalBytes ?? received,
                BytesPerSecond = 0,
                Status = "Download completato."
            });
        }

        private static string? BuildBypassUrl(string fileId, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            string id = ExtractFormValue(html, "id") ?? fileId;
            string? confirm = ExtractFormValue(html, "confirm");
            string? uuid = ExtractFormValue(html, "uuid");

            if (!string.IsNullOrEmpty(confirm) && !string.IsNullOrEmpty(uuid))
            {
                return $"https://drive.usercontent.google.com/download?id={id}&export=download&confirm={confirm}&uuid={uuid}";
            }

            if (!string.IsNullOrEmpty(uuid))
            {
                return $"https://drive.usercontent.google.com/download?id={id}&export=download&confirm=t&uuid={uuid}";
            }

            var confirmMatch = Regex.Match(html, @"confirm=([0-9A-Za-z_\-]+)", RegexOptions.IgnoreCase);
            if (confirmMatch.Success)
            {
                return $"https://drive.usercontent.google.com/download?id={id}&export=download&confirm={confirmMatch.Groups[1].Value}";
            }

            if (html.Contains("Virus scan warning", StringComparison.OrdinalIgnoreCase)
                || html.Contains("Google Drive", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://drive.usercontent.google.com/download?id={id}&export=download&confirm=t";
            }

            return null;
        }

        private static string? ExtractFormValue(string html, string name)
        {
            var match = Regex.Match(
                html,
                $@"name=[""']{name}[""']\s+value=[""']([^""']+)[""']",
                RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = Regex.Match(
                html,
                $@"value=[""']([^""']+)[""']\s+name=[""']{name}[""']",
                RegexOptions.IgnoreCase
            );

            return match.Success ? match.Groups[1].Value : null;
        }

        private static async Task<string> ResolveDirectDownloadUrlAsync(
            HttpClient client,
            string url,
            string fileId,
            CancellationToken cancellationToken)
        {
            using var probe = await client
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            string? contentType = probe.Content.Headers.ContentType?.MediaType;

            if (contentType != null && !contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            string html = await probe.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string? bypassUrl = BuildBypassUrl(fileId, html);

            return bypassUrl ?? url;
        }

        private static bool IsZipFile(string path)
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 4)
                return false;

            Span<byte> header = stackalloc byte[4];
            fs.Read(header);
            return header[0] == 0x50 && header[1] == 0x4B;
        }

        private static bool LooksLikeUltimaOnlineFolder(string path) => false;

        public static string? FindUltimaOnlineRoot(string startDirectory)
        {
            if (File.Exists(Path.Combine(startDirectory, "tiledata.mul")))
                return startDirectory;

            foreach (string dir in Directory.EnumerateDirectories(startDirectory, "*", SearchOption.AllDirectories))
            {
                if (File.Exists(Path.Combine(dir, "tiledata.mul")))
                    return dir;
            }

            return null;
        }

        public static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return unit == 0
                ? $"{bytes:0} {units[unit]}"
                : $"{size:0.##} {units[unit]}";
        }
    }
}
