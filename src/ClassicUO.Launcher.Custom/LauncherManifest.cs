namespace ClassicUO.Launcher.Custom
{
    internal static class LauncherManifest
    {
#if LAUNCHER_EDITION_PVP
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

        public const string LauncherVersion = "1.1.2";
        public const string ClientRuntimeVersion = "1.1.2";
        public const string GitHubRepo = "lall0nz/ClassicUO-UODreams";

        public static bool IsPvpEdition => Edition == "pvp";

        public static string ReleaseTag => $"{ReleaseTagPrefix}-v{LauncherVersion}";

        public static string ClientPackageFileName =>
            $"{AssetPrefix}-Client-v{ClientRuntimeVersion}.zip";

        public static string LauncherPackageFileName =>
            $"{AssetPrefix}-Launcher-v{LauncherVersion}.zip";

        public static string ClientPackageUrl =>
            $"https://github.com/{GitHubRepo}/releases/download/{ReleaseTag}/{ClientPackageFileName}";
    }
}
