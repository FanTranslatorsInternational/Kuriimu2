using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Progress;

namespace Konnect.Contract.DataClasses.Plugin.File;

/// <summary>
/// The class containing all environment instances for a <see cref="ICreateFiles.Create"/> action.
/// </summary>
public class CreateContext
{
    /// <summary>
    /// The progress context.
    /// </summary>
    public required IProgressContext ProgressContext { get; init; }

    /// <summary>
    /// The dialog manager.
    /// </summary>
    public IDialogManager? DialogManager { get; init; }
}