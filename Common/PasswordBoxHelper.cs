using System.Windows;
using System.Windows.Controls;

namespace xianyun.Common
{
    // PasswordBoxHelper类，帮助实现PasswordBox的双向数据绑定
    public static class PasswordBoxHelper
    {
        private static bool _isUpdating = false; // 防止递归更新的标志位

        // 定义一个附加依赖属性，用于绑定PasswordBox的密码
        public static readonly DependencyProperty BoundPassword =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        // 获取BoundPassword附加属性的值
        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPassword);
        }

        // 设置BoundPassword附加属性的值
        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPassword, value);
        }

        // 当BoundPassword属性的值发生变化时的回调方法
        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                // 先移除事件处理程序，以防止在更改属性时触发事件
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

                if (!_isUpdating) // 如果不在更新中，更新PasswordBox的密码
                {
                    passwordBox.Password = (string)e.NewValue;
                }

                // 重新附加PasswordChanged事件处理程序
                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        // 当PasswordBox的Password属性变化时触发的事件处理程序
        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) // 如果正在更新，直接返回，防止递归调用
                return;

            var passwordBox = sender as PasswordBox;
            var newPassword = passwordBox.Password;

            _isUpdating = true; // 设置标志位，表示正在更新
            SetBoundPassword(passwordBox, newPassword); // 更新绑定的Password属性值
            _isUpdating = false; // 重置标志位
        }
    }
}
