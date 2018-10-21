using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore
{
    /// <summary>
    /// Kore is the main brain library of Kuriimu. It performs all of the important and UI agnostic functions of Kuriimu.
    /// </summary>
    public sealed class Kore
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

#pragma warning restore 0649, 0169
        #endregion

        /// <summary>
        /// The list of currently open files being tracked by Kore.
        /// </summary>
        public List<KoreFileInfo> OpenFiles { get; }

        /// <summary>
        /// Provides an event that the UI can handle to present a plugin list to the user.
        /// </summary>
        public event EventHandler<IdentificationFailedEventArgs> IdentificationFailed;

        /// <summary>
        /// Allows the UI to display a list of blind plugins and to return one selected by the user.
        /// </summary>
        public class IdentificationFailedEventArgs
        {
            public List<ILoadFiles> BlindAdapters;
            public ILoadFiles SelectedAdapter = null;
        }

        /// <summary>
        /// Initializes a new Kore instance.
        /// </summary>
        public Kore()
        {
            ComposePlugins();
            OpenFiles = new List<KoreFileInfo>();
        }

        /// <summary>
        /// Initializes a new Kore instance with the given plugin directory.
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public Kore(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
            ComposePlugins();
            OpenFiles = new List<KoreFileInfo>();
        }

        /// <summary>
        /// Re/Loads the plugin container.
        /// </summary>
        private void ComposePlugins()
        {
            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            // Adds all the parts found in the same assembly as the Kore class.
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Kore).Assembly));

            if (Directory.Exists(_pluginDirectory) && Directory.GetFiles(_pluginDirectory, "*.dll").Length > 0)
                catalog.Catalogs.Add(new DirectoryCatalog(_pluginDirectory));

            // Create the CompositionContainer with the parts in the catalog.
            _container?.Dispose();
            _container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            _container.ComposeParts(this);
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
        /// Loads a file into the tracking list.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        /// <returns>Returns a KoreFileInfo for the opened file.</returns>
        public KoreFileInfo LoadFile(string filename)
        {
            var adapter = SelectAdapter(filename);

            // Ask the user to select a plugin directly.
            if (adapter == null)
            {
                var blindAdapters = _fileAdapters.Where(a => !(a is IIdentifyFiles)).ToList();

                var args = new IdentificationFailedEventArgs { BlindAdapters = blindAdapters };
                IdentificationFailed?.Invoke(this, args);

                //TODO: Handle this case better?
                if (args.SelectedAdapter == null)
                {
                    return null;
                }

                adapter = args.SelectedAdapter;
            }

            if (adapter == null)
                throw new LoadFileException("No plugins were able to ");

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
            if (!OpenFiles.Contains(kfi) || !(kfi.Adapter is ISaveFiles)) return;

            var adapter = (ISaveFiles)kfi.Adapter;

            if (filename == string.Empty)
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
            // Return an adapter that can Identify whose extension matches that of our filename and successfully identifies the file.
            return _fileAdapters.Where(adapter => adapter is IIdentifyFiles && ((PluginExtensionInfoAttribute)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute))).Extension.ToLower().TrimEnd(';').Split(';').Any(s => filename.ToLower().EndsWith(s.TrimStart('*')))).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(filename));
        }

        /// <summary>
        /// Provides a complete set of supported file format names and extensions for open file dialogs.
        /// </summary>
        public string FileFilters
        {
            get
            {
                // Add all of the adapter filters
                var allTypes = _fileAdapters.Select(x => new { ((PluginInfoAttribute)x.GetType().GetCustomAttribute(typeof(PluginInfoAttribute))).Name, Extension = ((PluginExtensionInfoAttribute)x.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute))).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

                // Add the special all supported files filter
                if (allTypes.Count > 0)
                    allTypes.Insert(0, new { Name = "All Supported Files", Extension = string.Join(";", allTypes.Select(x => x.Extension).Distinct()) });

                // Add the special all files filter
                allTypes.Add(new { Name = "All Files", Extension = "*.*" });

                return string.Join("|", allTypes.Select(x => $"{x.Name} ({x.Extension})|{x.Extension}"));
            }
        }

        public void Debug()
        {
            var sb = new StringBuilder();

            foreach (var adapter in _fileAdapters)
            {
                var pluginInfo = adapter.GetType().GetCustomAttribute(typeof(PluginInfoAttribute)) as PluginInfoAttribute;
                var extInfo = adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfoAttribute)) as PluginExtensionInfoAttribute;

                sb.AppendLine($"ID: {pluginInfo?.ID}");
                sb.AppendLine($"Name: {pluginInfo?.Name}");
                sb.AppendLine($"Short Name: {pluginInfo?.ShortName}");
                sb.AppendLine($"Author: {pluginInfo?.Author}");
                sb.AppendLine($"About: {pluginInfo?.About}");
                sb.AppendLine($"Extension(s): {extInfo?.Extension}");
                sb.AppendLine($"Identify: {adapter is IIdentifyFiles}");
                sb.AppendLine($"Load: {adapter is ILoadFiles}");
                sb.AppendLine($"Save: {adapter is ISaveFiles}");
                sb.AppendLine("");
            }

            MessageBox.Show(sb.ToString(), "Plugin Information");
        }
    }
}
