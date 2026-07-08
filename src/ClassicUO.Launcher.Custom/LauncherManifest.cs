namespace ClassicUO.Launcher.Custom
{
    internal static class LauncherManifest
    {
        public const string LauncherVersion = "1.0.3";
        public const string ClientRuntimeVersion = "1.0.3";
        public const string GitHubRepo = "lall0nz/ClassicUO-UODreams";

        public static string ClientPackageFileName =>
            $"UODreams-Client-v{ClientRuntimeVersion}.zip";

        public static string LauncherPackageFileName =>
            $"UODreams-Launcher-v{LauncherVersion}.zip";

        public static string ClientPackageUrl =>
            $"https://github.com/{GitHubRepo}/releases/download/v{ClientRuntimeVersion}/{ClientPackageFileName}";
    }
}
