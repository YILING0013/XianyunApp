using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
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
using xianyun.UserControl;
using xianyun.ViewModel;
using System.IO;
using System.IO.Compression;
using System.Windows.Media.Animation;
using xianyun.Common;
using xianyun.API;
using Newtonsoft.Json;
using System.Windows.Ink;
using System.Windows.Controls.Primitives;
using AduSkin.Utility.Media;

namespace xianyun.MainPages
{
    /// <summary>
    /// Txt2imgPage.xaml 的交互逻辑
    /// </summary>
    public partial class Txt2imgPage : Page
    {
        private bool dragInProgress = false;
        private bool isTagMenuOpen = false;

        private Stack<Stroke> UndoStack = new Stack<Stroke>();
        private Stack<Stroke> RedoStack = new Stack<Stroke>();

        // Transformations
        private TransformGroup panZoomTransformGroup;
        private ScaleTransform panZoomScaleTransform;
        private TranslateTransform panZoomTranslateTransform;

        // Mouse interaction
        private Point lastMousePosition;
        private bool isPanning = false;
        private bool isPanZoomMode = false;

        private double H = 0;
        private double S = 1;
        private double B = 1;

        private int R = 255;
        private int G = 255;
        private int _B = 255;
        private int A = 255;

        private BitmapImage originalImage;
        RenderTargetBitmap maskImage;
        private DragAdorner currentAdorner;
        private MainViewModel _viewModel;
        public Txt2imgPage()
        {
            InitializeComponent();
            _viewModel = App.GlobalViewModel;
            this.DataContext = _viewModel;
            this.Loaded += Txt2imgPage_Loaded;
            this.Unloaded += Txt2imgPage_Unloaded;
            inkCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = (Color)ColorConverter.ConvertFromString(TextHex.Text),
                Height = _viewModel.BrushHeight, // 画笔高度
                Width = _viewModel.BrushWidth,  // 画笔宽度
                IgnorePressure = _viewModel.IsIgnorePenPressure // 忽略笔压
            };
            inkCanvas.Background = Brushes.Transparent; // 背景透明
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;  // 默认绘制模式
            inkCanvas.UseCustomCursor = true;
            inkCanvas.Cursor = Cursors.Cross;
            inkCanvas.IsHitTestVisible = false;
        }
        /// <summary>
        /// HexTextBox的回车事件，触发LoseFocus使颜色值生效
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否按下回车键
            if (e.Key == Key.Enter)
            {
                HexTextLostFocus(sender, null);
                (sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }
        /// <summary>
        /// RGBA颜色值的文本框回车事件，触发LoseFocus使颜色值生效
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否按下回车键
            if (e.Key == Key.Enter)
            {
                TextBox_LostFocus(sender, null);
                (sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }

        /// <summary>
        /// EmotionDefry的点击事件，当点击时，将Emotion的Defry降低；低于0时，禁用减号按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmotionDefryReduce_Click(object sender, RoutedEventArgs e)
        {
            // 确保 Defry 值不能小于 0
            if (_viewModel.Emotion_Defry > 0)
            {
                _viewModel.Emotion_Defry--;
                UpdateDefryGrade();
            }

            // 禁用 EmotionDefryReduce 按钮当 Defry 为 0
            EmotionDefryReduce.IsEnabled = _viewModel.Emotion_Defry > 0;

            // 确保当 Defry 小于 5 时，EmotionDefryPlus 按钮始终启用
            EmotionDefryPlus.IsEnabled = _viewModel.Emotion_Defry < 5;
        }

        /// <summary>
        /// EmotionDefry的点击事件，当点击时，将Emotion的Defry增加；高于5时，禁用加号按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmotionDefryPlus_Click(object sender, RoutedEventArgs e)
        {
            // 确保 Defry 值不能大于 5
            if (_viewModel.Emotion_Defry < 5)
            {
                _viewModel.Emotion_Defry++;
                UpdateDefryGrade();
            }

            // 禁用 EmotionDefryPlus 按钮当 Defry 为 5
            EmotionDefryPlus.IsEnabled = _viewModel.Emotion_Defry < 5;

            // 确保当 Defry 大于 0 时，EmotionDefryReduce 按钮始终启用
            EmotionDefryReduce.IsEnabled = _viewModel.Emotion_Defry > 0;
        }

        /// <summary>
        /// EmotionDefry的文本更新事件，随defry值的变化更新defry等级对应文本
        /// </summary>
        private void UpdateDefryGrade()
        {
            string[] grades = { "Normal", "Slightly Weak", "Weak", "Even Weaker", "Very Weak", "Weakest" };
            defryGrade.Text = grades[_viewModel.Emotion_Defry];
        }

        /// <summary>
        /// 宽度的调节滑块逻辑，当宽度值改变时，更新高度滑块的最大值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HeightSlider == null)
            {
                return; // 如果 HeightSlider 未初始化，直接返回，避免空引用错误
            }
            // 获取当前的宽度值
            double widthValue = e.NewValue;
            double maxHeight = CalculateMaxHeight(widthValue);

            // 设置 Height 滑条的最大值
            HeightSlider.Maximum = maxHeight;
            // 如果当前高度值大于新的最大高度，更新高度值
            if (_viewModel.Height > maxHeight)
            {
                _viewModel.Height = (int)maxHeight;
            }
        }

        /// <summary>
        /// 通过宽度值计算最大高度值
        /// </summary>
        /// <param name="widthValue"></param>
        /// <returns></returns>
        private double CalculateMaxHeight(double widthValue)
        {
            // 定义最小和最大边界值
            double minBound = 1011712;
            double maxBound = 1048576;
            double maxHeight = 512;
            double closestBelow = 512;
            double closestBelowDiff = double.MaxValue;

            // 遍历高度范围
            for (double x = 512; x <= 2048; x += 64)
            {
                double product = x * widthValue;
                if (product >= minBound && product <= maxBound)
                {
                    maxHeight = x;
                }
                else if (product < minBound)
                {
                    double diff = minBound - product;
                    if (diff < closestBelowDiff)
                    {
                        closestBelow = x;
                        closestBelowDiff = diff;
                    }
                }
            }

            // 返回最大高度值
            return (maxHeight != 512) ? maxHeight : closestBelow;
        }
        public class NoteModel
        {
            public string Name { get; set; }
            public string PositivePrompt { get; set; }
            public string NegativePrompt { get; set; }
            public string ImagePath { get; set; }
        }
        private void SearchInTreeView(string searchText)
        {
            // 清空 ListBox 中的现有项
            TagListBox.Items.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                return; // 如果搜索文本为空，则不进行任何操作
            }

            // 遍历 TreeView 中的所有节点
            foreach (TreeViewItem fileNode in TagTreeView.Items)
            {
                foreach (TreeViewItem categoryNode in fileNode.Items)
                {
                    if (categoryNode.Tag is Dictionary<string, string> dictionary)
                    {
                        // 遍历字典中的所有键值对
                        foreach (var keyValuePair in dictionary)
                        {
                            // 如果键或值包含搜索文本，将键（中文）添加到 ListBox 中
                            if (keyValuePair.Key.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                keyValuePair.Value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // 将键添加为 ListBox 项目，显示中文文本
                                TagListBox.Items.Add(new ListBoxItem
                                {
                                    Content = keyValuePair.Key, // 显示中文文本
                                    Tag = keyValuePair.Value    // 将值存储在 Tag 中以备后用
                                });
                            }
                        }
                    }
                }
            }

            // 如果没有找到匹配项，显示一个提示
            if (TagListBox.Items.Count == 0)
            {
                TagListBox.Items.Add(new ListBoxItem { Content = "未找到匹配的结果。" });
            }
        }
        // 当按下回车键时进行搜索
        private void AutoSuggestBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var searchBox = sender as Wpf.Ui.Controls.AutoSuggestBox;
                string searchText = searchBox.Text;

                // 调用搜索方法
                SearchInTreeView(searchText);
            }
        }
        // 当输入框失去焦点时进行搜索
        private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var searchBox = sender as Wpf.Ui.Controls.AutoSuggestBox;
            string searchText = searchBox.Text;

            // 调用搜索方法
            SearchInTreeView(searchText);
        }
        private void LoadJsonFilesToTreeView(string folderPath)
        {
            // 确保文件夹存在
            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("指定的文件夹不存在。");
                return;
            }

            // 遍历文件夹中的所有 JSON 文件
            var jsonFiles = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in jsonFiles)
            {
                // 读取文件内容
                string jsonContent = File.ReadAllText(file);

                // 解析 JSON 内容到字典
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonContent);

                // 获取文件名作为 TreeView 顶层节点
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                TreeViewItem fileNode = new TreeViewItem
                {
                    Header = fileName
                };

                // 将 JSON 字典内容作为子节点添加到 TreeView 中
                foreach (var category in dict)
                {
                    TreeViewItem categoryNode = new TreeViewItem
                    {
                        Header = category.Key,
                        Tag = category.Value // 将字典内容存储在 Tag 中，方便后续操作
                    };
                    fileNode.Items.Add(categoryNode);
                }

                // 将顶层文件节点添加到 TreeView 中
                TagTreeView.Items.Add(fileNode);
            }
        }
        private void TagTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = TagTreeView.SelectedItem as TreeViewItem;
            if (selectedItem?.Tag is Dictionary<string, string> dictionary)
            {
                // 清空 ListBox
                TagListBox.Items.Clear();

                // 将字典中的键添加到 ListBox 中
                foreach (var key in dictionary.Keys)
                {
                    TagListBox.Items.Add(key);
                }
            }
        }
        private void TagListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 如果选中项是从 ListBox 中的搜索结果
            if (TagListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string value)
            {
                // 从搜索结果中获取键（中文）和值（英文）
                string selectedKey = selectedItem.Content.ToString(); // 键（中文）

                // 创建新的 TagControl，并添加到 TagsContainer
                TagControl newTagControl = new TagControl(value, value, selectedKey);
                newTagControl.TextChanged += TagControl_TextChanged;
                newTagControl.TagDeleted += TagControl_TagDeleted;
                TagsContainer.Children.Add(newTagControl);
            }
            // 如果是从 TreeView 中选择的项目
            else if (TagListBox.SelectedItem is string selectedKey)
            {
                var selectedTreeViewItem = TagTreeView.SelectedItem as TreeViewItem;
                if (selectedTreeViewItem?.Tag is Dictionary<string, string> dictionary)
                {
                    // 获取对应的值
                    var selectedValue = dictionary[selectedKey];

                    // 创建新的 TagControl，并添加到 TagsContainer
                    TagControl newTagControl = new TagControl(selectedValue, selectedValue, selectedKey);
                    newTagControl.TextChanged += TagControl_TextChanged;
                    newTagControl.TagDeleted += TagControl_TagDeleted;
                    TagsContainer.Children.Add(newTagControl);
                }
            }
            // 添加完 TagControl 后，更新 PositivePrompt 和 InputTextBox
            UpdateViewModelTagsText();
        }
        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            // 弹出输入框获取保存名称
            string noteName = Microsoft.VisualBasic.Interaction.InputBox("请输入保存名称", "保存笔记", "");

            if (!string.IsNullOrEmpty(noteName))
            {
                // 检查是否存在重名
                var existingNote = _viewModel.Notes.FirstOrDefault(note => note.Name == noteName);
                if (existingNote != null)
                {
                    MessageBox.Show("该名称已存在，请选择其他名称。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 保存正面和负面词条
                string positivePrompt = _viewModel.PositivePrompt;
                string negativePrompt = _viewModel.NegitivePrompt;

                // 检查是否有图像，如果有则保存图像
                string imagePath = null;
                if (ImageViewerControl.ImageSource != null)
                {
                    // 确保目标文件夹存在
                    string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NoteBookImgPath");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // 设置图像的保存路径
                    imagePath = System.IO.Path.Combine(folderPath, $"{noteName}.png");

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)ImageViewerControl.ImageSource));

                    // 保存图像
                    using (var fileStream = new System.IO.FileStream(imagePath, System.IO.FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                }
                else
                {
                    MessageBox.Show("没有图像可保存，保存的笔记将不包含图像。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 创建新的笔记对象并存储
                var newNote = new NoteModel
                {
                    Name = noteName,
                    PositivePrompt = positivePrompt,
                    NegativePrompt = negativePrompt,
                    ImagePath = imagePath // 如果没有图像，ImagePath 将为 null
                };
                _viewModel.Notes.Add(newNote);

                // 更新ListBoxItem
                UpdateListBoxItems();
                // 保存当前配置
                _viewModel.SaveParameters();
            }
        }

        private void UpdateListBoxItems()
        {
            // 确保 ViewModel 中的 Notes 集合已经更新
            ListBox listBox = this.FindName("NoteBookListBox") as ListBox;
            if (listBox != null)
            {
                // 因为 ListBox 已经通过 ItemsSource 绑定到 _viewModel.Notes，所以不需要手动清空和添加项目
                // 只需通知 UI 数据已更新即可，确保 Notes 集合是 ObservableCollection<NoteModel>
                // 如果不是 ObservableCollection，需要改为 ObservableCollection，以便自动通知 UI 更新
                listBox.ItemsSource = null; // 解除现有绑定
                listBox.ItemsSource = _viewModel.Notes; // 重新绑定
            }
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                // 更新正面和负面词条文本框
                PositiveTextBox.Text = selectedNote.PositivePrompt;
                NegativeTextBox.Text = selectedNote.NegativePrompt;

                // 更新图像控件
                if (!string.IsNullOrEmpty(selectedNote.ImagePath) && File.Exists(selectedNote.ImagePath))
                {
                    var image = new BitmapImage();
                    using (var stream = new FileStream(selectedNote.ImagePath, FileMode.Open, FileAccess.Read))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad; // 确保图像加载后不锁定文件
                        image.StreamSource = stream;
                        image.EndInit();
                    }
                    NoteImgPreview.Source = image;
                }
                else
                {
                    NoteImgPreview.Source = null; // 如果没有图像，清空预览
                }
            }
        }
        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                // 清空图像预览，确保文件不被锁定
                NoteImgPreview.Source = null;

                // 删除图像文件（如果存在）
                if (!string.IsNullOrEmpty(selectedNote.ImagePath) && File.Exists(selectedNote.ImagePath))
                {
                    try
                    {
                        File.Delete(selectedNote.ImagePath);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show($"删除文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 从集合中移除
                _viewModel.Notes.Remove(selectedNote);
                // 保存当前配置
                _viewModel.SaveParameters();
            }
            else
            {
                MessageBox.Show("请先选择一个笔记。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void UseNote_Click(object sender, RoutedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                // 更新 _viewModel 中的词条
                _viewModel.PositivePrompt = selectedNote.PositivePrompt;
                _viewModel.NegitivePrompt = selectedNote.NegativePrompt;
                InputTextBox.Text = _viewModel.PositivePrompt;
                UpdateTagsContainerForNotes();
            }
            else
            {
                MessageBox.Show("请先选择一个笔记。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public (string[] base64Images, double[] informationExtracted, double[] referenceStrength) ExtractImageData()
        {
            // 创建三个列表来存储图像的 base64 编码、InformationExtracted 和 ReferenceStrength 参数
            List<string> reference_image_multiple = new List<string>();
            List<double> reference_information_extracted_multiple = new List<double>();
            List<double> reference_strength_multiple = new List<double>();

            // 遍历 ImageWrapPanel 中的所有子控件
            foreach (var child in ImageWrapPanel.Children)
            {
                if (child is xianyun.UserControl.VibeTransfer vibeTransfer)
                {
                    // 获取图像的 base64 编码
                    string base64Image = vibeTransfer.GetImageAsBase64();
                    if (!string.IsNullOrEmpty(base64Image))
                    {
                        reference_image_multiple.Add(base64Image);
                    }

                    // 获取 InformationExtracted 和 ReferenceStrength 参数
                    double informationExtracted = vibeTransfer.InformationExtracted;
                    double referenceStrength = vibeTransfer.ReferenceStrength;

                    reference_information_extracted_multiple.Add(informationExtracted);
                    reference_strength_multiple.Add(referenceStrength);
                }
            }

            // 转换为数组并返回
            return (
                reference_image_multiple.ToArray(),
                reference_information_extracted_multiple.ToArray(),
                reference_strength_multiple.ToArray()
            );
        }
        private bool _isCancelling = false; // 用于标记是否取消
        private bool _isGenerating = false; // 用于标记是否正在生成

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // 如果当前正在生成图像，则点击按钮应取消后续操作，并锁定按钮
            if (_isGenerating)
            {
                _isCancelling = true;
                button.Content = "正在取消...";
                button.IsEnabled = false; 
            }
            else
            {
                _isCancelling = false;
                _isGenerating = true;
                button.Content = "取消生成";
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7DED0"));

                await GenerateImageRequest();

                // 恢复按钮状态
                button.Content = "Generate Image";
                button.Background = new SolidColorBrush(Colors.White);
                button.IsEnabled = true;
                _isGenerating = false;
            }
        }
        private async Task GenerateImageRequest()
        {
            try
            {
                var apiClient = new XianyunApiClient("https://nocaptchauri.idlecloud.cc", SessionManager.Session);
                Console.WriteLine(SessionManager.Session);

                // 获取 VibeTransfer 的数据
                var (base64Images, informationExtracted, referenceStrength) = ExtractImageData();

                // 生成随机种子的方法
                long GenerateRandomSeed()
                {
                    var random = new Random();
                    int length = random.Next(9, 13);
                    long seed = 0;
                    for (int i = 0; i < length; i++)
                    {
                        seed = seed * 10 + random.Next(0, 10);
                    }
                    return seed;
                }

                // 循环生成图像请求
                for (int i = 0; i < _viewModel.DrawingFrequency; i++)
                {
                    var seedValue = _viewModel.Seed?.ToString() ?? GenerateRandomSeed().ToString();

                    // 创建图像生成请求对象
                    var imageRequest = new ImageGenerationRequest
                    {
                        Model = _viewModel.Model,
                        PositivePrompt = _viewModel.PositivePrompt,
                        NegativePrompt = _viewModel.NegitivePrompt,
                        Scale = _viewModel.GuidanceScale,
                        Steps = _viewModel.Steps,
                        Width = _viewModel.Width,
                        Height = _viewModel.Height,
                        PromptGuidanceRescale = _viewModel.GuidanceRescale,
                        NoiseSchedule = _viewModel.NoiseSchedule,
                        Seed = seedValue,
                        Sampler = _viewModel.ActualSamplingMethod,
                        Decrisp = _viewModel.IsDecrisp,
                        Variety = _viewModel.IsVariety,
                        Sm = _viewModel.IsSMEA,
                        SmDyn = _viewModel.IsDYN,
                        PictureId = TotpGenerator.GenerateTotp(_viewModel._secretKey)
                    };

                    if (_viewModel.IsConvenientResolution)
                    {
                        string Resolution = _viewModel.Resolution;
                        // 解析分辨率字符串
                        string[] resolution = Resolution.Split('*');
                        imageRequest.Width = int.Parse(resolution[0]);
                        imageRequest.Height = int.Parse(resolution[1]);
                    }

                    // 检查是否有 VibeTransfer 数据
                    if (base64Images.Length > 0 && informationExtracted.Length > 0 && referenceStrength.Length > 0)
                    {
                        imageRequest.ReferenceImage = base64Images;
                        imageRequest.InformationExtracted = informationExtracted;
                        imageRequest.ReferenceStrength = referenceStrength;
                    }
                    else
                    {
                        // 如果没有VibeTransfer数据，则将这些字段设置为null
                        imageRequest.ReferenceImage = null;
                        imageRequest.InformationExtracted = null;
                        imageRequest.ReferenceStrength = null;
                    }
                    if (originalImage != null)
                    {
                        if (originalImage is BitmapImage image)
                        {
                            int width = image.PixelWidth;
                            int height = image.PixelHeight;
                            Common.tools.ValidateResolution(ref width, ref height);
                            BitmapImage resizedImage = Common.tools.ResizeImage(originalImage, width, height);
                            string base64Image = Common.tools.ConvertImageToBase64(resizedImage, new PngBitmapEncoder());
                            // 获取图像的长和宽
                            if (_viewModel.ReqType != null)
                            {
                                imageRequest.Width = width;
                                imageRequest.Height = height;
                                imageRequest.Image = base64Image;
                                imageRequest.ReqType = _viewModel.ReqType;

                                // 进一步检查 ReqType 是否为 "emotion" 或 "colorize"
                                if (_viewModel.ReqType == "emotion")
                                {
                                    // 设置 Prompt 和 Defry
                                    imageRequest.Prompt = _viewModel.ActualEmotion + ";;" + _viewModel.Emotion_Prompt;
                                    imageRequest.Defry = _viewModel.Emotion_Defry;
                                }
                                if (_viewModel.ReqType == "colorize")
                                {
                                    // 设置 Prompt 和 Defry
                                    imageRequest.Prompt = _viewModel.Colorize_Prompt;
                                    imageRequest.Defry = _viewModel.Colorize_Defry;
                                }
                            }
                            else
                            {
                                imageRequest.Width = width;
                                imageRequest.Height = height;
                                imageRequest.Image = base64Image;
                                imageRequest.Action = true;
                                imageRequest.Noise = _viewModel.Noise;
                                imageRequest.Strength = _viewModel.Strength;
                                if (MaskImageSource.Source != null)
                                {
                                    imageRequest.Mask = Common.tools.ConvertRenderTargetBitmapToBase64(maskImage);
                                }
                            }
                        }
                    }
                    var (jobId, initialQueuePosition) = await apiClient.GenerateImageAsync(imageRequest);
                    Console.WriteLine($"任务已提交，任务ID: {jobId}, 初始队列位置: {initialQueuePosition}");

                    int currentQueuePosition = initialQueuePosition;
                    _viewModel.ProgressValue = 0;

                    while (currentQueuePosition > 0)
                    {
                        var (status, imageBase64, queuePosition) = await apiClient.CheckResultAsync(jobId);
                        if (status == "processing")
                        {
                            _viewModel.ProgressValue = 70;
                            currentQueuePosition = queuePosition;
                        }
                        else if (status == "queued")
                        {
                            _viewModel.ProgressValue = 70 * (1 - (double)queuePosition / initialQueuePosition);
                            currentQueuePosition = queuePosition;
                        }
                        await Task.Delay(5000); // 轮询延迟
                    }

                    // 继续更新进度并处理图像生成
                    while (_viewModel.ProgressValue < 96)
                    {
                        var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                        if (status == "completed")
                        {
                            _viewModel.ProgressValue = 100;
                            Console.WriteLine("图像生成成功！");

                            var bitmapFrame = Common.tools.ConvertBase64ToBitmapFrame(imageBase64);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var imgPreview = new ImgPreview(imageBase64);
                                imgPreview.ImageClicked += _viewModel.OnImageClicked;
                                ImageStackPanel.Children.Add(imgPreview);
                                ImageViewerControl.ImageSource = bitmapFrame;
                            });
                            break;
                        }

                        _viewModel.ProgressValue += new Random().Next(1, 4);
                        await Task.Delay(1500);
                    }

                    // 最终检查生成完成
                    while (_viewModel.ProgressValue < 100)
                    {
                        await Task.Delay(2000);
                        var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                        if (status == "completed")
                        {
                            _viewModel.ProgressValue = 100;
                            var bitmapFrame = Common.tools.ConvertBase64ToBitmapFrame(imageBase64);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var imgPreview = new ImgPreview(imageBase64);
                                imgPreview.ImageClicked += _viewModel.OnImageClicked;
                                ImageStackPanel.Children.Add(imgPreview);
                                ImageViewerControl.ImageSource = bitmapFrame;
                            });
                            break;
                        }
                    }

                    await Task.Delay(3000); // 请求间隔3秒
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Txt2imgPage_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (ConfigurationService.ConfigurationExists())
            {
                _viewModel.LoadParameters();
                // 设置全局登录状态为已登录
                var app = (App)Application.Current;
                app.IsLoading = true;
            }
            if (viewModel != null)
            {
                viewModel.ImgPreviewArea = ImgPreviewArea;
                viewModel.ImageStackPanel = ImageStackPanel;
                viewModel.ImageViewerControl = ImageViewerControl;
            }
            InputTextBox.Text = viewModel.PositivePrompt;
            UpdateTagsContainer();
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json_files");
            LoadJsonFilesToTreeView(folderPath);
        }
        private void Txt2imgPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 保存当前配置
            _viewModel.SaveParameters();
        }
        private void TagMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isTagMenuOpen)
            {
                Storyboard storyboard = (Storyboard)FindResource("RightTagMenuClose");
                storyboard.Begin();
            }
            else
            {
                Storyboard storyboard = (Storyboard)FindResource("RightTagMenu");
                storyboard.Begin();
            }

            isTagMenuOpen = !isTagMenuOpen;
        }
        private void I2IMenuBtn_Open(object sender, RoutedEventArgs e)
        {
            Storyboard storyboard = (Storyboard)FindResource("Left_i2iMenu");
            storyboard.Begin();
        }
        private void I2IMenuBtn_Close(object sender, RoutedEventArgs e)
        {
            Storyboard storyboard = (Storyboard)FindResource("Left_i2iMenuClose");
            storyboard.Begin();
        }
        private void TagsContainer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) {
                if (TagsContainer.Children.Count > 0)
                {
                    var tagsText = string.Join(",", TagsContainer.Children.OfType<TagControl>().Select(tc => tc.GetAdjustedText()));
                    InputTextBox.Text = tagsText;
                }
                else
                {
                    InputTextBox.Text = string.Empty;
                }
                ScrollViewer.Visibility = Visibility.Collapsed;
                InputTextBox.Visibility = Visibility.Visible;
                InputTextBox.UpdateLayout();  // 强制刷新布局
                InputTextBox.Focus();
            }
        }
        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void Border_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var filePath in files)
                {
                    if (IsImageFile(filePath))
                    {
                        // 创建 VibeTransfer 控件实例
                        var vibeTransfer = new xianyun.UserControl.VibeTransfer();

                        // 使用文件路径设置图像
                        vibeTransfer.SetImageFromFile(filePath);

                        // 将 VibeTransfer 控件添加到 WrapPanel 中
                        ImageWrapPanel.Children.Add(vibeTransfer);
                    }
                }
                UploadStackPanel.Visibility = Visibility.Collapsed;
            }
        }
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (IsImageFile(filePath))
                    {
                        // 创建 VibeTransfer 控件实例
                        var vibeTransfer = new xianyun.UserControl.VibeTransfer();

                        // 使用文件路径设置图像
                        vibeTransfer.SetImageFromFile(filePath);

                        // 将 VibeTransfer 控件添加到 WrapPanel 中
                        ImageWrapPanel.Children.Add(vibeTransfer);
                    }
                }
                UploadStackPanel.Visibility = Visibility.Collapsed;
            }
        }
        private bool IsImageFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }
        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessInputText();
            InputTextBox.Visibility = Visibility.Collapsed;
            ScrollViewer.Visibility = Visibility.Visible;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessInputText();
                InputTextBox.Visibility = Visibility.Collapsed;
                ScrollViewer.Visibility = Visibility.Visible;
            }
        }
        private void UpdateTagsContainer()
        {
            string inputText = InputTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(inputText))
            {
                // 将中文逗号转换为英文逗号
                inputText = inputText.Replace("，", ",");

                // 分割文本
                string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                             .ToArray();

                // 获取现有的 TagControl 列表
                var existingTagControls = TagsContainer.Children.OfType<TagControl>().ToList();

                foreach (var tag in newTags)
                {
                    // 使用标签文本本身作为唯一标识符
                    var existingTagControl = existingTagControls.FirstOrDefault(tc => tc.GetAdjustedText() == tag);

                    if (existingTagControl == null)
                    {
                        // 如果不存在，则创建新的 TagControl 并添加到容器中
                        TagControl newTagControl = new TagControl(tag, tag);
                        newTagControl.TextChanged += TagControl_TextChanged;
                        newTagControl.TagDeleted += TagControl_TagDeleted;
                        TagsContainer.Children.Add(newTagControl);
                    }
                    else
                    {
                        // 如果存在，保持组件，并从现有列表中移除，避免删除
                        existingTagControls.Remove(existingTagControl);
                    }
                }

                // 删除不再需要的 TagControl
                foreach (var tagControl in existingTagControls)
                {
                    TagsContainer.Children.Remove(tagControl);
                }
            }
            // 隐藏 TextBox
            InputTextBox.Visibility = Visibility.Collapsed;
        }
        private void UpdateTagsContainerForNotes()
        {
            string inputText = InputTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(inputText))
            {
                // 将中文逗号转换为英文逗号
                inputText = inputText.Replace("，", ",");

                // 分割文本
                string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                             .ToArray();

                // 清空现有的 TagControl，确保顺序与新内容一致
                TagsContainer.Children.Clear();

                // 根据新的标签内容，按顺序重新生成 TagControl
                foreach (var tag in newTags)
                {
                    TagControl newTagControl = new TagControl(tag, tag);
                    newTagControl.TextChanged += TagControl_TextChanged;
                    newTagControl.TagDeleted += TagControl_TagDeleted;
                    TagsContainer.Children.Add(newTagControl);
                }

                // 更新 ViewModel 中的 PositivePrompt 以保持一致性
                UpdateViewModelTagsText();
            }

            // 隐藏 TextBox
            InputTextBox.Visibility = Visibility.Collapsed;
        }
        private void TagControl_TagDeleted(object sender, EventArgs e)
        {
            if (sender is TagControl tagControl)
            {
                TagsContainer.Children.Remove(tagControl);
                UpdateViewModelTagsText();
            }
        }
        private void TagControl_TextChanged(object sender, EventArgs e)
        {
            UpdateViewModelTagsText();
        }
        private void UpdateViewModelTagsText()
        {
            if (TagsContainer.Children.Count > 0)
            {
                var tagsText = string.Join(",", TagsContainer.Children.OfType<TagControl>().Select(tc => tc.GetAdjustedText()));
                InputTextBox.Text = tagsText;
                // 获取已绑定的 Txt2imgPageModel 实例
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null && viewModel != null)
                {
                    viewModel.PositivePrompt = tagsText;
                }
            }
        }
        private void ProcessInputText()
        {
            if (InputTextBox.Visibility == Visibility.Visible)
            {
                string inputText = InputTextBox.Text.Trim();

                if (string.IsNullOrEmpty(inputText))
                {
                    // 如果输入框为空，清空所有标签
                    TagsContainer.Children.Clear();
                    UpdateViewModelTagsText();
                }
                else
                {
                    // 将中文逗号转换为英文逗号
                    inputText = inputText.Replace("，", ",");

                    // 分割文本
                    string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                                 .ToArray();

                    // 获取现有的 TagControl 列表
                    var existingTagControls = TagsContainer.Children.OfType<TagControl>().ToList();

                    foreach (var tag in newTags)
                    {
                        // 使用标签文本作为标识符
                        var existingTagControl = existingTagControls.FirstOrDefault(tc => tc.GetAdjustedText() == tag);

                        if (existingTagControl == null)
                        {
                            // 如果不存在该标签
                            TagControl newTagControl = new TagControl(tag, tag);
                            newTagControl.TextChanged += TagControl_TextChanged;
                            TagsContainer.Children.Add(newTagControl);
                        }
                        else
                        {
                            // 如果存在，保持组件，并从现有列表中移除，避免删除
                            existingTagControls.Remove(existingTagControl);
                        }
                    }

                    // 删除不再需要的 TagControl
                    foreach (var tagControl in existingTagControls)
                    {
                        TagsContainer.Children.Remove(tagControl);
                    }
                    UpdateViewModelTagsText();
                }
                InputTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private string AutoCompleteBrackets(string text)
        {
            // 补全方括号 []
            int leftSquareBracketsCount = text.Count(c => c == '[');
            int rightSquareBracketsCount = text.Count(c => c == ']');
            if (leftSquareBracketsCount > rightSquareBracketsCount)
            {
                text = text.PadRight(text.Length + (leftSquareBracketsCount - rightSquareBracketsCount), ']');
            }
            else if (rightSquareBracketsCount > leftSquareBracketsCount)
            {
                text = text.PadLeft(text.Length + (rightSquareBracketsCount - leftSquareBracketsCount), '[');
            }

            // 补全花括号 {}
            int leftCurlyBracketsCount = text.Count(c => c == '{');
            int rightCurlyBracketsCount = text.Count(c => c == '}');
            if (leftCurlyBracketsCount > rightCurlyBracketsCount)
            {
                text = text.PadRight(text.Length + (leftCurlyBracketsCount - rightCurlyBracketsCount), '}');
            }
            else if (rightCurlyBracketsCount > leftCurlyBracketsCount)
            {
                text = text.PadLeft(text.Length + (rightCurlyBracketsCount - leftCurlyBracketsCount), '{');
            }

            return text;
        }

        private void TagsContainer_DragOver(object sender, DragEventArgs e)
        {
            dragInProgress = true;
            TagControl dragControl = null;
            try
            {
                IDataObject dataObject = e.Data;
                if (dataObject.GetData(typeof(TagControl)) == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }
                dragControl = (TagControl)dataObject.GetData(typeof(TagControl));
            }
            catch
            {
                e.Effects = DragDropEffects.None;
                return;
            }
            if (sender is WrapPanel panel)
            {
                // 获取鼠标相对于WrapPanel的位置
                Point position = e.GetPosition(panel);
                //System.Diagnostics.Debug.WriteLine(position);

                e.Effects = DragDropEffects.Move;

                // 定义两个变量，分别表示左边和右边最接近鼠标位置的子控件
                UIElement leftElement = null;
                UIElement rightElement = null;
                int insertIndex = -1;

                if (panel.Children.Count == 0)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // 遍历子控件，找到左边和右边最接近鼠标位置的子控件
                // System.Diagnostics.Debug.WriteLine(panel.Children.Count);
                for (int i = 0; i < panel.Children.Count; i++)
                {
                    var element = panel.Children[i];
                    UIElement nextElement = null;
                    if (i + 1 < panel.Children.Count)
                    {
                        nextElement = panel.Children[i + 1];
                    }
                    // System.Diagnostics.Debug.WriteLine(panel.Children.IndexOf(element));
                    //System.Diagnostics.Debug.WriteLine(element);
                    // 获取子控件相对于WrapPanel的坐标和大小
                    Point elementPosition = element.TranslatePoint(new Point(0, 0), panel);
                    //System.Diagnostics.Debug.WriteLine(elementPosition);
                    double elementWidth = element.DesiredSize.Width;
                    double elementHeight = element.DesiredSize.Height;

                    // 判断是否在同一行或同一列
                    var padding = 0;
                    if (i == 0 && position.Y < elementPosition.Y - padding)
                    {
                        rightElement = element;
                        leftElement = null;
                        insertIndex = 0;
                        break;
                    }
                    bool sameRow = position.Y >= elementPosition.Y - padding && position.Y <= elementPosition.Y + elementHeight + padding;

                    if (sameRow)
                    {
                        if (position.X >= elementPosition.X + elementWidth / 2)
                        {
                            if (nextElement == null)
                            {
                                leftElement = element;
                                rightElement = null;
                                insertIndex = panel.Children.Count;
                                break;
                            }
                            else
                            {
                                Point elementPositionNext = nextElement.TranslatePoint(new Point(0, 0), panel);
                                double elementWidthNext = nextElement.DesiredSize.Width;
                                double elementHeightNext = nextElement.DesiredSize.Height;
                                bool sameRowNext = position.Y >= elementPositionNext.Y - padding && position.Y <= elementPositionNext.Y + elementHeightNext + padding;
                                if (sameRowNext)
                                {
                                    if (position.X <= elementPositionNext.X + elementWidthNext / 2)
                                    {
                                        leftElement = element;
                                        rightElement = nextElement;
                                        insertIndex = i + 1;
                                        break;
                                    }
                                }
                                else
                                {
                                    leftElement = element;
                                    rightElement = null;
                                    insertIndex = i + 1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            rightElement = element;
                            leftElement = null;
                            insertIndex = i;
                            break;
                        }
                    }
                }

                if (insertIndex == -1)
                {
                    leftElement = panel.Children[panel.Children.Count - 1];
                    rightElement = null;
                    insertIndex = panel.Children.Count;
                }



                // 创建一个Adorner对象，用于显示一个指示器
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                if (adornerLayer != null)
                {
                    // 移除之前的Adorner对象
                    if (currentAdorner != null)
                    {
                        //adornerLayer.Remove(currentAdorner);
                        //currentAdorner = null;
                    }
                    else
                    {
                        currentAdorner = new DragAdorner(panel, dragControl, true, 0.8);
                        adornerLayer.Add(currentAdorner);
                    }

                    // 创建一个新的Adorner对象，并设置其位置和大小
                    //Border border = new Border();
                    //border.Width = 30;
                    //border.Height = 30;
                    //border.BorderThickness = Thickness.;
                    //border.BorderBrush = new 


                    if (leftElement != null)
                    {
                        // 如果左右都有子控件，那么指示器的位置在两个子控件之间
                        Point leftPosition = leftElement.TranslatePoint(new Point(0, 0), panel);

                        double leftWidth = leftElement.DesiredSize.Width;
                        double leftHeight = leftElement.DesiredSize.Height;


                        // Canvas.SetLeft(currentAdorner, leftPosition.X + leftWidth - dragControl.DesiredSize.Width/2);
                        // Canvas.SetTop(currentAdorner, leftPosition.Y);
                        // System.Diagnostics.Debug.WriteLine(leftPosition.X + leftWidth - dragControl.DesiredSize.Width / 2 + " " + leftPosition.Y);
                        currentAdorner.LeftOffset = leftPosition.X + leftWidth;
                        currentAdorner.TopOffset = leftPosition.Y + leftHeight / 2;
                    }
                    else if (rightElement != null)
                    {


                        Point rightPosition = rightElement.TranslatePoint(new Point(0, 0), panel);

                        double rightWidth = rightElement.DesiredSize.Width;
                        double rightHeight = rightElement.DesiredSize.Height;


                        currentAdorner.LeftOffset = rightPosition.X;
                        currentAdorner.TopOffset = rightPosition.Y + rightHeight / 2;

                    }
                    else
                    {
                        adornerLayer.Remove(currentAdorner);
                        currentAdorner = null;
                    }
                }
            }
        }

        private void TagsContainer_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is WrapPanel panel))
            {
                return;
            }

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null && currentAdorner != null)
            {
                adornerLayer.Remove(currentAdorner);
                currentAdorner = null;
            }

            TagControl dragControl;
            try
            {
                IDataObject dataObject = e.Data;
                if (dataObject.GetData(typeof(TagControl)) == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }
                dragControl = (TagControl)dataObject.GetData(typeof(TagControl));
            }
            catch
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (panel != null)
            {
                // 获取鼠标相对于WrapPanel的位置
                Point position = e.GetPosition(panel);

                // 定义插入位置
                int insertIndex = -1;
                for (int i = 0; i < panel.Children.Count; i++)
                {
                    var element = panel.Children[i];
                    Point elementPosition = element.TranslatePoint(new Point(0, 0), panel);
                    double elementWidth = element.DesiredSize.Width;
                    double elementHeight = element.DesiredSize.Height;

                    // 判断是否在同一行
                    if (position.Y >= elementPosition.Y && position.Y <= elementPosition.Y + elementHeight)
                    {
                        if (position.X < elementPosition.X + elementWidth / 2)
                        {
                            insertIndex = i;
                            break;
                        }
                    }
                }

                // 如果没有找到合适的位置，插入到最后
                if (insertIndex == -1)
                {
                    insertIndex = panel.Children.Count;
                }

                int currentIndex = panel.Children.IndexOf(dragControl);
                if (currentIndex != -1 && insertIndex != currentIndex)
                {
                    panel.Children.RemoveAt(currentIndex);
                    if (insertIndex > currentIndex) insertIndex--;
                    panel.Children.Insert(insertIndex, dragControl);
                }
                UpdateViewModelTagsText();
            }
        }

        private async void ExportImagesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 打开保存对话框，让用户选择保存路径
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "选择保存路径",
                    Filter = "ZIP 压缩包 (*.zip)|*.zip",
                    FileName = "ExportedImages.zip"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return; // 用户取消操作
                }

                // 获取用户选择的保存路径
                string zipFilePath = saveFileDialog.FileName;

                // 创建临时目录存放图像
                string tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ExportedImages");
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true); // 清理旧数据
                }
                Directory.CreateDirectory(tempDirectory);

                // 显示进度条
                _viewModel.IsCreatingZipVisible = true;
                _viewModel.CreateZipProgressValue = 0;

                // 获取控件数量
                int totalControls = ImageStackPanel.Children.OfType<ImgPreview>().Count();
                if (totalControls == 0)
                {
                    MessageBox.Show("没有图像可导出。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _viewModel.IsCreatingZipVisible = false;
                    return;
                }

                double progressIncrement = 100.0 / totalControls;
                double currentProgress = 0;

                // 遍历 StackPanel 中的 ImgPreview 控件
                foreach (var child in ImageStackPanel.Children)
                {
                    if (child is ImgPreview imgPreview)
                    {
                        // 获取图像的 BitmapImage 对象
                        BitmapImage bitmapImage = imgPreview.GetBitmapImage();
                        if (bitmapImage != null)
                        {
                            // 将 BitmapImage 保存为 PNG 文件
                            string fileName = $"{Guid.NewGuid()}.png"; // 随机文件名
                            string filePath = System.IO.Path.Combine(tempDirectory, fileName);

                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                BitmapEncoder encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                                encoder.Save(fileStream);
                            }
                        }

                        // 更新进度条
                        currentProgress += progressIncrement;
                        _viewModel.CreateZipProgressValue = Math.Min(currentProgress, 100);
                        await Task.Delay(10); // 模拟延迟以便可视化进度
                    }
                }

                // 创建压缩包
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath); // 删除旧的压缩包
                }
                ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);
                MessageBox.Show($"图像已成功导出为压缩包：{zipFilePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsCreatingZipVisible = false;
            }
        }


        private void TagsContainer_OnRealTargetDragLeave(object sender, DragEventArgs e)
        {
            if (!(sender is WrapPanel panel))
            {
                return;
            }
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null)
            {
                // 移除之前的Adorner对象
                if (currentAdorner != null)
                {
                    adornerLayer.Remove(currentAdorner);
                    currentAdorner = null;
                }
            }
        }
        private void TagsContainer_DragLeave(object sender, DragEventArgs e)
        {

            dragInProgress = false;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (dragInProgress == false) TagsContainer_OnRealTargetDragLeave(sender, e);
            }));


        }

        private void TagsContainer_DragEnter(object sender, DragEventArgs e)
        {
            dragInProgress = true;
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框选择图像
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            if (openFileDialog.ShowDialog() == true)
            {
                // 加载图像
                BitmapImage image = new BitmapImage(new Uri(openFileDialog.FileName));
                int imageWidth = image.PixelWidth;
                int imageHeight = image.PixelHeight;
                Common.tools.ValidateResolution(ref imageWidth, ref imageHeight);
                BitmapImage resizedImage = Common.tools.ResizeImage(image, imageWidth, imageHeight);
                originalImage = resizedImage;
                // 使用原始图像渲染
                RenderImage(originalImage);
                _viewModel.IsInkCanvasVisible=true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // 清空图像
            originalImage = null;
            image.Source = null;
            inkCanvas.Strokes.Clear();
            _viewModel.IsInkCanvasVisible = false;
        }

        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            // 将新笔画加入 UndoStack
            UndoStack.Push(e.Stroke);

            // 清空 RedoStack，因为新操作会使重做无效
            RedoStack.Clear();
        }

        private void Undo()
        {
            if (UndoStack.Count > 0)
            {
                // 从 UndoStack 弹出最近的笔画
                Stroke lastStroke = UndoStack.Pop();

                // 从 InkCanvas.Strokes 移除该笔画
                inkCanvas.Strokes.Remove(lastStroke);

                // 将笔画压入 RedoStack
                RedoStack.Push(lastStroke);
            }
            else
            {
                MessageBox.Show("没有可以撤回的操作！");
            }
        }

        private void Redo()
        {
            if (RedoStack.Count > 0)
            {
                // 从 RedoStack 弹出最近的笔画
                Stroke lastStroke = RedoStack.Pop();

                // 添加回 InkCanvas.Strokes
                inkCanvas.Strokes.Add(lastStroke);

                // 将笔画压入 UndoStack
                UndoStack.Push(lastStroke);
            }
            else
            {
                MessageBox.Show("没有可以重做的操作！");
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void RenderImage(BitmapImage bitmap)
        {
            // Set the image source
            image.Source = bitmap;

            // Initialize transformations if not already done
            if (panZoomTransformGroup == null)
            {
                panZoomTransformGroup = new TransformGroup();
                panZoomScaleTransform = new ScaleTransform();
                panZoomTranslateTransform = new TranslateTransform();
                panZoomTransformGroup.Children.Add(panZoomScaleTransform); // 先缩放
                panZoomTransformGroup.Children.Add(panZoomTranslateTransform); // 后平移
                panZoomCanvas.RenderTransform = panZoomTransformGroup;
            }

            // Reset transformations when loading a new image
            panZoomScaleTransform.ScaleX = 1.0;
            panZoomScaleTransform.ScaleY = 1.0;
            panZoomTranslateTransform.X = 0;
            panZoomTranslateTransform.Y = 0;

            // 获取Border的尺寸
            double borderWidth = imageBorder.ActualWidth;
            double borderHeight = imageBorder.ActualHeight;

            // 获取图像的尺寸
            double imageWidth = bitmap.PixelWidth;
            double imageHeight = bitmap.PixelHeight;

            // 计算缩放比例
            double scaleX = borderWidth / imageWidth;
            double scaleY = borderHeight / imageHeight;
            double scale = Math.Min(scaleX, scaleY);

            // 应用缩放
            image.Width = imageWidth;
            image.Height = imageHeight;

            inkCanvas.Width = imageWidth;
            inkCanvas.Height = imageHeight;

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            Canvas.SetLeft(inkCanvas, 0);
            Canvas.SetTop(inkCanvas, 0);

            panZoomScaleTransform.ScaleX = scale;
            panZoomScaleTransform.ScaleY = scale;

            panZoomTranslateTransform.X = (borderWidth - imageWidth * scale) / 2;
            panZoomTranslateTransform.Y = (borderHeight - imageHeight * scale) / 2;
        }

        /// <summary>
        /// 对画笔的宽度进行调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrushWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Width = e.NewValue;
            }
        }

        /// <summary>
        /// 对画笔的高度进行调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrushHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Height = e.NewValue;
            }
        }

        /// <summary>
        /// 通过HEX文本框对画笔的颜色进行调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (inkCanvas != null && !string.IsNullOrWhiteSpace(TextHex.Text))
            {
                try
                {
                    // 尝试将文本转换为颜色
                    var color = (Color)ColorConverter.ConvertFromString(TextHex.Text);
                    inkCanvas.DefaultDrawingAttributes.Color = color;
                }
                catch (FormatException)
                {
                    // 如果转换失败，设置一个默认颜色
                    inkCanvas.DefaultDrawingAttributes.Color = Colors.White;
                }
            }
        }

        /// <summary>
        /// 重置图像的尺寸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetPositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage != null)
            {
                // 获取Border的尺寸
                double borderWidth = imageBorder.ActualWidth;
                double borderHeight = imageBorder.ActualHeight;

                // 获取图像的尺寸
                double imageWidth = originalImage.PixelWidth;
                double imageHeight = originalImage.PixelHeight;

                // 计算缩放比例
                double scaleX = borderWidth / imageWidth;
                double scaleY = borderHeight / imageHeight;
                double scale = Math.Min(scaleX, scaleY);

                // 重置变换
                panZoomScaleTransform.ScaleX = scale;
                panZoomScaleTransform.ScaleY = scale;
                panZoomTranslateTransform.X = (borderWidth - imageWidth * scale) / 2;
                panZoomTranslateTransform.Y = (borderHeight - imageHeight * scale) / 2;
            }
        }

        /// <summary>
        /// 进入画布的平移缩放模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPanZoomMode)
            {
                isPanZoomMode = true;
                inkCanvas.IsHitTestVisible = false;
                panZoomCanvas.MouseWheel += PanZoomCanvas_MouseWheel;
                panZoomCanvas.MouseDown += PanZoomCanvas_MouseDown;
                panZoomCanvas.MouseMove += PanZoomCanvas_MouseMove;
                panZoomCanvas.MouseUp += PanZoomCanvas_MouseUp;
            }
        }

        /// <summary>
        /// 退出画布的平移缩放模式
        /// </summary>
        private void ExitPanZoomMode()
        {
            isPanZoomMode = false;
            inkCanvas.IsHitTestVisible = true;
            panZoomCanvas.MouseWheel -= PanZoomCanvas_MouseWheel;
            panZoomCanvas.MouseDown -= PanZoomCanvas_MouseDown;
            panZoomCanvas.MouseMove -= PanZoomCanvas_MouseMove;
            panZoomCanvas.MouseUp -= PanZoomCanvas_MouseUp;
        }

        /// <summary>
        /// 启用画笔
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            ExitPanZoomMode();
        }

        /// <summary>
        /// 启用橡皮擦
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            ExitPanZoomMode();
        }

        private void ColorChange(RgbaColor color)
        {
            R = color.R;
            G = color.G;
            _B = color.B;
            A = color.A;
            TextR.Text = R.ToString();
            TextG.Text = G.ToString();
            TextB.Text = _B.ToString();
            TextA.Text = A.ToString();
            TextHex.Text = color.HexString;
        }

        private void ThumbPro_ValueChanged(double xpercent, double ypercent)
        {
            H = 360 * ypercent;
            HsbaColor Hcolor = new HsbaColor(H, 1, 1, 1);
            viewSelectColor.Fill = Hcolor.SolidColorBrush;

            Hcolor = new HsbaColor(H, S, B, 1);
            _viewModel.SelectColor = Hcolor.SolidColorBrush;

            ColorChange(Hcolor.RgbaColor);
        }

        private void ThumbPro_ValueChanged_1(double xpercent, double ypercent)
        {
            S = xpercent;
            B = 1 - ypercent;
            HsbaColor Hcolor = new HsbaColor(H, S, B, 1);

            _viewModel.SelectColor = Hcolor.SolidColorBrush;

            ColorChange(Hcolor.RgbaColor);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string text = textBox.Text;

            if (!int.TryParse(TextR.Text, out int Rvalue) || (Rvalue > 255 || Rvalue < 0))
            {
                TextR.Text = R.ToString();
                return;
            }

            if (!int.TryParse(TextG.Text, out int Gvalue) || (Gvalue > 255 || Gvalue < 0))
            {
                TextG.Text = G.ToString();
                return;
            }

            if (!int.TryParse(TextB.Text, out int Bvalue) || (Bvalue > 255 || Bvalue < 0))
            {
                TextB.Text = _B.ToString();
                return;
            }
            if (!int.TryParse(TextA.Text, out int Avalue) || (Avalue > 255 || Avalue < 0))
            {
                TextA.Text = A.ToString();
                return;
            }

            R = Rvalue; G = Gvalue; _B = Bvalue; A = Avalue;

            RgbaColor Hcolor = new RgbaColor(R, G, _B, A);
            _viewModel.SelectColor = Hcolor.SolidColorBrush;

            TextHex.Text = Hcolor.HexString;

            // 转换 RGBA 到 HSB
            HsbaColor hsbaColor = Hcolor.ToHsbaColor();
            H = hsbaColor.H;
            S = hsbaColor.S;
            B = hsbaColor.B;

            // 更新滑块位置
            // 更新 thumbH 滑块的位置（通过 Y 轴控制色相的选择）
            thumbH.UpdatePositionByPercent(0.0, H / 360.0);

            // 更新 thumbSB 滑块的位置（通过 X 轴控制饱和度，Y 轴控制亮度）
            thumbSB.UpdatePositionByPercent(S, 1.0 - B);
        }

        private void HexTextLostFocus(object sender, RoutedEventArgs e)
        {
            // 解析输入的十六进制颜色值
            RgbaColor Hcolor = new RgbaColor(TextHex.Text);

            // 更新颜色显示
            _viewModel.SelectColor = Hcolor.SolidColorBrush;

            // 更新文本框中的颜色值
            TextR.Text = Hcolor.R.ToString();
            TextG.Text = Hcolor.G.ToString();
            TextB.Text = Hcolor.B.ToString();
            TextA.Text = Hcolor.A.ToString();

            // 转换 RGBA 到 HSB
            HsbaColor hsbaColor = Hcolor.ToHsbaColor();
            H = hsbaColor.H;
            S = hsbaColor.S;
            B = hsbaColor.B;

            // 更新滑块位置
            // 更新 thumbH 滑块的位置（通过 Y 轴控制色相的选择）
            thumbH.UpdatePositionByPercent(0.0, H / 360.0);

            // 更新 thumbSB 滑块的位置（通过 X 轴控制饱和度，Y 轴控制亮度）
            thumbSB.UpdatePositionByPercent(S, 1.0 - B);
        }

        private void PanZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (isPanZoomMode)
            {
                // 获取鼠标相对于 panZoomCanvas 的位置
                Point mousePosition = e.GetPosition(panZoomCanvas);

                // 缩放增量，根据需要调整系数以控制缩放速度
                double deltaScale = e.Delta * 0.001;

                // 定义最小和最大缩放比例
                double minScale = 0.1;
                double maxScale = 10.0;

                // 计算新的缩放比例
                double newScaleX = panZoomScaleTransform.ScaleX + deltaScale;
                double newScaleY = panZoomScaleTransform.ScaleY + deltaScale;

                // 限制缩放比例
                if (newScaleX < minScale || newScaleX > maxScale)
                    return;

                // 计算平移调整，使得缩放以鼠标为中心
                panZoomTranslateTransform.X -= mousePosition.X * deltaScale;
                panZoomTranslateTransform.Y -= mousePosition.Y * deltaScale;

                // 更新缩放
                panZoomScaleTransform.ScaleX = newScaleX;
                panZoomScaleTransform.ScaleY = newScaleY;

                // 防止事件继续冒泡
                e.Handled = true;
            }
        }

        private void PanZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isPanZoomMode && e.LeftButton == MouseButtonState.Pressed)
            {
                isPanning = true;
                lastMousePosition = e.GetPosition(imageBorder);
                panZoomCanvas.CaptureMouse();
            }
        }

        private void PanZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanZoomMode && isPanning)
            {
                Point currentPosition = e.GetPosition(imageBorder);
                Vector delta = currentPosition - lastMousePosition;
                panZoomTranslateTransform.X += delta.X;
                panZoomTranslateTransform.Y += delta.Y;
                lastMousePosition = currentPosition;
            }
        }

        private void PanZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanZoomMode && e.LeftButton == MouseButtonState.Released)
            {
                isPanning = false;
                panZoomCanvas.ReleaseMouseCapture();
            }
        }

        private void Upload_To_I2I_Click(object sender, RoutedEventArgs e)
        {
            // 获取ImageViewerControl.ImageSource的BitmapFrame对象
            BitmapFrame bitmapFrame = ImageViewerControl.ImageSource as BitmapFrame;
            BitmapImage bitmapImage = Common.tools.ConvertBitmapFrameToBitmapImage(bitmapFrame);
            //渲染到RenderImage
            originalImage = bitmapImage;
            if (originalImage != null) RenderImage(originalImage);
        }

        // Border 尺寸变化事件
        private void ImageBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (originalImage != null) RenderImage(originalImage);
        }
        // 获取原始图像的方法
        private BitmapImage GetOriginalImage()
        {
            if (originalImage != null)
            {
                return originalImage;
            }
            else
            {
                MessageBox.Show("尚未加载任何图像！");
                return null;
            }
        }
        private void ExportMaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("尚未加载任何图像！");
                return;
            }

            // 保存当前的变换
            double savedScaleX = panZoomScaleTransform.ScaleX;
            double savedScaleY = panZoomScaleTransform.ScaleY;
            double savedTranslateX = panZoomTranslateTransform.X;
            double savedTranslateY = panZoomTranslateTransform.Y;

            // 重置变换
            panZoomScaleTransform.ScaleX = 1.0;
            panZoomScaleTransform.ScaleY = 1.0;
            panZoomTranslateTransform.X = 0;
            panZoomTranslateTransform.Y = 0;

            // 强制布局更新
            panZoomCanvas.UpdateLayout();

            // 创建与原始图像大小一致的 RenderTargetBitmap
            int imageWidth = originalImage.PixelWidth;
            int imageHeight = originalImage.PixelHeight;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                imageWidth, imageHeight, 96, 96, PixelFormats.Pbgra32);

            // 创建一个DrawingVisual，用于绘制背景和笔画
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                // 绘制黑色背景
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, imageWidth, imageHeight));

                // 设置笔画的绘制属性为白色
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    // 创建白色的绘制属性
                    DrawingAttributes whiteAttributes = new DrawingAttributes
                    {
                        Color = Colors.White,
                        Width = stroke.DrawingAttributes.Width,
                        Height = stroke.DrawingAttributes.Height,
                        IgnorePressure = stroke.DrawingAttributes.IgnorePressure,
                        StylusTip = stroke.DrawingAttributes.StylusTip,
                        StylusTipTransform = stroke.DrawingAttributes.StylusTipTransform
                    };

                    // 创建新的笔画，应用白色属性
                    Stroke whiteStroke = new Stroke(stroke.StylusPoints, whiteAttributes);

                    // 绘制笔画
                    whiteStroke.Draw(dc);
                }
            }

            // 渲染到位图
            renderBitmap.Render(drawingVisual);

            // 保存为PNG
            //SaveFileDialog saveFileDialog = new SaveFileDialog
            //{
            //    Filter = "PNG文件|*.png",
            //    FileName = "mask.png"
            //};
            //if (saveFileDialog.ShowDialog() == true)
            //{
            //    using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
            //    {
            //        PngBitmapEncoder encoder = new PngBitmapEncoder();
            //        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            //        encoder.Save(fs);
            //    }
            //    MessageBox.Show("蒙版已成功导出！");
            //}

            // 将位图设置为Image控件的Source
            MaskImageSource.Source = renderBitmap;
            MaskViewBorder.Visibility = Visibility.Visible;
            maskImage = renderBitmap;
            // 恢复变换
            panZoomScaleTransform.ScaleX = savedScaleX;
            panZoomScaleTransform.ScaleY = savedScaleY;
            panZoomTranslateTransform.X = savedTranslateX;
            panZoomTranslateTransform.Y = savedTranslateY;

            // 强制布局更新
            panZoomCanvas.UpdateLayout();
        }

        private void DelMaskBth_Click(object sender, RoutedEventArgs e)
        {
            MaskImageSource.Source = null;
            MaskViewBorder.Visibility = Visibility.Collapsed;
            maskImage = null;
        }

    }
    /// <summary>
    /// 封装Canvas 到Thumb来简化 Thumb的使用，关注熟悉X,Y 表示 thumb在坐标中距离左，上的距离
    /// 默认canvas 里用一个小圆点来表示当前位置
    /// </summary>
    public class ThumbPro : Thumb
    {
        //距离Canvas的Top,模板中需要Canvas.Top 绑定此Top
        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Top.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TopProperty = DependencyProperty.Register("Top", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

        //距离Canvas的Top,模板中需要Canvas.Left 绑定此Left
        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Left.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeftProperty = DependencyProperty.Register("Left", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));
        private double FirstTop;
        private double FirstLeft;

        //小圆点的半径
        public double Xoffset { get; set; }
        public double Yoffset { get; set; }
        public bool VerticalOnly { get; set; } = false;

        public double Xpercent { get { return (Left + Xoffset) / ActualWidth; } }
        public double Ypercent { get { return (Top + Yoffset) / ActualHeight; } }

        public event Action<double, double> ValueChanged;

        public ThumbPro()
        {
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                if (!VerticalOnly)
                    Left = -Xoffset;
                Top = -Yoffset;
            };

            DragStarted += (object sender, DragStartedEventArgs e) =>
            {
                // 当开始拖动时，记录当前位置
                if (!VerticalOnly)
                {
                    Left = e.HorizontalOffset - Xoffset;
                    FirstLeft = Left;
                }
                Top = e.VerticalOffset - Yoffset;
                FirstTop = Top;

                // 触发事件
                ValueChanged?.Invoke(Xpercent, Ypercent);
            };

            DragDelta += (object sender, DragDeltaEventArgs e) =>
            {
                // 按住拖拽时，点随着鼠标移动
                if (!VerticalOnly)
                {
                    double x = FirstLeft + e.HorizontalChange;
                    Left = Clamp(x, -Xoffset, ActualWidth - Xoffset);
                }

                double y = FirstTop + e.VerticalChange;
                Top = Clamp(y, -Yoffset, ActualHeight - Yoffset);

                // 触发事件
                ValueChanged?.Invoke(Xpercent, Ypercent);
            };
        }
        private double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public void SetTopLeftByPercent(double xpercent, double ypercent)
        {
            Top = ypercent * ActualHeight - Yoffset;
            if (!VerticalOnly)
                Left = xpercent * ActualWidth - Xoffset;
        }

        public void UpdatePositionByPercent(double xpercent, double ypercent)
        {
            // 更新滑块位置
            SetTopLeftByPercent(xpercent, ypercent);

            // 更新拖动的初始位置
            FirstLeft = Left;
            FirstTop = Top;

            // 手动触发 ValueChanged 事件
            ValueChanged?.Invoke(Xpercent, Ypercent);
        }

        public void InitializePosition(double xpercent, double ypercent)
        {
            SetTopLeftByPercent(xpercent, ypercent);
            ValueChanged?.Invoke(Xpercent, Ypercent);
        }

        public void AnimateToPercent(double xpercent, double ypercent, double durationSeconds)
        {
            var topAnimation = new DoubleAnimation
            {
                To = ypercent * ActualHeight - Yoffset,
                Duration = TimeSpan.FromSeconds(durationSeconds),
            };

            var leftAnimation = new DoubleAnimation
            {
                To = xpercent * ActualWidth - Xoffset,
                Duration = TimeSpan.FromSeconds(durationSeconds),
            };

            BeginAnimation(TopProperty, topAnimation);
            if (!VerticalOnly)
            {
                BeginAnimation(LeftProperty, leftAnimation);
            }

            Dispatcher.InvokeAsync(() =>
            {
                ValueChanged?.Invoke(Xpercent, Ypercent);
            });
        }
    }
}
