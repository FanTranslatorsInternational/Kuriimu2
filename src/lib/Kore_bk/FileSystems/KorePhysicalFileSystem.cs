using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kontract.Interfaces.Managers.Streams;
using Kore.FileSystems.Watcher;
using Zio;
using Zio.FileSystems;

namespace Kore.FileSystems
{
    public class KorePhysicalFileSystem : PhysicalFileSystem, IKoreFileSystem
    {
        private readonly IStreamManager _streamManager;
        private readonly object _dispatcherLock;
        private KoreFileSystemEventDispatcher<Watcher> _dispatcher;

        public KorePhysicalFileSystem(IStreamManager streamManager)
        {
            _streamManager = streamManager;
            _dispatcherLock = new object();
        }

        private KorePhysicalFileSystem(IStreamManager streamManager, IList<Watcher> watchers)
        {
            _streamManager = streamManager;
            _dispatcherLock = new object();

            foreach (var watcher in watchers)
                GetOrCreateDispatcher().Add(watcher);
        }

        public IKoreFileSystem Clone(IStreamManager streamManager)
        {
            return new KorePhysicalFileSystem(streamManager, GetOrCreateDispatcher()?.Get());
        }

        protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share = FileShare.None)
        {
            var openedFile = base.OpenFileImpl(path, mode, access, share);

            _streamManager.Register(openedFile);
            GetOrCreateDispatcher()?.RaiseOpened(path);

            return openedFile;
        }

        protected override IFileSystemWatcher WatchImpl(UPath path)
        {
            // HACK: Get System.IO.FileSystemWatcher
            var watcher = base.WatchImpl(path);
            var nativeWatcher = (System.IO.FileSystemWatcher)watcher.GetType().GetField("_watcher", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(watcher);

            var ownWatcher = new Watcher(this, path, nativeWatcher);
            GetOrCreateDispatcher()?.Add(ownWatcher);

            return ownWatcher;
        }

        private KoreFileSystemEventDispatcher<Watcher> GetOrCreateDispatcher()
        {
            lock (_dispatcherLock)
            {
                _dispatcher ??= new KoreFileSystemEventDispatcher<Watcher>(this);

                return _dispatcher;
            }
        }

        private sealed class Watcher : KoreFileSystemWatcher
        {
            private readonly PhysicalFileSystem _fileSystem;
            private readonly System.IO.FileSystemWatcher _watcher;

            public override int InternalBufferSize
            {
                get => _watcher.InternalBufferSize;
                set => _watcher.InternalBufferSize = value;
            }

            public override Zio.NotifyFilters NotifyFilter
            {
                get => (Zio.NotifyFilters)_watcher.NotifyFilter;
                set => _watcher.NotifyFilter = (System.IO.NotifyFilters)value;
            }

            public override bool EnableRaisingEvents
            {
                get => _watcher.EnableRaisingEvents;
                set => _watcher.EnableRaisingEvents = value;
            }

            public override string Filter
            {
                get => _watcher.Filter;
                set
                {
                    _watcher.Filter = value;
                    base.Filter = value;
                }
            }

            public override bool IncludeSubdirectories
            {
                get => _watcher.IncludeSubdirectories;
                set => _watcher.IncludeSubdirectories = value;
            }

            public Watcher(PhysicalFileSystem fileSystem, UPath path, System.IO.FileSystemWatcher watcher) : base(fileSystem, path)
            {
                _fileSystem = fileSystem;
                _watcher = watcher;

                _watcher.Changed += (sender, args) => RaiseChanged(Remap(args));
                _watcher.Created += (sender, args) => RaiseCreated(Remap(args));
                _watcher.Deleted += (sender, args) => RaiseDeleted(Remap(args));
                _watcher.Error += (sender, args) => RaiseError(Remap(args));
                _watcher.Renamed += (sender, args) => RaiseRenamed(Remap(args));
            }

            ~Watcher()
            {
                Dispose(false);
            }

            private FileChangedEventArgs Remap(FileSystemEventArgs args)
            {
                var newChangeType = (Zio.WatcherChangeTypes)args.ChangeType;
                var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
                return new FileChangedEventArgs(FileSystem, newChangeType, newPath);
            }

            private FileSystemErrorEventArgs Remap(ErrorEventArgs args)
            {
                return new FileSystemErrorEventArgs(args.GetException());
            }

            private FileRenamedEventArgs Remap(RenamedEventArgs args)
            {
                var newChangeType = (Zio.WatcherChangeTypes)args.ChangeType;
                var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
                var newOldPath = _fileSystem.ConvertPathFromInternal(args.OldFullPath);
                return new FileRenamedEventArgs(FileSystem, newChangeType, newPath, newOldPath);
            }
        }
    }
}
