using HandyControl.Data;
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
using xianyun.MainPages;
using xianyun.ViewModel;

namespace xianyun.View
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //private MainViewModel _mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = App.GlobalViewModel;
            this.Loaded += (s, e) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (this.DataContext is MainViewModel viewModel)
                    {
                        viewModel.NavigateCommand.Execute("Welcome");
                        LogPage.LogMessage("程序启动成功");
                    }
                });
            };
            // 订阅窗口关闭事件
            this.Closing += MainWindow_Closing;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 在窗口关闭时调用保存数据的逻辑
            SaveData();
        }
        private void NotifyIcon_Settings_Click(object sender, RoutedEventArgs e)
        {
            // 打开设置窗口
        }
        private void NotifyIcon_CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            // 更新检查逻辑
        }
        private void SaveData()
        {
            // 数据保存逻辑
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource == sender)
            {
                this.DragMove();
            }
        }

        // 最小化按钮点击事件
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // 隐藏窗口
            notifyIcon.ShowBalloonTip("提示", "应用程序最小化到了系统托盘", NotifyIconInfoType.Info); // 显示气泡提示
        }

        // 托盘图标单击事件
        private void NotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            // 单击托盘图标时显示窗口
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // 托盘图标双击事件
        private void NotifyIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // 双击托盘图标时显示窗口
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // 托盘图标点击 "显示" 时的事件
        private void NotifyIcon_Show_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // 托盘图标点击 "退出" 时的事件
        private void NotifyIcon_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // 关闭应用程序
        }
    }
}
