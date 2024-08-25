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

namespace xianyun.UserControl
{
    /// <summary>
    /// TagControl.xaml 的交互逻辑
    /// </summary>
    public partial class TagControl
    {
        private string _originalText;
        private string _currentText;
        private Point? dragStartPoint = null;
        Point? potentialDragStartPoint = null;

        public TagControl(string englishText, string chineseText = null, Brush color = null)
        {
            InitializeComponent();

            // Initialize text and tooltip
            _originalText = englishText;
            _currentText = englishText;

            if (!string.IsNullOrEmpty(chineseText))
            {
                TextTag.Content = chineseText;
                TextTag.ToolTip = englishText;
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
        }

        private void AdjustTextStrength(bool increase)
        {
            if (increase)
            {
                if (_currentText.Contains("[")) _currentText = _currentText.Replace("[", "{").Replace("]", "}");
                else _currentText = "{" + _currentText.Trim('{', '}') + "}";
            }
            else
            {
                if (_currentText.Contains("{")) _currentText = _currentText.Replace("{", "[").Replace("}", "]");
                else _currentText = "[" + _currentText.Trim('[', ']') + "]";
            }

            // Update tooltip and content
            TextTag.Content = _currentText.Contains("]") ? _currentText : _currentText.Trim('{', '}');
            TextTag.ToolTip = _originalText;
        }

        // Method to retrieve adjusted text
        public string GetAdjustedText()
        {
            return _currentText;
        }
    }
}
