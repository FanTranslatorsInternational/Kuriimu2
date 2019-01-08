using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;

namespace Kontract
{
    public class PluginLoader
    {
        /// <summary>
        /// 
        /// </summary>
        private static Lazy<PluginLoader> _pluginLoaderInitializer = new Lazy<PluginLoader>(() => new PluginLoader("plugins"));

        /// <summary>
        /// 
        /// </summary>
        public static PluginLoader Instance => _pluginLoaderInitializer.Value;

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

        /// <summary>
        /// 
        /// </summary>
        public string PluginFolder { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginFolder"></param>
        public PluginLoader(string pluginFolder)
        {
            PluginFolder = Path.GetFullPath(pluginFolder);

            Plugins.ComposePlugins(this, PluginFolder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pluginID"></param>
        /// <returns></returns>
        public T CreateAdapter<T>(string pluginID)
        {
            switch (typeof(T).Name)
            {
                case nameof(ICreateFiles):
                    return CreateAdapter<ICreateFiles, T>(_createAdapters, pluginID);

                case nameof(ILoadFiles):
                    return CreateAdapter<ILoadFiles, T>(_loadAdapters, pluginID);

                case nameof(ITextAdapter):
                    return CreateAdapter<ITextAdapter, T>(_textAdapters, pluginID);

                case nameof(IImageAdapter):
                    return CreateAdapter<IImageAdapter, T>(_imageAdapters, pluginID);

                case nameof(IArchiveAdapter):
                    return CreateAdapter<IArchiveAdapter, T>(_archiveAdapters, pluginID);

                case nameof(IFontAdapter):
                    return CreateAdapter<IFontAdapter, T>(_fontAdapters, pluginID);

                case nameof(IGameAdapter):
                    return CreateAdapter<IGameAdapter, T>(_gameAdapters, pluginID);

                default:
                    return default(T);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="adapters"></param>
        /// <param name="pluginID"></param>
        /// <returns></returns>
        private TResult CreateAdapter<T, TResult>(List<T> adapters, string pluginID)
        {
            var adapter = adapters.FirstOrDefault(x => x.GetType().GetCustomAttribute<PluginInfoAttribute>().ID == pluginID);
            if (adapter == null) return default(TResult);

            return (TResult)Activator.CreateInstance(adapter.GetType());
        }

        public List<TResult> GetAdapters<T, TResult>()
        {
            switch (typeof(T).Name)
            {
                case nameof(ICreateFiles):
                    return GetAdapters<ICreateFiles, TResult>(_createAdapters.Where(x => x is TResult).ToList());

                case nameof(ILoadFiles):
                    return GetAdapters<ILoadFiles, TResult>(_loadAdapters.Where(x => x is TResult).ToList());

                case nameof(ITextAdapter):
                    return GetAdapters<ITextAdapter, TResult>(_textAdapters.Where(x => x is TResult).ToList());

                case nameof(IImageAdapter):
                    return GetAdapters<IImageAdapter, TResult>(_imageAdapters.Where(x => x is TResult).ToList());

                case nameof(IArchiveAdapter):
                    return GetAdapters<IArchiveAdapter, TResult>(_archiveAdapters.Where(x => x is TResult).ToList());

                case nameof(IFontAdapter):
                    return GetAdapters<IFontAdapter, TResult>(_fontAdapters.Where(x => x is TResult).ToList());

                case nameof(IGameAdapter):
                    return GetAdapters<IGameAdapter, TResult>(_gameAdapters.Where(x => x is TResult).ToList());

                default:
                    return null;
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="adapters"></param>
        /// <returns></returns>
        private List<TResult> GetAdapters<T, TResult>(List<T> adapters)
        {
            return adapters.Cast<TResult>().ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pluginID"></param>
        /// <returns></returns>
        public T GetMetadata<T>(object adapter) where T : Attribute, IPluginMetadata
        {
            return adapter.GetType().GetCustomAttribute<T>();
        }
    }
}
