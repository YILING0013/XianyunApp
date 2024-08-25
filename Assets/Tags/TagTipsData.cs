using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
namespace xianyun.UserControl
{
    public class TagTipsData : DependencyObject
    {
        public string name;
        public string value;
        public string comment;
        public string typeName;
        public Color typeColor;
        public BitmapImage preview;
        public string previewPath;
        public bool fromList;
        public bool canSetColor;
        public bool canDeleteColor;
        public TagControl control;
        public string colorString;
        public string color;
        public string loraLayerString;
    }
}
