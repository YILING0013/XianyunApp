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
        private void SaveData()
        {
            // 数据保存逻辑
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
