﻿using GetStoreApp.Contracts.Services.Root;
using GetStoreApp.Helpers;
using GetStoreApp.ViewModels.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.Views
{
    public sealed partial class DownloadPage : Page
    {
        public IResourceService ResourceService { get; } = IOCHelper.GetService<IResourceService>();

        public DownloadViewModel ViewModel { get; } = IOCHelper.GetService<DownloadViewModel>();

        public DownloadPage()
        {
            InitializeComponent();
        }

        public void TeachingTipClick(object sender, RoutedEventArgs args)
        {
            DownloadTeachingTip.IsOpen = true;
        }
    }
}