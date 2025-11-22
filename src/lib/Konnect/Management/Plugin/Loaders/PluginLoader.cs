using System.Reflection;
using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Management.Plugin.Loaders;
using Konnect.Contract.Plugin;
using Konnect.Management.Assemblies;

namespace Konnect.Management.Plugin.Loaders;

public abstract class PluginLoader : IPluginLoader
{
    public abstract IReadOnlyList<PluginLoadError> LoadErrors { get; }
    public abstract bool Exists(Guid pluginId);

    protected void LoadPlugins<TPlugin>(string[] pluginPaths, out IReadOnlyList<TPlugin> loadedPlugins, out IReadOnlyList<PluginLoadError> errors) where TPlugin : IPlugin
    {
        // 1. Get all assembly file paths from the designated plugin directories
        var assemblyFilePaths = pluginPaths.Select(p => p)
            .Where(Directory.Exists)
            .SelectMany(p => Directory.GetFiles(p, "*.dll"))
            .Select(Path.GetFullPath);

        // 2. Load the assemblies
        var assemblyFiles = assemblyFilePaths.Select(Assembly.LoadFile).ToArray();

        // 3. Process assemblies
        LoadPlugins(assemblyFiles, out loadedPlugins, out errors);
    }

    protected void LoadPlugins<TPlugin>(Assembly[] assemblyFiles, out IReadOnlyList<TPlugin> loadedPlugins, out IReadOnlyList<PluginLoadError> errors) where TPlugin : IPlugin
    {
        // 3. Get all public types assignable to IPlugin
        var pluginTypes = GetPublicTypes<TPlugin>(assemblyFiles, out var loadErrors);

        // 4. Create an instance of each IPlugin
        loadedPlugins = CreatePluginTypes<TPlugin>(pluginTypes, out var createErrors);

        // 5. Register referenced assemblies of the plugin
        RegisterReferencedAssemblies(loadedPlugins);

        errors = loadErrors.Concat(createErrors).ToArray();
    }

    private IList<Type> GetPublicTypes<TPlugin>(IEnumerable<Assembly> assemblies, out IList<PluginLoadError> errors)
    {
        var result = new List<Type>();
        errors = new List<PluginLoadError>();

        var pluginType = typeof(TPlugin);

        foreach (var assembly in assemblies)
        {
            try
            {
                var exportedTypes = assembly.GetExportedTypes();
                result.AddRange(exportedTypes.Where(t => pluginType.IsAssignableFrom(t)));
            }
            catch (Exception e)
            {
                errors.Add(new PluginLoadError
                {
                    AssemblyPath = assembly.Location,
                    Exception = e
                });
            }
        }

        return result;
    }

    private IReadOnlyList<TPlugin> CreatePluginTypes<TPlugin>(IEnumerable<Type> pluginTypes, out IList<PluginLoadError> errors)
    {
        var result = new List<TPlugin>();
        errors = new List<PluginLoadError>();

        foreach (Type pluginType in pluginTypes)
        {
            try
            {
                var instance = (TPlugin?)Activator.CreateInstance(pluginType);
                if(instance is null)
                    continue;

                result.Add(instance);
            }
            catch (Exception ex)
            {
                errors.Add(new PluginLoadError
                {
                    AssemblyPath = pluginType.Assembly.Location,
                    Exception = ex
                });
            }
        }

        return result;
    }

    private void RegisterReferencedAssemblies<TPlugin>(IReadOnlyList<TPlugin> loadedPlugins) where TPlugin : IPlugin
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        foreach (var loadedPlugin in loadedPlugins)
        {
            var assembly = loadedPlugin.GetType().Assembly;
            var domainContext = new AssemblyManager(assembly);

            loadedPlugin.RegisterAssemblies(domainContext);
        }
    }

    private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        var appDomain = (AppDomain)sender;
        return appDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
    }
}