using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace xianyun.UserControl
{
    public partial class ImgPreview
    {
        // 定义图像点击事件
        public event EventHandler<string> ImageClicked;

        // 构造函数
        public ImgPreview(string base64Image)
        {
            InitializeComponent();
            SetImage(base64Image);

            // 注册删除按钮的点击事件
            deleteButton.Click += DeleteButton_Click;

            // 直接为 Image 注册点击事件
            imageControl.MouseLeftButtonUp += ImgPreview_MouseLeftButtonUp;

            // 输出日志确认事件已注册
            Console.WriteLine("MouseLeftButtonUp event registered.");
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

        // 图像点击事件的处理方法
        private void ImgPreview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 触发 ImageClicked 事件并传递图像的 base64 数据
            ImageClicked?.Invoke(this, imageControl.Source.ToString());

            // 设置描边颜色表示选中状态
            outerBorder.BorderBrush = new SolidColorBrush(Colors.Blue);

            // 确保事件继续传播
            e.Handled = false;
        }
    }
}
