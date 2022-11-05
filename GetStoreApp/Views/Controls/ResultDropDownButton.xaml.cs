﻿using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.Views.Controls
{
    public sealed partial class ResultDropDownButton : DropDownButton
    {
        public ResultDropDownButton()
        {
            InitializeComponent();
        }

        public void SetCursor(InputCursor cursor)
        {
            ProtectedCursor = cursor;
        }
    }
}
