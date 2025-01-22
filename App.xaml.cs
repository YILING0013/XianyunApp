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

        // 全局状态变量，标识用户是否已经登录
        public bool IsLoading { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 初始化 GlobalViewModel
            GlobalViewModel = new MainViewModel();
            GlobalViewModel.LoadParameters();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            var app = (App)Application.Current;

            // 仅在用户已登录时保存相关状态
            if (app.IsLoading)
            {
                GlobalViewModel?.SaveParameters();
            }
            base.OnExit(e);
        }
    }
}
