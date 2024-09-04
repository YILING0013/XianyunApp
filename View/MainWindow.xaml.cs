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
using xianyun.Model;
using xianyun.ViewModel;

namespace xianyun.View
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        private Txt2imgPageViewModel _txt2ImgPageViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _mainViewModel = new MainViewModel();
            _txt2ImgPageViewModel = new Txt2imgPageViewModel();
            _mainViewModel.Txt2ImgPageViewModel = _txt2ImgPageViewModel;
            this.DataContext = _mainViewModel;
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

            if (DataContext is MainViewModel viewModel && viewModel.Txt2ImgPageViewModel != null)
            {
                viewModel.Txt2ImgPageViewModel.Txt2ImgPageModel.SaveParameters();
            }
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
