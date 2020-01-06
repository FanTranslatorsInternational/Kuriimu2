using Kontract.Interfaces.FileSystem;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the plugin state as saveable and exposes methods to save the current state.
    /// </summary>
    public interface ISaveFiles
    {
        /// <summary>
        /// Determine if the state got modified.
        /// </summary>
        bool ContentChanged { get; }

        /// <summary>
        /// Try to save the current state to a file.
        /// </summary>
        /// <param name="fileSystem">The file system to save the state into.</param>
        /// <param name="savePath">The new path to the initial file.</param>
        /// <returns>If the save procedure was successful.</returns>
        void Save(IFileSystem fileSystem, UPath savePath);
    }
}
