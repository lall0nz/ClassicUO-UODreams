using System;
using System.IO;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    internal static class DesktopShortcut
    {
        private const string ShortcutName = "UODreams Launcher.lnk";

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

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
                shortcut.Description = "UODreams Launcher";
                shortcut.IconLocation = $"{exePath},0";
                shortcut.Save();

                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
