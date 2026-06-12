using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Files;

namespace Konnect.Contract.DataClasses.Management.Files;

public class LoadResult
{
    /// <summary>
    /// The status of the load operation
    /// </summary>
    public required LoadStatus Status { get; init; }

    /// <summary>
    /// The reason for the error.
    /// </summary>
    public required LoadErrorReason Reason { get; init; }

    /// <summary>
    /// The fields requested by the plugin in the load process. Is only provided when <see cref="Reason"/> is <see cref="LoadErrorReason.NoOptions"/>.
    /// </summary>
    public IList<DialogField>? DialogFields { get; init; }

    /// <summary>
    /// Contains the result if the load process was successful. Is only provided when <see cref="Status"/> is <see cref="LoadStatus.Successful"/>.
    /// </summary>
    public IFileState? LoadedFileState { get; init; }

    /// <summary>
    /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
    /// </summary>
    public Exception? Exception { get; init; }
}