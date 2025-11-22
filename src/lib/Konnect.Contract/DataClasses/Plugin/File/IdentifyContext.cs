using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;

namespace Konnect.Contract.DataClasses.Plugin.File;

/// <summary>
/// The class containing all environment instances for a <see cref="IIdentifyFiles.IdentifyAsync"/> action.
/// </summary>
public class IdentifyContext
{
    /// <summary>
    /// The provider for temporary streams.
    /// </summary>
    public required ITemporaryStreamManager TemporaryStreamManager { get; init; }
}