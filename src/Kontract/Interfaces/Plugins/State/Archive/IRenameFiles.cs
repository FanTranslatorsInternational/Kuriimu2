using Kontract.Models;
using Kontract.Models.Archive;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Exposes methods to rename files in an archive.
    /// </summary>
    public interface IRenameFiles
    {
        /// <summary>
        /// Rename a given file.
        /// </summary>
        /// <param name="afi">The file to rename.</param>
        /// <param name="path">The new path of the file.</param>
        void Rename(ArchiveFileInfo afi, UPath path);
    }
}
