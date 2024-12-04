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
using HandyControl.Controls;

namespace xianyun.UserControl
{
    /// <summary>
    /// TagControl.xaml 的交互逻辑
    /// </summary>
    public partial class TagControl
    {
        public string TagId { get; private set; }
        private string _originalText;
        private string _currentText;
        private Point? dragStartPoint = null;
        Point? potentialDragStartPoint = null;
        public event EventHandler TextChanged;
        private Brush _originalBorderBrush; // 存储原始边框颜色

        public event EventHandler TagDeleted;

        public TagControl(string tagId, string englishText, string chineseText = null, Brush color = null)
        {
            InitializeComponent();
            TagId = tagId; // 设置唯一标识符
            // Initialize text and tooltip
            _originalText = englishText;
            _currentText = englishText;
            _originalBorderBrush = BorderBg.BorderBrush; // 初始化存储边框颜色

            if (!string.IsNullOrEmpty(chineseText))
            {
                TextTag.Content = chineseText;
                TextTag.ToolTip = englishText;
                TextTag.VerticalAlignment= VerticalAlignment.Center;
            }
            else
            {
                TextTag.Content = englishText;
            }

            // Set border color if provided
            if (color != null)
            {
                BorderBg.BorderBrush = color;
            }
            // Attach events for drag and drop
            this.PreviewMouseLeftButtonDown += TagControl_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += TagControl_PreviewMouseMove;
            this.PreviewMouseLeftButtonUp += TagControl_PreviewMouseLeftButtonUp;
        }
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                try
                {
                    // 将边框颜色设置为按钮对应的颜色
                    BorderBg.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
                }
                catch
                {
                    // 处理可能的异常
                    HandyControl.Controls.MessageBox.Show($"Failed to set color: {colorName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public Brush GetCurrentBorderBrush()
        {
            return BorderBg.BorderBrush;
        }
        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (potentialDragStartPoint == null)
            {
                potentialDragStartPoint = e.GetPosition(this);
            }


        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            potentialDragStartPoint = null;
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (potentialDragStartPoint == null) { return; }

            var dragPoint = e.GetPosition(this);

            Vector potentialDragLength = dragPoint - potentialDragStartPoint.Value;
            if (potentialDragLength.Length > 5)
            {
                DataObject data = new DataObject(this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                potentialDragStartPoint = null;
            }
        }
        private void TagControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(this);
        }
        private void TagControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartPoint == null)
                return;

            Point currentPosition = e.GetPosition(this);
            Vector difference = currentPosition - dragStartPoint.Value;

            if (difference.Length > 5)
            {
                DataObject data = new DataObject(typeof(TagControl), this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                dragStartPoint = null;
            }
        }
        private void TagControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
        }
        private void ButtonDecStrength_Click(object sender, RoutedEventArgs e)
        {
            AdjustTextStrength(false);
        }

        private void ButtonIncStrength_Click(object sender, RoutedEventArgs e)
        {
            AdjustTextStrength(true);
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            // Remove this control from its parent
            var parent = this.Parent as Panel;
            parent?.Children.Remove(this);
            TagDeleted?.Invoke(this, EventArgs.Empty);
        }

        private void AdjustTextStrength(bool increase)
        {
            // 提取当前的中文和英文显示内容
            var displayedChineseText = TextTag.Content.ToString();
            var displayedEnglishText = _currentText;

            if (increase)
            {
                if (displayedEnglishText.StartsWith("[") && displayedEnglishText.EndsWith("]"))
                {
                    // 如果文本以 [] 包裹，则移除一层 []
                    displayedEnglishText = RemoveOutermostBrackets(displayedEnglishText, '[', ']');
                    displayedChineseText = RemoveOutermostBrackets(displayedChineseText, '[', ']');
                }
                else
                {
                    // 否则增加一层 []
                    displayedEnglishText = "{" + displayedEnglishText + "}";
                    displayedChineseText = "{" + displayedChineseText + "}";
                }
            }
            else
            {
                if (displayedEnglishText.StartsWith("{") && displayedEnglishText.EndsWith("}"))
                {
                    // 如果文本以 {} 包裹，则移除一层 {}
                    displayedEnglishText = RemoveOutermostBrackets(displayedEnglishText, '{', '}');
                    displayedChineseText = RemoveOutermostBrackets(displayedChineseText, '{', '}');
                }
                else
                {
                    // 否则增加一层 []
                    displayedEnglishText = "[" + displayedEnglishText + "]";
                    displayedChineseText = "[" + displayedChineseText + "]";
                }
            }

            // 更新中文和英文文本
            _currentText = displayedEnglishText;
            TextTag.Content = displayedChineseText;
            TextTag.ToolTip = _currentText; // 将更新后的英文文本设置为工具提示
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        // 辅助方法：移除最外层的括号或大括号
        private string RemoveOutermostBrackets(string text, char openingBracket, char closingBracket)
        {
            if (text.StartsWith(openingBracket.ToString()) && text.EndsWith(closingBracket.ToString()))
            {
                return text.Substring(1, text.Length - 2);
            }
            return text;
        }
        // Method to retrieve adjusted text
        public string GetAdjustedText()
        {
            return _currentText;
        }
    }
}
