using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xianyun.Common;
using xianyun.ViewModel;

namespace xianyun.MainPages
{
    /// <summary>
    /// SettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPage : Page
    {
        private MainViewModel _viewModel;
        public SettingPage()
        {
            _viewModel = App.GlobalViewModel;
            this.DataContext = _viewModel;
            this.Loaded += SettingPage_Loaded;
            InitializeComponent();
            if (string.IsNullOrEmpty(SessionManager.Session) && !string.IsNullOrEmpty(SessionManager.Token))
            {
                NovelAIUserSettings.IsEnabled = true;
            }
            else { 
                _viewModel.DrawingMaxFrequency = 10;
            }
            CustomPrefixGroup.Visibility = _viewModel.NamingRule == _viewModel.NamingRules[1] ? Visibility.Visible : Visibility.Collapsed;
        }
        private void SettingPage_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (ConfigurationService.ConfigurationExists())
            {
                // 设置全局登录状态为已登录
                var app = (App)System.Windows.Application.Current;
                app.IsLoading = true;
                _viewModel.SaveParameters();
            }
        }
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用 FolderBrowserDialog 替代 FolderPicker
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                _viewModel.SaveDirectory = folderDialog.SelectedPath;
            }
        }

        private void NamingRuleComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (CustomPrefixGroup == null) return; // 如果控件未初始化，直接返回

            if (NamingRuleComboBox.SelectedIndex == 1) // 如果选择了自定义前缀
            {
                CustomPrefixGroup.Visibility = Visibility.Visible;
            }
            else
            {
                CustomPrefixGroup.Visibility = Visibility.Collapsed;
            }
        }
    }
}
