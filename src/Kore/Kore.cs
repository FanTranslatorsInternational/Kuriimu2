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
using Kontract.Interfaces.Archive;
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
        /// The plugin manager for this Kore instance
        /// </summary>
        private PluginLoader _manager;

        /// <summary>
        /// Stores the plugin directory that was set at construction time.
        /// </summary>
        private readonly string _pluginDirectory = "plugins";

        /// <summary>
        /// The list of currently open files being tracked by Kore.
        /// </summary>
        public List<KoreFileInfo> OpenFiles { get; } = new List<KoreFileInfo>();

        /// <summary>
        /// Provides an event that the UI can handle to present a plugin list to the user.
        /// </summary>
        public event EventHandler<IdentificationFailedEventArgs> IdentificationFailed;

        /// <summary>
        /// Provides an event that the UI can handle to load additional files in its own context
        /// </summary>
        public event EventHandler<RequestFileEventArgs> RequestFiles;

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
            _manager = PluginLoader.Global;
        }

        /// <summary>
        /// Initializes a new Kore instance with the given plugin directory.
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public Kore(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
            _manager = new PluginLoader(pluginDirectory);
        }

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
        public List<T> GetAdapters<T>() => _manager.GetAdapters<T>();

        /// <summary>
        /// Returns a list of the plugin interface type names that load files.
        /// </summary>
        /// <returns></returns>
        public List<string> GetFileLoadingAdapterNames() => new List<string>
        {
            nameof(ITextAdapter),
            nameof(IImageAdapter),
            nameof(IArchiveAdapter),
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
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            // Select adapter automatically
            var adapter = SelectAdapter(filename);

            // Ask the user to select a plugin directly.
            adapter = adapter ?? SelectAdapterManually();

            return LoadFile(filename, adapter, trackFile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="adapter"></param>
        /// <param name="trackFile"></param>
        /// <returns></returns>
        public KoreFileInfo LoadFile(string filename, ILoadFiles adapter, bool trackFile = true)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            // Instantiate a new instance of the adapter
            adapter = _manager.CreateAdapter<ILoadFiles>(_manager.GetMetadata<PluginInfoAttribute>(adapter).ID);

            // Load files(s)
            try
            {
                if (adapter is IRequestFiles reqAdapter)
                    reqAdapter.RequestFiles += Kore_RequestFile;
                adapter.Load(new StreamInfo { FileData = File.OpenRead(filename), FileName = filename });
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

        private void Kore_RequestFile(object sender, RequestFileEventArgs e)
        {
            RequestFiles?.Invoke(sender, e);
        }

        /// <summary>
        /// Saves an open file. Optionally to a new name.
        /// </summary>
        /// <param name="kfi">The KoreFileInfo to be saved.</param>
        /// <param name="filename">The optional new name of the file to be saved.</param>
        public void SaveFile(KoreFileInfo kfi, string filename = "")
        {
            //TODO: throw exception instead of just return?
            if (!OpenFiles.Contains(kfi) || !kfi.CanSave) return;

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
        /// <param name="file">The file to be selected against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(string file)
        {
            // Return an adapter that can Identify, whose extension matches that of our filename and successfully identifies the file.
            return _manager.GetAdapters<ILoadFiles>().
                Where(x => _manager.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.
                    ToLower().TrimEnd(';').Split(';').
                    Any(s => file.ToLower().EndsWith(s.TrimStart('*')))
                    ).
                FirstOrDefault(adapter =>
                    {
                        var info = new StreamInfo { FileData = File.OpenRead(file), FileName = file };

                        var res = ((IIdentifyFiles)adapter).Identify(info);
                        return res;
                    });
        }

        private ILoadFiles SelectAdapterManually()
        {
            var blindAdapters = _manager.GetAdapters<ILoadFiles>().Where(a => !(a is IIdentifyFiles)).ToList();

            var args = new IdentificationFailedEventArgs { BlindAdapters = blindAdapters };
            IdentificationFailed?.Invoke(this, args);

            //TODO: Handle this case better?
            //if (args.SelectedAdapter == null)
            //{
            //    return null;
            //}

            return args.SelectedAdapter;
        }

        /// <summary>
        /// Provides a complete set of file format names and extensions for open file dialogs.
        /// </summary>
        public string FileFilters
        {
            get
            {
                // Add all of the adapter filters
                var allTypes = _manager.GetAdapters<ILoadFiles>().Select(x => new { _manager.GetMetadata<PluginInfoAttribute>(x).Name, Extension = _manager.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

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
            var allTypes = _manager.GetAdapters<T>().Select(x => new { _manager.GetMetadata<PluginInfoAttribute>(x).Name, Extension = _manager.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

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
            return _manager.GetAdapters<T>().Select(x => _manager.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower().TrimStart('*')).OrderBy(o => o);
        }

        /// <inheritdoc />
        /// <summary>
        /// Shuts down Kore and closes all plugins and open files.
        /// </summary>
        public void Dispose()
        {
            //_container?.Dispose();

            foreach (var kfi in OpenFiles.Select(f => f))
                CloseFile(kfi);
        }

        private List<ILoadFiles> Debug()
        {
            return _manager.GetAdapters<ILoadFiles>();
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