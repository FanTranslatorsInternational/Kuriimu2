namespace Konnect.Contract.DataClasses.Management.Plugin.Loaders;

public class PluginLoadError
{
    public required string AssemblyPath { get; init; }

    public required Exception Exception { get; init; }
}