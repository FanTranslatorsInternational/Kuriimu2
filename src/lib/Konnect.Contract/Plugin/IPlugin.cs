using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Management.Assembly;

namespace Konnect.Contract.Plugin;

/// <summary>
/// Base interface that each plugin has to derive from.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// A static and unique Id.
    /// </summary>
    /// <remarks>If more plugins have the same Id, the first plugin loaded by the CLR will be prioritized.</remarks>
    Guid PluginId { get; }

    /// <summary>
    /// The metadata for this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Override to register assemblies that are not loaded by the main executable.
    /// </summary>
    /// <param name="manager">The manager to register the assemblies in.</param>
    void RegisterAssemblies(IAssemblyManager manager) { }
}