using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace xianyun.Common
{
    public class tools
    {
        // 生成允许的分辨率列表
        public static List<(int, int)> GenerateAllowedResolutions()
        {
            int maxProduct = 1048576;
            int maxValue = 2048;
            int step = 64;
            var allowedResolutions = new List<(int, int)>();

            for (int width = step; width <= maxValue; width += step)
            {
                for (int height = step; height <= maxValue; height += step)
                {
                    int product = width * height;
                    if (product <= maxProduct)
                    {
                        allowedResolutions.Add((width, height));
                        if (width != height)
                        {
                            allowedResolutions.Add((height, width));
                        }
                    }
                }
            }

            return allowedResolutions;
        }

        // 计算分辨率之间的欧几里得距离
        private static double GetResolutionDistance((int, int) res1, (int, int) res2)
        {
            return Math.Sqrt(Math.Pow(res1.Item1 - res2.Item1, 2) + Math.Pow(res1.Item2 - res2.Item2, 2));
        }

        // 找到最接近的分辨率
        private static (int, int) FindClosestResolution((int, int) currentResolution, List<(int, int)> allowedResolutions)
        {
            return allowedResolutions
                .OrderBy(res => GetResolutionDistance(currentResolution, res))
                .First();
        }

        public static void ValidateResolution(ref int width, ref int height)
        {
            // 定义允许的分辨率列表
            var allowedResolutions = GenerateAllowedResolutions();

            var currentResolution = (width, height);

            // 检查当前分辨率是否在允许列表中
            if (allowedResolutions.Contains(currentResolution))
            {
                // 如果在允许列表中，不做任何更改
                return;
            }
            else
            {
                // 找到最接近的分辨率
                var closestResolution = FindClosestResolution(currentResolution, allowedResolutions);
                width = closestResolution.Item1;
                height = closestResolution.Item2;
            }
        }

        public static BitmapImage ResizeImage(BitmapImage originalImage, int targetWidth, int targetHeight)
        {
            // 创建目标尺寸的 DrawingVisual
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // 绘制图像到目标尺寸
                drawingContext.DrawImage(originalImage, new System.Windows.Rect(0, 0, targetWidth, targetHeight));
            }

            // 渲染到 RenderTargetBitmap
            var resizedBitmap = new RenderTargetBitmap(
                targetWidth, targetHeight,
                96, 96, // DPI，通常为 96
                PixelFormats.Pbgra32);
            resizedBitmap.Render(drawingVisual);

            // 将 RenderTargetBitmap 转换为 BitmapImage
            var resizedImage = new BitmapImage();
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));
                encoder.Save(memoryStream);

                memoryStream.Position = 0;
                resizedImage.BeginInit();
                resizedImage.CacheOption = BitmapCacheOption.OnLoad;
                resizedImage.StreamSource = memoryStream;
                resizedImage.EndInit();
            }

            return resizedImage;
        }

        public static string ConvertImageToBase64(BitmapImage image, BitmapEncoder encoder = null)
        {
            // 如果没有提供编码器，默认使用 Png 编码器
            if (encoder == null)
            {
                encoder = new PngBitmapEncoder();
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                // 将 BitmapImage 转换为 Frame 并添加到编码器
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);

                // 将流转换为 Base64 字符串
                byte[] imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

        public static BitmapFrame ConvertBase64ToBitmapFrame(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return BitmapFrame.Create(bitmapImage);
        }

        public static BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }

        public static BitmapImage ConvertBitmapFrameToBitmapImage(BitmapFrame bitmapFrame)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(memoryStream);

                memoryStream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }

        public static string ConvertRenderTargetBitmapToBase64(RenderTargetBitmap renderBitmap)
        {
            // 使用 MemoryStream 存储 PNG 数据
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // 使用 PngBitmapEncoder 编码图像
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                // 将编码后的图像保存到 MemoryStream
                encoder.Save(memoryStream);

                // 将 MemoryStream 转换为字节数组
                byte[] imageBytes = memoryStream.ToArray();

                // 将字节数组转换为 Base64 字符串
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }
    }
}
