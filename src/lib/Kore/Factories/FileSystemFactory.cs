using System;
using System.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kore.Implementation.FileSystem;

namespace Kore.Factories
{
    /// <summary>
    /// Contains methods to create specific <see cref="IFileSystem"/> implementations.
    /// </summary>
    public static class FileSystemFactory
    {
        /// <summary>
        /// Create a <see cref="PhysicalFileSystem"/>.
        /// </summary>
        /// <param name="streamManager">The stream manager for this file system.</param>
        /// <returns>The created <see cref="PhysicalFileSystem"/> for this folder.</returns>
        public static IFileSystem CreatePhysicalFileSystem(IStreamManager streamManager)
        {
            return new PhysicalFileSystem(streamManager);
        }

        /// <summary>
        /// Creates a <see cref="SubFileSystem"/> from the physical path given.
        /// </summary>
        /// <param name="subPath">The path on a physical drive to root the file system to.</param>
        /// <param name="streamManager">The <see cref="IStreamManager"/> for the file system.</param>
        /// <returns>The rooted physical file system.</returns>
        public static IFileSystem CreateSubFileSystem(string subPath, IStreamManager streamManager)
        {
            var physicalFileSystem = new PhysicalFileSystem(streamManager);
            return CreateSubFileSystem(physicalFileSystem, physicalFileSystem.ConvertPathFromInternal(subPath));
        }

        /// <summary>
        /// Creates a <see cref="SubFileSystem"/> relative to a given path.
        /// </summary>
        /// <param name="fileSystem">The file system to root to the path.</param>
        /// <param name="subPath">The path to root the file system to.</param>
        /// <returns>The re-rooted file system.</returns>
        public static IFileSystem CreateSubFileSystem(IFileSystem fileSystem, UPath subPath)
        {
            if (!fileSystem.DirectoryExists(subPath))
                fileSystem.CreateDirectory(subPath);

            return new SubFileSystem(fileSystem, subPath);
        }

        /// <summary>
        /// Create a <see cref="AfiFileSystem"/> based on the given <see cref="IFileState"/>.
        /// </summary>
        /// <param name="fileState"><see cref="IFileState"/> to create the file system from.</param>
        /// <returns>The created <see cref="IFileSystem"/> for this state.</returns>
        public static IFileSystem CreateAfiFileSystem(IFileState fileState)
        {
            return CreateAfiFileSystem(fileState, UPath.Root);
        }

        /// <summary>
        /// Create a <see cref="AfiFileSystem"/> based on the given <see cref="IFileState"/>.
        /// </summary>
        /// <param name="fileState"><see cref="IFileState"/> to create the file system from.</param>
        /// <param name="path">The path of the virtual file system.</param>
        /// <returns>The created <see cref="IFileSystem"/> for this state.</returns>
        public static IFileSystem CreateAfiFileSystem(IFileState fileState, UPath path)
        {
            if (!(fileState.PluginState is IArchiveState))
                throw new InvalidOperationException("This state is not an archive.");

            return CreateAfiFileSystem(fileState, path, fileState.StreamManager);
        }

        /// <summary>
        /// Create a <see cref="AfiFileSystem"/> based on the given <see cref="IArchiveState"/>.
        /// </summary>
        /// <param name="fileState"><see cref="IFileState"/> to create the file system from.</param>
        /// <param name="path">The path of the virtual file system.</param>
        /// <param name="streamManager">The stream manager for this file system.</param>
        /// <returns>The created <see cref="IFileSystem"/> for this state.</returns>
        public static IFileSystem CreateAfiFileSystem(IFileState fileState, UPath path, IStreamManager streamManager)
        {
            var fileSystem = (IFileSystem)new AfiFileSystem(fileState, streamManager);
            if (path != UPath.Empty && path != UPath.Root)
                fileSystem = new SubFileSystem(fileSystem, path);

            return fileSystem;
        }

        /// <summary>
        /// Creates a <see cref="MemoryFileSystem"/> based on the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="streamFile">The in-memory file to add to the file system.</param>
        /// <param name="streamManager">The stream manager for this file system.</param>
        /// <returns>The created <see cref="IFileSystem"/> for this stream.</returns>
        public static IFileSystem CreateMemoryFileSystem(StreamFile streamFile, IStreamManager streamManager)
        {
            var stream = streamFile.Stream;
            var directory = streamFile.Path.GetDirectory();

            // 1. Create file system
            var fileSystem = new MemoryFileSystem(streamManager);
            if (!directory.IsEmpty && !fileSystem.DirectoryExists(directory))
                fileSystem.CreateDirectory(directory);

            var createdStream = fileSystem.OpenFile(streamFile.Path.ToAbsolute(), FileMode.CreateNew, FileAccess.Write, FileShare.Write);

            // 2. Copy data
            var bkPos = stream.Position;
            stream.Position = 0;
            stream.CopyTo(createdStream);
            stream.Position = bkPos;
            createdStream.Position = 0;
            createdStream.Close();

            return fileSystem;
        }

        /// <summary>
        /// Clone a <see cref="IFileSystem"/> with a new sub path.
        /// </summary>
        /// <param name="fileSystem"><see cref="IFileSystem"/> to clone.</param>
        /// <param name="path">The sub path of the cloned file system.</param>
        /// <param name="streamManager">The stream manager for this file system.</param>
        /// <returns>The cloned <see cref="IFileSystem"/>.</returns>
        public static IFileSystem CloneFileSystem(IFileSystem fileSystem, UPath path, IStreamManager streamManager)
        {
            var newFileSystem = fileSystem.Clone(streamManager);
            if (path != UPath.Empty)
                newFileSystem = new SubFileSystem(newFileSystem, path);

            return newFileSystem;
        }
    }
}
