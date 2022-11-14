﻿using GetStoreApp.Models.Base;
using Microsoft.UI.Xaml;

namespace GetStoreApp.Models.Controls.Home
{
    public class TypeModel : ModelBase
    {
        /// <summary>
        /// 获取应用类型显示名称
        /// </summary>
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(TypeModel), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取应用类型内部名称
        /// </summary>
        public string InternalName
        {
            get { return (string)GetValue(InternalNameProperty); }
            set { SetValue(InternalNameProperty, value); }
        }

        public static readonly DependencyProperty InternalNameProperty =
            DependencyProperty.Register("InternalName", typeof(string), typeof(TypeModel), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 获取应用类型简短名称（用作参数启动使用）
        /// </summary>
        public string ShortName
        {
            get { return (string)GetValue(ShortNameProperty); }
            set { SetValue(ShortNameProperty, value); }
        }

        public static readonly DependencyProperty ShortNameProperty =
            DependencyProperty.Register("ShortName", typeof(string), typeof(ChannelModel), new PropertyMetadata(string.Empty));
    }
}
