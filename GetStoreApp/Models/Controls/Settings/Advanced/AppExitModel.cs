﻿using Microsoft.UI.Xaml;

namespace GetStoreApp.Models.Controls.Settings.Advanced
{
    public class AppExitModel : DependencyObject
    {
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(AppExitModel), new PropertyMetadata(string.Empty));

        public string InternalName
        {
            get { return (string)GetValue(InternalNameProperty); }
            set { SetValue(InternalNameProperty, value); }
        }

        public static readonly DependencyProperty InternalNameProperty =
            DependencyProperty.Register("InternalName", typeof(string), typeof(AppExitModel), new PropertyMetadata(string.Empty));
    }
}
