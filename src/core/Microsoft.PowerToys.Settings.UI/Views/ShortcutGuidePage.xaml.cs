﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.PowerToys.Settings.UI.Lib;
using Microsoft.PowerToys.Settings.UI.Lib.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Microsoft.PowerToys.Settings.UI.Views
{
    public sealed partial class ShortcutGuidePage : Page
    {
        private ShortcutGuideViewModel ViewModel { get; set; }

        public ShortcutGuidePage()
        {
            InitializeComponent();

            ViewModel = new ShortcutGuideViewModel(GeneralSettingsCache<GeneralSettings>.Instance, ShellPage.SendDefaultIPCMessage);
            DataContext = ViewModel;
        }
    }
}
