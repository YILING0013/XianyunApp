using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xianyun.MainPages;
using xianyun.View;

namespace xianyun.UserControl
{
    /// <summary>
    /// VibeTransfer.xaml 的交互逻辑
    /// </summary>
    public partial class VibeTransfer
    {
        public VibeTransfer()
        {
            InitializeComponent();
        }
        private void DeleteControl_Click(object sender, RoutedEventArgs e)
        {
            // 获取父控件  
            var parent = this.Parent as Panel;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            if (parent != null)
            {
                // 从父容器中移除自身
                parent.Children.Remove(this);

                // 检查父容器是否还有其他子控件
                if (parent.Children.Count == 0)
                {
                    var mainPage = mainWindow?.MainWindowFrame.Content as Txt2imgPage;
                    // 如果没有控件，显示上传标志
                    if (mainPage != null)
                    {
                        mainPage.UploadStackPanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        // 处理右键点击事件
        private void Border_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }
        // 获取滑块值的属性
        public double InformationExtracted => InformationExtractedSlider.Value;
        public double ReferenceStrength => ReferenceStrengthSlider.Value;

        // 设置图像（通过 base64 字符串）
        public void SetImageFromBase64(string base64Image)
        {
            var imageBytes = Convert.FromBase64String(base64Image);
            using (var stream = new MemoryStream(imageBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                SetImage(bitmap);
            }
        }

        // 设置图像（通过文件路径）
        public void SetImageFromFile(string filePath)
        {
            var bitmap = new BitmapImage(new Uri(filePath));
            SetImage(bitmap);
        }

        // 调整大小并设置图像
        private void SetImage(BitmapImage bitmap)
        {
            var resizedBitmap = ResizeAndPadImage(bitmap, 448, 448);
            ImageControl.Source = resizedBitmap;
        }

        // 调整图像大小并填充
        private BitmapImage ResizeAndPadImage(BitmapImage originalImage, int targetWidth, int targetHeight)
        {
            double ratioX = (double)targetWidth / originalImage.PixelWidth;
            double ratioY = (double)targetHeight / originalImage.PixelHeight;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(originalImage.PixelWidth * ratio);
            int newHeight = (int)(originalImage.PixelHeight * ratio);

            var resizedImage = new TransformedBitmap(originalImage, new System.Windows.Media.ScaleTransform(ratio, ratio));

            // 创建一个新的空白图像，尺寸为目标大小，背景为黑色
            var targetImage = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(0, 0, targetWidth, targetHeight));
                drawingContext.DrawImage(resizedImage, new Rect((targetWidth - newWidth) / 2, (targetHeight - newHeight) / 2, newWidth, newHeight));
            }

            targetImage.Render(drawingVisual);

            var finalImage = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(targetImage));
                encoder.Save(stream);

                finalImage.BeginInit();
                finalImage.StreamSource = new MemoryStream(stream.ToArray());
                finalImage.CacheOption = BitmapCacheOption.OnLoad;
                finalImage.EndInit();
            }

            return finalImage;
        }

        // 获取图像的 base64 字符串
        public string GetImageAsBase64()
        {
            if (ImageControl.Source is BitmapSource bitmap)
            {
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                    return Convert.ToBase64String(stream.ToArray());
                }
            }

            return null;
        }
    }
}
