using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace xianyun.Extensions
{
    public static class ExpanderGroupBehavior
    {
        /// <summary>
        /// 附加属性 GroupName，用于标识 Expander 属于哪个组
        /// </summary>
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.RegisterAttached(
                "GroupName",
                typeof(string),
                typeof(ExpanderGroupBehavior),
                new PropertyMetadata(null, OnGroupNameChanged));

        // Getter：获取 GroupName
        public static string GetGroupName(DependencyObject obj)
        {
            return (string)obj.GetValue(GroupNameProperty);
        }

        // Setter：设置 GroupName
        public static void SetGroupName(DependencyObject obj, string value)
        {
            obj.SetValue(GroupNameProperty, value);
        }

        // 当 GroupName 属性改变时触发
        private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Expander expander)
            {
                // 注册 Expanded 事件
                expander.Expanded += (sender, args) =>
                {
                    var groupName = GetGroupName(expander);
                    if (string.IsNullOrEmpty(groupName)) return;

                    // 获取同一组中的其他 Expander
                    var parent = expander.Parent as Panel;
                    if (parent == null) return;

                    foreach (var child in parent.Children.OfType<Expander>())
                    {
                        if (child != expander && GetGroupName(child) == groupName)
                        {
                            child.IsExpanded = false;
                        }
                    }
                };
            }
        }
    }
}
