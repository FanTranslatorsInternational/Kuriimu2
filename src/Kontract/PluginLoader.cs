using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kontract
{
    public class PluginLoader
    {
        private static Lazy<PluginLoader> _lazy = new Lazy<PluginLoader>(() => new PluginLoader("plugins"));
        public static PluginLoader Global => _lazy.Value;

        #region Imports
#pragma warning disable 0649, 0169

        [ImportMany(typeof(ICreateFiles))]
        private List<ICreateFiles> _createAdapters;

        [ImportMany(typeof(ILoadFiles))]
        private List<ILoadFiles> _loadAdapters;

        [ImportMany(typeof(ITextAdapter))]
        private List<ITextAdapter> _textAdapters;

        [ImportMany(typeof(IImageAdapter))]
        private List<IImageAdapter> _imageAdapters;

        [ImportMany(typeof(IArchiveAdapter))]
        private List<IArchiveAdapter> _archiveAdapters;

        [ImportMany(typeof(IFontAdapter))]
        private List<IFontAdapter> _fontAdapters;

        //[ImportMany(typeof(IAudioAdapter))]
        //private List<Lazy<IAudioAdapter, IPluginMetadata>> _audioAdapters;

        //[ImportMany(typeof(IModelAdapter))]
        //private List<Lazy<IModelAdapter, IPluginMetadata>> _modelAdapters;

        [ImportMany(typeof(IGameAdapter))]
        private List<IGameAdapter> _gameAdapters;

#pragma warning restore 0649, 0169
        #endregion

        public string PluginFolder { get; private set; }

        public PluginLoader(string pluginFolder)
        {
            PluginFolder = Path.GetFullPath(pluginFolder);

            Plugins.ComposePlugins(this, PluginFolder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pluginId"></param>
        /// <returns></returns>
        public T CreateAdapter<T>(string pluginId)
        {
            switch (typeof(T).Name)
            {
                case nameof(ICreateFiles):
                    return CreateAdapter<ICreateFiles, T>(_createAdapters, pluginId);
                case nameof(ILoadFiles):
                    return CreateAdapter<ILoadFiles, T>(_loadAdapters, pluginId);

                case nameof(ITextAdapter):
                    return CreateAdapter<ITextAdapter, T>(_textAdapters, pluginId);

                case nameof(IImageAdapter):
                    return CreateAdapter<IImageAdapter, T>(_imageAdapters, pluginId);

                case nameof(IArchiveAdapter):
                    return CreateAdapter<IArchiveAdapter, T>(_archiveAdapters, pluginId);

                case nameof(IFontAdapter):
                    return CreateAdapter<IFontAdapter, T>(_fontAdapters, pluginId);

                case nameof(IGameAdapter):
                    return CreateAdapter<IGameAdapter, T>(_gameAdapters, pluginId);

                default:
                    return default(T);
            }
        }

        private TOut CreateAdapter<T, TOut>(List<T> adapters, string pluginId)
        {
            var adapter = adapters.FirstOrDefault(x => x.GetType().GetCustomAttribute<PluginInfoAttribute>().ID == pluginId);
            if (adapter == null) return default(TOut);

            return (TOut)Activator.CreateInstance(adapter.GetType());
        }

        /// <summary>
        /// Returns the currently loaded list of T type adapters.
        /// </summary>
        /// <typeparam name="T">Adapter type.</typeparam>
        /// <returns>List of adapters of type T.</returns>
        public List<T> GetAdapters<T>()
        {
            switch (typeof(T).Name)
            {
                case nameof(ICreateFiles):
                    return GetAdapters<ICreateFiles, T>(_createAdapters);
                case nameof(ILoadFiles):
                    return GetAdapters<ILoadFiles, T>(_loadAdapters);

                case nameof(ITextAdapter):
                    return GetAdapters<ITextAdapter, T>(_textAdapters);

                case nameof(IImageAdapter):
                    return GetAdapters<IImageAdapter, T>(_imageAdapters);

                case nameof(IArchiveAdapter):
                    return GetAdapters<IArchiveAdapter, T>(_archiveAdapters);

                case nameof(IFontAdapter):
                    return GetAdapters<IFontAdapter, T>(_fontAdapters);

                case nameof(IGameAdapter):
                    return GetAdapters<IGameAdapter, T>(_gameAdapters);

                default:
                    return null;
            }
        }

        private List<TOut> GetAdapters<T, TOut>(List<T> adapters)
        {
            return adapters.Cast<TOut>().ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMeta"></typeparam>
        /// <param name="pluginId"></param>
        /// <returns></returns>
        public TMeta GetMetadata<TMeta>(object adapter) where TMeta : Attribute, IPluginMetadata
        {
            return adapter.GetType().GetCustomAttribute<TMeta>();
        }

        //private TMeta GetMetadata<T, TMeta>(List<T> adapters, string pluginId) where TMeta : Attribute, IPluginMetadata
        //{
        //    return adapters.FirstOrDefault(x => x.GetType().GetCustomAttribute<PluginInfoAttribute>().ID == pluginId)?.GetType().GetCustomAttribute<TMeta>();
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adapter"></param>
        /// <returns></returns>
        //public bool AdapterInheritsFrom<T>(object adapter)
        //    => adapter is T;
    }
}
