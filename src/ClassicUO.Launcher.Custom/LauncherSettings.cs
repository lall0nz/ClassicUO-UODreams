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

        /// <summary>
        /// Assistants whose path was explicitly cleared by the user; auto-detect must not refill them.
        /// </summary>
        public List<string> AssistantPathsClearedByUser { get; set; } = new();

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
        public string EffectiveClientVersion
        {
            get
            {
                string version = string.IsNullOrWhiteSpace(InstalledClientVersion)
                    ? LauncherManifest.ClientRuntimeVersion
                    : InstalledClientVersion;
                return LauncherUpdater.NormalizeVersion(version);
            }
        }

        /// <summary>
        /// Repairs legacy values such as "1.1.9." parsed from package filenames.
        /// </summary>
        public void SanitizeInstalledClientVersion()
        {
            if (string.IsNullOrWhiteSpace(InstalledClientVersion))
            {
                return;
            }

            string normalized = LauncherUpdater.NormalizeVersion(InstalledClientVersion);
            if (string.Equals(normalized, InstalledClientVersion, StringComparison.Ordinal))
            {
                return;
            }

            InstalledClientVersion = normalized;
            Save();
        }

        /// <summary>
        /// Seeds InstalledClientVersion on first install only. Never overwrites a stored value,
        /// so a client updated via GitHub is not downgraded to the launcher's compile-time floor.
        /// </summary>
        public void SyncInstalledClientVersionFromRuntime()
        {
            if (!ClientRuntimeDownloader.IsInstalled())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(InstalledClientVersion))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(LauncherManifest.ClientRuntimeVersion))
            {
                return;
            }

            InstalledClientVersion = LauncherUpdater.NormalizeVersion(LauncherManifest.ClientRuntimeVersion);
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
                        loaded.SanitizeInstalledClientVersion();
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
            ClearAllAssistantPaths();
            EnhancedMapPath = "";
            EnhancedMapAutoOpen = false;
            ClientPath = "";
            UoDirectory = "";
            FirstRunCompleted = false;
        }

        public void ClearAllAssistantPaths()
        {
            ClassicAssistPath = "";
            RazorPath = "";
            OrionPath = "";
            UOSteamPath = "";
        }

        public void ClearAssistantPath(string assistant)
        {
            switch (assistant)
            {
                case "ClassicAssist":
                    ClassicAssistPath = "";
                    break;
                case "Razor Enhanced":
                    RazorPath = "";
                    break;
                case "Orion":
                    OrionPath = "";
                    break;
                case "UOSteam":
                    UOSteamPath = "";
                    break;
            }

            MarkAssistantPathClearedByUser(assistant);
        }

        public bool IsAssistantPathClearedByUser(string assistant) =>
            AssistantPathsClearedByUser?.Exists(a =>
                string.Equals(a, assistant, StringComparison.OrdinalIgnoreCase)) == true;

        public void MarkAssistantPathClearedByUser(string assistant)
        {
            AssistantPathsClearedByUser ??= new List<string>();
            if (!IsAssistantPathClearedByUser(assistant))
            {
                AssistantPathsClearedByUser.Add(assistant);
            }
        }

        public void ClearAssistantPathClearedFlag(string assistant)
        {
            AssistantPathsClearedByUser?.RemoveAll(a =>
                string.Equals(a, assistant, StringComparison.OrdinalIgnoreCase));
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
