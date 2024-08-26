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

namespace xianyun.MainPages
{
    /// <summary>
    /// Txt2imgPage.xaml 的交互逻辑
    /// </summary>
    public partial class Txt2imgPage : Page
    {
        private bool dragInProgress = false;
        private DragAdorner currentAdorner;
        public Txt2imgPage()
        {
            InitializeComponent();
            //AddTagControls();
        }
        private void AddTagControls()
        {
            // Example texts for the TagControls
            string[] englishTexts = { "First Tag", "Second Tag", "Third Tag" };
            string[] chineseTexts = { "第一标签", "第二标签", "第三标签" }; // Optional: use null or skip if not needed
            Brush[] colors = { Brushes.Red, Brushes.Green, Brushes.Blue }; // Optional: use null or skip if not needed

            for (int i = 0; i < englishTexts.Length; i++)
            {
                // Create a new instance of TagControl
                TagControl tagControl = new TagControl(englishTexts[i], chineseTexts[i], colors[i]);

                // Add the TagControl to the WrapPanel
                TagsContainer.Children.Add(tagControl);
            }
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
            if (InputTextBox.Visibility == Visibility.Visible)
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

                    // 清除现有的 TagControl
                    TagsContainer.Children.Clear();

                    // 添加新的 TagControl
                    foreach (var tag in newTags)
                    {
                        TagControl tagControl = new TagControl(tag);
                        tagControl.TextChanged += TagControl_TextChanged; // 监听文本内容变化事件
                        TagsContainer.Children.Add(tagControl);
                    }
                }

                // 隐藏 TextBox
                InputTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void TagControl_TextChanged(object sender, EventArgs e)
        {
            if (TagsContainer.Children.Count > 0)
            {
                var tagsText = string.Join(",", TagsContainer.Children.OfType<TagControl>().Select(tc => tc.GetAdjustedText()));
                InputTextBox.Text = tagsText;
            }
        }
        private void ProcessInputText()
        {
            if (InputTextBox.Visibility == Visibility.Visible)
            {
                string inputText = InputTextBox.Text.Trim();

                if (!string.IsNullOrEmpty(inputText))
                {
                    // 将中文逗号转换为英文逗号
                    inputText = inputText.Replace("，", ",");

                    // 分割文本
                    string[] tags = inputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // 清除现有的 TagControl
                    TagsContainer.Children.Clear();

                    foreach (var tag in tags)
                    {
                        // 自动补全括号
                        string adjustedTag = AutoCompleteBrackets(tag.Trim());

                        // 创建TagControl并添加到WrapPanel
                        TagControl tagControl = new TagControl(adjustedTag);
                        TagsContainer.Children.Add(tagControl);
                    }
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
            WrapPanel panel = sender as WrapPanel;
            if (panel != null)
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
            WrapPanel panel = sender as WrapPanel;
            if (panel == null)
            {
                return;
            }

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null && currentAdorner != null)
            {
                adornerLayer.Remove(currentAdorner);
                currentAdorner = null;
            }

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
            }
        }

        private void TagsContainer_OnRealTargetDragLeave(object sender, DragEventArgs e)
        {

            WrapPanel panel = sender as WrapPanel;
            if (panel == null)
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
