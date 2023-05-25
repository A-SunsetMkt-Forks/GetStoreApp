﻿using GetStoreApp.Extensions.DataType.Enums;
using GetStoreApp.Models.Base;

namespace GetStoreApp.Models.Dialogs.Settings
{
    /// <summary>
    /// 清理选项数据模型
    /// </summary>
    public class TraceCleanupModel : ModelBase
    {
        /// <summary>
        /// 清理选项是否被选择的标志
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
        /// 标志该清理选项是否清理失败
        /// </summary>
        private bool _isCleanFailed;

        public bool IsCleanFailed
        {
            get { return _isCleanFailed; }

            set
            {
                _isCleanFailed = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 清理选项显示的名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 清理选项内部的名称
        /// </summary>
        public CleanArgs InternalName { get; set; }

        /// <summary>
        /// 清理失败时详细的错误文字信息
        /// </summary>
        public string CleanFailedText { get; set; }
    }
}