using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    internal static class DesktopShortcut
    {
#if LAUNCHER_EDITION_ONEUO
        public const string CurrentRevision = "oneuo-1";
        private const string ShortcutName = "0nE UO Launcher.lnk";
        private const string IconFileName = "oneuo.ico";
        private const string LegacyIconFileName = "uodreams.ico";
        private const string ShortcutDescription = "0nE UO Launcher";
        private const string IconResourceName = "ClassicUO.Launcher.Custom.Resources.oneuo.ico";
        private static readonly string[] LegacyShortcutNames =
        {
            "UODreams Launcher.lnk",
            "UODreams Launcher pvp.lnk",
            "UODreams PVP Launcher.lnk",
        };
        private static readonly string[] LauncherExeNames =
        {
            "0nE UO Launcher.exe",
            "UODreams Launcher.exe",
        };
#else
        public const string CurrentRevision = "uodreams-1";
        private const string ShortcutName = "UODreams Launcher.lnk";
        private const string IconFileName = "uodreams.ico";
        private const string LegacyIconFileName = "";
        private const string ShortcutDescription = "UODreams Launcher";
        private const string IconResourceName = "ClassicUO.Launcher.Custom.Resources.uodreams.ico";
        private static readonly string[] LegacyShortcutNames = Array.Empty<string>();
        private static readonly string[] LauncherExeNames = { "UODreams Launcher.exe" };
#endif

        public static bool NeedsRefresh(string? storedRevision) =>
            !string.Equals(storedRevision, CurrentRevision, StringComparison.Ordinal);

        public static bool TryCreate() => TryEnsureCurrent();

        /// <summary>
        /// Creates or refreshes the desktop shortcut so target, icon, and branding match the
        /// current launcher build. Removes legacy shortcut names after a successful refresh.
        /// </summary>
        public static bool TryEnsureCurrent()
        {
            try
            {
                string? exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return false;
                }

                string installDir = Path.GetDirectoryName(exePath) ?? "";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, ShortcutName);

                RemoveLegacySidecarIcon(installDir);
                string? iconPath = ResolveShortcutIconPath(exePath, forceRefresh: true);

                if (!WriteShortcut(shortcutPath, exePath, iconPath))
                {
                    return false;
                }

                RemoveSupersededDesktopShortcuts(desktopPath, shortcutPath, installDir);
                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsLauncherExecutable(string? targetPath, string installDir)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            try
            {
                targetPath = Path.GetFullPath(targetPath);
            }
            catch
            {
                return false;
            }

            string? targetDir = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDir) ||
                !string.Equals(targetDir, installDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = Path.GetFileName(targetPath);
            foreach (string launcherExeName in LauncherExeNames)
            {
                if (string.Equals(fileName, launcherExeName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsLegacyShortcutName(string fileName)
        {
            foreach (string legacyName in LegacyShortcutNames)
            {
                if (string.Equals(fileName, legacyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveSupersededDesktopShortcuts(
            string desktopPath,
            string canonicalShortcutPath,
            string installDir)
        {
            foreach (string shortcutFile in Directory.EnumerateFiles(desktopPath, "*.lnk"))
            {
                if (string.Equals(shortcutFile, canonicalShortcutPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(shortcutFile);
                if (IsLegacyShortcutName(fileName))
                {
                    TryDeleteShortcut(shortcutFile);
                    continue;
                }

                if (!TryReadShortcutTarget(shortcutFile, out string? targetPath) ||
                    !IsLauncherExecutable(targetPath, installDir))
                {
                    continue;
                }

                if (!string.Equals(fileName, ShortcutName, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteShortcut(shortcutFile);
                }
            }
        }

        private static bool WriteShortcut(string shortcutPath, string exePath, string? iconPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
                shortcut.Description = ShortcutDescription;
                shortcut.IconLocation = string.IsNullOrEmpty(iconPath)
                    ? $"{exePath},0"
                    : $"{iconPath},0";
                shortcut.Save();

                if (!File.Exists(shortcutPath))
                {
                    return false;
                }

                File.SetLastWriteTimeUtc(shortcutPath, DateTime.UtcNow);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadShortcutTarget(string shortcutPath, out string? targetPath)
        {
            targetPath = null;

            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                targetPath = shortcut.TargetPath;
                return !string.IsNullOrWhiteSpace(targetPath);
            }
            catch
            {
                return false;
            }
        }

        private static void TryDeleteShortcut(string shortcutPath)
        {
            try
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private static void RemoveLegacySidecarIcon(string installDir)
        {
#if LAUNCHER_EDITION_ONEUO
            if (string.IsNullOrEmpty(LegacyIconFileName))
            {
                return;
            }

            string legacyIcon = Path.Combine(installDir, LegacyIconFileName);
            if (!File.Exists(legacyIcon))
            {
                return;
            }

            try
            {
                File.Delete(legacyIcon);
            }
            catch
            {
                // best-effort
            }
#endif
        }

        private static string? ResolveShortcutIconPath(string exePath, bool forceRefresh = false)
        {
            string installDir = Path.GetDirectoryName(exePath) ?? "";
            string sidecarIcon = Path.Combine(installDir, IconFileName);
            string resourcesIcon = Path.Combine(installDir, "Resources", IconFileName);

            if (!forceRefresh)
            {
                if (File.Exists(sidecarIcon))
                {
                    return sidecarIcon;
                }

                if (File.Exists(resourcesIcon))
                {
                    return resourcesIcon;
                }
            }

            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(IconResourceName);

                if (stream == null)
                {
                    return File.Exists(sidecarIcon) ? sidecarIcon : null;
                }

                Directory.CreateDirectory(installDir);
                using var fs = File.Create(sidecarIcon);
                stream.CopyTo(fs);
                return sidecarIcon;
            }
            catch
            {
                if (File.Exists(sidecarIcon))
                {
                    return sidecarIcon;
                }

                if (File.Exists(resourcesIcon))
                {
                    return resourcesIcon;
                }

                return null;
            }
        }
    }
}
