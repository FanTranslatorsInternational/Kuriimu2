using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Exposes methods to save files from a state.
    /// </summary>
    interface IFileSaver
    {
        /// <summary>
        /// Saves a state of a loaded file.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="savePath">The physical path to where the state should be saved to.</param>
        /// <remarks>If the given state is an archive child, it will not be saved in <paramref name="savePath"/> but in its parent only.</remarks>
        Task<SaveResult> SaveAsync(IStateInfo stateInfo, UPath savePath);

        /// <summary>
        /// Saves a state of a loaded file.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="fileSystem">The file system in which to save the file.</param>
        /// <param name="savePath">The virtual path to where the state should be saved t1o in the file system.</param>
        Task<SaveResult> SaveAsync(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath);
    }
}
