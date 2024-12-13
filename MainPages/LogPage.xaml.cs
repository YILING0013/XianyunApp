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

namespace xianyun.MainPages
{

    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR
    }

    public static class LogColor // 将 internal 改为 public
    {
        public static SolidColorBrush GetColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.INFO:
                    return Brushes.Green;  // 信息用绿色
                case LogLevel.WARNING:
                    return Brushes.Orange; // 警告用橙色
                case LogLevel.ERROR:
                    return Brushes.Red;    // 错误用红色
                default:
                    return Brushes.Black;
            }
        }
    }

    /// <summary>
    /// LogPage.xaml 的交互逻辑
    /// </summary>
    public partial class LogPage : Page
    {
        // 静态单例
        private static LogPage _instance;

        // 单例实例属性
        public static LogPage Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LogPage();
                }
                return _instance;
            }
        }

        private LogPage()
        {
            InitializeComponent();
        }

        // 静态方法：记录日志
        public static void LogMessage(LogLevel level, string message)
        {
            Instance.AppendLog(level, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public static void LogMessage(string message)
        {
            LogMessage(LogLevel.INFO, message);
        }

        // 私有方法：追加日志
        private void AppendLog(LogLevel level, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Dispatcher.Invoke(() =>
                {
                    // 为该条日志创建一个新段落
                    Paragraph paragraph = new Paragraph();
                    Run run = new Run(message)
                    {
                        Foreground = LogColor.GetColor(level) // 根据日志级别设置颜色
                    };
                    paragraph.Inlines.Add(run);

                    // 将该段落添加到 RichTextBox 的文档末尾
                    LogTextBox.Document.Blocks.Add(paragraph);
                    LogTextBox.ScrollToEnd();
                });
            }
        }
    }
}
