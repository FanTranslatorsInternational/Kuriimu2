using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Models;

namespace Kore.Managers.Plugins.PluginLoader
{
    public abstract class CsPluginLoader
    {
        protected bool TryLoadPlugins<TPlugin>(string[] pluginPaths, out IReadOnlyList<TPlugin> loadedPlugins, out IReadOnlyList<PluginLoadError> errors)
        {
            // 1. Get all assembly file paths from the designated plugin directories
            var assemblyFilePaths = pluginPaths.Where(Directory.Exists)
                .SelectMany(p => Directory.GetFiles(p, "*.dll"))
                .Select(Path.GetFullPath);

            // 2. Load the assemblies
            var assemblyFiles = assemblyFilePaths.Select(Assembly.LoadFile).ToArray();

            // 3. Process assemblies
            return TryLoadPlugins(assemblyFiles, out loadedPlugins, out errors);
        }

        protected bool TryLoadPlugins<TPlugin>(Assembly[] assemblyFiles, out IReadOnlyList<TPlugin> loadedPlugins, out IReadOnlyList<PluginLoadError> errors)
        {
            // 3. Get all public types assignable to IPlugin
            var pluginTypes = GetPublicTypes<TPlugin>(assemblyFiles, out var loadErrors);

            // 4. Create an instance of each IPlugin
            loadedPlugins = CreatePluginTypes<TPlugin>(pluginTypes, out var createErrors);

            errors = loadErrors.Concat(createErrors).ToArray();
            return !errors.Any();
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
                    errors.Add(new PluginLoadError(assembly.Location, e));
                }
            }

            return result;
        }

        private IReadOnlyList<TPlugin> CreatePluginTypes<TPlugin>(IEnumerable<Type> pluginTypes, out IList<PluginLoadError> errors)
        {
            var result = new List<TPlugin>();
            errors = new List<PluginLoadError>();

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var instance = (TPlugin)Activator.CreateInstance(pluginType);
                    result.Add(instance);
                }
                catch (Exception ex)
                {
                    errors.Add(new PluginLoadError(pluginType.Assembly.Location, ex));
                }
            }

            return result;
        }
    }
}
