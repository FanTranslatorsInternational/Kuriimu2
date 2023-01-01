using System;
using Zio;

namespace Kore.FileSystems.Watcher
{
    public class FileOpenedEventArgs : EventArgs
    {
        /// <summary>
        /// The filesystem originating this change.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Absolute path to the file or directory.
        /// </summary>
        public UPath FullPath { get; }

        /// <summary>
        /// Name of the file or directory.
        /// </summary>
        public string Name { get; }

        public FileOpenedEventArgs(IFileSystem fileSystem, UPath fullPath)
        {
            if (fileSystem is null) throw new ArgumentNullException(nameof(fileSystem));
            fullPath.AssertNotNull(nameof(fullPath));
            fullPath.AssertAbsolute(nameof(fullPath));

            FileSystem = fileSystem;
            FullPath = fullPath;
            Name = fullPath.GetName();
        }
    }
}
