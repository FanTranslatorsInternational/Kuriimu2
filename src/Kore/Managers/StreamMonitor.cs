using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models.IO;
using Kore.Factories;
using MoreLinq;

namespace Kore.Managers
{
    class StreamMonitor : IDisposable
    {
        private const string TempFolder_ = "tmp";

        private static readonly object _releaseLock = new object();
        private bool _isCollecting = false;

        private readonly Timer _temporaryContainerCollectionTimer;

        private readonly IList<IStreamManager> _streamManagers;
        private readonly IList<(IStreamManager, IFileSystem, UPath)> _temporaryContainers;

        public StreamMonitor()
        {
            _temporaryContainerCollectionTimer = new Timer(1000.0);
            _temporaryContainerCollectionTimer.Elapsed += TemporaryContainerCollectionTimer_Elapsed;
            _temporaryContainerCollectionTimer.Start();

            _streamManagers = new List<IStreamManager>();
            _temporaryContainers = new List<(IStreamManager, IFileSystem, UPath)>();
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

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var tempDirectory = UPath.Combine(baseDirectory, TempFolder_, Guid.NewGuid().ToString("D"));
            var temporaryFileSystem = FileSystemFactory.CreatePhysicalFileSystem(tempDirectory, streamManager);

            _temporaryContainers.Add((streamManager, temporaryFileSystem, tempDirectory));

            return temporaryFileSystem;
        }

        public void ReleaseTemporaryFileSystem(IFileSystem temporaryFileSystem)
        {
            if (_temporaryContainers.All(x => x.Item2 != temporaryFileSystem))
                return;

            var element = _temporaryContainers.Index().First(x => x.Value.Item2 == temporaryFileSystem);
            _temporaryContainers[element.Key] = (element.Value.Item1, null, element.Value.Item3);
        }

        public void Dispose()
        {
            _temporaryContainerCollectionTimer?.Dispose();

            if (_streamManagers != null)
                foreach (var streamManager in _streamManagers)
                    streamManager.ReleaseAll();

            if (_temporaryContainers != null)
                foreach (var temporaryContainer in _temporaryContainers)
                    ReleaseTemporaryContainer(temporaryContainer);

            _streamManagers?.Clear();
            _temporaryContainers?.Clear();
        }

        /// <summary>
        /// Acts as the garbage collection process per interval for this instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TemporaryContainerCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(_isCollecting)
                return;

            _isCollecting = true;

            foreach (var temporaryContainer in _temporaryContainers.ToArray())
            {
                if (temporaryContainer.Item2 != null)
                    continue;

                if (temporaryContainer.Item1.Count > 0)
                    continue;

                ReleaseTemporaryContainer(temporaryContainer);
            }

            _isCollecting = false;
        }

        private void ReleaseTemporaryContainer((IStreamManager, IFileSystem, UPath) element)
        {
            lock (_releaseLock)
            {
                // Remove temporary container
                if (_temporaryContainers.Contains(element))
                    _temporaryContainers.Remove(element);

                // Remove temporary directory
                if (Directory.Exists(element.Item3.FullName))
                    Directory.Delete(element.Item3.FullName, true);
            }
        }
    }
}
