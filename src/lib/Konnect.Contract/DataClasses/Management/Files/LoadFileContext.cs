using Konnect.Contract.Management.Files;
using Serilog;

namespace Konnect.Contract.DataClasses.Management.Files;

/// <summary>
/// The class containing all environment instances for a load process in <see cref="IPluginFileManager"/>.
/// </summary>
public class LoadFileContext
{
    /// <summary>
    /// The options for this load process.
    /// </summary>
    public IList<string> Options { get; } = new List<string>();

    /// <summary>
    /// The preset id of the plugin to use to load the file.
    /// </summary>
    public Guid PluginId { get; init; }

    /// <summary>
    /// The logger to use for the load file operation.
    /// </summary>
    public required ILogger Logger { get; init; }
}