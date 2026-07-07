using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Launcher.Custom
{
    public sealed class LauncherSettings
    {
        public string Assistant { get; set; } = "Nessuno"; // Nessuno | ClassicAssist | Razor
        public string ClassicAssistPath { get; set; } = "";
        public string RazorPath { get; set; } = "";
        public string ClientPath { get; set; } = "";
        public string UoDirectory { get; set; } = "";
        public string ShardIp { get; set; } = "login.uodreams.com";
        public int ShardPort { get; set; } = 2593;
        public int Encryption { get; set; } = 0;

        [JsonIgnore]
        public static string FilePath =>
            Path.Combine(AppContext.BaseDirectory, "launcher.settings.json");

        public static LauncherSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(FilePath));
                    if (loaded != null)
                        return loaded;
                }
            }
            catch
            {
                // corrupted settings file: fall back to defaults
            }

            return new LauncherSettings();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(
                    FilePath,
                    JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })
                );
            }
            catch
            {
                // read-only folder: ignore, settings just won't persist
            }
        }
    }
}
