using System;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Exposes methods to rename files in an archive.
    /// </summary>
    [Obsolete("Override IArchiveState.Rename instead")]
    public interface IRenameFiles
    {
        /// <summary>
        /// Rename a given file.
        /// </summary>
        /// <param name="afi">The file to rename.</param>
        /// <param name="path">The new path of the file.</param>
        void Rename(IArchiveFileInfo afi, UPath path);
    }
}
