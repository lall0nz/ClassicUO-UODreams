using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClassicUO.Launcher.Custom
{
    internal static class ClassicUOSettingsSync
    {
        public static void SyncForLaunch(string clientWorkingDir, string uoPath, string clientVersion)
        {
            if (string.IsNullOrWhiteSpace(clientVersion))
            {
                return;
            }

            SyncFile(Path.Combine(clientWorkingDir, "settings.json"), uoPath, clientVersion);

            string bootstrapSettings = Path.Combine(ClientRuntimeDownloader.ClientDir, "Bootstrap", "settings.json");
            if (!string.Equals(
                    Path.GetFullPath(clientWorkingDir),
                    Path.GetFullPath(ClientRuntimeDownloader.BootstrapDir),
                    StringComparison.OrdinalIgnoreCase))
            {
                SyncFile(bootstrapSettings, uoPath, clientVersion);
            }
        }

        private static void SyncFile(string settingsPath, string uoPath, string clientVersion)
        {
            if (!File.Exists(settingsPath))
            {
                return;
            }

            try
            {
                JsonNode? root = JsonNode.Parse(File.ReadAllText(settingsPath));
                if (root is not JsonObject obj)
                {
                    return;
                }

                obj["clientversion"] = clientVersion;

                if (!string.IsNullOrWhiteSpace(uoPath))
                {
                    obj["ultimaonlinedirectory"] = uoPath;
                }

                File.WriteAllText(
                    settingsPath,
                    obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                );
            }
            catch (Exception ex)
            {
                LauncherLog.Error($"Failed to sync settings.json at {settingsPath}", ex);
            }
        }
    }
}