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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace xianyun.View
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AdjustWindowSize();
        }
        // 调整窗口大小的函数
        private void AdjustWindowSize()
        {
            // 获取设备的屏幕宽度和高度
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // 设置窗口的宽度和高度，略小于屏幕分辨率
            this.Width = GetAdjustedWidth(screenWidth);
            this.Height = GetAdjustedHeight(screenHeight);

            // 窗口位置居中
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // 根据屏幕宽度获取调整后的窗口宽度
        private double GetAdjustedWidth(double screenWidth)
        {
            return screenWidth * 0.9;
        }

        // 根据屏幕高度获取调整后的窗口高度
        private double GetAdjustedHeight(double screenHeight)
        {
            return screenHeight * 0.9;
        }
    }
}
