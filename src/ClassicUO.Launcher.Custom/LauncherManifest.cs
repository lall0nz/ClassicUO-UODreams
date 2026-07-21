using System;
using System.Reflection;

namespace ClassicUO.Launcher.Custom
{
    internal static class LauncherManifest
    {
#if LAUNCHER_EDITION_ONEUO
        public const string Edition = "pvp";
        public const string ProductTitle = "0nE UO Launcher";
        public const string ReleaseTagPrefix = "pvp";
        public const string AssetPrefix = "UODreams-PVP";
#elif LAUNCHER_EDITION_PVP
        public const string Edition = "pvp";
        public const string ProductTitle = "UODreams PVP Launcher";
        public const string ReleaseTagPrefix = "pvp";
        public const string AssetPrefix = "UODreams-PVP";
#else
        public const string Edition = "classic";
        public const string ProductTitle = "UODreams Launcher";
        public const string ReleaseTagPrefix = "classic";
        public const string AssetPrefix = "UODreams-Classic";
#endif

#if LAUNCHER_EDITION_PVP
        public const string LauncherVersion = "1.4.0";
        public const string ClientRuntimeVersion = "1.4.0";
        public const string GitHubRepo = "lall0nz/UODreams-PVP-Launcher";
#else
        public const string LauncherVersion = "1.1.9";
        public const string ClientRuntimeVersion = "1.1.9";
        public const string GitHubRepo = "lall0nz/ClassicUO-UODreams";
#endif

        public static bool IsPvpEdition => Edition == "pvp";

        /// <summary>
        /// Launcher build version from the published assembly (matches release tag / FileVersion).
        /// Falls back to <see cref="LauncherVersion"/> when assembly metadata is unavailable.
        /// </summary>
        public static string RuntimeLauncherVersion
        {
            get
            {
                try
                {
                    Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                    if (version != null && (version.Major > 0 || version.Minor > 0 || version.Build > 0))
                    {
                        return $"{version.Major}.{version.Minor}.{version.Build}";
                    }
                }
                catch
                {
                    // fall back to compile-time constant
                }

                return LauncherVersion;
            }
        }

#if LAUNCHER_EDITION_PVP
        public static string ReleaseTag => $"v{LauncherVersion}";

        public static string ClientPackageFileName =>
            $"UODreams-PVP-by-lall0ne-Client-v{ClientRuntimeVersion}.zip";

        public static string LauncherPackageFileName =>
            $"UODreams-PVP-by-lall0ne-Launcher-v{LauncherVersion}.zip";
#else
        public static string ReleaseTag => $"{ReleaseTagPrefix}-v{LauncherVersion}";

        public static string ClientPackageFileName =>
            $"{AssetPrefix}-Client-v{ClientRuntimeVersion}.zip";

        public static string LauncherPackageFileName =>
            $"{AssetPrefix}-Launcher-v{LauncherVersion}.zip";
#endif

        public static string ClientPackageUrl =>
            $"https://github.com/{GitHubRepo}/releases/download/{ReleaseTag}/{ClientPackageFileName}";
    }
}

