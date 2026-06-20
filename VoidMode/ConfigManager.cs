using System;
using System.IO;
using Newtonsoft.Json;

namespace VoidMode
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppConfig();
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }
}
