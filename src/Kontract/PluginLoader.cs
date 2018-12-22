using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract
{
    public class PluginLoader
    {
        private static Lazy<PluginLoader> _lazy = new Lazy<PluginLoader>(() => new PluginLoader());
        public static PluginLoader Global => _lazy.Value;

        #region Imports
#pragma warning disable 0649, 0169

        [ImportMany(typeof(ICreateFiles))]
        private List<Lazy<ICreateFiles, PluginInfoAttribute>> _createAdapters;

        [ImportMany(typeof(ILoadFiles))]
        private List<Lazy<ILoadFiles, PluginInfoAttribute>> _loadAdapters;

        [ImportMany(typeof(ITextAdapter))]
        private List<Lazy<ITextAdapter, PluginInfoAttribute>> _textAdapters;

        [ImportMany(typeof(IImageAdapter))]
        private List<Lazy<IImageAdapter, PluginInfoAttribute>> _imageAdapters;

        [ImportMany(typeof(IArchiveAdapter))]
        private List<Lazy<IArchiveAdapter, PluginInfoAttribute>> _archiveAdapters;

        [ImportMany(typeof(IFontAdapter))]
        private List<Lazy<IFontAdapter, PluginInfoAttribute>> _fontAdapters;

        //[ImportMany(typeof(IAudioAdapter))]
        //private List<Lazy<IAudioAdapter, PluginInfoAttribute>> _audioAdapters;

        //[ImportMany(typeof(IModelAdapter))]
        //private List<Lazy<IModelAdapter, PluginInfoAttribute>> _modelAdapters;

        [ImportMany(typeof(IGameAdapter))]
        private List<Lazy<IGameAdapter, PluginInfoAttribute>> _gameAdapters;

#pragma warning restore 0649, 0169
        #endregion

        private string _pluginFolder;

        public PluginLoader(string pluginFolder = "plugins")
        {
            _pluginFolder = pluginFolder;

            Plugins.ComposePlugins(this, _pluginFolder);
        }

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

        private TOut CreateAdapter<T, TOut>(List<Lazy<T, PluginInfoAttribute>> adapters, string pluginId)
        {
            var adapter = adapters.FirstOrDefault(x => x.Metadata.ID == pluginId);
            if (adapter == null) return default(TOut);

            return (TOut)Activator.CreateInstance(adapter.Value.GetType());
        }

        public PluginInfoAttribute GetMetadata<T>(string pluginId)
        {
            switch (typeof(T).Name)
            {
                case nameof(ICreateFiles):
                    return _createAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;
                case nameof(ILoadFiles):
                    return _loadAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                case nameof(ITextAdapter):
                    return _textAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                case nameof(IImageAdapter):
                    return _imageAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                case nameof(IArchiveAdapter):
                    return _archiveAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                case nameof(IFontAdapter):
                    return _fontAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                case nameof(IGameAdapter):
                    return _gameAdapters.FirstOrDefault(x => x.Metadata.ID == pluginId)?.Metadata;

                default:
                    return null;
            }
        }

        public bool AdapterInheritsFrom<T>(object adapter)
            => adapter is T;
    }
}
