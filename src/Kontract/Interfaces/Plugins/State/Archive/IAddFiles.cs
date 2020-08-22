using Kontract.Interfaces.FileSystem;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Marks the archive state able to add a new file.
    /// </summary>
    public interface IAddFiles
    {
        /// <summary>
        /// Adds a file to the archive state.
        /// </summary>
        /// <param name="fileSystem">The file system from which to open <paramref name="filePath"/>.</param>
        /// <param name="filePath">the path of the file to add to this state.</param>
        /// <returns>The newly created <see cref="ArchiveFileInfo"/>.</returns>
        ArchiveFileInfo AddFile(IFileSystem fileSystem, UPath filePath);
    }
}
