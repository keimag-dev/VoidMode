using System;
using System.IO;

namespace VoidMode
{
    public static class AppPaths
    {
        public static string AppDataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoidMode");

        public static string ConfigPath => Path.Combine(AppDataDirectory, "config.json");

        public static string LogDirectory => Path.Combine(AppDataDirectory, "logs");

        public static string LogPath => Path.Combine(LogDirectory, "voidmode.log");

        public static string LegacyConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");

        public static void EnsureAppDataDirectory()
        {
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(LogDirectory);
        }
    }
}
