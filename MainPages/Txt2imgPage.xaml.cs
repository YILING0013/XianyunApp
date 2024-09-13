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
using System.Windows.Media.Animation;
using xianyun.Common;
using xianyun.API;

namespace xianyun.MainPages
{
    /// <summary>
    /// Txt2imgPage.xaml 的交互逻辑
    /// </summary>
    public partial class Txt2imgPage : Page
    {
        private bool dragInProgress = false;
        private bool isDrawerOpen = false;
        private DragAdorner currentAdorner;
        private MainViewModel _viewModel;
        public Txt2imgPage()
        {
            InitializeComponent();
            _viewModel = App.GlobalViewModel;
            this.DataContext = _viewModel;
            this.Loaded += Txt2imgPage_Loaded;
            this.Unloaded += Txt2imgPage_Unloaded;
        }
        public class NoteModel
        {
            public string Name { get; set; }
            public string PositivePrompt { get; set; }
            public string NegativePrompt { get; set; }
            public string ImagePath { get; set; }
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
                button.IsEnabled = false; // 锁定按钮，防止重复点击
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
                button.IsEnabled = true; // 恢复按钮为可点击状态
                _isGenerating = false; // 标记为生成已完成
            }
        }
        private async Task GenerateImageRequest()
        {
            try
            {
                var apiClient = new XianyunApiClient("https://nai3.xianyun.cool", SessionManager.Session);
                Console.WriteLine(SessionManager.Session);

                // 获取 VibeTransfer 的数据
                var (base64Images, informationExtracted, referenceStrength) = ExtractImageData();

                // 生成随机种子的方法
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
                        Sm = _viewModel.IsSMEA,
                        SmDyn = _viewModel.IsDYN,
                        PictureId = TotpGenerator.GenerateTotp(_viewModel._secretKey)
                    };

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
                        await Task.Delay(2000); // 轮询延迟
                    }

                    // 继续更新进度并处理图像生成
                    while (_viewModel.ProgressValue < 96)
                    {
                        var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                        if (status == "completed")
                        {
                            _viewModel.ProgressValue = 100;
                            Console.WriteLine("图像生成成功！");

                            var bitmapFrame = _viewModel.ConvertBase64ToBitmapFrame(imageBase64);
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
                            var bitmapFrame = _viewModel.ConvertBase64ToBitmapFrame(imageBase64);
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
                Console.WriteLine("错误: " + ex.Message);
            }
        }
        private void Txt2imgPage_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (ConfigurationService.ConfigurationExists())
            {
                _viewModel.LoadParameters();
            }
            if (viewModel != null)
            {
                viewModel.ImgPreviewArea = ImgPreviewArea;
                viewModel.ImageStackPanel = ImageStackPanel;
                viewModel.ImageViewerControl = ImageViewerControl;
            }
            InputTextBox.Text = viewModel.PositivePrompt;
            UpdateTagsContainer();
        }
        private void Txt2imgPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 保存当前配置
            _viewModel.SaveParameters();
        }
        private void TagMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isDrawerOpen)
            {
                Storyboard storyboard = (Storyboard)FindResource("RightTagMenuClose");
                storyboard.Begin();
            }
            else
            {
                Storyboard storyboard = (Storyboard)FindResource("RightTagMenu");
                storyboard.Begin();
            }

            isDrawerOpen = !isDrawerOpen;
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

                // 隐藏上传标志
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

                // 隐藏上传标志
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
                        newTagControl.TextChanged += TagControl_TextChanged; // 监听文本内容变化事件
                        newTagControl.TagDeleted += TagControl_TagDeleted; // Subscribe to the delete event
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
        private void TagControl_TagDeleted(object sender, EventArgs e)
        {
            if (sender is TagControl tagControl)
            {
                TagsContainer.Children.Remove(tagControl);
                UpdateViewModelTagsText();  // Update PositivePrompt after removal
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
                            // 如果不存在该标签，创建新的 TagControl 并添加到容器中
                            TagControl newTagControl = new TagControl(tag, tag);
                            newTagControl.TextChanged += TagControl_TextChanged; // 监听文本内容变化事件
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

                // 隐藏TextBox
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
    }
}
