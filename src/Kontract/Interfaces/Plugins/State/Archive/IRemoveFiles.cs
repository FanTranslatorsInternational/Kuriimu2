using Kontract.Models.Archive;

namespace Kontract.Interfaces.Plugins.State.Archive
{
    /// <summary>
    /// Marks the archive state able to remove files.
    /// </summary>
    public interface IRemoveFiles : IArchiveState
    {
        /// <summary>
        /// Removes a single file from the archive state.
        /// </summary>
        /// <param name="afi">The file to remove.</param>
        void RemoveFile(ArchiveFileInfo afi);

        /// <summary>
        /// Removes all files from the archive state.
        /// </summary>
        void RemoveAll();
    }
}
