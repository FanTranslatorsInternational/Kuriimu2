using Kontract.Models.Archive;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Marks the archive state able to remove files.
    /// </summary>
    public interface IRemoveFiles
    {
        /// <summary>
        /// Removes a single file from the archive state.
        /// </summary>
        /// <param name="afi">The file to remove.</param>
        void RemoveFile(IArchiveFileInfo afi);

        /// <summary>
        /// Removes all files from the archive state.
        /// </summary>
        void RemoveAll();
    }
}
