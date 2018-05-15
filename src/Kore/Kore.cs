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

        #endregion

        #region Properties

        public List<KoreFile> OpenFiles { get; }

        #endregion

        public Kore()
        {
            OpenFiles = new List<KoreFile>();

            // MEF Cataloging
            // An aggregate catalog that combines multiple catalogs.
            var catalog = new AggregateCatalog();

            // Adds all the parts found in the same assembly as the Program class.
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Kore).Assembly));

            // Create the CompositionContainer with the parts in the catalog.
            _container = new CompositionContainer(catalog);

            // Fill the imports of this object.
            _container.ComposeParts(this);
        }

        public KoreFile LoadFile(string filename)
        {
            var adapter = SelectAdapter(filename);

            if (adapter == null)
                return null;

            adapter.Load(filename);

            var kf = new KoreFile
            {
                FileInfo = new FileInfo(filename),
                HasChanges = false,
                Adapter = adapter
            };

            OpenFiles.Add(kf);

            return kf;
        }

        private ILoadFiles SelectAdapter(string filename)
        {
            // Return an adapter that can Identify whose extension matches that of our filename and sucessfully identifies the file.
            return _fileAdapters.Where(adapter => adapter is IIdentifyFiles && ((PluginExtensionInfo)adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfo))).Extension.ToLower().TrimEnd(';').Split(';').Any(s => filename.ToLower().EndsWith(s.TrimStart('*')))).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(filename));
        }

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
            foreach (var adapter in _textAdapters)
            {
                var pluginInfo = adapter.GetType().GetCustomAttribute(typeof(PluginInfo)) as PluginInfo;
                var extInfo = adapter.GetType().GetCustomAttribute(typeof(PluginExtensionInfo)) as PluginExtensionInfo;

                var sb = new StringBuilder();
                sb.AppendLine($"ID: {pluginInfo?.ID}");
                sb.AppendLine($"Name: {pluginInfo?.Name}");
                sb.AppendLine($"Short Name: {pluginInfo?.ShortName}");
                sb.AppendLine($"Author: {pluginInfo?.Author}");
                sb.AppendLine($"About: {pluginInfo?.About}");
                sb.AppendLine($"Extension(s): {extInfo?.Extension}");
                sb.AppendLine($"Identify: {adapter is IIdentifyFiles}");
                sb.AppendLine($"Load: {adapter is ILoadFiles}");
                sb.AppendLine($"Save: {adapter is ISaveFiles}");

                MessageBox.Show(sb.ToString(), "Plugin Information");
            }
        }
    }
}
