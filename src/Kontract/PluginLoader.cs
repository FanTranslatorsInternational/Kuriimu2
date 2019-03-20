using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces.Common;

namespace Kontract
{
    public class PluginLoader
    {
        /// <summary>
        /// Lazy loads the <see cref="PluginLoader"/> singleton instance.
        /// </summary>
        private static readonly Lazy<PluginLoader> _pluginLoaderInitializer = new Lazy<PluginLoader>(() => new PluginLoader("plugins"));

        /// <summary>
        /// Provides access to the <see cref="PluginLoader"/> singleton instance.
        /// </summary>
        public static PluginLoader Instance => _pluginLoaderInitializer.Value;

        #region Imports
#pragma warning disable 0649, 0169

        [ImportMany(typeof(IPlugin))]
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
        public PluginLoader(string pluginFolder)
        {
            PluginFolder = Path.GetFullPath(pluginFolder);

            Plugins.ComposePlugins(this, PluginFolder);
        }

        /// <summary>
        /// Instantiates a new instance of the given plugin type <see cref="T"/> using its pluginID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pluginID"></param>
        /// <returns></returns>
        public T CreateAdapter<T>(string pluginID)
        {
            var adapter = _plugins.FirstOrDefault(x => x.GetType().GetCustomAttribute<PluginInfoAttribute>()?.ID == pluginID);
            if (adapter == null) return default(T);

            return (T)Activator.CreateInstance(adapter.GetType());
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
