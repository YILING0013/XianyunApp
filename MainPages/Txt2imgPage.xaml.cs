using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using xianyun.API;
using xianyun.Common;
using xianyun.UserControl;
using xianyun.ViewModel;
using AduSkin.Utility.Media;
using static xianyun.ViewModel.MainViewModel;

namespace xianyun.MainPages
{
    /// <summary>
    /// Txt2imgPage.xaml 的交互逻辑
    /// </summary>
    public partial class Txt2imgPage : Page
    {
        #region Fields

        private bool _dragInProgress;
        private bool _isTagMenuOpen;
        private bool _isCancelling;
        private bool _isGenerating;

        // InkCanvas undo/redo
        private readonly Stack<Stroke> _undoStack = new Stack<Stroke>();
        private readonly Stack<Stroke> _redoStack = new Stack<Stroke>();

        // Transformations for pan/zoom
        private TransformGroup _panZoomTransformGroup;
        private ScaleTransform _panZoomScaleTransform;
        private TranslateTransform _panZoomTranslateTransform;
        private Point _lastMousePosition;
        private bool _isPanning;
        private bool _isPanZoomMode;

        // HSBA / RGBA fields
        private double _H;
        private double _S = 1;
        private double _B = 1;
        private int _R = 255;
        private int _G = 255;
        private int _BVal = 255;
        private int _AVal = 255;
        private float _AFloat = 255;

        // Original image references
        private BitmapImage _originalImage;
        private RenderTargetBitmap _maskImage;

        // Drag adorner
        private DragAdorner _currentAdorner;

        private MainViewModel _viewModel;

        private const string BackupFilePath = "characterPromptsBackup.json";

        public class CharacterPromptBackup
        {
            public string Prompt { get; set; }
            public string UndesiredContent { get; set; }
            public string SelectedPosition { get; set; }
            public string BorderColor { get; set; }
        }

        #endregion

        #region Constructor / Initialization

        public Txt2imgPage()
        {
            InitializeComponent();
            _viewModel = App.GlobalViewModel;
            DataContext = _viewModel;

            Loaded += Txt2imgPage_Loaded;

            // Setup InkCanvas
            inkCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = (Color)ColorConverter.ConvertFromString(TextHex.Text),
                Height = _viewModel.BrushHeight,
                Width = _viewModel.BrushWidth,
                IgnorePressure = _viewModel.IsIgnorePenPressure
            };
            inkCanvas.Background = Brushes.Transparent;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.UseCustomCursor = true;
            inkCanvas.Cursor = Cursors.Cross;
            inkCanvas.IsHitTestVisible = false;
            inkCanvas.StrokeCollected += InkCanvas_StrokeCollected;

            if (string.IsNullOrEmpty(SessionManager.Session) && !string.IsNullOrEmpty(SessionManager.Token))
            {
                Opus.Text = "剩余点数:" + SessionManager.Opus;
            }
            else
            {
                // Hide the "Opus" text if no valid token
                Opus.Visibility = Visibility.Collapsed;
                _viewModel.DrawingMaxFrequency = 10;
            }
            LogPage.LogMessage(LogLevel.INFO, "绘图初始化成功");
        }

        private void Txt2imgPage_Loaded(object sender, RoutedEventArgs e)
        {
            // If configuration exists, set IsLoading to true and persist parameters
            if (ConfigurationService.ConfigurationExists())
            {
                var app = (App)Application.Current;
                app.IsLoading = true;
                _viewModel.SaveParameters();
            }

            // Link relevant UI elements to the ViewModel
            _viewModel.ImgPreviewArea = ImgPreviewArea;
            _viewModel.ImageStackPanel = ImageStackPanel;
            _viewModel.ImageViewerControl = ImageViewerControl;

            InputTextBox.Text = _viewModel.PositivePrompt;
            UpdateTagsContainer();

            // Load JSON files into TreeView
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json_files");
            LoadJsonFilesToTreeView(folderPath);

            // Restore character prompts data
            ImportCharacterPromptsData(CharacterPromptsWrapPanel, BackupFilePath);

            // Load notes
            LoadNotesFromFile();
        }

        #endregion

        #region CharacterPrompts Backup / Restore

        /// <summary>
        /// Exports the CharacterPrompts data in the WrapPanel to a JSON file.
        /// </summary>
        public void ExportCharacterPromptsData(WrapPanel characterPromptsWrapPanel, string filePath)
        {
            var backupData = new List<CharacterPromptBackup>();

            foreach (object child in characterPromptsWrapPanel.Children)
            {
                if (child is CharacterPrompts characterPrompt)
                {
                    dynamic state = characterPrompt.GetControlState();
                    var border = characterPrompt.FindName("CharacterBorder") as Border;
                    string borderColor = border?.BorderBrush.ToString();

                    backupData.Add(new CharacterPromptBackup
                    {
                        Prompt = state.prompt,
                        UndesiredContent = state.uc,
                        SelectedPosition = state.selectedPosition,
                        BorderColor = borderColor
                    });
                }
            }

            string json = JsonConvert.SerializeObject(backupData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Imports the CharacterPrompts data from a JSON file into the WrapPanel.
        /// </summary>
        public void ImportCharacterPromptsData(WrapPanel characterPromptsWrapPanel, string filePath)
        {
            characterPromptsWrapPanel.Children.Clear();
            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            var backupData = JsonConvert.DeserializeObject<List<CharacterPromptBackup>>(json);
            if (backupData == null) return;

            foreach (CharacterPromptBackup data in backupData)
            {
                var newCharacterPrompt = new CharacterPrompts
                {
                    Prompt = { Text = data.Prompt },
                    UndesiredContent = { Text = data.UndesiredContent },
                    SelectedPositionText = { Text = data.SelectedPosition }
                };

                if (!string.IsNullOrEmpty(data.BorderColor))
                {
                    var border = newCharacterPrompt.FindName("CharacterBorder") as Border;
                    if (border != null)
                    {
                        var colorConverter = new BrushConverter();
                        try
                        {
                            border.BorderBrush = (Brush)colorConverter.ConvertFromString(data.BorderColor);
                        }
                        catch
                        {
                            // Ignore invalid color
                        }
                    }
                }
                characterPromptsWrapPanel.Children.Add(newCharacterPrompt);
                CharacterPromptsStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Color Picker & RGBA/HSBA Handling

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HexTextLostFocus(sender, e);
                (sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox_LostFocus(sender, e);
                (sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }

        private void TextHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (inkCanvas == null || string.IsNullOrWhiteSpace(TextHex.Text)) return;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TextHex.Text);
                inkCanvas.DefaultDrawingAttributes.Color = color;
            }
            catch (FormatException)
            {
                // If invalid color, default to white
                inkCanvas.DefaultDrawingAttributes.Color = Colors.White;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Validate RGBA
            if (!int.TryParse(TextR.Text, out int Rvalue) || Rvalue is < 0 or > 255)
            {
                TextR.Text = _R.ToString();
                return;
            }
            if (!int.TryParse(TextG.Text, out int Gvalue) || Gvalue is < 0 or > 255)
            {
                TextG.Text = _G.ToString();
                return;
            }
            if (!int.TryParse(TextB.Text, out int Bvalue) || Bvalue is < 0 or > 255)
            {
                TextB.Text = _BVal.ToString();
                return;
            }
            if (!int.TryParse(TextA.Text, out int Avalue) || Avalue is < 0 or > 255)
            {
                TextA.Text = _AVal.ToString();
                return;
            }

            _R = Rvalue;
            _G = Gvalue;
            _BVal = Bvalue;
            _AVal = Avalue;
            _AFloat = _AVal / 255f;

            var rgbaColor = new RgbaColor(_R, _G, _BVal, _AVal);
            _viewModel.SelectColor = rgbaColor.SolidColorBrush;
            TextHex.Text = rgbaColor.HexString;

            // Convert RGBA to HSBA
            HsbaColor hsbaColor = rgbaColor.ToHsbaColor();
            _H = hsbaColor.H;
            _S = hsbaColor.S;
            _B = hsbaColor.B;

            thumbH.UpdatePositionByPercent(0.0, _H / 360.0);
            thumbSB.UpdatePositionByPercent(_S, 1.0 - _B);
            thumbA.UpdatePositionByPercent(1 - _AFloat, 0.0);
        }

        private void HexTextLostFocus(object sender, RoutedEventArgs e)
        {
            var rgbaColor = new RgbaColor(TextHex.Text);
            _viewModel.SelectColor = rgbaColor.SolidColorBrush;
            TextR.Text = rgbaColor.R.ToString();
            TextG.Text = rgbaColor.G.ToString();
            TextB.Text = rgbaColor.B.ToString();
            TextA.Text = rgbaColor.A.ToString();

            var hsbaColor = rgbaColor.ToHsbaColor();
            _H = hsbaColor.H;
            _S = hsbaColor.S;
            _B = hsbaColor.B;
            _AVal = rgbaColor.A;

            thumbH.UpdatePositionByPercent(0.0, _H / 360.0);
            thumbSB.UpdatePositionByPercent(_S, 1.0 - _B);
            thumbA.UpdatePositionByPercent(1 - (rgbaColor.A / 255.0), 0.0);
        }

        private void ThumbPro_ValueChanged(double xpercent, double ypercent)
        {
            // Hue
            _H = 360 * ypercent;
            var hColor = new HsbaColor(_H, 1, 1, 1);
            viewSelectColor.Fill = hColor.SolidColorBrush;

            var newColor = new HsbaColor(_H, _S, _B, _AVal / 255.0);
            _viewModel.SelectColor = newColor.SolidColorBrush;
            _viewModel.SelectColor_A = newColor.Color;

            ColorChange(newColor.RgbaColor);
        }

        private void ThumbPro_ValueChanged_1(double xpercent, double ypercent)
        {
            // Saturation, Brightness
            _S = xpercent;
            _B = 1 - ypercent;

            var newColor = new HsbaColor(_H, _S, _B, _AVal / 255.0);
            _viewModel.SelectColor = newColor.SolidColorBrush;
            _viewModel.SelectColor_A = newColor.Color;

            ColorChange(newColor.RgbaColor);
        }

        private void ThumbPro_ValueChanged_A(double xpercent, double ypercent)
        {
            // Alpha
            _AVal = (int)((1 - xpercent) * 255);
            var rgbaColor = new RgbaColor(_R, _G, _BVal, _AVal);
            _viewModel.SelectColor = rgbaColor.SolidColorBrush;
            TextA.Text = _AVal.ToString();
            TextHex.Text = rgbaColor.HexString;
        }

        private void ColorChange(RgbaColor color)
        {
            _R = color.R;
            _G = color.G;
            _BVal = color.B;
            _AVal = color.A;

            TextR.Text = _R.ToString();
            TextG.Text = _G.ToString();
            TextB.Text = _BVal.ToString();
            TextA.Text = _AVal.ToString();
            TextHex.Text = color.HexString;
        }

        #endregion

        #region Emotion Defry

        private void EmotionDefryReduce_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Emotion_Defry > 0)
            {
                _viewModel.Emotion_Defry--;
                UpdateDefryGrade();
            }
            EmotionDefryReduce.IsEnabled = _viewModel.Emotion_Defry > 0;
            EmotionDefryPlus.IsEnabled = _viewModel.Emotion_Defry < 5;
        }

        private void EmotionDefryPlus_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Emotion_Defry < 5)
            {
                _viewModel.Emotion_Defry++;
                UpdateDefryGrade();
            }
            EmotionDefryPlus.IsEnabled = _viewModel.Emotion_Defry < 5;
            EmotionDefryReduce.IsEnabled = _viewModel.Emotion_Defry > 0;
        }

        private void UpdateDefryGrade()
        {
            string[] grades = { "Normal", "Slightly Weak", "Weak", "Even Weaker", "Very Weak", "Weakest" };
            defryGrade.Text = grades[_viewModel.Emotion_Defry];
        }

        #endregion

        #region Width / Height Adjustment

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HeightSlider == null) return;
            double widthValue = e.NewValue;
            double maxHeight = CalculateMaxHeight(widthValue);

            HeightSlider.Maximum = maxHeight;
            if (_viewModel.Height > maxHeight)
            {
                _viewModel.Height = (int)maxHeight;
            }
        }

        private double CalculateMaxHeight(double widthValue)
        {
            double minBound = 1011712;
            double maxBound = 1048576;
            double maxHeight = 512;
            double closestBelow = 512;
            double closestBelowDiff = double.MaxValue;

            if (_viewModel.UseOpsEnabled)
            {
                minBound = 3063808;
                maxBound = 3145728;
            }

            // Step through the possible heights
            double upperLimit = _viewModel.UseOpsEnabled ? 4096 : 2048;
            for (double x = 512; x <= upperLimit; x += 64)
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

            return (maxHeight != 512) ? maxHeight : closestBelow;
        }

        #endregion

        #region Tag Tree / Search

        private void LoadJsonFilesToTreeView(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("指定的文件夹不存在。");
                return;
            }

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            foreach (string file in jsonFiles)
            {
                string jsonContent = File.ReadAllText(file);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonContent);

                // Create top-level node for file
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                TreeViewItem fileNode = new TreeViewItem { Header = fileName };

                foreach (var category in dict)
                {
                    TreeViewItem categoryNode = new TreeViewItem
                    {
                        Header = category.Key,
                        Tag = category.Value // store the dictionary in Tag
                    };
                    fileNode.Items.Add(categoryNode);
                }
                TagTreeView.Items.Add(fileNode);
            }
        }

        private void TagTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TagTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Dictionary<string, string> dictionary)
            {
                TagListBox.Items.Clear();
                foreach (string key in dictionary.Keys)
                {
                    TagListBox.Items.Add(key);
                }
            }
        }

        private void TagListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // If from search results
            if (TagListBox.SelectedItem is ListBoxItem listBoxItem && listBoxItem.Tag is string valueFromSearch)
            {
                string selectedKey = listBoxItem.Content.ToString();
                var newTagControl = new TagControl(valueFromSearch, valueFromSearch, selectedKey);
                newTagControl.TextChanged += TagControl_TextChanged;
                newTagControl.TagDeleted += TagControl_TagDeleted;
                TagsContainer.Children.Add(newTagControl);
            }
            // If directly from the TagTreeView
            else if (TagListBox.SelectedItem is string selectedKey)
            {
                if (TagTreeView.SelectedItem is TreeViewItem treeItem && treeItem.Tag is Dictionary<string, string> dictionary)
                {
                    string selectedValue = dictionary[selectedKey];
                    var newTagControl = new TagControl(selectedValue, selectedValue, selectedKey);
                    newTagControl.TextChanged += TagControl_TextChanged;
                    newTagControl.TagDeleted += TagControl_TagDeleted;
                    TagsContainer.Children.Add(newTagControl);
                }
            }
            UpdateViewModelTagsText();
        }

        private void SearchInTreeView(string searchText)
        {
            TagListBox.Items.Clear();
            if (string.IsNullOrEmpty(searchText)) return;

            foreach (TreeViewItem fileNode in TagTreeView.Items)
            {
                foreach (object child in fileNode.Items)
                {
                    if (child is TreeViewItem categoryNode && categoryNode.Tag is Dictionary<string, string> dictionary)
                    {
                        foreach (var kv in dictionary)
                        {
                            bool keyMatch = kv.Key.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                            bool valueMatch = kv.Value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                            if (keyMatch || valueMatch)
                            {
                                TagListBox.Items.Add(new ListBoxItem
                                {
                                    Content = kv.Key,
                                    Tag = kv.Value
                                });
                            }
                        }
                    }
                }
            }
            if (TagListBox.Items.Count == 0)
            {
                TagListBox.Items.Add(new ListBoxItem { Content = "未找到匹配的结果。" });
            }
        }

        private void AutoSuggestBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is Wpf.Ui.Controls.AutoSuggestBox searchBox)
                {
                    string searchText = searchBox.Text;
                    SearchInTreeView(searchText);
                }
            }
        }

        private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.AutoSuggestBox searchBox)
            {
                string searchText = searchBox.Text;
                SearchInTreeView(searchText);
            }
        }

        #endregion

        #region Notebook (Save / Load Notes)

        public class NoteModel
        {
            public string Name { get; set; }
            public string PositivePrompt { get; set; }
            public string NegativePrompt { get; set; }
            public string ImagePath { get; set; }
            public List<CharacterPromptBackup> CharacterPromptsData { get; set; }
        }

        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            string noteName = Microsoft.VisualBasic.Interaction.InputBox("请输入保存名称", "保存笔记", "");
            if (string.IsNullOrEmpty(noteName)) return;

            var existingNote = _viewModel.Notes.FirstOrDefault(n => n.Name == noteName);
            if (existingNote != null)
            {
                MessageBox.Show("该名称已存在，请选择其他名称。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string positivePrompt = _viewModel.PositivePrompt;
            string negativePrompt = _viewModel.NegitivePrompt;

            var characterPromptsData = new List<CharacterPromptBackup>();
            foreach (object child in CharacterPromptsWrapPanel.Children)
            {
                if (child is CharacterPrompts characterPrompt)
                {
                    dynamic state = characterPrompt.GetControlState();
                    var border = characterPrompt.FindName("CharacterBorder") as Border;
                    string borderColor = border?.BorderBrush.ToString();

                    characterPromptsData.Add(new CharacterPromptBackup
                    {
                        Prompt = state.prompt,
                        UndesiredContent = state.uc,
                        SelectedPosition = state.selectedPosition,
                        BorderColor = borderColor
                    });
                }
            }

            // Save image if available
            string imagePath = null;
            if (ImageViewerControl.ImageSource != null)
            {
                string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NoteBookImgPath");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                imagePath = System.IO.Path.Combine(folderPath, $"{noteName}.png");
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)ImageViewerControl.ImageSource));
                using var fileStream = new FileStream(imagePath, FileMode.Create);
                encoder.Save(fileStream);
            }
            else
            {
                MessageBox.Show("没有图像可保存，保存的笔记将不包含图像。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            var newNote = new NoteModel
            {
                Name = noteName,
                PositivePrompt = positivePrompt,
                NegativePrompt = negativePrompt,
                ImagePath = imagePath,
                CharacterPromptsData = characterPromptsData
            };

            _viewModel.Notes.Add(newNote);
            SaveNoteToFile(newNote);
            UpdateListBoxItems();
        }

        private void SaveNoteToFile(NoteModel note)
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes.json");
            var notes = new List<NoteModel>();

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                notes = JsonConvert.DeserializeObject<List<NoteModel>>(json) ?? new List<NoteModel>();
            }
            notes.Add(note);

            string newJson = JsonConvert.SerializeObject(notes, Formatting.Indented);
            File.WriteAllText(filePath, newJson);
        }

        private void LoadNotesFromFile()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes.json");
            if (!File.Exists(filePath)) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var notes = JsonConvert.DeserializeObject<List<NoteModel>>(json) ?? new List<NoteModel>();
                _viewModel.Notes.Clear();

                foreach (var note in notes)
                {
                    _viewModel.Notes.Add(note);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载笔记时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LogPage.LogMessage(LogLevel.ERROR, "加载笔记时发生错误: " + ex.Message);
            }
        }

        private void UpdateListBoxItems()
        {
            ListBox listBox = FindName("NoteBookListBox") as ListBox;
            if (listBox != null)
            {
                listBox.ItemsSource = null;
                listBox.ItemsSource = _viewModel.Notes;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                PositiveTextBox.Text = selectedNote.PositivePrompt;
                NegativeTextBox.Text = selectedNote.NegativePrompt;

                // Preview image
                if (!string.IsNullOrEmpty(selectedNote.ImagePath) && File.Exists(selectedNote.ImagePath))
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new FileStream(selectedNote.ImagePath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    NoteImgPreview.Source = bitmap;
                }
                else
                {
                    NoteImgPreview.Source = null;
                }

                // Rebuild character prompts
                CharacterPromptsWrapPanel.Children.Clear();
                if (selectedNote.CharacterPromptsData != null)
                {
                    foreach (var data in selectedNote.CharacterPromptsData)
                    {
                        var newCharacterPrompt = new CharacterPrompts
                        {
                            Prompt = { Text = data.Prompt },
                            UndesiredContent = { Text = data.UndesiredContent },
                            SelectedPositionText = { Text = data.SelectedPosition }
                        };

                        if (!string.IsNullOrEmpty(data.BorderColor))
                        {
                            var border = newCharacterPrompt.FindName("CharacterBorder") as Border;
                            if (border != null)
                            {
                                var colorConverter = new BrushConverter();
                                try
                                {
                                    border.BorderBrush = (Brush)colorConverter.ConvertFromString(data.BorderColor);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }
                        CharacterPromptsWrapPanel.Children.Add(newCharacterPrompt);
                    }
                }
            }
        }

        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                NoteImgPreview.Source = null;
                if (!string.IsNullOrEmpty(selectedNote.ImagePath) && File.Exists(selectedNote.ImagePath))
                {
                    try
                    {
                        File.Delete(selectedNote.ImagePath);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show($"删除文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogPage.LogMessage(LogLevel.ERROR, "删除文件时发生错误: " + ex.Message);
                        return;
                    }
                }
                _viewModel.Notes.Remove(selectedNote);
                RemoveNoteFromFile(selectedNote);
            }
            else
            {
                MessageBox.Show("请先选择一个笔记。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveNoteFromFile(NoteModel note)
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notes.json");
            if (!File.Exists(filePath)) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var notes = JsonConvert.DeserializeObject<List<NoteModel>>(json) ?? new List<NoteModel>();
                notes.RemoveAll(n => n.Name == note.Name);

                string newJson = JsonConvert.SerializeObject(notes, Formatting.Indented);
                File.WriteAllText(filePath, newJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新笔记文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LogPage.LogMessage(LogLevel.ERROR, "更新笔记文件时发生错误: " + ex.Message);
            }
        }

        private void UseNote_Click(object sender, RoutedEventArgs e)
        {
            if (NoteBookListBox.SelectedItem is NoteModel selectedNote)
            {
                _viewModel.PositivePrompt = selectedNote.PositivePrompt;
                _viewModel.NegitivePrompt = selectedNote.NegativePrompt;
                InputTextBox.Text = _viewModel.PositivePrompt;

                // Rebuild character prompts
                CharacterPromptsWrapPanel.Children.Clear();
                if (selectedNote.CharacterPromptsData != null)
                {
                    foreach (var data in selectedNote.CharacterPromptsData)
                    {
                        var newCharacterPrompt = new CharacterPrompts
                        {
                            Prompt = { Text = data.Prompt },
                            UndesiredContent = { Text = data.UndesiredContent },
                            SelectedPositionText = { Text = data.SelectedPosition }
                        };

                        if (!string.IsNullOrEmpty(data.BorderColor))
                        {
                            var border = newCharacterPrompt.FindName("CharacterBorder") as Border;
                            if (border != null)
                            {
                                var colorConverter = new BrushConverter();
                                try
                                {
                                    border.BorderBrush = (Brush)colorConverter.ConvertFromString(data.BorderColor);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }
                        CharacterPromptsWrapPanel.Children.Add(newCharacterPrompt);
                    }
                }
                UpdateTagsContainerForNotes();
            }
            else
            {
                MessageBox.Show("请先选择一个笔记。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Drawing / API Generation

        /// <summary>
        /// Extracts image data (base64) and reference parameters from VibeTransfer controls.
        /// </summary>
        private (string[] base64Images, double[] informationExtracted, double[] referenceStrength) ExtractImageData()
        {
            var referenceImageMultiple = new List<string>();
            var referenceInformationExtractedMultiple = new List<double>();
            var referenceStrengthMultiple = new List<double>();

            foreach (object child in ImageWrapPanel.Children)
            {
                if (child is VibeTransfer vibeTransfer)
                {
                    string base64Image = vibeTransfer.GetImageAsBase64();
                    if (!string.IsNullOrEmpty(base64Image))
                    {
                        referenceImageMultiple.Add(base64Image);
                    }
                    referenceInformationExtractedMultiple.Add(vibeTransfer.InformationExtracted);
                    referenceStrengthMultiple.Add(vibeTransfer.ReferenceStrength);
                }
            }
            return (
                referenceImageMultiple.ToArray(),
                referenceInformationExtractedMultiple.ToArray(),
                referenceStrengthMultiple.ToArray()
            );
        }

        /// <summary>
        /// Collects character prompts data for building requests.
        /// </summary>
        private (List<CharacterPrompt> characterPrompts,
                 List<CharCaption> v4PromptCharCaptions,
                 List<CharCaption> v4NegativePromptCharCaptions)
            GetCharacterPromptsData(WrapPanel characterPromptsWrapPanel)
        {
            var characterPrompts = new List<CharacterPrompt>();
            var v4PromptCharCaptions = new List<CharCaption>();
            var v4NegativePromptCharCaptions = new List<CharCaption>();

            foreach (object child in characterPromptsWrapPanel.Children)
            {
                if (child is CharacterPrompts cp)
                {
                    dynamic state = cp.GetControlState();
                    characterPrompts.Add(new CharacterPrompt
                    {
                        Prompt = state.prompt,
                        Uc = state.uc,
                        Center = new Center { X = state.center.x, Y = state.center.y }
                    });
                    v4PromptCharCaptions.Add(new CharCaption
                    {
                        CharCaptionText = state.prompt,
                        Centers = new List<Center> { new Center { X = state.center.x, Y = state.center.y } }
                    });
                    v4NegativePromptCharCaptions.Add(new CharCaption
                    {
                        CharCaptionText = state.uc,
                        Centers = new List<Center> { new Center { X = state.center.x, Y = state.center.y } }
                    });
                }
            }
            return (characterPrompts, v4PromptCharCaptions, v4NegativePromptCharCaptions);
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // If already generating, clicking tries to cancel
            if (_isGenerating)
            {
                _isCancelling = true;
                button.Content = "正在取消...";
                button.IsEnabled = false;
                return;
            }

            _isCancelling = false;
            _isGenerating = true;
            button.Content = "取消生成";
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7DED0"));

            await GenerateImageRequest();

            // Restore button state
            button.Content = "Generate Image";
            button.Background = new SolidColorBrush(Colors.White);
            button.IsEnabled = true;
            _isGenerating = false;
        }

        private async Task GenerateImageRequest()
        {
            try
            {
                // If there's no Xianyun session but a valid NovelAI token, use NovelAI; otherwise use Xianyun
                if (string.IsNullOrEmpty(SessionManager.Session) && !string.IsNullOrEmpty(SessionManager.Token))
                {
                    var novelAiClient = new NovelAiApiClient("https://image.novelai.net", SessionManager.Token);
                    Console.WriteLine("使用 NovelAI API");

                    var (base64Images, informationExtracted, referenceStrength) = ExtractImageData();
                    var (characterPrompts, v4PromptCharCaptions, v4NegativePromptCharCaptions)
                        = GetCharacterPromptsData(CharacterPromptsWrapPanel);

                    // Random seed generator
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

                    for (int i = 0; i < _viewModel.DrawingFrequency; i++)
                    {
                        if (_isCancelling) break; // If cancel is requested, stop looping.

                        var seedValue = _viewModel.Seed ?? GenerateRandomSeed();
                        var novelAiRequest = new NovelAiRequest
                        {
                            Action = "generate",
                            Input = _viewModel.PositivePrompt,
                            Model = _viewModel.Model,
                            Parameters = new NovelAiParameters
                            {
                                AddOriginalImage = _viewModel.AddOriginalImage,
                                Width = _viewModel.Width,
                                Height = _viewModel.Height,
                                Scale = _viewModel.GuidanceScale,
                                Sampler = _viewModel.ActualSamplingMethod,
                                Steps = _viewModel.Steps,
                                Seed = (uint)seedValue,
                                QualityToggle = false,
                                Sm = _viewModel.IsSMEA,
                                SmDyn = _viewModel.IsDYN,
                                NegativePrompt = _viewModel.NegitivePrompt,
                                CfgRescale = _viewModel.GuidanceRescale,
                                Noise = (int)_viewModel.Noise,
                                Strength = _viewModel.Strength,
                                ReferenceImageMultiple = base64Images.Length > 0 ? base64Images : null,
                                ReferenceInformationExtractedMultiple = informationExtracted.Length > 0 ? informationExtracted : null,
                                ReferenceStrengthMultiple = referenceStrength.Length > 0 ? referenceStrength : null,
                                UseCoords = !_viewModel.IsUseAIChoicePositions,
                                CharacterPrompts = characterPrompts,
                                V4Prompt = new V4Prompt
                                {
                                    Caption = new V4PromptCaption
                                    {
                                        BaseCaption = _viewModel.PositivePrompt,
                                        CharCaptions = v4PromptCharCaptions
                                    },
                                    UseCoords = !_viewModel.IsUseAIChoicePositions,
                                    UseOrder = true
                                },
                                V4NegativePrompt = new V4NegativePrompt
                                {
                                    Caption = new V4NegativePromptCaption
                                    {
                                        BaseCaption = _viewModel.NegitivePrompt,
                                        CharCaptions = v4NegativePromptCharCaptions
                                    }
                                }
                            }
                        };

                        if (_viewModel.IsConvenientResolution)
                        {
                            string[] resolution = _viewModel.Resolution.Split('*');
                            novelAiRequest.Parameters.Width = int.Parse(resolution[0]);
                            novelAiRequest.Parameters.Height = int.Parse(resolution[1]);
                        }

                        if (_originalImage != null)
                        {
                            if (_originalImage is BitmapImage image)
                            {
                                int w = image.PixelWidth;
                                int h = image.PixelHeight;
                                Tools.ValidateResolution(ref w, ref h);
                                BitmapImage resized = Tools.ResizeImage(_originalImage, w, h);
                                string base64Image = Tools.ConvertImageToBase64(resized, new PngBitmapEncoder());

                                novelAiRequest.Action = "img2img";
                                novelAiRequest.Parameters.Width = w;
                                novelAiRequest.Parameters.Height = h;
                                novelAiRequest.Parameters.Image = base64Image;
                                novelAiRequest.Parameters.ExtraNoiseSeed = (uint)seedValue;
                                if (MaskImageSource.Source != null)
                                {
                                    novelAiRequest.Action = "infill";
                                    if (_viewModel.Model == "nai-diffusion-4-full")
                                    {
                                        novelAiRequest.Model = "nai-diffusion-4-full-inpainting";
                                    }
                                    else { novelAiRequest.Model = "nai-diffusion-3-inpainting"; }
                                    novelAiRequest.Parameters.Mask = Tools.ConvertRenderTargetBitmapToBase64(_maskImage);
                                }
                                if (_viewModel.ReqType != null)
                                {
                                    // Switch to stylized calls or other API calls
                                    novelAiRequest.Action = null;
                                    novelAiRequest.Input = null;
                                    novelAiRequest.Model = null;
                                    novelAiRequest.Parameters = null;
                                    novelAiRequest.Width = w;
                                    novelAiRequest.Height = h;
                                    novelAiRequest.Image = base64Image;
                                    novelAiRequest.ReqType = _viewModel.ReqType;

                                    if (_viewModel.ReqType == "emotion")
                                    {
                                        novelAiRequest.Prompt = _viewModel.ActualEmotion + ";;" + _viewModel.Emotion_Prompt;
                                        novelAiRequest.Defry = _viewModel.Emotion_Defry;
                                    }
                                    if (_viewModel.ReqType == "colorize")
                                    {
                                        novelAiRequest.Prompt = _viewModel.Colorize_Prompt;
                                        novelAiRequest.Defry = _viewModel.Colorize_Defry;
                                    }
                                }
                            }
                        }

                        _viewModel.ProgressValue = 90;
                        string imageB64 = await novelAiClient.GenerateImageAsync(novelAiRequest);
                        await GetSubscription.GetSubscriptionInfoAsync(); // Refresh subscription info

                        var bitmapFrame = Tools.ConvertBase64ToBitmapFrame(imageB64);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var imgPreview = new ImgPreview(imageB64);
                            imgPreview.ImageClicked += _viewModel.OnImageClicked;
                            ImageStackPanel.Children.Add(imgPreview);
                            ImageViewerControl.ImageSource = bitmapFrame;
                        });
                        LogPage.LogMessage(LogLevel.INFO, "图像生成成功");

                        if (_viewModel.AutoSaveEnabled)
                        {
                            string fileName = ImageSaver.GenerateFileName(_viewModel.CustomPrefix);
                            try
                            {
                                ImageSaver.SaveImage(imageB64, _viewModel.SaveDirectory, fileName);
                                MessageBox.Show("图像已保存到: " + System.IO.Path.Combine(_viewModel.SaveDirectory, fileName));
                                LogPage.LogMessage(LogLevel.INFO, "成功保存图像到: " +
                                    System.IO.Path.Combine(_viewModel.SaveDirectory, fileName));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("保存图像失败: " + ex.Message);
                                LogPage.LogMessage(LogLevel.ERROR, "保存图像失败: " + ex.Message);
                            }
                        }

                        Opus.Text = "剩余点数:" + SessionManager.Opus;
                        _viewModel.ProgressValue = 100;

                        // Wait 3s between requests
                        await Task.Delay(3000);
                    }
                }
                else
                {
                    var apiClient = new XianyunApiClient("https://nocaptchauri.idlecloud.cc", SessionManager.Session);
                    Console.WriteLine(SessionManager.Session);

                    var (base64Images, informationExtracted, referenceStrength) = ExtractImageData();
                    var (characterPrompts, v4PromptCharCaptions, v4NegativePromptCharCaptions)
                        = GetCharacterPromptsData(CharacterPromptsWrapPanel);

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

                    for (int i = 0; i < _viewModel.DrawingFrequency; i++)
                    {
                        if (_isCancelling) break; // if cancellation requested, exit

                        var seedValue = _viewModel.Seed?.ToString() ?? GenerateRandomSeed().ToString();
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
                            PictureId = TotpGenerator.GenerateTotp(_viewModel._secretKey),
                            CharacterPrompts = characterPrompts,
                            V4PromptCharCaptions = v4PromptCharCaptions,
                            V4NegativePromptCharCaptions = v4NegativePromptCharCaptions,
                            UseCoords = !_viewModel.IsUseAIChoicePositions
                        };

                        if (_viewModel.IsConvenientResolution)
                        {
                            string[] resolution = _viewModel.Resolution.Split('*');
                            imageRequest.Width = int.Parse(resolution[0]);
                            imageRequest.Height = int.Parse(resolution[1]);
                        }

                        if (base64Images.Length > 0 && informationExtracted.Length > 0 && referenceStrength.Length > 0)
                        {
                            imageRequest.ReferenceImage = base64Images;
                            imageRequest.InformationExtracted = informationExtracted;
                            imageRequest.ReferenceStrength = referenceStrength;
                        }

                        // If there's an original image, we handle i2i or custom request
                        if (_originalImage != null && _originalImage is BitmapImage orgImg)
                        {
                            int w = orgImg.PixelWidth;
                            int h = orgImg.PixelHeight;
                            Tools.ValidateResolution(ref w, ref h);
                            BitmapImage resized = Tools.ResizeImage(_originalImage, w, h);
                            string base64Img = Tools.ConvertImageToBase64(resized, new PngBitmapEncoder());

                            if (_viewModel.ReqType != null)
                            {
                                imageRequest.Width = w;
                                imageRequest.Height = h;
                                imageRequest.Image = base64Img;
                                imageRequest.ReqType = _viewModel.ReqType;

                                if (_viewModel.ReqType == "emotion")
                                {
                                    imageRequest.Prompt = _viewModel.ActualEmotion + ";;" + _viewModel.Emotion_Prompt;
                                    imageRequest.Defry = _viewModel.Emotion_Defry;
                                }
                                else if (_viewModel.ReqType == "colorize")
                                {
                                    imageRequest.Prompt = _viewModel.Colorize_Prompt;
                                    imageRequest.Defry = _viewModel.Colorize_Defry;
                                }
                            }
                            else
                            {
                                imageRequest.Width = w;
                                imageRequest.Height = h;
                                imageRequest.Image = base64Img;
                                imageRequest.Action = true;
                                imageRequest.Noise = _viewModel.Noise;
                                imageRequest.Strength = _viewModel.Strength;
                                if (MaskImageSource.Source != null)
                                {
                                    imageRequest.Mask = Tools.ConvertRenderTargetBitmapToBase64(_maskImage);
                                }
                            }
                        }

                        // Send request
                        var (jobId, initialPos) = await apiClient.GenerateImageAsync(imageRequest);
                        Console.WriteLine($"任务已提交，任务ID: {jobId}, 初始队列位置: {initialPos}");
                        LogPage.LogMessage(LogLevel.INFO, $"任务已提交，任务ID: {jobId}, 初始队列位置: {initialPos}");

                        int currentQueuePosition = initialPos;
                        _viewModel.ProgressValue = 0;

                        // Poll until job starts processing
                        while (currentQueuePosition > 0)
                        {
                            var (status, imageBase64, queuePos) = await apiClient.CheckResultAsync(jobId);
                            if (status == "processing")
                            {
                                _viewModel.ProgressValue = 70;
                                currentQueuePosition = queuePos;
                            }
                            else if (status == "queued")
                            {
                                _viewModel.ProgressValue = 70 * (1 - (double)queuePos / initialPos);
                                currentQueuePosition = queuePos;
                            }
                            if (_isCancelling) break;
                            await Task.Delay(5000);
                        }

                        // If user canceled during the queue
                        if (_isCancelling) break;

                        // Continue updating progress while processing
                        while (_viewModel.ProgressValue < 96)
                        {
                            var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                            if (status == "completed")
                            {
                                _viewModel.ProgressValue = 100;
                                LogPage.LogMessage(LogLevel.INFO, "图像生成成功！");
                                HandlePostGenerationSave(imageBase64);
                                break;
                            }
                            _viewModel.ProgressValue += new Random().Next(1, 4);
                            if (_isCancelling) break;
                            await Task.Delay(1500);
                        }

                        // Final check
                        while (_viewModel.ProgressValue < 100 && !_isCancelling)
                        {
                            await Task.Delay(2000);
                            var (status, imageBase64, _) = await apiClient.CheckResultAsync(jobId);
                            if (status == "completed")
                            {
                                _viewModel.ProgressValue = 100;
                                HandlePostGenerationSave(imageBase64);
                                break;
                            }
                        }
                        await Task.Delay(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LogPage.LogMessage(LogLevel.ERROR, "生成错误: " + ex.Message);
            }
        }

        /// <summary>
        /// Handle saving the returned Base64 image, updating UI, etc.
        /// </summary>
        private void HandlePostGenerationSave(string imageBase64)
        {
            var bitmapFrame = Tools.ConvertBase64ToBitmapFrame(imageBase64);
            Application.Current.Dispatcher.Invoke(() =>
            {
                var imgPreview = new ImgPreview(imageBase64);
                imgPreview.ImageClicked += _viewModel.OnImageClicked;
                ImageStackPanel.Children.Add(imgPreview);
                ImageViewerControl.ImageSource = bitmapFrame;
            });

            if (_viewModel.AutoSaveEnabled)
            {
                string fileName = ImageSaver.GenerateFileName(_viewModel.CustomPrefix);
                try
                {
                    ImageSaver.SaveImage(imageBase64, _viewModel.SaveDirectory, fileName);
                    LogPage.LogMessage(LogLevel.INFO, "成功保存图像到: " +
                        System.IO.Path.Combine(_viewModel.SaveDirectory, fileName));
                }
                catch (Exception ex)
                {
                    LogPage.LogMessage(LogLevel.ERROR, "保存图像失败: " + ex.Message);
                }
            }
        }

        #endregion

        #region Tag Menu & Animations

        private void TagMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isTagMenuOpen)
            {
                ((Storyboard)FindResource("RightTagMenuClose")).Begin();
            }
            else
            {
                ((Storyboard)FindResource("RightTagMenu")).Begin();
            }
            _isTagMenuOpen = !_isTagMenuOpen;
        }

        private void I2IMenuBtn_Open(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("Left_i2iMenu")).Begin();
        }

        private void I2IMenuBtn_Close(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("Left_i2iMenuClose")).Begin();
        }

        #endregion

        #region Positive Prompt Tag Management

        private void TagsContainer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (TagsContainer.Children.Count > 0)
                {
                    string tagsText = string.Join(",", TagsContainer.Children.OfType<TagControl>()
                                                .Select(tc => tc.GetAdjustedText()));
                    InputTextBox.Text = tagsText;
                }
                else
                {
                    InputTextBox.Text = string.Empty;
                }
                ScrollViewer.Visibility = Visibility.Collapsed;
                InputTextBox.Visibility = Visibility.Visible;
                InputTextBox.UpdateLayout();
                InputTextBox.Focus();
            }
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

        private void ProcessInputText()
        {
            if (InputTextBox.Visibility != Visibility.Visible) return;

            string inputText = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(inputText))
            {
                TagsContainer.Children.Clear();
                UpdateViewModelTagsText();
            }
            else
            {
                inputText = inputText.Replace("，", ","); // Convert to English comma
                string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                            .ToArray();

                var existing = TagsContainer.Children.OfType<TagControl>().ToList();
                foreach (string tag in newTags)
                {
                    var existingControl = existing.FirstOrDefault(tc => tc.GetAdjustedText() == tag);
                    if (existingControl == null)
                    {
                        var newTagControl = new TagControl(tag, tag);
                        newTagControl.TextChanged += TagControl_TextChanged;
                        newTagControl.TagDeleted += TagControl_TagDeleted;
                        TagsContainer.Children.Add(newTagControl);
                    }
                    else
                    {
                        existing.Remove(existingControl);
                    }
                }
                // Remove old
                foreach (var oldControl in existing) TagsContainer.Children.Remove(oldControl);

                UpdateViewModelTagsText();
            }
        }

        private string AutoCompleteBrackets(string text)
        {
            // Square brackets
            int leftSquare = text.Count(c => c == '[');
            int rightSquare = text.Count(c => c == ']');
            if (leftSquare > rightSquare)
            {
                text = text.PadRight(text.Length + (leftSquare - rightSquare), ']');
            }
            else if (rightSquare > leftSquare)
            {
                text = text.PadLeft(text.Length + (rightSquare - leftSquare), '[');
            }

            // Curly braces
            int leftCurly = text.Count(c => c == '{');
            int rightCurly = text.Count(c => c == '}');
            if (leftCurly > rightCurly)
            {
                text = text.PadRight(text.Length + (leftCurly - rightCurly), '}');
            }
            else if (rightCurly > leftCurly)
            {
                text = text.PadLeft(text.Length + (rightCurly - leftCurly), '{');
            }
            return text;
        }

        private void UpdateTagsContainer()
        {
            string inputText = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(inputText))
            {
                InputTextBox.Visibility = Visibility.Collapsed;
                return;
            }

            inputText = inputText.Replace("，", ",");
            string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                        .ToArray();

            var existingTagControls = TagsContainer.Children.OfType<TagControl>().ToList();
            foreach (string tag in newTags)
            {
                var existing = existingTagControls.FirstOrDefault(tc => tc.GetAdjustedText() == tag);
                if (existing == null)
                {
                    var newTagControl = new TagControl(tag, tag);
                    newTagControl.TextChanged += TagControl_TextChanged;
                    newTagControl.TagDeleted += TagControl_TagDeleted;
                    TagsContainer.Children.Add(newTagControl);
                }
                else
                {
                    existingTagControls.Remove(existing);
                }
            }

            // Remove leftover
            foreach (var leftover in existingTagControls) TagsContainer.Children.Remove(leftover);
            InputTextBox.Visibility = Visibility.Collapsed;
        }

        private void UpdateTagsContainerForNotes()
        {
            string inputText = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(inputText))
            {
                return;
            }
            inputText = inputText.Replace("，", ",");
            string[] newTags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(tag => AutoCompleteBrackets(tag.Trim()))
                                        .ToArray();

            TagsContainer.Children.Clear();
            foreach (string tag in newTags)
            {
                var newTagControl = new TagControl(tag, tag);
                newTagControl.TextChanged += TagControl_TextChanged;
                newTagControl.TagDeleted += TagControl_TagDeleted;
                TagsContainer.Children.Add(newTagControl);
            }
            UpdateViewModelTagsText();
            InputTextBox.Visibility = Visibility.Collapsed;
        }

        private void TagControl_TextChanged(object sender, EventArgs e)
        {
            UpdateViewModelTagsText();
        }

        private void TagControl_TagDeleted(object sender, EventArgs e)
        {
            if (sender is TagControl tagControl)
            {
                TagsContainer.Children.Remove(tagControl);
                UpdateViewModelTagsText();
            }
        }

        private void UpdateViewModelTagsText()
        {
            if (TagsContainer.Children.Count == 0)
            {
                _viewModel.PositivePrompt = string.Empty;
                InputTextBox.Text = string.Empty;
                return;
            }

            var tagsText = string.Join(",", TagsContainer.Children.OfType<TagControl>().Select(tc => tc.GetAdjustedText()));
            InputTextBox.Text = tagsText;
            _viewModel.PositivePrompt = tagsText;
        }

        #endregion

        #region Drag-and-Drop Handling for TagControls

        private void TagsContainer_DragOver(object sender, DragEventArgs e)
        {
            _dragInProgress = true;
            if (sender is not WrapPanel panel)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (!e.Data.GetDataPresent(typeof(TagControl)))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = DragDropEffects.Move;
            var dataObject = e.Data;
            var draggedControl = (TagControl)dataObject.GetData(typeof(TagControl));

            // Position logic
            Point position = e.GetPosition(panel);
            UIElement leftElement = null;
            UIElement rightElement = null;
            int insertIndex = -1;

            if (panel.Children.Count == 0)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            for (int i = 0; i < panel.Children.Count; i++)
            {
                var element = panel.Children[i];
                UIElement nextElement = null;
                if (i + 1 < panel.Children.Count) nextElement = panel.Children[i + 1];

                Point elementPos = element.TranslatePoint(new Point(0, 0), panel);
                double elementWidth = element.DesiredSize.Width;
                double elementHeight = element.DesiredSize.Height;
                int padding = 0;

                if (i == 0 && position.Y < elementPos.Y - padding)
                {
                    rightElement = element;
                    leftElement = null;
                    insertIndex = 0;
                    break;
                }

                bool sameRow = position.Y >= elementPos.Y - padding && position.Y <= elementPos.Y + elementHeight + padding;
                if (!sameRow) continue;

                if (position.X >= elementPos.X + elementWidth / 2)
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
                        Point nextPos = nextElement.TranslatePoint(new Point(0, 0), panel);
                        double nextWidth = nextElement.DesiredSize.Width;
                        double nextHeight = nextElement.DesiredSize.Height;
                        bool sameRowNext = position.Y >= nextPos.Y - padding && position.Y <= nextPos.Y + nextHeight + padding;

                        if (sameRowNext)
                        {
                            if (position.X <= nextPos.X + nextWidth / 2)
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

            if (insertIndex == -1)
            {
                leftElement = panel.Children[^1];
                rightElement = null;
                insertIndex = panel.Children.Count;
            }

            // Adorner layer
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer == null) return;

            if (_currentAdorner == null)
            {
                _currentAdorner = new DragAdorner(panel, draggedControl, true, 0.8);
                adornerLayer.Add(_currentAdorner);
            }

            if (leftElement != null)
            {
                Point leftPosition = leftElement.TranslatePoint(new Point(0, 0), panel);
                double lw = leftElement.DesiredSize.Width;
                double lh = leftElement.DesiredSize.Height;

                _currentAdorner.LeftOffset = leftPosition.X + lw;
                _currentAdorner.TopOffset = leftPosition.Y + lh / 2;
            }
            else if (rightElement != null)
            {
                Point rightPosition = rightElement.TranslatePoint(new Point(0, 0), panel);
                double rw = rightElement.DesiredSize.Width;
                double rh = rightElement.DesiredSize.Height;

                _currentAdorner.LeftOffset = rightPosition.X;
                _currentAdorner.TopOffset = rightPosition.Y + rh / 2;
            }
            else
            {
                adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
            }
        }

        /// <summary>
        /// 当在 ScrollViewer 中拖拽时，若接近其上下边缘，则自动滚动
        /// </summary>
        private void ScrollViewer_PreviewDragOver(object sender, DragEventArgs e)
        {
            // 确保确实是 TagControl
            if (!e.Data.GetDataPresent(typeof(TagControl)))
            {
                return;
            }

            // 获取鼠标在 ScrollViewer 中的位置
            Point pos = e.GetPosition(ScrollViewer);

            // 距离边缘多少时开始滚动
            double tolerance = 10;
            // 每次滚动多少距离
            double offset = 20;

            // 如果鼠标在可视区域顶部像素范围内，则向上滚
            if (pos.Y < tolerance)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - offset);
            }
            // 如果鼠标在可视区域底部像素范围内，则向下滚
            else if (pos.Y > ScrollViewer.ActualHeight - tolerance)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + offset);
            }
        }

        private void TagsContainer_DragLeave(object sender, DragEventArgs e)
        {
            _dragInProgress = false;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_dragInProgress)
                    TagsContainer_OnRealTargetDragLeave(sender, e);
            }));
        }

        private void TagsContainer_OnRealTargetDragLeave(object sender, DragEventArgs e)
        {
            if (sender is not WrapPanel panel) return;
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null && _currentAdorner != null)
            {
                adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
            }
        }

        private void TagsContainer_DragEnter(object sender, DragEventArgs e)
        {
            _dragInProgress = true;
        }

        private void TagsContainer_Drop(object sender, DragEventArgs e)
        {
            if (sender is not WrapPanel panel) return;

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null && _currentAdorner != null)
            {
                adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
            }

            if (!e.Data.GetDataPresent(typeof(TagControl)))
            {
                e.Effects = DragDropEffects.None;
                return;
            }
            var draggedControl = (TagControl)e.Data.GetData(typeof(TagControl));

            Point position = e.GetPosition(panel);
            int insertIndex = -1;

            for (int i = 0; i < panel.Children.Count; i++)
            {
                var element = panel.Children[i];
                Point elementPos = element.TranslatePoint(new Point(0, 0), panel);
                double w = element.DesiredSize.Width;
                double h = element.DesiredSize.Height;

                bool sameRow = position.Y >= elementPos.Y && position.Y <= elementPos.Y + h;
                if (!sameRow) continue;

                if (position.X < elementPos.X + w / 2)
                {
                    insertIndex = i;
                    break;
                }
            }
            if (insertIndex == -1) insertIndex = panel.Children.Count;

            int currentIndex = panel.Children.IndexOf(draggedControl);
            if (currentIndex != -1 && insertIndex != currentIndex)
            {
                panel.Children.RemoveAt(currentIndex);
                if (insertIndex > currentIndex) insertIndex--;
                panel.Children.Insert(insertIndex, draggedControl);
            }
            UpdateViewModelTagsText();
        }

        #endregion

        #region VibeTransfer Drag & Drop

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string filePath in files)
            {
                if (IsImageFile(filePath))
                {
                    var vibeTransfer = new VibeTransfer();
                    vibeTransfer.SetImageFromFile(filePath);
                    ImageWrapPanel.Children.Add(vibeTransfer);
                }
            }
            UploadStackPanel.Visibility = Visibility.Collapsed;
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (IsImageFile(filePath))
                    {
                        var vibeTransfer = new VibeTransfer();
                        vibeTransfer.SetImageFromFile(filePath);
                        ImageWrapPanel.Children.Add(vibeTransfer);
                    }
                }
                UploadStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsImageFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension is ".jpg" or ".jpeg" or ".png";
        }

        #endregion

        #region CharacterPrompts (Add on Border Click)

        private void CharacterBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // If clicking inside the existing CharacterPrompts, ignore
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is CharacterPrompts) return;
                source = VisualTreeHelper.GetParent(source);
            }

            // Max 6
            if (CharacterPromptsWrapPanel.Children.Count >= 6)
            {
                MessageBox.Show("最多允许创建 6 个角色词条控件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create new
            var newCharacterPrompts = new CharacterPrompts();
            CharacterPromptsWrapPanel.Children.Add(newCharacterPrompts);
            CharacterPromptsStackPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Export / Save Images

        private async void ExportImagesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "选择保存路径",
                    Filter = "ZIP 压缩包 (*.zip)|*.zip",
                    FileName = "ExportedImages.zip"
                };
                if (saveFileDialog.ShowDialog() != true) return;
                string zipFilePath = saveFileDialog.FileName;

                string tempDirectory = Path.Combine(Path.GetTempPath(), "ExportedImages");
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
                Directory.CreateDirectory(tempDirectory);

                _viewModel.IsCreatingZipVisible = true;
                _viewModel.CreateZipProgressValue = 0;

                int totalControls = ImageStackPanel.Children.OfType<ImgPreview>().Count();
                if (totalControls == 0)
                {
                    MessageBox.Show("没有图像可导出。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _viewModel.IsCreatingZipVisible = false;
                    return;
                }

                double progressIncrement = 100.0 / totalControls;
                double currentProgress = 0;

                foreach (object child in ImageStackPanel.Children)
                {
                    if (child is ImgPreview imgPreview)
                    {
                        BitmapImage bmp = imgPreview.GetBitmapImage();
                        if (bmp != null)
                        {
                            string fileName = $"{Guid.NewGuid()}.png";
                            string filePath = Path.Combine(tempDirectory, fileName);
                            using var fs = new FileStream(filePath, FileMode.Create);
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bmp));
                            encoder.Save(fs);
                        }
                        currentProgress += progressIncrement;
                        _viewModel.CreateZipProgressValue = Math.Min(currentProgress, 100);
                        await Task.Delay(10);
                    }
                }

                if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);
                MessageBox.Show($"图像已成功导出为压缩包：{zipFilePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                LogPage.LogMessage(LogLevel.INFO, "图像已经成功导出为压缩包：" + zipFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LogPage.LogMessage(LogLevel.ERROR, "导出失败" + ex.Message);
            }
            finally
            {
                _viewModel.IsCreatingZipVisible = false;
            }
        }

        #endregion

        #region Upload to I2I

        private void Upload_To_I2I_Click(object sender, RoutedEventArgs e)
        {
            if (ImageViewerControl.ImageSource is BitmapFrame frame)
            {
                BitmapImage newImage = Tools.ConvertBitmapFrameToBitmapImage(frame);
                _originalImage = newImage;
                if (_originalImage != null) RenderImage(_originalImage);
                _viewModel.IsInkCanvasVisible = true;
            }
        }

        #endregion

        #region InkCanvas + Pan/Zoom

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var img = new BitmapImage(new Uri(openFileDialog.FileName));
                int w = img.PixelWidth;
                int h = img.PixelHeight;
                Tools.ValidateResolution(ref w, ref h);
                BitmapImage resized = Tools.ResizeImage(img, w, h);
                _originalImage = resized;
                RenderImage(_originalImage);
                _viewModel.IsInkCanvasVisible = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _originalImage = null;
            image.Source = null;
            inkCanvas.Strokes.Clear();
            _viewModel.IsInkCanvasVisible = false;
        }

        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            _undoStack.Push(e.Stroke);
            _redoStack.Clear();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                Stroke stroke = _undoStack.Pop();
                inkCanvas.Strokes.Remove(stroke);
                _redoStack.Push(stroke);
            }
            else
            {
                MessageBox.Show("没有可以撤回的操作！");
            }
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                Stroke stroke = _redoStack.Pop();
                inkCanvas.Strokes.Add(stroke);
                _undoStack.Push(stroke);
            }
            else
            {
                MessageBox.Show("没有可以重做的操作！");
            }
        }

        private void ClearInkButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private void BrushWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Width = e.NewValue;
            }
        }

        private void BrushHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Height = e.NewValue;
            }
        }

        private void PanZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isPanZoomMode)
            {
                _isPanZoomMode = true;
                inkCanvas.IsHitTestVisible = false;
                panZoomCanvas.MouseWheel += PanZoomCanvas_MouseWheel;
                panZoomCanvas.MouseDown += PanZoomCanvas_MouseDown;
                panZoomCanvas.MouseMove += PanZoomCanvas_MouseMove;
                panZoomCanvas.MouseUp += PanZoomCanvas_MouseUp;
            }
        }

        private void ExitPanZoomMode()
        {
            _isPanZoomMode = false;
            inkCanvas.IsHitTestVisible = true;
            panZoomCanvas.MouseWheel -= PanZoomCanvas_MouseWheel;
            panZoomCanvas.MouseDown -= PanZoomCanvas_MouseDown;
            panZoomCanvas.MouseMove -= PanZoomCanvas_MouseMove;
            panZoomCanvas.MouseUp -= PanZoomCanvas_MouseUp;
        }

        private void InkButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            ExitPanZoomMode();
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            ExitPanZoomMode();
        }

        private void PanZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isPanZoomMode) return;

            Point mousePos = e.GetPosition(panZoomCanvas);
            double deltaScale = e.Delta * 0.001;
            const double minScale = 0.1;
            const double maxScale = 10.0;

            double newScaleX = _panZoomScaleTransform.ScaleX + deltaScale;
            double newScaleY = _panZoomScaleTransform.ScaleY + deltaScale;
            if (newScaleX < minScale || newScaleX > maxScale) return;

            _panZoomTranslateTransform.X -= mousePos.X * deltaScale;
            _panZoomTranslateTransform.Y -= mousePos.Y * deltaScale;

            _panZoomScaleTransform.ScaleX = newScaleX;
            _panZoomScaleTransform.ScaleY = newScaleY;
            e.Handled = true;
        }

        private void PanZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPanZoomMode && e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(imageBorder);
                panZoomCanvas.CaptureMouse();
            }
        }

        private void PanZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanZoomMode && _isPanning)
            {
                Point currPos = e.GetPosition(imageBorder);
                Vector delta = currPos - _lastMousePosition;
                _panZoomTranslateTransform.X += delta.X;
                _panZoomTranslateTransform.Y += delta.Y;
                _lastMousePosition = currPos;
            }
        }

        private void PanZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanZoomMode && e.LeftButton == MouseButtonState.Released)
            {
                _isPanning = false;
                panZoomCanvas.ReleaseMouseCapture();
            }
        }

        private void ImageBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_originalImage != null) RenderImage(_originalImage);
        }

        private void RenderImage(BitmapImage bitmap)
        {
            image.Source = bitmap;

            if (_panZoomTransformGroup == null)
            {
                _panZoomTransformGroup = new TransformGroup();
                _panZoomScaleTransform = new ScaleTransform();
                _panZoomTranslateTransform = new TranslateTransform();
                _panZoomTransformGroup.Children.Add(_panZoomScaleTransform);
                _panZoomTransformGroup.Children.Add(_panZoomTranslateTransform);
                panZoomCanvas.RenderTransform = _panZoomTransformGroup;
            }

            // Reset transforms
            _panZoomScaleTransform.ScaleX = 1.0;
            _panZoomScaleTransform.ScaleY = 1.0;
            _panZoomTranslateTransform.X = 0;
            _panZoomTranslateTransform.Y = 0;

            double borderWidth = imageBorder.ActualWidth;
            double borderHeight = imageBorder.ActualHeight;

            double imageWidth = bitmap.PixelWidth;
            double imageHeight = bitmap.PixelHeight;

            double scaleX = borderWidth / imageWidth;
            double scaleY = borderHeight / imageHeight;
            double scale = Math.Min(scaleX, scaleY);

            image.Width = imageWidth;
            image.Height = imageHeight;
            inkCanvas.Width = imageWidth;
            inkCanvas.Height = imageHeight;

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            Canvas.SetLeft(inkCanvas, 0);
            Canvas.SetTop(inkCanvas, 0);

            _panZoomScaleTransform.ScaleX = scale;
            _panZoomScaleTransform.ScaleY = scale;

            _panZoomTranslateTransform.X = (borderWidth - imageWidth * scale) / 2;
            _panZoomTranslateTransform.Y = (borderHeight - imageHeight * scale) / 2;
        }

        private void ResetPositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null) return;

            double borderWidth = imageBorder.ActualWidth;
            double borderHeight = imageBorder.ActualHeight;
            double imgWidth = _originalImage.PixelWidth;
            double imgHeight = _originalImage.PixelHeight;

            double scaleX = borderWidth / imgWidth;
            double scaleY = borderHeight / imgHeight;
            double scale = Math.Min(scaleX, scaleY);

            _panZoomScaleTransform.ScaleX = scale;
            _panZoomScaleTransform.ScaleY = scale;
            _panZoomTranslateTransform.X = (borderWidth - imgWidth * scale) / 2;
            _panZoomTranslateTransform.Y = (borderHeight - imgHeight * scale) / 2;
        }

        #endregion

        #region Mask / Export

        private void ExportMaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null)
            {
                MessageBox.Show("尚未加载任何图像！");
                return;
            }

            // 保存当前的变换状态
            double savedScaleX = _panZoomScaleTransform.ScaleX;
            double savedScaleY = _panZoomScaleTransform.ScaleY;
            double savedTransX = _panZoomTranslateTransform.X;
            double savedTransY = _panZoomTranslateTransform.Y;

            // 重置变换，确保正确导出尺寸
            _panZoomScaleTransform.ScaleX = 1.0;
            _panZoomScaleTransform.ScaleY = 1.0;
            _panZoomTranslateTransform.X = 0;
            _panZoomTranslateTransform.Y = 0;
            panZoomCanvas.UpdateLayout();

            int w = _originalImage.PixelWidth;
            int h = _originalImage.PixelHeight;

            // 首先创建一个临时的蒙版位图，用于检测笔画
            var tempBitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            var tempVisual = new DrawingVisual();

            using (DrawingContext dc = tempVisual.RenderOpen())
            {
                // 黑色背景
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));

                // 将所有笔画绘制为白色
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    var whiteAttrs = new DrawingAttributes
                    {
                        Color = Colors.White,
                        Width = stroke.DrawingAttributes.Width,
                        Height = stroke.DrawingAttributes.Height,
                        IgnorePressure = stroke.DrawingAttributes.IgnorePressure,
                        StylusTip = stroke.DrawingAttributes.StylusTip,
                        StylusTipTransform = stroke.DrawingAttributes.StylusTipTransform
                    };
                    var whiteStroke = new Stroke(stroke.StylusPoints, whiteAttrs);
                    whiteStroke.Draw(dc);
                }
            }
            tempBitmap.Render(tempVisual);

            // 创建最终的蒙版位图
            var finalBitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            var finalVisual = new DrawingVisual();

            // 分析网格并创建块状蒙版
            using (DrawingContext dc = finalVisual.RenderOpen())
            {
                // 绘制黑色背景
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));

                // 调用网格分析函数，生成块状蒙版
                GenerateBlockMask(tempBitmap, dc, w, h);
            }

            finalBitmap.Render(finalVisual);

            // 显示蒙版并存储
            MaskImageSource.Source = finalBitmap;
            MaskViewBorder.Visibility = Visibility.Visible;
            _maskImage = finalBitmap;

            // 恢复之前的变换
            _panZoomScaleTransform.ScaleX = savedScaleX;
            _panZoomScaleTransform.ScaleY = savedScaleY;
            _panZoomTranslateTransform.X = savedTransX;
            _panZoomTranslateTransform.Y = savedTransY;
            panZoomCanvas.UpdateLayout();

            LogPage.LogMessage(LogLevel.INFO, "已生成块状蒙版");
        }

        private void GenerateBlockMask(RenderTargetBitmap sourceBitmap, DrawingContext targetDC, int width, int height)
        {
            // 获取源位图的像素数据
            var pixelData = new byte[width * height * 4];
            sourceBitmap.CopyPixels(pixelData, width * 4, 0);

            // 创建网格填充状态数组 (8x8像素为一个网格单元)
            int gridWidth = (int)Math.Ceiling(width / 8.0);
            int gridHeight = (int)Math.Ceiling(height / 8.0);
            var gridFillState = new int[gridWidth, gridHeight];

            // 计算每个网格单元的填充状态 (0=空白, 1=部分填充, 2=完全填充)
            for (int gy = 0; gy < gridHeight; gy++)
            {
                for (int gx = 0; gx < gridWidth; gx++)
                {
                    int filledPixels = 0;
                    int totalPixels = 0;

                    // 检查当前网格内的所有像素
                    for (int y = gy * 8; y < Math.Min((gy + 1) * 8, height); y++)
                    {
                        for (int x = gx * 8; x < Math.Min((gx + 1) * 8, width); x++)
                        {
                            int pixelIndex = (y * width + x) * 4;
                            // 像素不是黑色就算作填充
                            if (pixelData[pixelIndex] > 0 || pixelData[pixelIndex + 1] > 0 || pixelData[pixelIndex + 2] > 0)
                            {
                                filledPixels++;
                            }
                            totalPixels++;
                        }
                    }

                    // 根据填充比例确定网格状态
                    if (filledPixels == 0)
                    {
                        gridFillState[gx, gy] = 0; // 空白
                    }
                    else if (filledPixels == totalPixels)
                    {
                        gridFillState[gx, gy] = 2; // 完全填充
                    }
                    else
                    {
                        gridFillState[gx, gy] = 1; // 部分填充
                    }
                }
            }

            // 处理部分填充的网格，生成32x32块状蒙版
            for (int gy = 0; gy < gridHeight; gy++)
            {
                for (int gx = 0; gx < gridWidth; gx++)
                {
                    if (gridFillState[gx, gy] == 1) // 只处理部分填充的网格
                    {
                        // 检查九宫格周围的网格
                        bool[] surroundingFilled = new bool[9];

                        // 九宫格索引映射: 
                        // 0 1 2
                        // 3 4 5
                        // 6 7 8

                        // 检查九宫格内每个位置的填充状态
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                int checkX = gx + j;
                                int checkY = gy + i;
                                int index = (i + 1) * 3 + (j + 1);

                                // 边界检查
                                if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                                {
                                    surroundingFilled[index] = (gridFillState[checkX, checkY] == 2); // 只关注完全填充的网格
                                }
                            }
                        }

                        // 九宫格的顶点索引为 0, 2, 6, 8
                        // 边中点索引为 1, 3, 5, 7

                        // 确定32x32区块的起始坐标
                        int blockStartX, blockStartY;

                        // 检查顶点
                        if (surroundingFilled[0]) // 左上角
                        {
                            blockStartX = (gx - 1) * 8;
                            blockStartY = (gy - 1) * 8;
                        }
                        else if (surroundingFilled[2]) // 右上角
                        {
                            blockStartX = gx * 8;
                            blockStartY = (gy - 1) * 8;
                        }
                        else if (surroundingFilled[6]) // 左下角
                        {
                            blockStartX = (gx - 1) * 8;
                            blockStartY = gy * 8;
                        }
                        else if (surroundingFilled[8]) // 右下角
                        {
                            blockStartX = gx * 8;
                            blockStartY = gy * 8;
                        }
                        // 检查边中点
                        else if (surroundingFilled[1]) // 上边中点
                        {
                            blockStartX = gx * 8 - 16;
                            blockStartY = (gy - 1) * 8;
                        }
                        else if (surroundingFilled[3]) // 左边中点
                        {
                            blockStartX = (gx - 1) * 8;
                            blockStartY = gy * 8 - 16;
                        }
                        else if (surroundingFilled[5]) // 右边中点
                        {
                            blockStartX = gx * 8;
                            blockStartY = gy * 8 - 16;
                        }
                        else if (surroundingFilled[7]) // 下边中点
                        {
                            blockStartX = gx * 8 - 16;
                            blockStartY = gy * 8;
                        }
                        // 默认情况：当前网格的左上角
                        else
                        {
                            blockStartX = gx * 8 - 16;
                            blockStartY = gy * 8 - 16;
                        }

                        // 边界检查和调整
                        blockStartX = Math.Max(0, Math.Min(width - 32, blockStartX));
                        blockStartY = Math.Max(0, Math.Min(height - 32, blockStartY));

                        // 绘制32x32的白色方块
                        targetDC.DrawRectangle(Brushes.White, null, new Rect(blockStartX, blockStartY, 32, 32));
                    }
                    else if (gridFillState[gx, gy] == 2)
                    {
                        // 对于完全填充的网格，直接绘制8x8的白色方块
                        targetDC.DrawRectangle(Brushes.White, null, new Rect(gx * 8, gy * 8, 8, 8));
                    }
                }
            }
        }

        private void DelMaskBth_Click(object sender, RoutedEventArgs e)
        {
            MaskImageSource.Source = null;
            MaskViewBorder.Visibility = Visibility.Collapsed;
            _maskImage = null;
        }

        private void SaveDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null)
            {
                MessageBox.Show("尚未加载任何图像！");
                return;
            }

            double savedScaleX = _panZoomScaleTransform.ScaleX;
            double savedScaleY = _panZoomScaleTransform.ScaleY;
            double savedTransX = _panZoomTranslateTransform.X;
            double savedTransY = _panZoomTranslateTransform.Y;

            // Reset
            _panZoomScaleTransform.ScaleX = 1.0;
            _panZoomScaleTransform.ScaleY = 1.0;
            _panZoomTranslateTransform.X = 0;
            _panZoomTranslateTransform.Y = 0;
            panZoomCanvas.UpdateLayout();

            int w = _originalImage.PixelWidth;
            int h = _originalImage.PixelHeight;

            var renderBitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(_originalImage, new Rect(0, 0, w, h));
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    stroke.Draw(dc);
                }
            }
            renderBitmap.Render(drawingVisual);

            // Restore
            _panZoomScaleTransform.ScaleX = savedScaleX;
            _panZoomScaleTransform.ScaleY = savedScaleY;
            _panZoomTranslateTransform.X = savedTransX;
            _panZoomTranslateTransform.Y = savedTransY;
            panZoomCanvas.UpdateLayout();

            // Convert to BitmapImage
            BitmapImage bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(ms);
                ms.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
            }
            _originalImage = bitmapImage;
        }

        #endregion
    }

    /// <summary>
    /// A specialized Thumb control that tracks X/Y as a percentage inside a Canvas.
    /// </summary>
    public class ThumbPro : Thumb
    {
        public double Top
        {
            get => (double)GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }
        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register(nameof(Top), typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

        public double Left
        {
            get => (double)GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register(nameof(Left), typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

        public double Xoffset { get; set; }
        public double Yoffset { get; set; }
        public bool VerticalOnly { get; set; }
        public bool HorizontalOnly { get; set; }

        public double Xpercent => (Left + Xoffset) / ActualWidth;
        public double Ypercent => (Top + Yoffset) / ActualHeight;

        public event Action<double, double> ValueChanged;

        private double _firstTop;
        private double _firstLeft;

        public ThumbPro()
        {
            Loaded += (s, e) =>
            {
                if (HorizontalOnly)
                {
                    Top = -Yoffset;
                }
                else if (!VerticalOnly)
                {
                    Left = -Xoffset;
                    Top = -Yoffset;
                }
                else
                {
                    Top = -Yoffset;
                }
            };

            DragStarted += (s, e) =>
            {
                if (HorizontalOnly)
                {
                    Left = e.HorizontalOffset - Xoffset;
                    _firstLeft = Left;
                }
                else if (!VerticalOnly)
                {
                    Left = e.HorizontalOffset - Xoffset;
                    _firstLeft = Left;
                    Top = e.VerticalOffset - Yoffset;
                    _firstTop = Top;
                }
                else
                {
                    Top = e.VerticalOffset - Yoffset;
                    _firstTop = Top;
                }
                ValueChanged?.Invoke(Xpercent, Ypercent);
            };

            DragDelta += (s, e) =>
            {
                if (HorizontalOnly)
                {
                    double x = _firstLeft + e.HorizontalChange;
                    Left = Clamp(x, -Xoffset, ActualWidth - Xoffset);
                    Top = -Yoffset;
                }
                else if (!VerticalOnly)
                {
                    double x = _firstLeft + e.HorizontalChange;
                    Left = Clamp(x, -Xoffset, ActualWidth - Xoffset);

                    double y = _firstTop + e.VerticalChange;
                    Top = Clamp(y, -Yoffset, ActualHeight - Yoffset);
                }
                else
                {
                    double y = _firstTop + e.VerticalChange;
                    Top = Clamp(y, -Yoffset, ActualHeight - Yoffset);
                }
                ValueChanged?.Invoke(Xpercent, Ypercent);
            };
        }

        private double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

        public void SetTopLeftByPercent(double xpercent, double ypercent)
        {
            Top = ypercent * ActualHeight - Yoffset;
            if (!VerticalOnly)
                Left = xpercent * ActualWidth - Xoffset;
        }

        public void UpdatePositionByPercent(double xpercent, double ypercent)
        {
            SetTopLeftByPercent(xpercent, ypercent);
            _firstLeft = Left;
            _firstTop = Top;
            ValueChanged?.Invoke(Xpercent, Ypercent);
        }

        public void InitializePosition(double xpercent, double ypercent)
        {
            SetTopLeftByPercent(xpercent, ypercent);
            ValueChanged?.Invoke(Xpercent, Ypercent);
        }

        public void AnimateToPercent(double xpercent, double ypercent, double durationSeconds)
        {
            var topAnim = new DoubleAnimation
            {
                To = ypercent * ActualHeight - Yoffset,
                Duration = TimeSpan.FromSeconds(durationSeconds),
            };
            var leftAnim = new DoubleAnimation
            {
                To = xpercent * ActualWidth - Xoffset,
                Duration = TimeSpan.FromSeconds(durationSeconds),
            };

            BeginAnimation(TopProperty, topAnim);
            if (!VerticalOnly)
            {
                BeginAnimation(LeftProperty, leftAnim);
            }
            Dispatcher.InvokeAsync(() =>
            {
                ValueChanged?.Invoke(Xpercent, Ypercent);
            });
        }
    }
}
