﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wox.Infrastructure.Logger;
using Wox.Infrastructure.Storage;
using Win32Program = Microsoft.Plugin.Program.Programs.Win32Program;

namespace Microsoft.Plugin.Program.Storage
{
    internal class Win32ProgramRepository : ListRepository<Programs.Win32Program>, IProgramRepository
    {
        private const string LnkExtension = ".lnk";
        private const string UrlExtension = ".url";

        private IStorage<IList<Programs.Win32Program>> _storage;
        private ProgramPluginSettings _settings;
        private IList<IFileSystemWatcherWrapper> _fileSystemWatcherHelpers;
        private string[] _pathsToWatch;
        private int _numberOfPathsToWatch;
        private Collection<string> extensionsToWatch = new Collection<string> { "*.exe", $"*{LnkExtension}", "*.appref-ms", $"*{UrlExtension}" };

        private static ConcurrentQueue<string> commonEventHandlingQueue = new ConcurrentQueue<string>();

        public Win32ProgramRepository(IList<IFileSystemWatcherWrapper> fileSystemWatcherHelpers, IStorage<IList<Win32Program>> storage, ProgramPluginSettings settings, string[] pathsToWatch)
        {
            _fileSystemWatcherHelpers = fileSystemWatcherHelpers;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage), "Win32ProgramRepository requires an initialized storage interface");
            _settings = settings ?? throw new ArgumentNullException(nameof(settings), "Win32ProgramRepository requires an initialized settings object");
            _pathsToWatch = pathsToWatch;
            _numberOfPathsToWatch = pathsToWatch.Length;
            InitializeFileSystemWatchers();

            // This task would always run in the background trying to dequeue file paths from the queue at regular intervals.
            Task.Run(() =>
            {
                while (true)
                {
                    int dequeueDelay = 500;
                    string appPath = EventHandler.GetAppPathFromQueue(commonEventHandlingQueue, dequeueDelay);

                    // To allow for the installation process to finish.
                    Thread.Sleep(5000);

                    if (!string.IsNullOrEmpty(appPath))
                    {
                        Programs.Win32Program app = Programs.Win32Program.GetAppFromPath(appPath);
                        if (app != null)
                        {
                            Add(app);
                        }
                    }
                }
            }).ConfigureAwait(false);
        }

        private void InitializeFileSystemWatchers()
        {
            for (int index = 0; index < _numberOfPathsToWatch; index++)
            {
                // To set the paths to monitor
                _fileSystemWatcherHelpers[index].Path = _pathsToWatch[index];

                // to be notified when there is a change to a file
                _fileSystemWatcherHelpers[index].NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

                // filtering the app types that we want to monitor
                _fileSystemWatcherHelpers[index].Filters = extensionsToWatch;

                // Registering the event handlers
                _fileSystemWatcherHelpers[index].Created += OnAppCreated;
                _fileSystemWatcherHelpers[index].Deleted += OnAppDeleted;
                _fileSystemWatcherHelpers[index].Renamed += OnAppRenamed;
                _fileSystemWatcherHelpers[index].Changed += OnAppChanged;

                // Enable the file system watcher
                _fileSystemWatcherHelpers[index].EnableRaisingEvents = true;

                // Enable it to search in sub folders as well
                _fileSystemWatcherHelpers[index].IncludeSubdirectories = true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentially keeping the process alive>")]
        private void OnAppRenamed(object sender, RenamedEventArgs e)
        {
            string oldPath = e.OldFullPath;
            string newPath = e.FullPath;

            string extension = Path.GetExtension(newPath);
            Win32Program.ApplicationType appType = Win32Program.GetAppTypeFromPath(newPath);
            Programs.Win32Program newApp = Programs.Win32Program.GetAppFromPath(newPath);
            Programs.Win32Program oldApp = null;

            // Once the shortcut application is renamed, the old app does not exist and therefore when we try to get the FullPath we get the lnk path instead of the exe path
            // This changes the hashCode() of the old application.
            // Therefore, instead of retrieving the old app using the GetAppFromPath(), we construct the application ourself
            // This situation is not encountered for other application types because the fullPath is the path itself, instead of being computed by using the path to the app.
            try
            {
                if (appType == Win32Program.ApplicationType.ShortcutApplication)
                {
                    oldApp = new Win32Program() { Name = Path.GetFileNameWithoutExtension(e.OldName), ExecutableName = newApp.ExecutableName, FullPath = newApp.FullPath };
                }
                else if (appType == Win32Program.ApplicationType.InternetShortcutApplication)
                {
                    oldApp = new Win32Program() { Name = Path.GetFileNameWithoutExtension(e.OldName), ExecutableName = Path.GetFileName(e.OldName), FullPath = newApp.FullPath };
                }
                else
                {
                    oldApp = Win32Program.GetAppFromPath(oldPath);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"|Win32ProgramRepository|OnAppRenamed-{extension}Program|{oldPath}|Unable to create program from {oldPath}| {ex.Message}");
            }

            // To remove the old app which has been renamed and to add the new application.
            if (oldApp != null)
            {
                Remove(oldApp);
            }

            if (newApp != null)
            {
                Add(newApp);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally keeping the process alive")]
        private void OnAppDeleted(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            string extension = Path.GetExtension(path);
            Programs.Win32Program app = null;

            try
            {
                // To mitigate the issue of not having a FullPath for a shortcut app, we iterate through the items and find the app with the same hashcode.
                if (extension.Equals(LnkExtension, StringComparison.OrdinalIgnoreCase))
                {
                    app = GetAppWithSameLnkResolvedPath(path);
                }
                else if (extension.Equals(UrlExtension, StringComparison.OrdinalIgnoreCase))
                {
                    app = GetAppWithSameNameAndExecutable(Path.GetFileNameWithoutExtension(path), Path.GetFileName(path));
                }
                else
                {
                    app = Programs.Win32Program.GetAppFromPath(path);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"|Win32ProgramRepository|OnAppDeleted-{extension}Program|{path}|Unable to create program from {path}| {ex.Message}");
            }

            if (app != null)
            {
                Remove(app);
            }
        }

        // When a URL application is deleted, we can no longer get the HashCode directly from the path because the FullPath a Url app is the URL obtained from reading the file
        private Win32Program GetAppWithSameNameAndExecutable(string name, string executableName)
        {
            foreach (Win32Program app in Items)
            {
                if (name.Equals(app.Name, StringComparison.CurrentCultureIgnoreCase) && executableName.Equals(app.ExecutableName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return app;
                }
            }

            return null;
        }

        // To mitigate the issue faced (as stated above) when a shortcut application is renamed, the Exe FullPath and executable name must be obtained.
        // Unlike the rename event args, since we do not have a newPath, we iterate through all the programs and find the one with the same LnkResolved path.
        private Programs.Win32Program GetAppWithSameLnkResolvedPath(string lnkResolvedPath)
        {
            foreach (Programs.Win32Program app in Items)
            {
                if (lnkResolvedPath.ToLower(CultureInfo.CurrentCulture).Equals(app.LnkResolvedPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    return app;
                }
            }

            return null;
        }

        private void OnAppCreated(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            if (!Path.GetExtension(path).Equals(UrlExtension, StringComparison.CurrentCultureIgnoreCase) && !Path.GetExtension(path).Equals(LnkExtension, StringComparison.CurrentCultureIgnoreCase))
            {
                Programs.Win32Program app = Programs.Win32Program.GetAppFromPath(path);
                if (app != null)
                {
                    Add(app);
                }
            }
        }

        private void OnAppChanged(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            if (Path.GetExtension(path).Equals(UrlExtension, StringComparison.CurrentCultureIgnoreCase) || Path.GetExtension(path).Equals(LnkExtension, StringComparison.CurrentCultureIgnoreCase))
            {
                // When a url or lnk app is installed, multiple created and changed events are triggered.
                // To prevent the code from acting on the first such event (which may still be during app installation), the events are added a common queue and dequeued by a background task at regular intervals - https://github.com/microsoft/PowerToys/issues/6429.
                commonEventHandlingQueue.Enqueue(path);
            }
        }

        public void IndexPrograms()
        {
            var applications = Programs.Win32Program.All(_settings);
            Set(applications);
        }

        public void Save()
        {
            _storage.Save(Items);
        }

        public void Load()
        {
            var items = _storage.TryLoad(Array.Empty<Win32Program>());
            Set(items);
        }
    }
}
