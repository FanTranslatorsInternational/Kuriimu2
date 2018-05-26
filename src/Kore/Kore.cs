using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Kontract.Attribute;
using Kontract.Interface;

namespace Kore
{
    public class Kore
    {
        /// <summary>
        /// Stores the currently loaded MEF plugins.
        /// </summary>
        private CompositionContainer _container;

        #region Plugins

        [ImportMany(typeof(ILoadFiles))]
        private List<ILoadFiles> _fileAdapters;

        [ImportMany(typeof(ITextAdapter))]
        private List<ITextAdapter> _textAdapters;

        //[ImportMany(typeof(IImageAdapter))]
        //private List<IImageAdapter> _imageAdapters;

        //[ImportMany(typeof(IArchiveAdapter))]
        //private List<IArchiveAdapter> _archiveAdapters;

        [ImportMany(typeof(IFontAdapter))]
        private List<IFontAdapter> _fontAdapters;

        #endregion

        #region Properties

        /// <summary>
        /// The list of currently open files being tracked by Kore.
        /// </summary>
        public List<KoreFileInfo> OpenFiles { get; }

        #endregion

        #region Events
        public event EventHandler<CantIdentifyEventArgs> CantIdentify;
        public class CantIdentifyEventArgs
        {
            public List<ILoadFiles> cantIdentify;
            public ILoadFiles selectedIdentify = null;
        }
        #endregion

        /// <summary>
        /// Initializes a new Kore instance.
        /// </summary>
        public Kore()
        {
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

            // Adds all the parts found in the same assembly as the Program class.
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Kore).Assembly));

            if (Directory.Exists("plugins") && Directory.GetFiles("plugins", "*.dll").Length > 0)
                catalog.Catalogs.Add(new DirectoryCatalog("plugins"));

            // Create the CompositionContainer with the parts in the catalog.
            _container?.Dispose();
            _container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            _container.ComposeParts(this);
        }

        /// <summary>
        /// Loads a file into the tracking list.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        /// <returns>Returns a KoreFileInfo for the opened file.</returns>
        public KoreFileInfo LoadFile(string filename)
        {
            var adapter = SelectAdapter(filename);

            if (adapter == null)
            {
                var cantIdentify = _fileAdapters.Where(a => !(a is IIdentifyFiles)).ToList();

                var args = new CantIdentifyEventArgs { cantIdentify = cantIdentify };
                CantIdentify(this, args);

                //TODO: Handle this case better?
                if (args.selectedIdentify == null)
                    return null;

                adapter = args.selectedIdentify;
            }

            adapter.Load(filename);

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
        /// Closes an open file.
        /// </summary>
        /// <param name="kfi">The file to be closed.</param>
        /// <returns>True if file was closed, False otherwise.</returns>
        public bool CloseFile(KoreFileInfo kfi)
        {
            if (!OpenFiles.Contains(kfi)) return false;
            OpenFiles.Remove(kfi);
            return true;
        }

        /// <summary>
        /// Attempts to select a compatible adapter that is capable of identifying files.
        /// </summary>
        /// <param name="filename">The file to be selcted against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(string filename)
        {
            // Return an adapter that can Identify whose extension matches that of our filename and sucessfully identifies the file.
            return _fileAdapters.Where(adapter => adapter is IIdentifyFiles && ((PluginExtensionInfo)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfo))).Extension.ToLower().TrimEnd(';').Split(';').Any(s => filename.ToLower().EndsWith(s.TrimStart('*')))).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(filename));
        }

        /// <summary>
        /// Provides a complete set of supported file format names and extensions for open file dialogs.
        /// </summary>
        public string FileFilters
        {
            get
            {
                // Add all of the adapter filters
                var alltypes = _fileAdapters.Select(x => new { ((PluginInfo)x.GetType().GetCustomAttribute(typeof(PluginInfo))).Name, Extension = ((PluginExtensionInfo)x.GetType().GetCustomAttribute(typeof(PluginExtensionInfo))).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

                // Add the special all supported files filter
                if (alltypes.Count > 0)
                    alltypes.Insert(0, new { Name = "All Supported Files", Extension = string.Join(";", alltypes.Select(x => x.Extension).Distinct()) });

                // Add the special all files filter
                alltypes.Add(new { Name = "All Files", Extension = "*.*" });

                return string.Join("|", alltypes.Select(x => $"{x.Name} ({x.Extension})|{x.Extension}"));
            }
        }

        public void Debug()
        {
            var sb = new StringBuilder();

            foreach (var adapter in _fileAdapters)
            {
                var pluginInfo = adapter.GetType().GetCustomAttribute(typeof(PluginInfo)) as PluginInfo;
                var extInfo = adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfo)) as PluginExtensionInfo;

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
