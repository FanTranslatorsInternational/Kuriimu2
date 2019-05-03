using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Exceptions;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Models;

namespace Kontract
{
    /// <summary>
    /// <see cref="PluginLoader"/> is used to load in all available plugins.
    /// </summary>
    public class PluginLoader
    {
        /// <summary>
        /// Lazy loads the <see cref="PluginLoader"/> singleton instance.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<PluginLoader> _pluginLoaderInitializer = new Lazy<PluginLoader>(() => new PluginLoader("plugins"));

        /// <summary>
        /// Provides access to the <see cref="PluginLoader"/> singleton instance.
        /// </summary>
        public static PluginLoader Instance => _pluginLoaderInitializer.Value;

        /// <summary>
        /// Re/Loads a plugin container for a given parent object.
        /// </summary>
        /// <param name="parent">The parent object to load plugins for.</param>
        /// <param name="pluginDirectory">The directory to load plugins from</param>
        /// <param name="types">Extra types to load plugins from.</param>
        /// <exception cref="PluginInconsistencyException">If plugins demand types that can't be satisfied on load.</exception>
        public static void ComposePlugins(object parent, string pluginDirectory = "plugins", params Type[] types)
        {
            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            // Loads plugins from the Assembly of the given types.
            foreach (var type in types)
                catalog.Catalogs.Add(new AssemblyCatalog(type.Assembly));

            // Loads plugins from all DLLs found in the plugin directory.
            if (Directory.Exists(pluginDirectory) && Directory.GetFiles(pluginDirectory, "*.dll").Length > 0)
                catalog.Catalogs.Add(new DirectoryCatalog(pluginDirectory));

            // Create the CompositionContainer with the parts in the catalog.
            var container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            try
            {
                container.ComposeParts(parent);
            }
            catch (TypeLoadException e)
            {
                throw new PluginInconsistencyException();
            }
            catch (Exception e)
            {
                throw new PluginInconsistencyException();
            }
        }

        #region Imports
#pragma warning disable 0649, 0169

        [ImportMany(typeof(IPlugin))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private List<IPlugin> _plugins;

#pragma warning restore 0649, 0169
        #endregion

        /// <summary>
        /// Defines the sub directory used to scan for plugin DLLs.
        /// </summary>
        public string PluginFolder { get; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="PluginLoader"/> and composes all of the plugins found in the <see cref="PluginFolder"/> sub directory.
        /// </summary>
        /// <param name="pluginFolder"></param>
        /// <exception cref="PluginInconsistencyException">If plugins demand types that can't be satisfied on load.</exception>
        public PluginLoader(string pluginFolder)
        {
            PluginFolder = Path.GetFullPath(pluginFolder);

            ComposePlugins(this, PluginFolder);
        }

        /// <summary>
        /// Creates a new instance of the given plugin type <typeparamref name="T"/> using its fully qualified name.
        /// </summary>
        /// <typeparam name="T">Type to return the adapter as.</typeparam>
        /// <param name="fqn">The fully qualified name of the adapter type.</param>
        /// <returns>The new instance of the adapter.</returns>
        public T CreateNewAdapter<T>(string fqn)
        {
            var adapter = _plugins.FirstOrDefault(x => x.GetType().FullName == fqn);

            if (adapter == null)
                return default;

            return (T)Activator.CreateInstance(adapter.GetType());
        }

        /// <summary>
        /// Creates a new instance of the given plugin type <typeparamref name="T"/> using an adapter.
        /// </summary>
        /// <typeparam name="T">Type to return the adapter as.</typeparam>
        /// <param name="adapter">The adapter to create a new instance of.</param>
        /// <returns>The new instance of the adapter.</returns>
        public T CreateNewAdapter<T>(IPlugin adapter)
        {
            var adapterType = adapter.GetType();
            var chosenAdapter = _plugins.FirstOrDefault(x => x.GetType() == adapterType && x is T);

            if (chosenAdapter == null)
                return default;

            return (T)Activator.CreateInstance(adapterType);
        }

        /// <summary>
        /// Returns the currently loaded list of <see cref="T"/> type adapters.
        /// </summary>
        /// <typeparam name="T">Adapter type.</typeparam>
        /// <returns>List of adapters of type T.</returns>
        public List<T> GetAdapters<T>() => _plugins.Where(p => p is T).Cast<T>().ToList();

        /// <summary>
        /// Returns the given <see cref="T"/> metadata attribute that is attached to the adapter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public T GetMetadata<T>(object adapter) where T : Attribute, IPluginMetadata
        {
            return adapter.GetType().GetCustomAttribute<T>();
        }
    }
}
