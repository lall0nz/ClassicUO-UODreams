using System;
using System.IO;

namespace ClassicUO.Launcher.Custom
{
    internal static class LauncherLog
    {
        private static readonly object Lock = new();

        public static string LogPath => Path.Combine(AppContext.BaseDirectory, "Logs", "launcher.log");

        public static string? LastError { get; private set; }

        public static void Info(string message) => Write("INFO", message);

        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception? ex = null)
        {
            try
            {
                string logDir = Path.GetDirectoryName(LogPath)!;
                Directory.CreateDirectory(logDir);

                lock (Lock)
                {
                    using var writer = new StreamWriter(LogPath, append: true);
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level} {message}");
                    if (ex != null)
                    {
                        writer.WriteLine(ex);
                    }
                }

                if (level == "ERROR")
                {
                    LastError = ex == null ? message : $"{message}: {ex.Message}";
                }
            }
            catch
            {
                // logging must never break the launcher
            }
        }
    }
}
