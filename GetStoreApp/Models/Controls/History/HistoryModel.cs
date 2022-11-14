﻿using GetStoreApp.Models.Base;
using Microsoft.UI.Xaml;

namespace GetStoreApp.Models.Controls.History
{
    public class HistoryModel : ModelBase
    {
        /// <summary>
        /// 在多选模式下，该行历史记录是否被选择的标志
        /// </summary>
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 历史记录生成时对应的时间戳，本地存储时使用的是格林尼治标准时间（GMT+0）
        /// </summary>
        public long CreateTimeStamp
        {
            get { return (long)GetValue(CurrentTimeStampProperty); }
            set { SetValue(CurrentTimeStampProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeStampProperty =
            DependencyProperty.Register("CreateTimeStamp", typeof(string), typeof(HistoryModel), new PropertyMetadata(default(int)));

        /// <summary>
        /// 历史记录的索引键值
        /// </summary>
        public string HistoryKey
        {
            get { return (string)GetValue(HistoryKeyProperty); }
            set { SetValue(HistoryKeyProperty, value); }
        }

        public static readonly DependencyProperty HistoryKeyProperty =
            DependencyProperty.Register("HistoryKey", typeof(string), typeof(HistoryModel), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 历史记录中包含的类型，数据库存储的原始名称
        /// </summary>
        public string HistoryType
        {
            get { return (string)GetValue(HistoryTypeProperty); }
            set { SetValue(HistoryTypeProperty, value); }
        }

        public static readonly DependencyProperty HistoryTypeProperty =
            DependencyProperty.Register("HistoryType", typeof(string), typeof(HistoryModel), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 历史记录中包含的通道
        /// </summary>
        public string HistoryChannel
        {
            get { return (string)GetValue(HistoryChannelProperty); }
            set { SetValue(HistoryChannelProperty, value); }
        }

        public static readonly DependencyProperty HistoryChannelProperty =
            DependencyProperty.Register("HistoryChannel", typeof(string), typeof(HistoryModel), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 历史记录包含的链接
        /// </summary>
        public string HistoryLink
        {
            get { return (string)GetValue(HistoryLinkProperty); }
            set { SetValue(HistoryLinkProperty, value); }
        }

        public static readonly DependencyProperty HistoryLinkProperty =
            DependencyProperty.Register("HistoryLink", typeof(string), typeof(HistoryModel), new PropertyMetadata(string.Empty));
    }
}
