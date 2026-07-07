using System;
using System.IO;

namespace ClassicUO.Launcher.Custom
{
    internal static class AssistantPaths
    {
        private static readonly string[] OrionLauncherCandidates =
        {
            "OrionLauncher64.exe",
            "Orion Launcher64.exe"
        };

        public static bool IsExternalLauncher(string assistant) =>
            assistant is "UOSteam" or "Orion";

        public static bool IsValidAtPath(string assistant, string? path)
        {
            return assistant switch
            {
                "UOSteam" => ResolveUOSteamExe(path) != null,
                "Orion" => ResolveOrionLauncherExe(path) != null,
                _ => AssistantDownloader.IsPluginValidAtPath(assistant, path)
            };
        }

        public static string? ResolveUOSteamExe(string? path)
        {
            string trimmed = (path ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(trimmed))
            {
                return ClientRuntimeDownloader.DetectUOSteamExe();
            }

            if (File.Exists(trimmed) &&
                Path.GetFileName(trimmed).Equals("UOS.exe", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (Directory.Exists(trimmed))
            {
                string candidate = Path.Combine(trimmed, "UOS.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string? parent = Path.GetDirectoryName(trimmed);
            if (!string.IsNullOrEmpty(parent))
            {
                string candidate = Path.Combine(parent, "UOS.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static string? ResolveOrionLauncherExe(string? path)
        {
            string trimmed = (path ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(trimmed))
            {
                return ClientRuntimeDownloader.DetectOrionLauncherExe();
            }

            if (File.Exists(trimmed) && IsOrionLauncherExe(Path.GetFileName(trimmed)))
            {
                return trimmed;
            }

            if (Directory.Exists(trimmed))
            {
                return FindOrionLauncherInDir(trimmed);
            }

            string? parent = Path.GetDirectoryName(trimmed);
            if (!string.IsNullOrEmpty(parent))
            {
                return FindOrionLauncherInDir(parent);
            }

            return null;
        }

        public static string? ResolveOrionInstallRoot(string? path)
        {
            string? exe = ResolveOrionLauncherExe(path);
            return exe == null ? null : Path.GetDirectoryName(exe);
        }

        private static string? FindOrionLauncherInDir(string dir)
        {
            foreach (string name in OrionLauncherCandidates)
            {
                string candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            foreach (string file in Directory.EnumerateFiles(dir, "*Launcher*.exe", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file);
                if (IsOrionLauncherExe(fileName))
                {
                    return file;
                }
            }

            return null;
        }

        private static bool IsOrionLauncherExe(string fileName) =>
            fileName.Equals("OrionLauncher64.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("Orion Launcher64.exe", StringComparison.OrdinalIgnoreCase);
    }
}
