﻿using GetStoreApp.Contracts.Services.App;
using GetStoreApp.Helpers;
using GetStoreApp.ViewModels.Controls.About;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.UI.Controls.About
{
    public sealed partial class HeaderControl : UserControl
    {
        public IResourceService ResourceService { get; }

        public HeaderViewModel ViewModel { get; }

        public HeaderControl()
        {
            ResourceService = IOCHelper.GetService<IResourceService>();
            ViewModel = IOCHelper.GetService<HeaderViewModel>();
            InitializeComponent();
        }

        public string LocalizedAppVersion(string appVersion)
        {
            return string.Format(ResourceService.GetLocalized("/About/AppVersion"), appVersion);
        }
    }
}
