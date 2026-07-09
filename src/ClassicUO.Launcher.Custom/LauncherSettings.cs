using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Launcher.Custom
{
    public sealed class ShardServer
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";
        public int Port { get; set; } = 2593;
    }

    public sealed class LauncherSettings
    {
        public string Assistant { get; set; } = "Nessuno"; // Nessuno | ClassicAssist | Razor Enhanced | Orion | UOSteam
        public string ClassicAssistPath { get; set; } = "";
        public string RazorPath { get; set; } = "";
        public string OrionPath { get; set; } = "";
        public string UOSteamPath { get; set; } = "";
        public string EnhancedMapPath { get; set; } = "";
        public bool EnhancedMapAutoOpen { get; set; } = false;
        public string ClientPath { get; set; } = "";
        public string UoDirectory { get; set; } = "";
        public string ShardIp { get; set; } = "login.uodreams.com";
        public int ShardPort { get; set; } = 2593;
        public int Encryption { get; set; } = 0;
        public bool DesktopShortcutCreated { get; set; } = false;
        public bool FirstRunCompleted { get; set; } = false;
        public string Language { get; set; } = "en"; // it | en
        public string InstalledClientVersion { get; set; } = "";

        public List<ShardServer> Servers { get; set; } = new();
        public string SelectedServer { get; set; } = "UODreams";

        public static List<ShardServer> DefaultServers() => new()
        {
            new ShardServer { Name = "UODreams", Ip = "login.uodreams.com", Port = 2593 },
            new ShardServer { Name = "UODreams TC", Ip = "login.uodreams.com", Port = 2594 },
            new ShardServer { Name = "UODreams Staff TC", Ip = "login.uodreams.com", Port = 2596 }
        };

        /// <summary>
        /// Ensures the default UODreams servers are always present (merged with any custom ones).
        /// </summary>
        public void EnsureDefaultServers()
        {
            if (Servers == null)
            {
                Servers = new List<ShardServer>();
            }

            foreach (ShardServer def in DefaultServers())
            {
                bool exists = Servers.Exists(s =>
                    string.Equals(s.Name, def.Name, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    Servers.Add(def);
                }
            }
        }

        [JsonIgnore]
        public static string FilePath =>
            Path.Combine(AppContext.BaseDirectory, "launcher.settings.json");

        [JsonIgnore]
        public string EffectiveClientVersion =>
            LauncherUpdater.MaxVersion(InstalledClientVersion, LauncherManifest.ClientRuntimeVersion);

        /// <summary>
        /// When the client is installed, keep settings in sync with the launcher build so stale
        /// InstalledClientVersion values do not trigger false update prompts.
        /// </summary>
        public void SyncInstalledClientVersionFromRuntime()
        {
            if (!ClientRuntimeDownloader.IsInstalled())
            {
                return;
            }

            string effective = EffectiveClientVersion;
            if (string.IsNullOrWhiteSpace(effective))
            {
                return;
            }

            if (string.Equals(InstalledClientVersion, effective, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            InstalledClientVersion = effective;
            Save();
        }

        public static LauncherSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.EnsureDefaultServers();
                        loaded.MigrateFirstRunFlag();
                        return loaded;
                    }
                }
            }
            catch
            {
                // corrupted settings file: fall back to defaults
            }

            var fresh = CreateFresh();
            return fresh;
        }

        public static LauncherSettings CreateFresh()
        {
            var fresh = new LauncherSettings();
            fresh.EnsureDefaultServers();
            fresh.ResetUserPaths();
            return fresh;
        }

        public void ResetUserPaths()
        {
            Assistant = "Nessuno";
            ClassicAssistPath = "";
            RazorPath = "";
            OrionPath = "";
            UOSteamPath = "";
            EnhancedMapPath = "";
            EnhancedMapAutoOpen = false;
            ClientPath = "";
            UoDirectory = "";
            FirstRunCompleted = false;
        }

        /// <summary>
        /// Existing installs without FirstRunCompleted keep their saved paths.
        /// </summary>
        public void MigrateFirstRunFlag()
        {
            if (FirstRunCompleted)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(UoDirectory) ||
                !string.IsNullOrWhiteSpace(ClassicAssistPath) ||
                !string.IsNullOrWhiteSpace(RazorPath) ||
                !string.IsNullOrWhiteSpace(OrionPath) ||
                !string.IsNullOrWhiteSpace(UOSteamPath) ||
                !string.IsNullOrWhiteSpace(ClientPath) ||
                !string.Equals(Assistant, "Nessuno", StringComparison.OrdinalIgnoreCase))
            {
                FirstRunCompleted = true;
            }
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
