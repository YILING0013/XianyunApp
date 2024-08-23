using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace xianyun.ViewModel
{
    internal class MainViewModel
    {
        public ICommand CloseWindowCommand { get; }

        public MainViewModel()
        {
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
    }
}
