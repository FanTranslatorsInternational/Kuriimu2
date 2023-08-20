using Kontract.Models.FileSystem;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Exposes methods to rename files in an archive.
    /// </summary>
    public interface IRenameFiles
    {
        /// <summary>
        /// RenameFile a given file.
        /// </summary>
        /// <param name="afi">The file to rename.</param>
        /// <param name="path">The new path of the file.</param>
        void RenameFile(IArchiveFileInfo afi, UPath path);
    }
}
