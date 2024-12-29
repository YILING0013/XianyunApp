using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace xianyun.MainPages
{
    /// <summary>
    /// Img2ImgPage.xaml 的交互逻辑
    /// </summary>
    public partial class Img2ImgPage : Page
    {
        public ObservableCollection<string> Items { get; set; }
        public Img2ImgPage()
        {
            InitializeComponent();
            // 初始化数据源
            Items = new ObservableCollection<string>
            {
                "Item 1",
                "Item 2",
                "Item 3",
                "Item 4",
                "Item 5"
            };

            // 绑定数据源到ListBox
            listBox.ItemsSource = Items;
        }
        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var listBox = sender as ListBox;
                var selectedItem = listBox.SelectedItem;

                if (selectedItem != null)
                {
                    DragDrop.DoDragDrop(listBox, selectedItem, DragDropEffects.Move);
                }
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            var data = e.Data.GetData(typeof(string)) as string;
            var target = listBox.SelectedItem as string;

            if (data != null && target != null)
            {
                int sourceIndex = Items.IndexOf(data);
                int targetIndex = Items.IndexOf(target);

                if (sourceIndex != targetIndex)
                {
                    Items.RemoveAt(sourceIndex);
                    Items.Insert(targetIndex, data);
                }
            }
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }
}
