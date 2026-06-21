using System;
using System.IO;

namespace VoidMode
{
    public static class AppLogger
    {
        private static readonly object LockObject = new object();

        public static void Initialize()
        {
            try
            {
                AppPaths.EnsureAppDataDirectory();
                Info("=== VoidMode log started ===");
                Info($"BaseDirectory: {AppContext.BaseDirectory}");
                Info($"ConfigPath: {AppPaths.ConfigPath}");
                Info($"LogPath: {AppPaths.LogPath}");
            }
            catch
            {
                // Logging must never prevent app startup.
            }
        }

        public static void Info(string message) => Write("INFO", message);

        public static void Error(string message, Exception? exception = null)
        {
            Write("ERROR", exception == null ? message : $"{message}\n{exception}");
        }

        private static void Write(string level, string message)
        {
            try
            {
                AppPaths.EnsureAppDataDirectory();
                var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
                lock (LockObject)
                {
                    File.AppendAllText(AppPaths.LogPath, line);
                }
            }
            catch
            {
                // Logging must never crash VoidMode.
            }
        }
    }
}
