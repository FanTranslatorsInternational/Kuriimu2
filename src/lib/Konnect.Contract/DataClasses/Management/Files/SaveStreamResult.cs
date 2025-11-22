using Konnect.Contract.DataClasses.FileSystem;

namespace Konnect.Contract.DataClasses.Management.Files;

public class SaveStreamResult : SaveResult
{
    /// <summary>
    /// The list of in-memory files, that were saved by the operation.
    /// </summary>
    public IList<StreamFile> SavedStreams { get; init; }
}