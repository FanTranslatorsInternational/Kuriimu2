using System.Reflection;
using Konnect.Contract.Management.Assembly;

namespace Konnect.Management.Assemblies;

public class AssemblyManager : IAssemblyManager
{
    private readonly Assembly _pluginAssembly;

    public AssemblyManager(Assembly pluginAssembly)
    {
        _pluginAssembly = pluginAssembly;
    }

    /// <summary>
    /// Register an assembly from a physical path.
    /// </summary>
    /// <param name="path">The path of the assembly, relative to the plugin it was called from.</param>
    public void FromPath(string path)
    {
        var assemblyDirectory = Path.GetDirectoryName(_pluginAssembly.Location);
        if (string.IsNullOrEmpty(assemblyDirectory))
            throw new InvalidOperationException("No assembly directory given.");

        var assemblyLocation = Path.Combine(assemblyDirectory, path);
        if (File.Exists(assemblyLocation))
            throw new FileNotFoundException($"Could not find '{assemblyLocation}'.");

        var assemblyStream = File.OpenRead(assemblyLocation);
        FromStream(assemblyStream);
    }

    /// <summary>
    /// Register an assembly from an embedded resource.
    /// </summary>
    /// <param name="resource">The name of the embedded resource in the plugin assembly.</param>
    public void FromResource(string resource)
    {
        var resourceStream = _pluginAssembly.GetManifestResourceStream(resource);
        FromStream(resourceStream);
    }

    /// <summary>
    /// Register an assembly from an assembly stream.
    /// </summary>
    /// <param name="stream">The stream containing a valid assembly.</param>
    public void FromStream(Stream? stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        Assembly.Load(GetAssemblyBytes(stream));
    }

    private byte[] GetAssemblyBytes(Stream input)
    {
        var assemblyBytes = new byte[input.Length];
        input.Read(assemblyBytes, 0, assemblyBytes.Length);

        return assemblyBytes;
    }
}