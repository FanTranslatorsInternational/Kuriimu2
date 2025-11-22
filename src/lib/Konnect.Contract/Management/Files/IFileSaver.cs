using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.FileSystem;

namespace Konnect.Contract.Management.Files;

/// <summary>
/// Exposes methods to save files from a state.
/// </summary>
public interface IFileSaver
{
    /// <summary>
    /// Saves a state of a loaded file to any relative file in a file system.
    /// </summary>
    /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
    /// <param name="fileSystem">The file system in which to save the file.</param>
    /// <param name="savePath">The virtual path to where the state should be saved t1o in the file system.</param>
    /// <param name="saveInfo">The context for the save operation.</param>
    Task<SaveResult> SaveAsync(IFileState fileState, IFileSystem fileSystem, UPath savePath, SaveFileOptions saveInfo);
}