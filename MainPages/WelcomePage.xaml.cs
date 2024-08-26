using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using NLog;

namespace xianyun.MainPages
{
    /// <summary>
    /// WelcomePage.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomePage : Page
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // 用于定时刷新内容的定时器
        private DispatcherTimer refreshTimer;

        // 用于取消异步操作的取消令牌源
        private CancellationTokenSource cancellationTokenSource;

        // 默认文本，当文本加载失败且没有之前的文本时使用
        private const string DefaultText = "欢迎来到我的应用！";

        public WelcomePage()
        {
            InitializeComponent();

            // 初始化取消令牌源
            cancellationTokenSource = new CancellationTokenSource();

            // 加载初始背景和文本内容
            _ = LoadBackgroundAndTextWithFadeAsync(cancellationTokenSource.Token);

            // 启动定时刷新定时器
            StartRefreshTimer();

            // 在页面加载时订阅导航事件
            this.Loaded += WelcomePage_Loaded;
        }

        private void WelcomePage_Loaded(object sender, RoutedEventArgs e)
        {
            // 仅当 NavigationService 不为 null 时才订阅导航事件
            if (NavigationService != null)
            {
                NavigationService.Navigating += OnNavigatingFromPage;
            }
        }

        /// <summary>
        /// 异步加载背景图像和文本，并添加淡入淡出效果
        /// </summary>
        /// <param name="cancellationToken">用于取消操作的取消令牌</param>
        private async Task LoadBackgroundAndTextWithFadeAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 执行淡出动画
                await FadeOutAsync();

                // 检查是否请求取消操作
                if (cancellationToken.IsCancellationRequested)
                    return;

                // 定义图像和文本的URL
                string imageUrl = "https://t.alcy.cc/ycy";
                string textUrl = "https://v1.jinrishici.com/rensheng.txt";

                // 使用HttpClient加载图像
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        var imageStream = await client.GetStreamAsync(imageUrl);

                        // 检查是否请求取消操作
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // 创建并配置BitmapImage对象
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = imageStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        // 设置背景图像
                        BackgroundImageBrush.ImageSource = bitmap;
                    }
                    catch
                    {
                        // 如果图像加载失败且已有图像，则不改变
                        if (BackgroundImageBrush.ImageSource == null)
                        {
                            // 可以在此处设置一个默认图像（可选）
                        }
                    }
                }

                // 检查是否请求取消操作
                if (cancellationToken.IsCancellationRequested)
                    return;

                // 使用HttpClient加载文本
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        string hitokotoText = await client.GetStringAsync(textUrl);

                        // 检查是否请求取消操作
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // 设置文本内容
                        SubTextBlock.Text = hitokotoText;
                    }
                    catch
                    {
                        // 如果文本加载失败且已有文本，则不改变
                        if (string.IsNullOrEmpty(SubTextBlock.Text))
                        {
                            // 如果没有之前的文本，则使用默认文本
                            SubTextBlock.Text = DefaultText;
                        }
                    }
                }

                // 执行淡入动画
                await FadeInAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "加载背景图像和文本时发生错误");
            }
        }

        /// <summary>
        /// 执行淡出动画，将页面透明度从1降至0
        /// </summary>
        private async Task FadeOutAsync()
        {
            // 创建淡出动画，从完全不透明到完全透明，持续0.6秒
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(0.6),
                From = 1.0,
                To = 0.0
            };

            // 使用TaskCompletionSource等待动画完成
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            fadeOutAnimation.Completed += (s, e) => tcs.SetResult(true);

            // 将动画应用于页面的Opacity属性
            this.BeginAnimation(Page.OpacityProperty, fadeOutAnimation);

            // 等待动画完成
            await tcs.Task;
        }

        /// <summary>
        /// 执行淡入动画，将页面透明度从0升至1
        /// </summary>
        private async Task FadeInAsync()
        {
            // 创建淡入动画，从完全透明到完全不透明，持续0.6秒
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(0.6),
                From = 0.0,
                To = 1.0
            };

            // 使用TaskCompletionSource等待动画完成
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            fadeInAnimation.Completed += (s, e) => tcs.SetResult(true);

            // 将动画应用于页面的Opacity属性
            this.BeginAnimation(Page.OpacityProperty, fadeInAnimation);

            // 等待动画完成
            await tcs.Task;
        }

        /// <summary>
        /// 启动定时刷新定时器，每隔30秒刷新一次内容
        /// </summary>
        private void StartRefreshTimer()
        {
            // 初始化DispatcherTimer
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // 设置时间间隔为30秒
            };

            // 订阅Tick事件，每次触发时执行内容刷新
            refreshTimer.Tick += async (sender, args) =>
            {
                // 检查之前的取消令牌，若已取消则创建新的
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                await LoadBackgroundAndTextWithFadeAsync(cancellationTokenSource.Token);
            };

            // 启动定时器
            refreshTimer.Start();
        }

        /// <summary>
        /// 在导航离开当前页面时触发，取消所有正在进行的操作
        /// </summary>
        private void OnNavigatingFromPage(object sender, NavigatingCancelEventArgs e)
        {
            // 取消所有正在进行的操作
            cancellationTokenSource.Cancel();

            // 停止定时器
            refreshTimer?.Stop();

            // 取消订阅导航事件
            if (NavigationService != null)
            {
                NavigationService.Navigating -= OnNavigatingFromPage;
            }
        }

        /// <summary>
        /// 按钮点击事件处理，打开指定的URL
        /// </summary>
        private void Welcome_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://nai3.xianyun.cool/ordinance",
                UseShellExecute = true
            });
        }
    }
}
