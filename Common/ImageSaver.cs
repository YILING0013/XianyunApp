using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace xianyun.Common
{
    public static class ImageSaver
    {
        // 保存图像的公共方法，传入Base64图像数据、保存路径以及文件名
        public static void SaveImage(string base64Image, string saveDirectory, string fileName)
        {
            if (string.IsNullOrEmpty(saveDirectory))
            {
                throw new ArgumentException("保存路径不能为空。");
            }

            // 将Base64字符串转换为字节数组
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            BitmapImage image = new BitmapImage();

            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
            }

            // 完整的保存路径
            string filePath = Path.Combine(saveDirectory, fileName);

            // 保存图像
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fs);
            }
        }

        // 根据选择的命名规则生成文件名
        public static string GenerateFileName(string customPrefix = null)
        {
            string fileName = string.Empty;

            if (!string.IsNullOrEmpty(customPrefix))
            {
                fileName = customPrefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            }
            else
            {
                fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            }

            return fileName;
        }
    }
}
