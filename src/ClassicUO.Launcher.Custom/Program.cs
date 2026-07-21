using System;
using System.Windows.Forms;

namespace ClassicUO.Launcher.Custom
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (!ClientRuntimeDownloader.IsInstalled())
            {
                using var bootstrapForm = DownloadProgressForm.ForClientRuntime();
                bootstrapForm.StartPosition = FormStartPosition.CenterScreen;

                DialogResult result = bootstrapForm.ShowDialog();

                if (result != DialogResult.OK)
                {
                    MessageBox.Show(
                        "Per usare UODreams Launcher è necessario scaricare i componenti di gioco.\n" +
                        "Riavvia il launcher quando sei pronto.",
                        "UODreams Launcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                ResetSettingsAfterCleanInstall();
            }

            EnsureDesktopShortcut();
            Application.Run(new MainForm());
        }

        private static void ResetSettingsAfterCleanInstall()
        {
            try
            {
                var settings = LauncherSettings.Load();
                settings.ResetUserPaths();
                settings.Save();
            }
            catch
            {
                // best-effort
            }
        }

        private static void EnsureDesktopShortcut()
        {
            try
            {
                var settings = LauncherSettings.Load();
                if (!DesktopShortcut.NeedsRefresh(settings.DesktopShortcutRevision) &&
                    settings.DesktopShortcutCreated)
                {
                    return;
                }

                if (DesktopShortcut.TryEnsureCurrent())
                {
                    settings.DesktopShortcutCreated = true;
                    settings.DesktopShortcutRevision = DesktopShortcut.CurrentRevision;
                    settings.Save();
                }
            }
            catch
            {
                // shortcut refresh is best-effort
            }
        }
    }
}
