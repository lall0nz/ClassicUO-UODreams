using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    internal static class DesktopShortcut
    {
#if LAUNCHER_EDITION_ONEUO
        private const string ShortcutName = "0nE UO Launcher.lnk";
        private const string IconFileName = "oneuo.ico";
        private const string ShortcutDescription = "0nE UO Launcher";
        private const string IconResourceName = "ClassicUO.Launcher.Custom.Resources.oneuo.ico";
#else
        private const string ShortcutName = "UODreams Launcher.lnk";
        private const string IconFileName = "uodreams.ico";
        private const string ShortcutDescription = "UODreams Launcher";
        private const string IconResourceName = "ClassicUO.Launcher.Custom.Resources.uodreams.ico";
#endif

        public static bool TryCreate()
        {
            try
            {
                string? exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return false;
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, ShortcutName);

                if (File.Exists(shortcutPath))
                {
                    return false;
                }

                string? iconPath = ResolveShortcutIconPath(exePath);

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

                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        private static string? ResolveShortcutIconPath(string exePath)
        {
            string installDir = Path.GetDirectoryName(exePath) ?? "";
            string sidecarIcon = Path.Combine(installDir, IconFileName);
            string resourcesIcon = Path.Combine(installDir, "Resources", IconFileName);

            if (File.Exists(sidecarIcon))
            {
                return sidecarIcon;
            }

            if (File.Exists(resourcesIcon))
            {
                return resourcesIcon;
            }

            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(IconResourceName);

                if (stream == null)
                {
                    return null;
                }

                Directory.CreateDirectory(installDir);
                using var fs = File.Create(sidecarIcon);
                stream.CopyTo(fs);
                return sidecarIcon;
            }
            catch
            {
                return null;
            }
        }
    }
}
