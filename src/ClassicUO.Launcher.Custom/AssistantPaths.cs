using System;
using System.IO;

namespace ClassicUO.Launcher.Custom
{
    internal static class AssistantPaths
    {
        /// <summary>
        /// Fixed assistant install root next to the launcher executable ({LauncherDir}\Assistant\).
        /// </summary>
        public static string LauncherAssistantRoot =>
            Path.Combine(AppContext.BaseDirectory, "Assistant");

        public static void EnsureLauncherAssistantRoot() =>
            Directory.CreateDirectory(LauncherAssistantRoot);

        public static string GetInstallDirectory(string assistant)
        {
            string subfolder = assistant switch
            {
                "ClassicAssist" => "ClassicAssist",
                "Razor Enhanced" => "RazorEnhanced",
                "Orion" => "Orion",
                "UOSteam" => "UOSteam",
                _ => assistant.Replace(" ", "", StringComparison.Ordinal)
            };

            return Path.Combine(LauncherAssistantRoot, subfolder);
        }

        /// <summary>
        /// Default bundled Razor Enhanced P.E. path: {LauncherDir}\Assistant\RazorEnhanced\RazorEnhanced.exe
        /// </summary>
        public static string GetDefaultRazorExePath() =>
            Path.Combine(LauncherAssistantRoot, "RazorEnhanced", "RazorEnhanced.exe");

        /// <summary>
        /// Saved paths under Client\Data\Plugins are legacy and must not override Assistant defaults.
        /// </summary>
        public static bool IsLegacyClientPluginsRazorPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/');
            return normalized.Contains("/Client/Data/Plugins/", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains("/Data/Plugins/RazorEnhanced", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bundled or downloaded Razor Enhanced P.E. under Assistant\ only (not Client\Data\Plugins).
        /// </summary>
        public static string? DetectBundledRazorExe()
        {
            string defaultExe = GetDefaultRazorExePath();
            if (File.Exists(defaultExe))
            {
                return defaultExe;
            }

            return FindRazorExeInDirectory(LauncherAssistantRoot);
        }

        public static string? DetectRazorInstallDirectory()
        {
            string? exe = DetectBundledRazorExe();
            if (exe == null)
            {
                return null;
            }

            if (File.Exists(exe))
            {
                return Path.GetDirectoryName(exe);
            }

            return Directory.Exists(exe) ? exe : null;
        }

        private static string? FindRazorExeInDirectory(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return null;
            }

            string flatExe = Path.Combine(root, "RazorEnhanced.exe");
            if (File.Exists(flatExe))
            {
                return flatExe;
            }

            foreach (string dir in Directory.EnumerateDirectories(root))
            {
                if (dir.Contains("RazorEnhanced", StringComparison.OrdinalIgnoreCase))
                {
                    string nestedExe = Path.Combine(dir, "RazorEnhanced.exe");
                    if (File.Exists(nestedExe))
                    {
                        return nestedExe;
                    }
                }
            }

            return null;
        }

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
