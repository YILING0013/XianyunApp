using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using xianyun.View;
using xianyun.ViewModel;

namespace xianyun
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static MainViewModel GlobalViewModel { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 初始化 GlobalViewModel
            GlobalViewModel = new MainViewModel();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            // 在程序退出时保存参数
            GlobalViewModel?.SaveParameters();
            base.OnExit(e);
        }
    }
}
