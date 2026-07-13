using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace ClassicUO.Launcher.Custom
{
    internal static class UoClientVersionDetector
    {
        private static readonly string[] ClientExeNames =
        {
            "client.exe",
            "Client.exe",
            "ultima.exe",
            "Ultima.exe"
        };

        public static string? Detect(string uoDirectory)
        {
            if (string.IsNullOrWhiteSpace(uoDirectory) || !Directory.Exists(uoDirectory))
            {
                return null;
            }

            foreach (string exeName in ClientExeNames)
            {
                string clientExe = Path.Combine(uoDirectory, exeName);
                if (!File.Exists(clientExe))
                {
                    continue;
                }

                if (TryReadFileVersionInfo(clientExe, out string? version) && IsPlausibleVersion(version))
                {
                    return version;
                }

                if (TryParseFromEmbeddedVersionResource(clientExe, out version) && IsPlausibleVersion(version))
                {
                    return version;
                }
            }

            return DetectFromCliloc(uoDirectory);
        }

        private static bool TryReadFileVersionInfo(string clientExe, out string? version)
        {
            version = null;

            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(clientExe);
                string? candidate = NormalizeVersionText(info.ProductVersion)
                    ?? NormalizeVersionText(info.FileVersion);

                if (candidate != null)
                {
                    version = candidate;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryParseFromEmbeddedVersionResource(string clientPath, out string? version)
        {
            version = null;

            try
            {
                using var fs = new FileStream(clientPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] buffer = new byte[Math.Min(fs.Length, 8 * 1024 * 1024)];
                int read = fs.Read(buffer, 0, buffer.Length);

                Span<byte> marker =
                    stackalloc byte[]
                    {
                        0x56, 0x00, 0x53, 0x00, 0x5F, 0x00, 0x56,
                        0x00, 0x45, 0x00, 0x52, 0x00, 0x53, 0x00,
                        0x49, 0x00, 0x4F, 0x00, 0x4E, 0x00, 0x5F,
                        0x00, 0x49, 0x00, 0x4E, 0x00, 0x46, 0x00,
                        0x4F, 0x00
                    };

                for (int i = 0; i + marker.Length + 10 < read; i++)
                {
                    if (!buffer.AsSpan(i, marker.Length).SequenceEqual(marker))
                    {
                        continue;
                    }

                    int offset = i + 42;
                    if (offset + 8 > read)
                    {
                        break;
                    }

                    int minorPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset));
                    int majorPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 2));
                    int privatePart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 4));
                    int buildPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 6));

                    version = $"{majorPart}.{minorPart}.{buildPart}.{privatePart}";
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static string? DetectFromCliloc(string uoDirectory)
        {
            string clilocPath = Path.Combine(uoDirectory, "Cliloc.enu");
            if (!File.Exists(clilocPath))
            {
                clilocPath = Path.Combine(uoDirectory, "cliloc.enu");
            }

            if (!File.Exists(clilocPath))
            {
                return null;
            }

            try
            {
                byte[] header = new byte[Math.Min(64, new FileInfo(clilocPath).Length)];
                using (var fs = new FileStream(clilocPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Read(header, 0, header.Length) < header.Length)
                    {
                        return null;
                    }
                }

                if (LooksLikeUncompressedCliloc(header))
                {
                    return "7.0.102.3";
                }

                return "7.0.113.56";
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeUncompressedCliloc(ReadOnlySpan<byte> data)
        {
            if (data.Length < 13)
            {
                return false;
            }

            int id = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(6));
            int length = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(11));

            if (id <= 0 || id > 5_000_000)
            {
                return false;
            }

            if (length <= 0 || length > 16_384)
            {
                return false;
            }

            return 6 + 7 + length <= data.Length;
        }

        private static string? NormalizeVersionText(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string text = raw.Trim().Replace(',', '.');
            int space = text.IndexOf(' ');
            if (space > 0)
            {
                text = text[..space];
            }

            string[] parts = text.Split('.');
            if (parts.Length < 3 || parts.Length > 4)
            {
                return null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (i == parts.Length - 1 && parts[i].Length > 0 && !char.IsDigit(parts[i][0]))
                {
                    continue;
                }

                if (!int.TryParse(parts[i], out _))
                {
                    return null;
                }
            }

            return text.ToLowerInvariant();
        }

        private static bool IsPlausibleVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            string[] parts = version.Split('.');
            if (parts.Length < 3 || parts.Length > 4)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out int major) || major < 1 || major > 9)
            {
                return false;
            }

            return true;
        }
    }
}