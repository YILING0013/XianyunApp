using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace xianyun.Common
{
    public static class ConfigurationService
    {
        // 配置文件路径
        private static string _configFilePath = "config.json";
        // 副本保存的文件夹路径
        private static string _backupFolderPath = "ConfigBackups";
        // 最大保留副本数量
        private static int _maxBackupCount = 30;

        // 用于同步文件操作
        private static readonly object _fileLock = new object();

        static ConfigurationService()
        {
            // 确保副本文件夹存在
            if (!Directory.Exists(_backupFolderPath))
            {
                Directory.CreateDirectory(_backupFolderPath);
            }
        }

        // 保存配置并生成副本
        public static void SaveConfiguration<T>(T configuration)
        {
            lock (_fileLock)  // 确保文件操作的线程安全
            {
                // 1. 保存主配置文件
                string json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);

                // 2. 创建副本
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string backupFilePath = Path.Combine(_backupFolderPath, $"{timestamp}_config.json");

                // 保存副本
                File.WriteAllText(backupFilePath, json);

                // 3. 清理多余的副本
                CleanUpOldBackups();
            }
        }

        // 加载配置，首先尝试加载主配置文件，如果失败则加载最新的副本
        public static T LoadConfiguration<T>() where T : new()
        {
            lock (_fileLock)  // 确保文件操作的线程安全
            {
                // 尝试加载主配置文件
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    return JsonConvert.DeserializeObject<T>(json);
                }

                // 如果主配置文件不存在，加载最近的副本
                var backupFiles = GetBackupFiles().OrderByDescending(file => file.CreationTime).ToList();
                if (backupFiles.Any())
                {
                    // 加载最新副本
                    string json = File.ReadAllText(backupFiles.First().FullName);
                    return JsonConvert.DeserializeObject<T>(json);
                }

                // 如果没有配置文件或副本，返回新实例
                return new T();
            }
        }

        // 检查配置文件是否存在
        public static bool ConfigurationExists()
        {
            lock (_fileLock)  // 确保文件操作的线程安全
            {
                return File.Exists(_configFilePath);
            }
        }

        // 获取副本文件列表
        private static IEnumerable<FileInfo> GetBackupFiles()
        {
            lock (_fileLock)  // 确保文件操作的线程安全
            {
                DirectoryInfo dirInfo = new DirectoryInfo(_backupFolderPath);
                return dirInfo.GetFiles("*.json").OrderBy(f => f.CreationTime);
            }
        }

        // 清理多余的副本
        private static void CleanUpOldBackups()
        {
            lock (_fileLock)  // 确保文件操作的线程安全
            {
                var backupFiles = GetBackupFiles().ToList();
                int excessCount = backupFiles.Count - _maxBackupCount;

                if (excessCount > 0)
                {
                    // 删除最早的副本
                    foreach (var file in backupFiles.Take(excessCount))
                    {
                        file.Delete();
                    }
                }
            }
        }
    }
}
