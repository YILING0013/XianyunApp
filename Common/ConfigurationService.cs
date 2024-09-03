using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xianyun.Model;
using System.IO;
using System.Diagnostics;

namespace xianyun.Common
{
    public static class ConfigurationService
    {
        private static string _configFilePath = "config.json";

        public static void SaveConfiguration<T>(T configuration)
        {
            string json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
        }

        public static T LoadConfiguration<T>() where T : new()
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            return new T();
        }

        public static bool ConfigurationExists()
        {
            return File.Exists(_configFilePath);
        }
    }
}
