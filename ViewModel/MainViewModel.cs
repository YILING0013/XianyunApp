using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using xianyun.API;
using xianyun.Common;
using xianyun.UserControl;

namespace xianyun.ViewModel
{
    public class MainViewModel : NotifyBase
    {
        // 从 Txt2imgPageModel 导入的字段
        private double _progressValue = 0;
        private string _model;
        private int _drawingFrequency = 1;
        private int _steps = 28;
        private long? _seed = null;
        private int _width = 1024;
        private int _height = 1024;
        private bool _isConvenientResolution = false;
        private bool _isSMEA = false;
        private bool _isDYN = false;
        private bool _isDYNEnabled = false;
        private float _guidanceScale = 5.0f;
        private float _guidanceRescale = 0.0f;
        private string _samplingMethod;
        private string _resolution;
        private string _noiseSchedule;
        private bool _selectedLineArt;
        private bool _selectedSketch;
        private bool _selectedDeclutter;
        private bool _selectedEmotion;
        private bool _selectedColorize;
        private string _positivePrompt;
        private string _negitivePrompt = "lowres, {bad}, error, fewer, extra, missing, worst quality, jpeg artifacts, bad quality, watermark, unfinished, displeasing, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract]";

        // 密钥
        private readonly string _secretKey = "fGCGrffh$*hdr#(7904-(cSGDTJGTCLOPIYSWQSFESADZFDBJ%+)):m;(&@1#+$*hHBBB23$c(&#46&(890*@2$%&c#5$#2147905*&/MJMMLLPwr#fhdefts&dcr24x#g4*r@3&(uourw1fcgd-5cdgc$-4fhfxf+dvhvd#d*xe#&frzxhg&efxgthd@2vdffhr*ts2#g4cr#f3xffde@3$ffsxdvh4swa$gr$grOJHNBUGVCDAssddd$ss4+f5$s23xgv(/njvd1d4+g7g$213yfdh*$j*QZDEHg";

        private readonly Dictionary<string, string> _samplingMethodMapping = new Dictionary<string, string>
        {
            { "Euler", "k_euler" },
            { "Euler Ancestral", "k_euler_ancestral" },
            { "DPM++ 2S Ancestral", "k_dpmpp_2s_ancestral" },
            { "DPM++ SDE", "k_dpmpp_sde" },
            { "DPM++ 2M", "k_dpmpp_2m" },
            { "DDIM", "ddim_v3" }
        };
        private readonly Dictionary<string, string> _reverseSamplingMethodMapping;
        public List<string> Models { get; set; } = new List<string> { "nai-diffusion-3", "nai-diffusion-furry-3" };
        public List<string> SamplingMethods { get; set; } = new List<string> { "Euler", "Euler Ancestral", "DPM++ 2S Ancestral", "DPM++ SDE", "DPM++ 2M", "DDIM" };
        public List<string> Resolutions { get; set; } = new List<string> { "1024*1024", "1216*832", "832*1216" };
        public List<string> NoiseSchedules { get; set; } = new List<string> { "native", "karras", "exponential", "polyexponential" };
        // 其他从 MainViewModel 导入的属性和命令
        public ICommand NavigateCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand GenerateImageCommand { get; set; }
        [JsonIgnore]
        public System.Windows.Controls.ScrollViewer ImgPreviewArea { get; set; }
        [JsonIgnore]
        public StackPanel ImageStackPanel { get; set; }
        [JsonIgnore]
        public ImageViewer ImageViewerControl { get; set; }
        public FrameworkElement _mainContent;
        public FrameworkElement MainConTent
        {
            get { return _mainContent; }
            set { _mainContent = value; this.DoNotify(); }
        }

        // 构造函数
        public MainViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
            // 初始化反向映射字典
            _reverseSamplingMethodMapping = _samplingMethodMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Model = Models[0];
            SamplingMethod = SamplingMethods[0];
            Resolution = Resolutions[0];
            NoiseSchedule = NoiseSchedules[0];
            CloseWindowCommand = new RelayCommand<System.Windows.Window>(async window =>
            {
                if (window != null)
                {
                    await Task.Run(() =>
                    {
                        window.Dispatcher.Invoke(() => window.Close());
                    });
                }
            });

            GenerateImageCommand = new AsyncRelayCommand(OnGenerateButtonClick);
        }
        public void Navigate(string pageName)
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
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                DoNotify();
            }
        }
        public string PositivePrompt
        {
            get => _positivePrompt;
            set
            {
                if (_positivePrompt != value)
                {
                    _positivePrompt = value;
                    DoNotify();
                }
            }
        }
        public string NegitivePrompt
        {
            get => _negitivePrompt;
            set
            {
                if (_negitivePrompt != value)
                {
                    _negitivePrompt = value;
                    DoNotify();
                }
            }
        }
        public bool SelectedLineArt
        {
            get => _selectedLineArt;
            set
            {
                if (_selectedLineArt != value || value == false)
                {
                    _selectedLineArt = value;
                    DoNotify();
                }
                else { _selectedLineArt = !value; DoNotify(); }
            }
        }
        public bool SelectedSketch
        {
            get => _selectedSketch;
            set
            {
                if (_selectedSketch != value || value == false)
                {
                    _selectedSketch = value;
                    DoNotify();
                }
                else { _selectedSketch = !value; DoNotify(); }
            }
        }
        public bool SelectedDeclutter
        {
            get => _selectedDeclutter;
            set
            {
                if (_selectedDeclutter != value || value == false)
                {
                    _selectedDeclutter = value;
                    DoNotify();
                }
                else { _selectedDeclutter = !value; DoNotify(); }
            }
        }
        public bool SelectedEmotion
        {
            get => _selectedEmotion;
            set
            {
                if (_selectedEmotion != value || value == false)
                {
                    _selectedEmotion = value;
                    DoNotify();
                }
                else { _selectedEmotion = !value; DoNotify(); }
            }
        }
        public bool SelectedColorize
        {
            get => _selectedColorize;
            set
            {
                if (_selectedColorize != value || value == false)
                {
                    _selectedColorize = value;
                    DoNotify();
                }
                else { _selectedColorize = !value; DoNotify(); }
            }
        }
        public int DrawingFrequency
        {
            get => _drawingFrequency;
            set
            {
                _drawingFrequency = value;
                DoNotify();
            }
        }
        public int Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                DoNotify();
            }
        }
        public long? Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                DoNotify();
                System.Diagnostics.Debug.WriteLine("Seed: " + _seed);
            }
        }
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                DoNotify();
            }
        }
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                DoNotify();
            }
        }
        public bool IsConvenientResolution
        {
            get => _isConvenientResolution;
            set
            {
                _isConvenientResolution = value;
                DoNotify();
            }
        }
        public bool IsSMEA
        {
            get => _isSMEA;
            set
            {
                _isSMEA = value;
                IsDYNEnabled = _isSMEA;
                if (!_isSMEA)
                {
                    IsDYN = false;
                }
                DoNotify();
            }
        }
        public bool IsDYNEnabled
        {
            get => _isDYNEnabled;
            set
            {
                _isDYNEnabled = value;
                DoNotify();
            }
        }
        public bool IsDYN
        {
            get => _isDYN;
            set
            {
                _isDYN = value;
                DoNotify();
            }
        }
        public float GuidanceScale
        {
            get => _guidanceScale;
            set
            {
                _guidanceScale = (float)Math.Round(value, 2); // 保留两位小数
                DoNotify();
            }
        }
        public float GuidanceRescale
        {
            get => _guidanceRescale;
            set
            {
                _guidanceRescale = (float)Math.Round(value, 2); // 保留两位小数
                DoNotify();
            }
        }
        public string Model
        {
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    DoNotify();
                }
            }
        }

        public string SamplingMethod
        {
            get => _samplingMethod;
            set
            {
                if (_samplingMethod != value)
                {
                    _samplingMethod = value;
                    DoNotify();
                }
            }
        }

        public string Resolution
        {
            get => _resolution;
            set
            {
                if (_resolution != value)
                {
                    _resolution = value;
                    DoNotify();
                }
            }
        }

        public string NoiseSchedule
        {
            get => _noiseSchedule;
            set
            {
                if (_noiseSchedule != value)
                {
                    _noiseSchedule = value;
                    DoNotify();
                }
            }
        }

        // 加载参数的方法
        public void LoadParameters()
        {
            var loadedConfig = ConfigurationService.LoadConfiguration<MainViewModel>();

            // 将加载的参数应用到当前对象
            this.Model = loadedConfig.Model;
            this.DrawingFrequency = loadedConfig.DrawingFrequency;
            this.Steps = loadedConfig.Steps;
            this.Seed = loadedConfig.Seed;
            this.Width = loadedConfig.Width;
            this.Height = loadedConfig.Height;
            this.IsConvenientResolution = loadedConfig.IsConvenientResolution;
            this.IsSMEA = loadedConfig.IsSMEA;
            this.IsDYN = loadedConfig.IsDYN;
            this.IsDYNEnabled = loadedConfig.IsDYNEnabled;
            this.GuidanceScale = loadedConfig.GuidanceScale;
            this.GuidanceRescale = loadedConfig.GuidanceRescale;
            this.SamplingMethod = loadedConfig.SamplingMethod;
            this.Resolution = loadedConfig.Resolution;
            this.NoiseSchedule = loadedConfig.NoiseSchedule;
            this.SelectedLineArt = loadedConfig.SelectedLineArt;
            this.SelectedSketch = loadedConfig.SelectedSketch;
            this.SelectedDeclutter = loadedConfig.SelectedDeclutter;
            this.SelectedEmotion = loadedConfig.SelectedEmotion;
            this.SelectedColorize = loadedConfig.SelectedColorize;
            this.NegitivePrompt = loadedConfig.NegitivePrompt;
            this.PositivePrompt = loadedConfig.PositivePrompt;
        }

        // 保存参数的方法，来自 Txt2imgPageModel
        public void SaveParameters()
        {
            ConfigurationService.SaveConfiguration(this);
        }

        // 生成图像的命令方法，来自 MainViewModel
        private async Task OnGenerateButtonClick()
        {
            try
            {
                var apiClient = new XianyunApiClient("https://nai3.xianyun.cool", SessionManager.Session);
                Console.WriteLine(SessionManager.Session);

                // 用于生成随机种子的函数
                long GenerateRandomSeed()
                {
                    var random = new Random();
                    int length = random.Next(9, 13); // 生成9到12位长度的随机数
                    long seed = 0;
                    for (int i = 0; i < length; i++)
                    {
                        seed = seed * 10 + random.Next(0, 10);
                    }
                    return seed;
                }

                // 循环生成图像请求
                for (int i = 0; i < this.DrawingFrequency; i++)
                {
                    var seedValue = this.Seed?.ToString() ?? GenerateRandomSeed().ToString();

                    var imageRequest = new ImageGenerationRequest
                    {
                        Model = this.Model,
                        PositivePrompt = this.PositivePrompt,
                        NegativePrompt = this.NegitivePrompt,
                        Scale = this.GuidanceScale,
                        Steps = this.Steps,
                        Width = this.Width,
                        Height = this.Height,
                        PromptGuidanceRescale = this.GuidanceRescale,
                        NoiseSchedule = this.NoiseSchedule,
                        Seed = seedValue,  // 使用新的随机种子
                        Sampler = this.ActualSamplingMethod,
                        Sm = this.IsSMEA,
                        SmDyn = this.IsDYN,
                        PictureId = TotpGenerator.GenerateTotp(_secretKey)
                    };

                    var (jobId, initialQueuePosition) = await apiClient.GenerateImageAsync(imageRequest);
                    Console.WriteLine($"任务已提交，任务ID: {jobId}, 初始队列位置: {initialQueuePosition}");

                    int currentQueuePosition = initialQueuePosition;
                    ProgressValue = 0;

                    while (currentQueuePosition > 0)
                    {
                        var (status, imageBase64, queuePosition) = await apiClient.CheckResultAsync(jobId);
                        if (status == "processing")
                        {
                            // 队列已到0，进入processing状态，生成图像中
                            ProgressValue = 70;
                            Console.WriteLine($"进度: {ProgressValue}% (正在生成图像)");
                            currentQueuePosition = queuePosition;
                        }
                        else if (status == "queued")
                        {
                            // 根据队列位置更新进度
                            ProgressValue = 70 * (1 - (double)queuePosition / initialQueuePosition);
                            Console.WriteLine($"进度: {ProgressValue}% (队列位置: {queuePosition})");
                            currentQueuePosition = queuePosition;
                        }
                        await Task.Delay(2000); // 每2秒检查一次
                    }

                    // 当状态为processing时，模拟从70%到96%的进度
                    while (ProgressValue < 96)
                    {
                        ProgressValue += new Random().Next(1, 4);
                        Console.WriteLine($"进度: {ProgressValue}%");
                        await Task.Delay(1500);
                    }

                    // 检查状态直到生成完成
                    while (true)
                    {
                        var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                        if (status == "completed")
                        {
                            ProgressValue = 100;
                            Console.WriteLine("图像生成成功！");

                            var bitmapFrame = ConvertBase64ToBitmapFrame(imageBase64);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var imgPreview = new ImgPreview(imageBase64);
                                imgPreview.ImageClicked += OnImageClicked;
                                ImageStackPanel.Children.Add(imgPreview);
                                ImageViewerControl.ImageSource = bitmapFrame;
                            });
                            break;
                        }
                        await Task.Delay(2000); // 每2秒检查一次生成状态
                    }

                    Console.WriteLine("进度: 100%");
                    await Task.Delay(3000); // 请求间隔3秒
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("错误: " + ex.Message);
            }
        }
        // 将 Base64 字符串转换为 BitmapFrame 的方法
        private BitmapFrame ConvertBase64ToBitmapFrame(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return BitmapFrame.Create(bitmapImage);
        }

        // 图像点击事件的处理方法
        private void OnImageClicked(object sender, string base64Image)
        {
            var bitmapFrame = ConvertBase64ToBitmapFrame(base64Image);
            ImageViewerControl.ImageSource = bitmapFrame;
        }

        // 获取实际的采样方法
        private string ActualSamplingMethod => _samplingMethodMapping.TryGetValue(SamplingMethod, out var actualMethod) ? actualMethod : null;
    }
}
