using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Providers
{
    /// <summary>
    /// An interface to provide access to file system creation.
    /// </summary>
    public interface IFileSystemProvider
    {
        /// <summary>
        /// Creates a physical file system from a path.
        /// </summary>
        /// <param name="path">The path to create a file system instance from.</param>
        /// <returns><see cref="IFileSystem"/> from the physical path.</returns>
        IFileSystem CreatePhysicalFileSystem(string path);

        /// <summary>
        /// Creates a virtual file system from a loaded <see cref="IArchiveState"/>.
        /// </summary>
        /// <param name="archiveState">The archive state to use for the file system.</param>
        /// <returns><see cref="IFileSystem"/> from the <see cref="IArchiveState"/>.</returns>
        IFileSystem CreateAfiFileSystem(IArchiveState archiveState);

        /// <summary>
        /// Creates a virtual file system from a loaded <see cref="IArchiveState"/>.
        /// </summary>
        /// <param name="archiveState">The archive state to use for the file system.</param>
        /// <param name="path">The path into the file system to root to.</param>
        /// <returns><see cref="IFileSystem"/> from the <see cref="IArchiveState"/>.</returns>
        IFileSystem CreateAfiFileSystem(IArchiveState archiveState, UPath path);
    }
}
