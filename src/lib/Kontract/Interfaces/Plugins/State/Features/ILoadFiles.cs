using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace Kontract.Interfaces.Plugins.State.Features
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
        /// <param name="loadContext">The context for this load operation, containing environment instances.</param>
        /// <returns>If the load procedure was successful.</returns>
        Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext);
    }
}
