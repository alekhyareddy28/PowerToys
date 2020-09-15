// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerToys.Settings.UI.Lib.Interface;

namespace Microsoft.PowerToys.Settings.UI.Lib
{
    public class GeneralSettings : IGeneralSettingsData
    {
        private static readonly GeneralSettings GeneralSettingsData = new GeneralSettings();

        public static GeneralSettings Instance
        {
            get
            {
                return GeneralSettingsData;
            }
        }

        // Gets or sets a value indicating whether packaged.
        public bool Packaged { get; set; }

        // Gets or sets a value indicating whether run powertoys on start-up.
        public bool Startup { get; set; }

        // Gets or sets a value indicating whether the powertoy elevated.
        public bool IsElevated { get; set; }

        // Gets or sets a value indicating whether powertoys should run elevated.
        public bool RunElevated { get; set; }

        // Gets or sets a value indicating whether is admin.
        public bool IsAdmin { get; set; }

        // Gets or sets theme Name.
        public string Theme { get; set; }

        // Gets or sets system theme name.
        public string SystemTheme { get; set; }

        // Gets or sets powertoys version number.
        public string PowertoysVersion { get; set; }

        public string CustomActionName { get; set; }

        public EnabledModules Enabled { get; set; }

        public bool AutoDownloadUpdates { get; set; }

        private GeneralSettings()
        {
            Packaged = false;
            Startup = false;
            IsAdmin = false;
            IsElevated = false;
            AutoDownloadUpdates = false;
            Theme = "system";
            SystemTheme = "light";
            try
            {
                PowertoysVersion = DefaultPowertoysVersion();
            }
            catch
            {
                PowertoysVersion = "v0.0.0";
            }

            Enabled = new EnabledModules();
            CustomActionName = string.Empty;
        }

        // converts the current to a json string.
        public string ToJsonString()
        {
            return JsonSerializer.Serialize((IGeneralSettingsData)this);
        }

        private string DefaultPowertoysVersion()
        {
            return interop.CommonManaged.GetProductVersion();
        }
    }
}
