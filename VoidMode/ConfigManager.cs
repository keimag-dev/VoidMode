using System;
using System.IO;
using Newtonsoft.Json;

namespace VoidMode
{
    public static class ConfigManager
    {
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(AppPaths.ConfigPath))
                {
                    AppLogger.Info($"Loading config from {AppPaths.ConfigPath}");
                    return LoadFromFile(AppPaths.ConfigPath);
                }

                // Migration path for older development builds that stored config next to the executable.
                if (File.Exists(AppPaths.LegacyConfigPath))
                {
                    AppLogger.Info($"Migrating legacy config from {AppPaths.LegacyConfigPath} to {AppPaths.ConfigPath}");
                    var legacyConfig = LoadFromFile(AppPaths.LegacyConfigPath);
                    Save(legacyConfig);
                    return legacyConfig;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to load config. Falling back to defaults.", ex);
            }

            AppLogger.Info("Using default config.");
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            AppPaths.EnsureAppDataDirectory();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(AppPaths.ConfigPath, json);
            AppLogger.Info($"Saved config to {AppPaths.ConfigPath}");
        }

        private static AppConfig LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
        }
    }
}
