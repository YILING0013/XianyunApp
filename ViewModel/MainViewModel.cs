using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xianyun.API;
using xianyun.Common;
using xianyun.UserControl;
using static xianyun.MainPages.Txt2imgPage;

namespace xianyun.ViewModel
{
    public class MainViewModel : NotifyBase
    {
        private bool _isCreatingZipVisible = false;
        private bool _isEmotionVisible=false;
        private bool _isColorizeVisible=false;
        private bool _isInkCanvasVisible = false;
        private double _progressValue = 0;
        private double _createZipProgressValue = 0;
        private string _model;
        private string _reqType=null;
        private int _emotionDefry = 0;
        private int _colorizeDefry = 0;
        private int _drawingFrequency = 1;
        private int _steps = 28;
        private long? _seed = null;
        private int _width = 1024;
        private int _height = 1024;
        private bool _isConvenientResolution = false;
        private bool _isVariety= false;
        private bool _isDecrisp = false;
        private bool _isSMEA = false;
        private bool _isDYN = false;
        private bool _isDYNEnabled = false;
        private float _guidanceScale = 5.0f;
        private float _guidanceRescale = 0.0f;
        private float _strength = 0.70f;
        private float _noise = 0.00f;
        private string _samplingMethod;
        private string _emotion;
        private string _resolution;
        private string _noiseSchedule;
        private bool _selectedLineArt;
        private bool _selectedSketch;
        private bool _selectedDeclutter;
        private bool _selectedEmotion;
        private bool _selectedColorize;
        private string _positivePrompt;
        private string _negitivePrompt = "lowres, {bad}, error, fewer, extra, missing, worst quality, jpeg artifacts, bad quality, watermark, unfinished, displeasing, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract]";
        private string _emotionPrompt = null;
        private string _colorizePrompt = null;
        public readonly string _secretKey = "XianyunWebSite";
        SolidColorBrush _SelectColor = Brushes.White;
        private int _brushHeight = 20;
        private int _brushWidth = 20;
        private bool _isIgnorePenPressure = true;

        private readonly Dictionary<string, string> _samplingMethodMapping = new Dictionary<string, string>
        {
            { "Euler", "k_euler" },
            { "Euler Ancestral", "k_euler_ancestral" },
            { "DPM++ 2S Ancestral", "k_dpmpp_2s_ancestral" },
            { "DPM++ SDE", "k_dpmpp_sde" },
            { "DPM++ 2M", "k_dpmpp_2m" },
            { "DDIM", "ddim_v3" }
        };
        // Emotion 与其值的反向映射词典
        private readonly Dictionary<string, string> _emotionMapping = new Dictionary<string, string>
        {
            { "中立", "neutral" },
            { "开心", "happy" },
            { "悲伤", "sad" },
            { "愤怒", "angry" },
            { "惊讶", "surprised" },
            { "厌恶", "disgusted" },
            { "害怕", "scared" },
            { "困惑", "confused" },
            { "疲倦", "tired" },
            { "兴奋", "excited" },
            { "尴尬", "embarrassed" },
            { "害羞", "shy" },
            { "得意", "smug" },
            { "坚定", "determined" },
            { "无聊", "bored" },
            { "思考", "thinking" },
            { "紧张", "nervous" },
            { "大笑", "laughing" },
            { "恼火", "irritated" },
            { "激动", "aroused" },
            { "担忧", "worried" },
            { "恋爱", "love" },
            { "痛苦", "hurt" },
            { "调皮", "playful" }
        };

        private readonly Dictionary<string, string> _reverseSamplingMethodMapping;
        private readonly Dictionary<string, string> _reverseEmotionMapping;
        public List<string> Emotions { get; set; } = new List<string>{"中立","开心","悲伤","愤怒","惊讶","厌恶","害怕","困惑","疲倦","兴奋","尴尬","害羞","得意","坚定","无聊","思考","紧张","大笑","恼火","激动","担忧","恋爱","痛苦","调皮"};
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
        private ObservableCollection<NoteModel> _notes;
        public ObservableCollection<NoteModel> Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                this.DoNotify();
            }
        }
        // 构造函数
        public MainViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
            _notes = new ObservableCollection<NoteModel>();
            // 初始化反向映射字典
            _reverseSamplingMethodMapping = _samplingMethodMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            _reverseEmotionMapping = _emotionMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Model = Models[0];
            SamplingMethod = SamplingMethods[0];
            Emotion = Emotions[0];
            Resolution = Resolutions[0];
            NoiseSchedule = NoiseSchedules[0];
            CloseWindowCommand = new RelayCommand<System.Windows.Window>(async window =>
            {
                SaveParameters();
                if (window != null)
                {
                    await Task.Run(() =>
                    {
                        window.Dispatcher.Invoke(() => window.Close());
                    });
                }
            });
        }
        private Dictionary<string, Page> _pageCache = new Dictionary<string, Page>();
        public void Navigate(string pageName)
        {
            var frame = Application.Current.MainWindow.FindName("MainWindowFrame") as Frame;

            if (frame != null)
            {
                Page page;
                if (_pageCache.ContainsKey(pageName))
                {
                    // 如果页面已经缓存，直接使用缓存的页面实例
                    page = _pageCache[pageName];
                }
                else
                {
                    // 否则创建新页面并缓存
                    switch (pageName)
                    {
                        case "Welcome":
                            page = new MainPages.WelcomePage();
                            break;
                        case "Page1":
                            page = new MainPages.Txt2imgPage();
                            break;
                        case "Page2":
                            page = new MainPages.Img2ImgPage();
                            break;
                        case "Page3":
                            page = new MainPages.superResolutionPage();
                            break;
                        default:
                            return;
                    }

                    _pageCache[pageName] = page;  // 将页面实例缓存
                }

                // 导航到目标页面
                frame.Navigate(page);
            }
        }
        public string ReqType
        {
            get => _reqType;
            set
            {
                _reqType = value;
                DoNotify();
            }
        }
        // 新增方法来更新 ReqType
        private void UpdateReqType()
        {
            if (SelectedLineArt)
            {
                ReqType = "lineart";
            }
            else if (SelectedSketch)
            {
                ReqType = "sketch";
            }
            else if (SelectedDeclutter)
            {
                ReqType = "declutter";
            }
            else if (SelectedEmotion)
            {
                ReqType = "emotion";
            }
            else if (SelectedColorize)
            {
                ReqType = "colorize";
            }
            else
            {
                ReqType = null; // 当没有选中的控件时，设置 ReqType 为 null
            }
        }
        public bool IsCreatingZipVisible
        {
            get => _isCreatingZipVisible;
            set
            {
                _isCreatingZipVisible = value;
                DoNotify();
            }
        }
        public bool IsInkCanvasVisible
        {
            get => _isInkCanvasVisible;
            set
            {
                _isInkCanvasVisible = value;
                DoNotify();
            }
        }
        public bool IsEmotionVisible
        {
            get => _isEmotionVisible;
            set
            {
                _isEmotionVisible = value;
                DoNotify();
            }
        }
        public bool IsColorizeVisible
        {
            get => _isColorizeVisible;
            set
            {
                _isColorizeVisible = value;
                DoNotify();
            }
        }
        private void UpdateDynamicRowVisibility()
        {
            // 仅当SelectedEmotion或SelectedColorize为true时，才显示控件
            IsEmotionVisible = SelectedEmotion;
            IsColorizeVisible = SelectedColorize;
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
        public double CreateZipProgressValue
        {
            get => _createZipProgressValue;
            set
            {
                _createZipProgressValue = value;
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
        public string Emotion_Prompt
        {
            get => _emotionPrompt;
            set
            {
                if (_emotionPrompt != value)
                {
                    _emotionPrompt = value;
                    DoNotify();
                }
            }
        }
        public string Colorize_Prompt
        {
            get => _colorizePrompt;
            set
            {
                if (_colorizePrompt != value)
                {
                    _colorizePrompt = value;
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
                    UpdateDynamicRowVisibility(); // 更新是否显示控件
                    UpdateReqType();
                }
                else 
                { 
                    _selectedLineArt = !value;
                    DoNotify();
                    UpdateReqType();
                }
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
                    UpdateDynamicRowVisibility(); // 更新是否显示控件
                    UpdateReqType();
                }
                else 
                { 
                    _selectedSketch = !value; 
                    DoNotify();
                    UpdateReqType();
                }
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
                    UpdateDynamicRowVisibility(); // 更新是否显示控件
                    UpdateReqType();
                }
                else 
                { 
                    _selectedDeclutter = !value; 
                    DoNotify();
                    UpdateReqType();
                }
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
                    UpdateDynamicRowVisibility(); // 更新是否显示控件
                    UpdateReqType();
                }
                else 
                { 
                    _selectedEmotion = !value; 
                    DoNotify();
                    UpdateDynamicRowVisibility();
                    UpdateReqType();
                }
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
                    UpdateDynamicRowVisibility(); // 更新是否显示控件
                    UpdateReqType();
                }
                else
                { 
                    _selectedColorize = !value;
                    DoNotify();
                    UpdateDynamicRowVisibility();
                    UpdateReqType();
                }
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
        public int Emotion_Defry
        {
            get => _emotionDefry;
            set
            {
                _emotionDefry = value;
                DoNotify();
            }
        }
        public int Colorize_Defry
        {
            get => _colorizeDefry;
            set
            {
                _colorizeDefry = value;
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
        public bool IsVariety
        {
            get => _isVariety;
            set
            {
                _isVariety = value;
                DoNotify();
            }
        }
        public bool IsDecrisp
        {
            get => _isDecrisp;
            set
            {
                _isDecrisp = value;
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
        public float Strength
        {
            get => _strength;
            set
            {
                _strength = (float)Math.Round(value, 2); // 保留两位小数
                DoNotify();
            }
        }
        public float Noise
        {
            get => _noise;
            set
            {
                _noise = (float)Math.Round(value, 2); // 保留两位小数
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
        public string Emotion
        {
            get => _emotion;
            set
            {
                if (_emotion != value)
                {
                    _emotion = value;
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

        public SolidColorBrush SelectColor
        {
            get => _SelectColor;
            set
            {
                _SelectColor = value;
                DoNotify();
            }
        }

        public int BrushHeight
        {
            get => _brushHeight;
            set
            {
                _brushHeight = value;
                DoNotify();
            }
        }

        public int BrushWidth
        {
            get => _brushWidth;
            set
            {
                _brushWidth = value;
                DoNotify();
            }
        }

        public bool IsIgnorePenPressure
        {
            get => _isIgnorePenPressure;
            set
            {
                _isIgnorePenPressure = value;
                DoNotify();
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
            this.Emotion = loadedConfig.Emotion;
            this.Resolution = loadedConfig.Resolution;
            this.NoiseSchedule = loadedConfig.NoiseSchedule;
            this.SelectedLineArt = loadedConfig.SelectedLineArt;
            this.SelectedSketch = loadedConfig.SelectedSketch;
            this.SelectedDeclutter = loadedConfig.SelectedDeclutter;
            this.SelectedEmotion = loadedConfig.SelectedEmotion;
            this.SelectedColorize = loadedConfig.SelectedColorize;
            this.NegitivePrompt = loadedConfig.NegitivePrompt;
            this.PositivePrompt = loadedConfig.PositivePrompt;
            this.Notes = loadedConfig.Notes;
            this.BrushWidth= loadedConfig.BrushWidth;
            this.BrushHeight = loadedConfig.BrushHeight;
            this.SelectColor = loadedConfig.SelectColor;
        }

        // 保存参数的方法
        public void SaveParameters()
        {
            ConfigurationService.SaveConfiguration(this);
        }

        // 图像点击事件的处理方法
        public void OnImageClicked(object sender, string base64Image)
        {
            var bitmapFrame = Common.tools.ConvertBase64ToBitmapFrame(base64Image);
            ImageViewerControl.ImageSource = bitmapFrame;
        }

        // 获取实际的采样方法
        public string ActualSamplingMethod => _samplingMethodMapping.TryGetValue(SamplingMethod, out var actualMethod) ? actualMethod : null;
        public string ActualEmotion
        {
            get
            {
                if (_emotionMapping.TryGetValue(Emotion, out var actualEmotion))
                {
                    System.Diagnostics.Debug.WriteLine($"ActualEmotion: {actualEmotion}");
                    return actualEmotion;
                }
                System.Diagnostics.Debug.WriteLine("ActualEmotion: null");
                return null;
            }
        }
    }
}
