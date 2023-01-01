using System;
using Zio;

namespace Kore.FileSystems.Watcher
{
    class KoreWrapFileSystemWatcher : KoreFileSystemWatcher
    {
        private readonly IKoreFileSystemWatcher _watcher;

        public KoreWrapFileSystemWatcher(IKoreFileSystem fileSystem, UPath path, IKoreFileSystemWatcher watcher) : base(fileSystem, path)
        {
            _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));

            RegisterEvents(_watcher);
        }

        /// <inheritdoc />
        public override int InternalBufferSize
        {
            get => _watcher.InternalBufferSize;
            set => _watcher.InternalBufferSize = value;
        }

        /// <inheritdoc />
        public override NotifyFilters NotifyFilter
        {
            get => _watcher.NotifyFilter;
            set => _watcher.NotifyFilter = value;
        }

        /// <inheritdoc />
        public override bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        /// <inheritdoc />
        public override string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        /// <inheritdoc />
        public override bool IncludeSubdirectories
        {
            get => _watcher.IncludeSubdirectories;
            set => _watcher.IncludeSubdirectories = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterEvents(_watcher);
                _watcher.Dispose();
            }
        }
    }
}
