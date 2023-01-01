using System;
using Zio;
using Zio.FileSystems;

namespace Kore.FileSystems.Watcher
{
    public class KoreFileSystemWatcher : FileSystemWatcher, IKoreFileSystemWatcher
    {
        private FilterPattern _filterPattern;

        public event EventHandler<FileOpenedEventArgs>? Opened;

        public override string Filter
        {
            get => base.Filter;
            set
            {
                base.Filter = value;

                if (base.Filter == value)
                    return;

                _filterPattern = FilterPattern.Parse(base.Filter);
            }
        }

        public KoreFileSystemWatcher(IFileSystem fileSystem, UPath path) : base(fileSystem, path)
        {
        }

        /// <summary>
        /// Raises the <see cref="Opened"/> event. 
        /// </summary>
        /// <param name="eventArgs">Arguments for the event.</param>
        public void RaiseOpened(FileOpenedEventArgs eventArgs)
        {
            if (!ShouldRaiseEvent(eventArgs))
                return;

            Opened?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Listens to events from another <see cref="IKoreFileSystemWatcher"/> instance to forward them
        /// into this instance.
        /// </summary>
        /// <param name="watcher">Other instance to listen to.</param>
        protected void RegisterEvents(IKoreFileSystemWatcher watcher)
        {
            base.RegisterEvents(watcher);

            watcher.Opened += OnOpened;
        }

        /// <summary>
        /// Stops listening to events from another <see cref="IKoreFileSystemWatcher"/>.
        /// </summary>
        /// <param name="watcher">Instance to remove event handlers from.</param>
        protected void UnregisterEvents(IKoreFileSystemWatcher watcher)
        {
            base.UnregisterEvents(watcher);

            watcher.Opened -= OnOpened;
        }

        private void OnOpened(object sender, FileOpenedEventArgs args)
        {
            var newPath = TryConvertPath(args.FullPath);
            if (!newPath.HasValue)
                return;

            var newArgs = new FileOpenedEventArgs(FileSystem, newPath.Value);
            RaiseOpened(newArgs);
        }

        private bool ShouldRaiseEvent(FileOpenedEventArgs args)
        {
            return EnableRaisingEvents &&
                   _filterPattern.Match(args.Name) &&
                   args.FullPath.IsInDirectory(Path, IncludeSubdirectories);
        }
    }
}
