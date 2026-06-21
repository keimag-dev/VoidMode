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
            return CreateDefaultConfig();
        }

        public static void Save(AppConfig config)
        {
            config.Normalize();
            AppPaths.EnsureAppDataDirectory();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(AppPaths.ConfigPath, json);
            AppLogger.Info($"Saved config to {AppPaths.ConfigPath}");
        }

        private static AppConfig LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<AppConfig>(json) ?? CreateDefaultConfig();
            config.Normalize();
            return config;
        }

        private static AppConfig CreateDefaultConfig()
        {
            var config = new AppConfig();
            config.Normalize();
            return config;
        }
    }
}
