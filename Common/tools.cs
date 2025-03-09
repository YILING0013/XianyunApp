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
    public class Tools
    {
        // 计算最大公约数
        private static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        // 生成允许的分辨率列表 - 使用优化算法，只保留每种比例下面积最大的尺寸
        public static List<(int, int)> GenerateAllowedResolutions()
        {
            int maxProduct = 1048576; // 1024 * 1024
            int maxValue = 2048;
            int step = 64;
            var groups = new Dictionary<string, (int width, int height, int area)>();

            for (int width = step; width <= maxValue; width += step)
            {
                for (int height = step; height <= maxValue; height += step)
                {
                    int product = width * height;
                    if (product <= maxProduct)
                    {
                        int normW = width / step;
                        int normH = height / step;
                        int d = GCD(normW, normH);
                        string key = $"{normW / d}:{normH / d}";
                        int area = width * height;

                        if (!groups.ContainsKey(key) || area > groups[key].area)
                        {
                            groups[key] = (width, height, area);
                        }
                    }
                }
            }

            return groups.Values.Select(v => (v.width, v.height)).ToList();
        }

        // 找到最接近的分辨率 - 改用按比例选择最合适的
        private static (int, int) FindClosestResolution((int, int) currentResolution, List<(int, int)> allowedResolutions)
        {
            int origWidth = currentResolution.Item1;
            int origHeight = currentResolution.Item2;
            double inputRatio = (double)origWidth / origHeight;
            double bestDiff = double.MaxValue;

            // 找到比例差异最小的值
            foreach (var (w, h) in allowedResolutions)
            {
                double diff = Math.Abs((double)w / h - inputRatio);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                }
            }

            // 找出所有比例差异最小的候选
            var closestCandidates = allowedResolutions
                .Where(res => Math.Abs((double)res.Item1 / res.Item2 - inputRatio) == bestDiff)
                .ToList();

            // 上采样：选择比原图大的、面积最大的候选
            var upscaleCandidates = closestCandidates
                .Where(res => res.Item1 >= origWidth && res.Item2 >= origHeight)
                .ToList();

            if (upscaleCandidates.Any())
            {
                return upscaleCandidates
                    .OrderByDescending(res => res.Item1 * res.Item2)
                    .First();
            }

            // 下采样：选择比原图小的、面积最大的候选
            var downCandidates = closestCandidates
                .Where(res => res.Item1 <= origWidth && res.Item2 <= origHeight)
                .ToList();

            if (downCandidates.Any())
            {
                return downCandidates
                    .OrderByDescending(res => res.Item1 * res.Item2)
                    .First();
            }

            // 如果都未命中，取比例最接近的第一个
            return closestCandidates.First();
        }

        /// <summary>
        /// 验证分辨率是否合法，如果不合法，调整为最接近的合法分辨率
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
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

        /// <summary>
        /// 将 BitmapImage 转换为 Bitmap
        /// </summary>
        /// <param name="originalImage"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 将 BitmapImage 转换为 Base64 字符串
        /// </summary>
        /// <param name="image"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 将 Base64 字符串转换为 BitmapFrame
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 将 Base64 字符串转换为 BitmapImage
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 将 BitmapFrame 转换为 BitmapImage
        /// </summary>
        /// <param name="bitmapFrame"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 将 RenderTargetBitmap 转换为 Base64 字符串
        /// </summary>
        /// <param name="renderBitmap"></param>
        /// <returns></returns>
        public static string ConvertRenderTargetBitmapToBase64(RenderTargetBitmap renderBitmap)
        {
            // 使用 MemoryStream 存储 PNG 数据
            using (MemoryStream memoryStream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }
    }
}