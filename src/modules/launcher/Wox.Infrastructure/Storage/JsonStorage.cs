﻿using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Wox.Infrastructure.Logger;

namespace Wox.Infrastructure.Storage
{
    /// <summary>
    /// Serialize object using json format.
    /// </summary>
    public class JsonStorage<T>
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private T _data;
        // need a new directory name
        public const string DirectoryName = "Settings";
        public const string FileSuffix = ".json";
        public string FilePath { get; set; }
        public string DirectoryPath { get; set; }

        // This storage helper returns whether or not to delete the json storage items
        private static readonly int JSON_STORAGE = 1;
        private StorageHelper _storageHelper = new StorageHelper(JSON_STORAGE);

        internal JsonStorage()
        {
            // use property initialization instead of DefaultValueAttribute
            // easier and flexible for default value of object
            _serializerSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public T Load(bool validateItem = true)
        {
            // Depending on the version number of the previously installed PT Run, delete the cache if it found to be incompatible
            if (_storageHelper.clearCache)
            {
                if(File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }

            if (File.Exists(FilePath))
            {
                var serialized = File.ReadAllText(FilePath);
                if (!string.IsNullOrWhiteSpace(serialized))
                {
                    Deserialize(serialized);
                }
                else
                {
                    LoadDefault();
                }
            }
            else
            {
                LoadDefault();
            }
            return _data.NonNull();
        }

        private void Deserialize(string serialized)
        {
            try
            {
                _data = JsonConvert.DeserializeObject<T>(serialized, _serializerSettings);
            }
            catch (JsonException e)
            {
                LoadDefault();
                Log.Exception($"|JsonStorage.Deserialize|Deserialize error for json <{FilePath}>", e);
            }

            if (_data == null)
            {
                LoadDefault();
            }
        }

        private void LoadDefault()
        {
            if (File.Exists(FilePath))
            {
                BackupOriginFile();
            }

            _data = JsonConvert.DeserializeObject<T>("{}", _serializerSettings);
            Save();
        }

        private void BackupOriginFile()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", CultureInfo.CurrentUICulture);
            var directory = Path.GetDirectoryName(FilePath).NonNull();
            var originName = Path.GetFileNameWithoutExtension(FilePath);
            var backupName = $"{originName}-{timestamp}{FileSuffix}";
            var backupPath = Path.Combine(directory, backupName);
            File.Copy(FilePath, backupPath, true);
            // todo give user notification for the backup process
        }

        public void Save()
        {
            string serialized = JsonConvert.SerializeObject(_data, Formatting.Indented);
            File.WriteAllText(FilePath, serialized);
        }
    }
}
