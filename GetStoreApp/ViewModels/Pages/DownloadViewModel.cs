﻿using GetStoreApp.Extensions.DataType.Enums;
using GetStoreApp.Extensions.Messaging;
using GetStoreApp.Services.Controls.Settings.Common;
using GetStoreApp.Services.Window;
using GetStoreApp.ViewModels.Base;
using GetStoreApp.Views.Pages;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.ViewModels.Pages
{
    /// <summary>
    /// 下载页面视图模型
    /// </summary>
    public sealed class DownloadViewModel : ViewModelBase
    {
        private bool _useInsVisValue;

        public bool UseInsVisValue
        {
            get { return _useInsVisValue; }

            set
            {
                _useInsVisValue = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// DownloadPivot选中项发生变化时，关闭离开页面的事件，开启要导航到的页面的事件，并更新新页面的数据
        /// </summary>
        public void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.RemovedItems.Count > 0)
            {
                Pivot pivot = sender as Pivot;
                if (pivot is not null)
                {
                    Messenger.Default.Send(pivot.SelectedIndex, MessageToken.PivotSelection);
                }
            }
        }

        /// <summary>
        /// 初次加载页面时，开启下载中页面的所有事件，加载下载中页面的数据
        /// </summary>
        public void OnNavigatedTo()
        {
            UseInsVisValue = UseInstructionService.UseInsVisValue;
            Messenger.Default.Send(0, MessageToken.PivotSelection);
        }

        /// <summary>
        /// 离开该页面时，关闭所有事件，并通知注销所有事件（防止内存泄露）
        /// </summary>
        public void OnNavigatedFrom()
        {
            Messenger.Default.Send(-1, MessageToken.PivotSelection);
        }

        /// <summary>
        /// 了解更多下载管理说明
        /// </summary>
        public void LearnMore()
        {
            NavigationService.NavigateTo(typeof(AboutPage), AppNaviagtionArgs.SettingsHelp);
        }

        /// <summary>
        /// 打开应用“下载设置”
        /// </summary>
        public void OpenDownloadSettings()
        {
            NavigationService.NavigateTo(typeof(SettingsPage), AppNaviagtionArgs.DownloadOptions);
        }
    }
}
