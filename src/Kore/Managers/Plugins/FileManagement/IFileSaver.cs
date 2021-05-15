using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Models;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Exposes methods to save files from a state.
    /// </summary>
    interface IFileSaver
    {
        /// <summary>
        /// Saves a state of a loaded file to any relative file in a file system.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
        /// <param name="fileSystem">The file system in which to save the file.</param>
        /// <param name="savePath">The virtual path to where the state should be saved t1o in the file system.</param>
        /// <param name="saveInfo">The context for the save operation.</param>
        Task<SaveResult> SaveAsync(IFileState fileState, IFileSystem fileSystem, UPath savePath, SaveInfo saveInfo);
    }
}
