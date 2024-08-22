using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace xianyun.Common
{
    // 实现INotifyPropertyChanged接口的基类，用于通知属性值的变化
    public class NotifyBase : INotifyPropertyChanged
    {
        // PropertyChanged事件，当属性值发生变化时通知UI更新
        public event PropertyChangedEventHandler PropertyChanged;

        // 通知属性值变化的方法
        public void DoNotify([CallerMemberName] string propName = "")
        {
            // 如果有订阅PropertyChanged事件，则触发该事件
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
