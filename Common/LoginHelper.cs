using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Blake2Fast;
using System;
using System.Text;

namespace xianyun.Common
{
    public class LoginHelper
    {
        // 实现 argon_hash 方法
        private static string ArgonHash(string email, string password, int size, string domain)
        {
            // 构建 pre_salt
            string preSalt = $"{password.Substring(0, Math.Min(6, password.Length))}{email}{domain}";

            // 使用 BLAKE2b 生成 salt
            byte[] salt = ComputeBlake2bHash(preSalt, 16); // 使用 Blake2b 计算 16 字节的盐值

            // 配置 Argon2 参数
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 1, // parallelism=1
                Iterations = 2,           // iterations=2
                MemorySize = 2000000 / 1024, // memory size in KB (2000000 / 1024 = 1953 KB)
            };

            // 生成 hash，并获取指定字节数的结果
            byte[] hash = argon2.GetBytes(size);

            // 转换为 URL-safe Base64
            string hashed = Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return hashed;
        }

        // 使用 Blake2b 计算哈希
        private static byte[] ComputeBlake2bHash(string input, int hashLength)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            return Blake2b.ComputeHash(hashLength, inputBytes); // 计算哈希并返回字节数组
        }

        /// <summary>
        /// 获取访问密钥
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GetAccessKey(string email, string password)
        {
            // 调用 ArgonHash 方法获取 key
            return ArgonHash(email, password, 64, "novelai_data_access_key").Substring(0, 64);
        }
    }
}