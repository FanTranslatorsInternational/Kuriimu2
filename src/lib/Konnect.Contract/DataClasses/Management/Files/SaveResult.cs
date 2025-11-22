using Konnect.Contract.Enums.Management.Files;

namespace Konnect.Contract.DataClasses.Management.Files;

public class SaveResult
{
    /// <summary>
    /// Declares if the save process was successful.
    /// </summary>
    public required bool IsSuccessful { get; init; }

    /// <summary>
    /// The reason for the error.
    /// </summary>
    public required SaveErrorReason Reason { get; init; }

    /// <summary>
    /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
    /// </summary>
    public Exception? Exception { get; init; }
}