using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zio;
using Zio.FileSystems;

namespace Kore.FileSystems.Watcher
{
    public class KoreFileSystemEventDispatcher<T> : FileSystemEventDispatcher<T>
        where T : KoreFileSystemWatcher
    {
        public KoreFileSystemEventDispatcher(IKoreFileSystem fileSystem) : base(fileSystem)
        {
        }

        /// <summary>
        /// Returns all watchers in this dispatcher.
        /// </summary>
        /// <returns>The watchers of this dispatcher.</returns>
        public IList<T> Get()
        {
            var watchers = (IList<T>)typeof(FileSystemEventDispatcher<T>).GetField("_watchers", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this);
            return watchers?.ToArray();
        }

        /// <summary>
        /// Raise the <see cref="IKoreFileSystemWatcher.Opened"/> event on watchers.
        /// </summary>
        /// <param name="path">Absolute path to the opened file.</param>
        public void RaiseOpened(UPath path)
        {
            var args = new FileOpenedEventArgs(FileSystem, path);

            // HACK: Call Dispatch method
            var action = new Action<T, FileOpenedEventArgs>((w, a) => w.RaiseOpened(a));
            var method = typeof(FileSystemEventDispatcher<T>).GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic)?.MakeGenericMethod(typeof(FileOpenedEventArgs));
            method?.Invoke(this, new object[] { args, action });
        }
    }
}
