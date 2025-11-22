using Konnect.Contract.Plugin.File;
using Konnect.Contract.Progress;

namespace Konnect.Contract.DataClasses.Plugin.File;

/// <summary>
/// The class containing all environment instances for a <see cref="ISaveFiles.Save"/> action.
/// </summary>
public class SaveContext
{
    /// <summary>
    /// The progress context.
    /// </summary>
    public required IProgressContext ProgressContext { get; init; }
}