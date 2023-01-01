using System;
using Zio;

namespace Kore.FileSystems.Watcher
{
    public interface IKoreFileSystemWatcher : IFileSystemWatcher
    {
        /// <summary>
        /// Event for when a file gets opened.
        /// </summary>
        event EventHandler<FileOpenedEventArgs>? Opened;
    }
}
