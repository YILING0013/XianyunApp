using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace xianyun.Common
{
    public static class SessionManager
    {
        public static string Session { get; set; }
        public static string Token { get; set; }

        public static int Opus { get; set; }
    }
    public class TotpGenerator
    {
        public static string GenerateTotp(string secretKey)
        {
            // 获取当前时间的分钟时间戳
            long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;

            // 将时间戳与 secretKey 拼接
            string data = epoch + secretKey;

            // 使用SHA-256进行哈希
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));

                // 将哈希值转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
