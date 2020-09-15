// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.PowerToys.Settings.UI.Lib.Interface
{
    public interface IGeneralSettingsData
    {
        [JsonPropertyName("packaged")]
        bool Packaged { get; set; }

        [JsonPropertyName("startup")]
        bool Startup { get; set; }

        [JsonPropertyName("is_elevated")]
        bool IsElevated { get; set; }

        [JsonPropertyName("run_elevated")]
        bool RunElevated { get; set; }

        [JsonPropertyName("is_admin")]
        bool IsAdmin { get; set; }

        [JsonPropertyName("theme")]
        string Theme { get; set; }

        [JsonPropertyName("system_theme")]
        string SystemTheme { get; set; }

        [JsonPropertyName("powertoys_version")]
        string PowertoysVersion { get; set; }

        [JsonPropertyName("action_name")]
        string CustomActionName { get; set; }

        [JsonPropertyName("enabled")]
        EnabledModules Enabled { get; set; }

        [JsonPropertyName("download_updates_automatically")]
        bool AutoDownloadUpdates { get; set; }

        string ToJsonString();
    }
}
