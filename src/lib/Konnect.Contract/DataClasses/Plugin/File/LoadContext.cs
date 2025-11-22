using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Progress;

namespace Konnect.Contract.DataClasses.Plugin.File;

/// <summary>
/// The class containing all environment instances for a <see cref="ILoadFiles.Load"/> action.
/// </summary>
public class LoadContext
{
    /// <summary>
    /// The provider for temporary streams.
    /// </summary>
    public required ITemporaryStreamManager TemporaryStreamManager { get; init; }

    /// <summary>
    /// The progress context.
    /// </summary>
    public required IProgressContext ProgressContext { get; init; }

    /// <summary>
    /// The dialog manager.
    /// </summary>
    public IDialogManager? DialogManager { get; init; }
}