using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the plugin as loadable and exposes methods to load a file into the state.
    /// </summary>
    public interface ILoadFiles
    {
        /// <summary>
        /// Load the file into the state.
        /// </summary>
        /// <param name="fileSystem">The file system from which the file is requested.</param>
        /// <param name="filePath">The path to the file requested by the user.</param>
        /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
        /// <param name="progress">The progress for this action.</param>
        /// <returns>If the load procedure was successful.</returns>
        void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider, IProgressContext progress);
    }
}
