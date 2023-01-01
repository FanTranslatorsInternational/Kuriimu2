using System.IO;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State.Archive;

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
        /// <param name="fileData">The file stream to set to the <see cref="IArchiveFileInfo"/>.</param>
        /// <param name="filePath">The path of the file to add to this state.</param>
        /// <returns>The newly created <see cref="ArchiveFileInfo"/>.</returns>
        IArchiveFileInfo AddFile(Stream fileData, UPath filePath);
    }
}
