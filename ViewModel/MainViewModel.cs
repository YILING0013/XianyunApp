using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using xianyun.Common;

namespace xianyun.ViewModel
{
    public class MainViewModel : NotifyBase
    {
        public ICommand NavigateCommand { get; }
        private void Navigate(string pageName)
        {
            var frame = Application.Current.MainWindow.FindName("MainWindowFrame") as Frame;

            if (frame != null)
            {
                switch (pageName)
                {
                    case "Welcome":
                        frame.Navigate(new MainPages.WelcomePage());
                        break;
                    case "Page1":
                        frame.Navigate(new MainPages.Txt2imgPage());
                        break;
                    case "Page2":
                        frame.Navigate(new MainPages.Img2ImgPage());
                        break;
                    case "Page3":
                        frame.Navigate(new MainPages.superResolutionPage());
                        break;
                    default:
                        break;
                }
            }
        }
        public ICommand CloseWindowCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
            Navigate("Welcome");
            CloseWindowCommand = new RelayCommand<Window>(async window =>
            {
                if (window != null)
                {
                    // 异步任务关闭窗口
                    await Task.Run(() =>
                    {
                        // 确保在 UI 线程上关闭窗口
                        window.Dispatcher.Invoke(() => window.Close());
                    });
                }
            });
        }
        public FrameworkElement _mainContent;
        public FrameworkElement MainConTent
        {
            get { return _mainContent; }
            set { _mainContent = value; this.DoNotify(); }
        }
    }
}
