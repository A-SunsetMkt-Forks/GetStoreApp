﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetStoreApp.Contracts.Services.Controls.Settings.Appearance;
using GetStoreApp.Contracts.Services.Shell;
using GetStoreApp.Extensions.DataType.Enums;
using GetStoreApp.Helpers.Root;
using GetStoreApp.Models.Controls.Settings.Appearance;
using GetStoreApp.ViewModels.Pages;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Generic;

namespace GetStoreApp.ViewModels.Controls.Settings.Appearance
{
    public partial class BackdropViewModel : ObservableRecipient
    {
        private IBackdropService BackdropService { get; } = ContainerHelper.GetInstance<IBackdropService>();

        private INavigationService NavigationService { get; } = ContainerHelper.GetInstance<INavigationService>();

        public List<BackdropModel> BackdropList => BackdropService.BackdropList;

        private BackdropModel _backdrop;

        public BackdropModel Backdrop
        {
            get { return _backdrop; }

            set { SetProperty(ref _backdrop, value); }
        }

        public bool BackdropIsEnabled { get; }

        // 背景色不可用时具体信息了解
        public IRelayCommand BackdropTipCommand => new RelayCommand(() =>
        {
            App.NavigationArgs = AppNaviagtionArgs.SettingsHelp;
            NavigationService.NavigateTo(typeof(AboutViewModel).FullName, null, new DrillInNavigationTransitionInfo(), false);
        });

        // 背景色修改设置
        public IRelayCommand BackdropSelectCommand => new RelayCommand(async () =>
        {
            await BackdropService.SetBackdropAsync(Backdrop);
            await BackdropService.SetAppBackdropAsync();
        });

        public BackdropViewModel()
        {
            Backdrop = BackdropService.AppBackdrop;

            ulong BuildNumber = InfoHelper.GetSystemVersion()["BuildNumber"];

            if (BuildNumber < 22000)
            {
                Backdrop = BackdropList[0];
                BackdropIsEnabled = false;
            }
            else
            {
                BackdropIsEnabled = true;
            }
        }
    }
}
