using System;
using Kontract.Interfaces.Managers.Streams;
using Kore.FileSystems.Watcher;
using Zio;
using Zio.FileSystems;

namespace Kore.FileSystems
{
    public class KoreSubFileSystem : SubFileSystem, IKoreFileSystem
    {
        public KoreSubFileSystem(IKoreFileSystem fileSystem, UPath subPath, bool owned = true) : base(fileSystem, subPath, owned)
        {
        }

        public IKoreFileSystem Clone(IStreamManager streamManager)
        {
            var clonedFs = ((IKoreFileSystem)FallbackSafe).Clone(streamManager);
            return new KoreSubFileSystem(clonedFs, SubPath, Owned);
        }

        protected override IFileSystemWatcher WatchImpl(UPath path)
        {
            var watcher = FallbackSafe.Watch(ConvertPathToDelegate(path));
            var wrappedWatcher = new Watcher(this, path, (IKoreFileSystemWatcher)watcher);

            return wrappedWatcher;
        }

        private class Watcher : KoreWrapFileSystemWatcher
        {
            private readonly KoreSubFileSystem _fileSystem;

            public Watcher(KoreSubFileSystem fileSystem, UPath path, IKoreFileSystemWatcher watcher)
                : base(fileSystem, path, watcher)
            {
                _fileSystem = fileSystem;
            }

            protected override UPath? TryConvertPath(UPath pathFromEvent)
            {
                if (!pathFromEvent.IsInDirectory(_fileSystem.SubPath, true))
                    return null;

                return _fileSystem.ConvertPathFromDelegate(pathFromEvent);
            }
        }

        protected override UPath ConvertPathFromDelegate(UPath path)
        {
            var fullPath = path.FullName;
            if (!fullPath.StartsWith(SubPath.FullName) || (fullPath.Length > SubPath.FullName.Length && fullPath[SubPath == UPath.Root ? 0 : SubPath.FullName.Length] != UPath.DirectorySeparator))
            {
                // More a safe guard, as it should never happen, but if a delegate filesystem doesn't respect its root path
                // we are throwing an exception here
                throw new InvalidOperationException($"The path `{path}` returned by the delegate filesystem is not rooted to the subpath `{SubPath}`");
            }

            var subPath = fullPath.Substring(SubPath.FullName.Length);
            return subPath == string.Empty ? UPath.Root : new UPath(subPath);
        }
    }
}
