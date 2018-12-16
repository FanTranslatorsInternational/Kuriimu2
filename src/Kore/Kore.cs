using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;

namespace Kore
{
    /// <summary>
    /// Kore is the main brain library of Kuriimu. It performs all of the important and UI agnostic functions of Kuriimu.
    /// </summary>
    public sealed class Kore : IDisposable
    {
        /// <summary>
        /// Stores the currently loaded MEF plugins.
        /// </summary>
        private CompositionContainer _container;

        /// <summary>
        /// Stores the plugin directory that was set at construction time.
        /// </summary>
        private readonly string _pluginDirectory = "plugins";

        #region Plugins
#pragma warning disable 0649, 0169

        [ImportMany(typeof(ICreateFiles))]
        private List<ICreateFiles> _createAdapters;

        [ImportMany(typeof(ILoadFiles))]
        private List<ILoadFiles> _fileAdapters;

        [ImportMany(typeof(ITextAdapter))]
        private List<ITextAdapter> _textAdapters;

        [ImportMany(typeof(IImageAdapter))]
        private List<IImageAdapter> _imageAdapters;

        //[ImportMany(typeof(IArchiveAdapter))]
        //private List<IArchiveAdapter> _archiveAdapters;

        [ImportMany(typeof(IFontAdapter))]
        private List<IFontAdapter> _fontAdapters;

        //[ImportMany(typeof(IAudioAdapter))]
        //private List<IAudioAdapter> _audioAdapters;

        //[ImportMany(typeof(IModelAdapter))]
        //private List<IModelAdapter> _modelAdapters;

        [ImportMany(typeof(IGameAdapter))]
        private List<IGameAdapter> _gameAdapters;

#pragma warning restore 0649, 0169
        #endregion

        /// <summary>
        /// The list of currently open files being tracked by Kore.
        /// </summary>
        public List<KoreFileInfo> OpenFiles { get; } = new List<KoreFileInfo>();

        /// <summary>
        /// Provides an event that the UI can handle to present a plugin list to the user.
        /// </summary>
        public event EventHandler<IdentificationFailedEventArgs> IdentificationFailed;

        /// <inheritdoc />
        /// <summary>
        /// Allows the UI to display a list of blind plugins and to return one selected by the user.
        /// </summary>
        public class IdentificationFailedEventArgs : EventArgs
        {
            public List<ILoadFiles> BlindAdapters;
            public ILoadFiles SelectedAdapter = null;
        }

        /// <summary>
        /// Initializes a new Kore instance.
        /// </summary>
        public Kore()
        {
            Plugins.ComposePlugins(this, _pluginDirectory);
        }

        /// <summary>
        /// Initializes a new Kore instance with the given plugin directory.
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public Kore(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
            Plugins.ComposePlugins(this, _pluginDirectory);
        }

        /// <summary>
        /// Re/Loads the plugin container.
        /// </summary>
        //private void ComposePlugins()
        //{
        //    // An aggregate catalog that combines multiple catalogs.
        //    var catalog = new AggregateCatalog();

        //    // Adds all the parts found in the same assembly as the Kore class.
        //    catalog.Catalogs.Add(new AssemblyCatalog(typeof(Kore).Assembly));

        //    if (Directory.Exists(_pluginDirectory) && Directory.GetFiles(_pluginDirectory, "*.dll").Length > 0)
        //        catalog.Catalogs.Add(new DirectoryCatalog(_pluginDirectory));

        //    // Create the CompositionContainer with the parts in the catalog.
        //    _container?.Dispose();
        //    _container = new CompositionContainer(catalog);

        //    // Fill the imports of this object.
        //    _container.ComposeParts(this);
        //}

        // TEMPORARY
        public static void ComposeSamplePlugins(object parent, CompositionContainer container)
        {
            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            // Adds all the parts found in the same assembly as the Kore class.
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Kore).Assembly));

            // Create the CompositionContainer with the parts in the catalog if it doesn't exist.
            if (container == null)
                container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            container.ComposeParts(parent);
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
                    return _createAdapters.Cast<T>().ToList();
                case nameof(ILoadFiles):
                    return _fileAdapters.Cast<T>().ToList();
                case nameof(ITextAdapter):
                    return _textAdapters.Cast<T>().ToList();
                case nameof(IImageAdapter):
                    return _imageAdapters.Cast<T>().ToList();
                case nameof(IFontAdapter):
                    return _fontAdapters.Cast<T>().ToList();
                case nameof(IGameAdapter):
                    return _gameAdapters.Cast<T>().ToList();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a list of the plugin interface type names that load files.
        /// </summary>
        /// <returns></returns>
        public List<string> GetFileLoadingAdapterNames() => new List<string>
        {
            nameof(ITextAdapter),
            nameof(IImageAdapter),
            nameof(IFontAdapter)
        };

        /// <summary>
        /// Loads a file into the tracking list.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        /// <param name="trackFile">Id the file should be tracked by Kore</param>
        /// <returns>Returns a KoreFileInfo for the opened file.</returns>
        public KoreFileInfo LoadFile(string filename, bool trackFile = true)
        {
            var adapter = SelectAdapter(filename);

            // Ask the user to select a plugin directly.
            if (adapter == null)
            {
                var blindAdapters = _fileAdapters.Where(a => !(a is IIdentifyFiles)).ToList();

                var args = new IdentificationFailedEventArgs { BlindAdapters = blindAdapters };
                IdentificationFailed?.Invoke(this, args);

                //TODO: Handle this case better?
                //if (args.SelectedAdapter == null)
                //{
                //    return null;
                //}

                adapter = args.SelectedAdapter;
            }

            if (adapter == null)
                return null; //throw new LoadFileException("No plugins were able to open the file.");

            // Instantiate a new instance of the adapter.
            adapter = (ILoadFiles)Activator.CreateInstance(adapter.GetType());

            // Load the file(s).
            try
            {
                adapter.Load(filename);
            }
            catch (Exception ex)
            {
                var pi = (PluginInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute));
                throw new LoadFileException($"The {pi?.Name} plugin failed to load \"{Path.GetFileName(filename)}\".{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.StackTrace}");
            }

            // Create a KoreFileInfo to keep track of the now open file.
            var kfi = new KoreFileInfo
            {
                FileInfo = new FileInfo(filename),
                HasChanges = false,
                Adapter = adapter
            };

            if (trackFile)
                OpenFiles.Add(kfi);

            return kfi;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public KoreFileInfo LoadFile(string filename, ILoadFiles adapter)
        {
            if (adapter == null)
                throw new ArgumentException("The adapter is null.");

            // Instantiate a new instance of the adapter.
            adapter = (ILoadFiles)Activator.CreateInstance(adapter.GetType());

            // Load the file(s).
            try
            {
                adapter.Load(filename);
            }
            catch (Exception ex)
            {
                var pi = (PluginInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute));
                throw new LoadFileException($"The {pi?.Name} plugin failed to load \"{Path.GetFileName(filename)}\".\r\n\r\n{ex.Message}\r\n\r\n{ex.StackTrace}");
            }

            // Create a KoreFileInfo to keep track of the now open file.
            var kfi = new KoreFileInfo
            {
                FileInfo = new FileInfo(filename),
                HasChanges = false,
                Adapter = adapter
            };

            OpenFiles.Add(kfi);

            return kfi;
        }

        /// <summary>
        /// Saves an open file. Optionally to a new name.
        /// </summary>
        /// <param name="kfi">The KoreFileInfo to be saved.</param>
        /// <param name="filename">The optional new name of the file to be saved.</param>
        public void SaveFile(KoreFileInfo kfi, string filename = "")
        {
            //TODO: throw exception instead of just return?
            if (!OpenFiles.Contains(kfi) || !(kfi.Adapter is ISaveFiles)) return;

            var adapter = (ISaveFiles)kfi.Adapter;

            if (string.IsNullOrEmpty(filename))
                adapter.Save(kfi.FileInfo.FullName);
            else
            {
                adapter.Save(filename);
                kfi.FileInfo = new FileInfo(filename);
            }
        }

        /// <summary>
        /// Closes an open file.
        /// </summary>
        /// <param name="kfi">The file to be closed.</param>
        /// <returns>True if file was closed, False otherwise.</returns>
        public bool CloseFile(KoreFileInfo kfi)
        {
            if (!OpenFiles.Contains(kfi)) return false;
            kfi.Adapter.Dispose();
            OpenFiles.Remove(kfi);
            return true;
        }

        /// <summary>
        /// Attempts to select a compatible adapter that is capable of identifying files.
        /// </summary>
        /// <param name="filename">The file to be selected against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(string filename)
        {
            // Return an adapter that can Identify, whose extension matches that of our filename and successfully identifies the file.
            return _fileAdapters.Where(adapter =>
                adapter is IIdentifyFiles && ((PluginExtensionInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute))).Extension.ToLower().TrimEnd(';').Split(';').Any(s => filename.ToLower().EndsWith(s.TrimStart('*')))).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(filename));
        }

        /// <summary>
        /// Provides a complete set of file format names and extensions for open file dialogs.
        /// </summary>
        public string FileFilters
        {
            get
            {
                // Add all of the adapter filters
                var allTypes = _fileAdapters.Select(x => new { x.GetType().GetCustomAttribute<PluginInfoAttribute>().Name, Extension = x.GetType().GetCustomAttribute<PluginExtensionInfoAttribute>().Extension.ToLower() }).OrderBy(o => o.Name).ToList();

                // Add the special all supported files filter
                if (allTypes.Count > 0)
                    allTypes.Insert(0, new { Name = "All Supported Files", Extension = string.Join(";", allTypes.Select(x => x.Extension).Distinct()) });

                // Add the special all files filter
                allTypes.Add(new { Name = "All Files", Extension = "*.*" });

                return string.Join("|", allTypes.Select(x => $"{x.Name} ({x.Extension})|{x.Extension}"));
            }
        }

        /// <summary>
        /// Provides a limited set of file format names and extensions for open file dialogs.
        /// </summary>
        /// <typeparam name="T">The plugin interface to load extensions for.</typeparam>
        /// <param name="allSupportedFiles">Sets the string shown for the combined format filter.</param>
        /// <param name="includeAllFiles">Determines whether or not to include the "All Files" filter.</param>
        /// <returns></returns>
        public string FileFiltersByType<T>(string allSupportedFiles = "", bool includeAllFiles = false)
        {
            // Add all of the adapter filters
            var allTypes = _fileAdapters.OfType<T>().Select(x => new { x.GetType().GetCustomAttribute<PluginInfoAttribute>().Name, Extension = x.GetType().GetCustomAttribute<PluginExtensionInfoAttribute>().Extension.ToLower() }).OrderBy(o => o.Name).ToList();

            // Add the special all supported files filter
            if (allTypes.Count > 0 && !string.IsNullOrEmpty(allSupportedFiles))
                allTypes.Insert(0, new { Name = allSupportedFiles, Extension = string.Join(";", allTypes.Select(x => x.Extension).Distinct()) });

            // Add the special all files filter
            if (includeAllFiles)
                allTypes.Add(new { Name = "All Files", Extension = "*.*" });

            return string.Join("|", allTypes.Select(x => $"{x.Name} ({x.Extension})|{x.Extension}"));
        }

        /// <summary>
        /// Provides a limited set of file format extensions for directory file enumeration.
        /// </summary>
        /// <typeparam name="T">The plugin interface to load extensions for.</typeparam>
        /// <returns></returns>
        public IEnumerable<string> FileExtensionsByType<T>()
        {
            return _fileAdapters.OfType<T>().Select(x => x.GetType().GetCustomAttribute<PluginExtensionInfoAttribute>().Extension.ToLower().TrimStart('*')).OrderBy(o => o);
        }

        /// <inheritdoc />
        /// <summary>
        /// Shuts down Kore and closes all plugins and open files.
        /// </summary>
        public void Dispose()
        {
            _container?.Dispose();

            foreach (var kfi in OpenFiles.Select(f => f))
                CloseFile(kfi);
        }

        private List<ILoadFiles> Debug()
        {
            return _fileAdapters;
            //var sb = new StringBuilder();

            //foreach (var adapter in _fileAdapters)
            //{
            //    var pluginInfo = adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute)) as PluginInfoAttribute;
            //    var extInfo = adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute)) as PluginExtensionInfoAttribute;

            //    sb.AppendLine($"ID: {pluginInfo?.ID}");
            //    sb.AppendLine($"Name: {pluginInfo?.Name}");
            //    sb.AppendLine($"Short Name: {pluginInfo?.ShortName}");
            //    sb.AppendLine($"Author: {pluginInfo?.Author}");
            //    sb.AppendLine($"About: {pluginInfo?.About}");
            //    sb.AppendLine($"Extension(s): {extInfo?.Extension}");
            //    sb.AppendLine($"Identify: {adapter is IIdentifyFiles}");
            //    sb.AppendLine($"Load: {adapter is ILoadFiles}");
            //    sb.AppendLine($"Save: {adapter is ISaveFiles}");
            //    sb.AppendLine("");
            //}

            //MessageBox.Show(sb.ToString(), "Plugin Information");
        }
    }
}
