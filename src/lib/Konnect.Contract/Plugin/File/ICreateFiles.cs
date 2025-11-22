using Konnect.Contract.DataClasses.Plugin.File;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// This interface allows a plugin to create files.
/// </summary>
public interface ICreateFiles : IFilePluginState
{
    /// <summary>
    /// Creates a new instance of the underlying format.
    /// </summary>
    /// <param name="createContext">The context for this create operation, containing environment instances.</param>
    Task Create(CreateContext createContext);
}