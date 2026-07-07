namespace ClassicUO.Launcher.Custom
{
    // Minimal inline localization: call Loc.S("italiano", "english").
    // Language is a process-wide toggle applied by re-running the text setters.
    internal static class Loc
    {
        public static string Lang { get; set; } = "it";

        public static bool IsEn => Lang == "en";

        public static string S(string it, string en) => IsEn ? en : it;
    }
}
