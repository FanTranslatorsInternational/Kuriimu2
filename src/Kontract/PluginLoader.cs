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
using Kontract.MEF;
using Kontract.MEF.Catalogs;
using Kontract.MEF.ErrorReports;
using Kontract.MEF.Interfaces;
using Kontract.MEF.Providers;
using Kontract.Providers;
using Kontract.Providers.Models;

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
        /// <param name="pluginDirectory">The directory to load plugins from.</param>
        /// <param name="errors">List of occured composition errors.</param>
        /// <exception cref="PluginInconsistencyException">If plugins demand types that can't be satisfied on composition.</exception>
        /// <returns>Was composition successful.</returns>
        public static bool TryComposePlugins(object parent, string pluginDirectory, out IList<IErrorReport> errors)
            => TryComposePlugins(parent, pluginDirectory, null, out errors);

        /// <summary>
        /// Re/Loads a plugin container for a given parent object.
        /// </summary>
        /// <param name="parent">The parent object to load plugins for.</param>
        /// <param name="pluginDirectory">The directory to load plugins from.</param>
        /// <param name="types">Extra types to load plugins from.</param>
        /// <param name="errors">List of occured composition errors.</param>
        /// <exception cref="PluginInconsistencyException">If plugins demand types that can't be satisfied on load.</exception>
        /// <returns>Was composition successful.</returns>
        public static bool TryComposePlugins(object parent, string pluginDirectory, Type[] types, out IList<IErrorReport> errors)
        {
            errors = new List<IErrorReport>();

            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            // Loads plugins from the Assembly of the given types.
            if (types != null)
                foreach (var type in types)
                    catalog.Catalogs.Add(new AssemblyCatalog(type.Assembly));

            // Loads plugins from all DLLs found in the plugin directory.
            KuriimuDirectoryCatalog dirCatalog = null;
            if (Directory.Exists(pluginDirectory) && Directory.EnumerateFiles(pluginDirectory, "*.dll").Any())
            {
                dirCatalog = new KuriimuDirectoryCatalog(pluginDirectory, "*.dll");
                catalog.Catalogs.Add(dirCatalog);
            }

            // Create the CompositionContainer with the parts in the catalog.
            var exportProvider = new KuriimuExportProvider(catalog);
            var container = new CompositionContainer(exportProvider);

            try
            {
                // Fill the imports of the parent object.
                container.ComposeParts(parent);
            }
            catch (Exception e)
            {
                errors.Add(new DefaultErrorReport(e));
                return false;
            }

            if (dirCatalog != null && dirCatalog.ErrorReports.Any())
            {
                foreach (var e in dirCatalog.ErrorReports)
                    errors.Add(e);
            }
            if (exportProvider.HasErrorReports)
            {
                foreach (var e in exportProvider.ErrorReports)
                    errors.Add(e);
            }

            return !errors.Any();
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
        /// Provides a list of possible composition errors.
        /// </summary>
        public IList<IErrorReport> CompositionErrors { get; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="PluginLoader"/> and composes all of the plugins found in the <see cref="PluginFolder"/> sub directory.
        /// </summary>
        /// <param name="pluginFolder"></param>
        /// <exception cref="PluginInconsistencyException">If plugins demand types that can't be satisfied on load.</exception>
        public PluginLoader(string pluginFolder)
        {
            PluginFolder = Path.GetFullPath(pluginFolder);

            if (!TryComposePlugins(this, PluginFolder, out var errors))
                CompositionErrors = errors;
            if (_plugins == null)
                _plugins = new List<IPlugin>();
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
