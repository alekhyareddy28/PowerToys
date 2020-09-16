﻿// Copyright (c) Microsoft Corporation
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
        [JsonPropertyName("packaged")]
        public bool Packaged { get; set; }

        // Gets or sets a value indicating whether run powertoys on start-up.
        [JsonPropertyName("startup")]
        public bool Startup { get; set; }

        // Gets or sets a value indicating whether the powertoy elevated.
        [JsonPropertyName("is_elevated")]
        public bool IsElevated { get; set; }

        // Gets or sets a value indicating whether powertoys should run elevated.
        [JsonPropertyName("run_elevated")]
        public bool RunElevated { get; set; }

        // Gets or sets a value indicating whether is admin.
        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }

        // Gets or sets theme Name.
        [JsonPropertyName("theme")]
        public string Theme { get; set; }

        // Gets or sets system theme name.
        [JsonPropertyName("system_theme")]
        public string SystemTheme { get; set; }

        // Gets or sets powertoys version number.
        [JsonPropertyName("powertoys_version")]
        public string PowertoysVersion { get; set; }

        [JsonPropertyName("action_name")]
        public string CustomActionName { get; set; }

        [JsonPropertyName("enabled")]
        public EnabledModules Enabled { get; set; }

        [JsonPropertyName("download_updates_automatically")]
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
            return JsonSerializer.Serialize(this);
        }

        private string DefaultPowertoysVersion()
        {
            return interop.CommonManaged.GetProductVersion();
        }
    }
}
