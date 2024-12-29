using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xianyun.MainPages;
using xianyun.View;

namespace xianyun.UserControl
{
    /// <summary>
    /// CharacterPrompts.xaml 的交互逻辑
    /// </summary>
    public partial class CharacterPrompts
    {
        private bool _isCollapsed = false; // 用于记录当前状态
        public CharacterPrompts()
        {
            InitializeComponent();
            CharacterBorder.MouseEnter += CharacterBorder_MouseEnter;
        }
        private void CharacterBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            // 获取第一个文本框的内容
            string textContent = Prompt.Text;
            string truncatedText = textContent.Length > 30 ? textContent.Substring(0, 30) + "..." : textContent;

            // 获取当前选择的位置
            string selectedPosition = SelectedPositionText.Text;

            // 更新 ToolTip 内容
            ToolTipText.Text = truncatedText;
            ToolTipPosition.Text = "Position: " + selectedPosition;
        }

        // 映射规则
        private double MapColumnToX(string column)
        {
            return column switch
            {
                "A" => 0.1,
                "B" => 0.3,
                "C" => 0.5,
                "D" => 0.7,
                "E" => 0.9,
                _ => 0 // 默认值
            };
        }

        private double MapRowToY(string row)
        {
            return row switch
            {
                "1" => 0.1,
                "2" => 0.3,
                "3" => 0.5,
                "4" => 0.7,
                "5" => 0.9,
                _ => 0 // 默认值
            };
        }

        // 获取控件的状态
        public object GetControlState()
        {
            string promptText = Prompt.Text;
            string undesiredContentText = UndesiredContent.Text;

            string selectedPosition = SelectedPositionText.Text;
            double x = 0, y = 0;

            if (!string.IsNullOrEmpty(selectedPosition) && selectedPosition.Length == 2)
            {
                string column = selectedPosition.Substring(0, 1);
                string row = selectedPosition.Substring(1, 1);

                x = MapColumnToX(column);
                y = MapRowToY(row);
            }

            return new
            {
                prompt = promptText,
                uc = undesiredContentText,
                selectedPosition = selectedPosition,
                center = new { x, y }
            };
        }

        // 点击按钮事件处理
        private void SelectedPositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                SelectedPositionText.Text = button.Content.ToString();
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            DoubleAnimation animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(300)
            };

            if (_isCollapsed)
            {
                // 展开
                animation.To = 220; // 展开后的高度
                button.Content = "Collapse";
            }
            else
            {
                // 折叠
                animation.To = 20; // 折叠后的高度
                button.Content = "Expand";
            }

            tabControl.BeginAnimation(FrameworkElement.HeightProperty, animation);
            _isCollapsed = !_isCollapsed;
        }

        private void DelMenuBth_Click(object sender, RoutedEventArgs e)
        {
            // 获取父控件  
            var parent = this.Parent as Panel;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            if (parent != null)
            {
                // 从父容器中移除自身
                parent.Children.Remove(this);

                // 检查父容器是否还有其他子控件
                if (parent.Children.Count == 0)
                {
                    var mainPage = mainWindow?.MainWindowFrame.Content as Txt2imgPage;
                    // 如果没有控件，显示上传标志
                    if (mainPage != null)
                    {
                        mainPage.CharacterPromptsStackPanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void UpMenuBth_Click(object sender, RoutedEventArgs e)
        {
            MoveControl(-1); // 上移
        }

        private void DownMenuBth_Click(object sender, RoutedEventArgs e)
        {
            MoveControl(1); // 下移
        }

        private void MoveControl(int direction)
        {
            // 获取当前控件的父容器（WrapPanel）
            var parent = Parent as Panel;
            if (parent == null) return;

            // 获取当前控件在父容器中的索引
            int currentIndex = parent.Children.IndexOf(this);

            // 计算目标索引
            int targetIndex = currentIndex + direction;

            // 检查目标索引是否有效
            if (targetIndex >= 0 && targetIndex < parent.Children.Count)
            {
                // 从父容器中移除当前控件
                parent.Children.Remove(this);

                // 将控件插入到目标位置
                parent.Children.Insert(targetIndex, this);
            }
        }

        private void ChangeBorderColor_Click(object sender, RoutedEventArgs e)
        {
            // 获取点击的按钮
            var button = sender as Button;
            if (button == null) return;

            // 获取 Tag 中的颜色值
            string colorValue = button.Tag as string;
            if (string.IsNullOrEmpty(colorValue)) return;

            // 将颜色值转换为 Brush
            var colorConverter = new BrushConverter();
            try
            {
                var brush = (Brush)colorConverter.ConvertFromString(colorValue);
                if (brush != null)
                {
                    // 修改 Border 的 BorderBrush
                    var border = FindName("CharacterBorder") as Border;
                    if (border != null)
                    {
                        border.BorderBrush = brush;
                    }
                }
            }
            catch
            {
                MessageBox.Show("颜色转换失败，请检查颜色值。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetBorderColor_Click(object sender, RoutedEventArgs e)
        {
            // 重置 Border 的 BorderBrush 为透明
            var border = FindName("CharacterBorder") as Border;
            if (border != null)
            {
                border.BorderBrush = Brushes.Transparent;
            }
        }
    }
}
