using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace xianyun.UserControl
{
    public partial class ImgPreview
    {
        // 保存传入的 Base64 字符串
        private string _base64Image;
        // 定义图像点击事件
        public event EventHandler<string> ImageClicked;

        // 构造函数
        public ImgPreview(string base64Image)
        {
            InitializeComponent();
            _base64Image = base64Image;  // 保存 Base64 字符串
            SetImage(base64Image);

            // 注册删除按钮的点击事件
            deleteButton.Click += DeleteButton_Click;

            // 注册 ViewPreview_Bth 的点击事件，用于预览图像
            ViewPreview_Bth.Click += ViewPreview_Click;
        }

        // 设置图像的方法
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

        // 处理删除按钮点击事件
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 从父级容器中移除当前控件
            var parent = this.Parent as Panel;
            parent?.Children.Remove(this);
        }

        // ViewPreview_Bth 点击事件的处理方法
        private void ViewPreview_Click(object sender, RoutedEventArgs e)
        {
            // 使用存储的 Base64 字符串触发 ImageClicked 事件
            ImageClicked?.Invoke(this, _base64Image);
        }

        // 获取图像的 BitmapImage 对象
        public BitmapImage GetBitmapImage()
        {
            if (!string.IsNullOrEmpty(_base64Image))
            {
                var imageBytes = Convert.FromBase64String(_base64Image);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
            return null;
        }
    }
}
