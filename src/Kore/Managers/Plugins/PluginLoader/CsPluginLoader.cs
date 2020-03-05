using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kore.Models;

namespace Kore.Managers.Plugins.PluginLoader
{
    abstract class CsPluginLoader
    {
        // TODO: Make more separate methods for better error handling
        protected bool TryLoadPlugins<TPlugin>(string[] pluginPaths, out IReadOnlyList<TPlugin> loadedPlugins, out PluginLoadError[] errors)
        {
            var pluginType = typeof(TPlugin);

            // 1. Get all assembly file paths from the designated plugin directories
            var assemblyFilePaths = pluginPaths.Where(Directory.Exists)
                .SelectMany(p => Directory.GetFiles(p, "*.dll"))
                .Select(Path.GetFullPath);

            // 2. Load the assemblies
            var assemblyFiles = assemblyFilePaths.Select(Assembly.LoadFile);

            // 3. Get all public types assignable to IPlugin
            var pluginTypes = assemblyFiles.SelectMany(a =>
                a.GetExportedTypes().Where(t => pluginType.IsAssignableFrom(t)));

            // 4. Create an instance of each IPlugin
            loadedPlugins = pluginTypes.Select(pt => (TPlugin)Activator.CreateInstance(pt)).ToList();

            errors = null;
            return true;
        }
    }
}
