using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using xianyun.ViewModel;
using System.Diagnostics;

namespace xianyun.View
{
    /// <summary>
    /// Loginview.xaml 的交互逻辑
    /// </summary>
    public partial class Loginview : Window
    {
        public Loginview()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel();
        }
        private void Win_MoveleftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton== MouseButtonState.Pressed)
                this.DragMove();
        }
        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string url)
            {
                // 打开指定的URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
    }
}
