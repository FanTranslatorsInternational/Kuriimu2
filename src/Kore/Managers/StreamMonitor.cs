using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models.IO;
using Kore.Factories;

namespace Kore.Managers
{
    class StreamMonitor : IDisposable
    {
        private const string TempFolder_ = "tmp";

        private bool _isCollecting;

        private readonly Timer _temporaryContainerCollectionTimer;

        private readonly IList<IStreamManager> _streamManagers;
        private readonly ConcurrentDictionary<IFileSystem, (IStreamManager, UPath)> _temporaryFileSystemMapping;
        private readonly ConcurrentDictionary<IStreamManager, (IFileSystem, UPath)> _streamManagerMapping;

        public StreamMonitor()
        {
            _temporaryContainerCollectionTimer = new Timer(1000.0);
            _temporaryContainerCollectionTimer.Elapsed += TemporaryContainerCollectionTimer_Elapsed;
            _temporaryContainerCollectionTimer.Start();

            _streamManagers = new List<IStreamManager>();

            _temporaryFileSystemMapping = new ConcurrentDictionary<IFileSystem, (IStreamManager, UPath)>();
            _streamManagerMapping = new ConcurrentDictionary<IStreamManager, (IFileSystem, UPath)>();
        }

        public IStreamManager CreateStreamManager()
        {
            var streamManager = new StreamManager();
            _streamManagers.Add(streamManager);

            return streamManager;
        }

        public IFileSystem CreateTemporaryFileSystem()
        {
            var streamManager = CreateStreamManager();

            var tempDirectory = CreateTemporaryDirectory();
            var temporaryFileSystem = FileSystemFactory.CreatePhysicalFileSystem(tempDirectory, streamManager);

            _temporaryFileSystemMapping.GetOrAdd(temporaryFileSystem, x => (streamManager, tempDirectory));
            _streamManagerMapping.GetOrAdd(streamManager, x => (temporaryFileSystem, tempDirectory));

            return temporaryFileSystem;
        }

        public IStreamManager GetStreamManager(IFileSystem fileSystem)
        {
            if (_temporaryFileSystemMapping.TryGetValue(fileSystem, out var element))
                return element.Item1;

            throw new InvalidOperationException("The file system was not created by this instance.");
        }

        public void ReleaseTemporaryFileSystem(IFileSystem temporaryFileSystem)
        {
            if (!_temporaryFileSystemMapping.TryRemove(temporaryFileSystem, out var element))
                return;

            _streamManagerMapping.AddOrUpdate(element.Item1, x => (null, element.Item2), (y, z) => (null, element.Item2));
        }

        public void Dispose()
        {
            _temporaryContainerCollectionTimer?.Dispose();

            if (_streamManagers != null)
                foreach (var streamManager in _streamManagers)
                    streamManager.ReleaseAll();

            if (_streamManagerMapping != null)
                foreach (var mapping in _streamManagerMapping)
                    RemoveDirectory(mapping.Value.Item2.FullName);

            _streamManagers?.Clear();
            _streamManagerMapping?.Clear();
            _temporaryFileSystemMapping?.Clear();
        }

        /// <summary>
        /// Acts as the garbage collection process per interval for this instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TemporaryContainerCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isCollecting)
                return;

            _isCollecting = true;

            foreach (var streamManager in _streamManagerMapping.ToArray().Where(x => x.Value.Item1 != null).Select(x => x.Key))
            {
                if (streamManager.Count > 0)
                    continue;

                if (!_streamManagerMapping.TryRemove(streamManager, out var element))
                    continue;

                RemoveDirectory(element.Item2.FullName);
            }

            _isCollecting = false;
        }

        private void RemoveDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        private string CreateTemporaryDirectory()
        {
            var currentDirectory = GetCurrentDirectory();
            return Path.Combine(currentDirectory, TempFolder_, Guid.NewGuid().ToString("D"));
        }

        private string GetCurrentDirectory()
        {
            var process = Process.GetCurrentProcess().MainModule;
            return process == null ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(process.FileName);
        }
    }
}
