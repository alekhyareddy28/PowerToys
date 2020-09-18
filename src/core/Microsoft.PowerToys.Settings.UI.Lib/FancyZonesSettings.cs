﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.PowerToys.Settings.UI.Lib.Interface;

namespace Microsoft.PowerToys.Settings.UI.Lib
{
    public class FancyZonesSettings : BasePTModuleSettings, ISettingsConfig
    {
        public const string ModuleName = "FancyZones";

        public FancyZonesSettings()
        {
            Version = "1.0";
            Name = ModuleName;
            Properties = new FZConfigProperties();
        }

        [JsonPropertyName("properties")]
        public FZConfigProperties Properties { get; set; }
    }
}
