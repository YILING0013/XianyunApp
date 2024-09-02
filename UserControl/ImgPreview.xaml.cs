using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System;

namespace xianyun.UserControl
{
    public partial class ImgPreview
    {
        public event EventHandler<string> ImageClicked;

        public ImgPreview(string base64Image)
        {
            InitializeComponent();
            SetImage(base64Image);

            deleteButton.Click += DeleteButton_Click;
            this.MouseLeftButtonUp += ImgPreview_MouseLeftButtonUp;
        }

        private void SetImage(string base64Image)
        {
            if (!string.IsNullOrEmpty(base64Image))
            {
                var imageBytes = Convert.FromBase64String(base64Image);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    imageControl.Source = bitmapImage;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 从父级容器中移除自己
            var parent = this.Parent as Panel;
            parent?.Children.Remove(this);
        }

        private void ImgPreview_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 触发 ImageClicked 事件，将 base64 字符串传递给页面
            ImageClicked?.Invoke(this, imageControl.Source.ToString());

            // 设置描边颜色表示被选中
            outerBorder.BorderBrush = new SolidColorBrush(Colors.Blue);
        }
    }
}
