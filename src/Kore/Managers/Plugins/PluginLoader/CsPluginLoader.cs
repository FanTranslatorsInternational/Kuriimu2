using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;
using Kore.Models;

namespace Kore.Managers.Plugins.PluginLoader
{
    class CsPluginLoader : IPluginLoader<IFilePlugin>
    {
        /// <inheritdoc />
        public IReadOnlyList<IFilePlugin> Plugins { get; private set; }

        public CsPluginLoader(params string[] pluginPaths)
        {
            if (!TryLoadPlugins(pluginPaths, out var errors))
            {
                throw new AggregateException(errors.Select(e => new InvalidOperationException(e.ToString())));
            }
        }

        /// <inheritdoc />
        public bool Exists(Guid pluginId)
        {
            return Plugins.Any(p => p.PluginId == pluginId);
        }

        /// <inheritdoc />
        public IFilePlugin GetPlugin(Guid pluginId)
        {
            return Plugins.FirstOrDefault(ep => ep.PluginId == pluginId);
        }

        // TODO: Make more separate methods for better error handling
        private bool TryLoadPlugins(string[] pluginPaths, out PluginLoadError[] errors)
        {
            var pluginType = typeof(IFilePlugin);

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
            Plugins = pluginTypes.Select(pt => (IFilePlugin)Activator.CreateInstance(pt)).ToList();

            errors = null;
            return true;
        }
    }
}
