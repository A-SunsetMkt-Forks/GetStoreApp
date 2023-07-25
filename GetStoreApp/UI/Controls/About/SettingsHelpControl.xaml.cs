﻿using GetStoreApp.Helpers.Controls.Extensions;
using GetStoreApp.Services.Controls.Settings.Appearance;
using GetStoreApp.UI.Dialogs.About;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace GetStoreApp.UI.Controls.About
{
    /// <summary>
    /// 关于页面：设置选项说明控件
    /// </summary>
    public sealed partial class SettingsHelpControl : Expander
    {
        public SettingsHelpControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 应用信息
        /// </summary>
        public async void OnAppInformationClicked(object sender, RoutedEventArgs args)
        {
            await ContentDialogHelper.ShowAsync(new AppInformationDialog(), this);
        }

        /// <summary>
        /// 应用设置
        /// </summary>
        public async void OnAppSettingsClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));
        }

        /// <summary>
        /// 了解包文件
        /// </summary>
        public async void OnBlockMapClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format(@"https://docs.microsoft.com/{0}/uwp/schemas/blockmapschema/app-package-block-map", LanguageService.AppLanguage.InternalName)));
        }

        /// <summary>
        /// 了解 WinGet 配置选项
        /// </summary>
        public async void OnLearnWinGetConfigClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format(@"https://docs.microsoft.com/{0}/uwp/schemas/blockmapschema/app-package-block-map", LanguageService.AppLanguage.InternalName)));
        }

        /// <summary>
        /// 了解以".e"开头的加密包文件
        /// </summary>
        public async void OnStartWithEClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/MicrosoftDocs/msix-docs/blob/main/msix-src/package/create-app-package-with-makeappx-tool.md#encrypt-or-decrypt-a-package-or-bundle"));
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        public async void OnSystemInformationClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:about"));
        }
    }
}
