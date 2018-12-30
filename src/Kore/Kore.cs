using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.Interfaces.VirtualFS;

namespace Kore
{
    /// <summary>
    /// Kore is the main brain library of Kuriimu. It performs all of the important and UI agnostic functions of Kuriimu.
    /// </summary>
    public sealed class KoreManager : IDisposable
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
        public KoreManager()
        {
            _manager = PluginLoader.Global;
        }

        /// <summary>
        /// Initializes a new Kore instance with the given plugin directory.
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public KoreManager(string pluginDirectory)
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
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(KoreManager).Assembly));

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
        /// Returns the metadata with the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adapter">Adapter to get metadata from</param>
        /// <returns></returns>
        public T GetMetadata<T>(object adapter) where T : Attribute, IPluginMetadata => _manager.GetMetadata<T>(adapter);

        //TODO: Do we need that?
        /// <summary>
        /// Retrieves the PluginLoader of this Kore instance
        /// </summary>
        /// <returns></returns>
        public PluginLoader GetPluginLoader()
        {
            return _manager;
        }

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

        public KoreFileInfo LoadFile(KoreLoadInfo klf)
        {
            if (klf.Adapter == null)
            {
                // Select adapter automatically
                var adapter = SelectAdapter(klf);

                // Ask the user to select a plugin directly.
                adapter = adapter ?? SelectAdapterManually();

                if (adapter == null) return null;
                klf.Adapter = adapter;
            }

            // Instantiate a new instance of the adapter
            klf.Adapter = _manager.CreateAdapter<ILoadFiles>(_manager.GetMetadata<PluginInfoAttribute>(klf.Adapter).ID);

            // Load files(s)
            klf.FileData.Position = 0;
            var streaminfo = new StreamInfo { FileData = klf.FileData, FileName = klf.FileName };
            try
            {
                if (klf.Adapter is IMultipleFiles multFileAdapter) multFileAdapter.FileSystem = klf.FileSystem;

                klf.Adapter.LeaveOpen = klf.LeaveOpen;
                klf.Adapter.Load(streaminfo);
            }
            catch (Exception ex)
            {
                var pi = _manager.GetMetadata<PluginInfoAttribute>(klf.Adapter);
                throw new LoadFileException($"The {pi?.Name} plugin failed to load \"{Path.GetFileName(klf.FileName)}\".{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.StackTrace}");
            }

            // Check if the stream still follows the LeaveOpen restriction
            if (!klf.FileData.CanRead && klf.LeaveOpen)
                throw new InvalidOperationException($"Plugin with ID {_manager.GetMetadata<PluginInfoAttribute>(klf.Adapter).ID} closed the streams whole loading the file.");

            // Create a KoreFileInfo to keep track of the now open file.
            var kfi = new KoreFileInfo
            {
                StreamFileInfo = streaminfo,
                HasChanges = false,
                Adapter = klf.Adapter
            };

            if (klf.TrackFile)
                OpenFiles.Add(kfi);

            return kfi;
        }

        public void SaveFile(KoreFileInfo kfi, string tempFolder, int version = 0)
        {
            // Execute SaveFile down the child tree
            if (kfi.ChildKfi != null)
                foreach (var child in kfi.ChildKfi)
                    SaveFile(child, tempFolder);

            // Save data with the adapter
            var guid = Guid.NewGuid().ToString();
            var fs = new PhysicalFileSystem(Path.Combine(tempFolder, guid));
            if (kfi.Adapter is IMultipleFiles multFileAdapter)
                multFileAdapter.FileSystem = fs;
            kfi.Adapter.LeaveOpen = false;
            var streaminfo = new StreamInfo { FileData = fs.CreateFile(Path.GetFileName(kfi.StreamFileInfo.FileName)), FileName = Path.GetFileName(kfi.StreamFileInfo.FileName) };
            (kfi.Adapter as ISaveFiles).Save(streaminfo, version);

            // Replace files in adapter
            if (kfi.ParentKfi != null)
            {
                var parentArchiveAdapter = kfi.ParentKfi.Adapter as IArchiveAdapter;
                RecursiveUpdate(parentArchiveAdapter, fs, Path.Combine(Path.GetFullPath(tempFolder), guid));
            }
            else
            {
                // TODO: Implement save if no parent is given
            }

            // Update archive file states in this level
            if (kfi.Adapter is IArchiveAdapter archiveAdapter)
                foreach (var afi in archiveAdapter.Files)
                    afi.State = ArchiveFileState.Archived;

            // Update archive file states up the parent tree
            if (kfi.ParentKfi != null)
                kfi.UpdateState(ArchiveFileState.Replaced);
        }

        private void RecursiveUpdate(IArchiveAdapter parentAdapter, IVirtualFSRoot physicalFS, string root)
        {
            // Loop through all directories
            foreach (var dir in physicalFS.EnumerateDirectories(true))
                RecursiveUpdate(parentAdapter, physicalFS.GetDirectory(dir), root);

            // Update files of this directory
            foreach (var file in physicalFS.EnumerateFiles())
            {
                var openedFile = physicalFS.OpenFile(file.Remove(0, root.Length + 1));
                var afi = parentAdapter.Files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == UnifyPathDelimiters(file.Remove(0, root.Length + 1)));
                if (afi != null)
                {
                    afi.FileData.Dispose();
                    afi.FileData = openedFile;
                    afi.State = ArchiveFileState.Replaced;
                }
            }
        }

        private string UnifyPathDelimiters(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        }

        //TODO: Redefine SaveFile
        /// <summary>
        /// Saves an open file. Optionally to a new name.
        /// </summary>
        /// <param name="kfi">The KoreFileInfo to be saved.</param>
        /// <param name="filename">The optional new name of the file to be saved.</param>
        //public void SaveFile(KoreFileInfo kfi, KoreSaveInfo ksi)
        //{
        // All files created or updated by the plugin will be put into this FileSystem
        //if (kfi.Adapter is IMultipleFiles multFilesAdapter)
        //    multFilesAdapter.FileSystem = new PhysicalFileSystem(ksi.TempFolder);

        //if (ksi.InPlaceSave)
        //    SaveInPlace(kfi, ksi);
        //else
        //    SaveNew(kfi, ksi);

        //TODO: throw exception instead of just return?
        //if (!OpenFiles.Contains(kfi) || !kfi.CanSave) return;

        //var adapter = (ISaveFiles)kfi.Adapter;

        //if (string.IsNullOrEmpty(filename))
        //    adapter.Save(kfi.FileInfo.FullName);
        //else
        //{
        //    adapter.Save(filename);
        //    kfi.FileName = filename;
        //}
        //}

        //private void SaveInPlace(KoreFileInfo kfi, KoreSaveInfo ksi)
        //{
        //    var streamInfo = kfi.StreamFileInfo;

        //    streamInfo.FileData.Position = 0;
        //    (kfi.Adapter as ISaveFiles).Save(streamInfo, ksi.Version);
        //}

        //private void SaveNew(KoreFileInfo kfi, KoreSaveInfo ksi)
        //{
        //    if (string.IsNullOrEmpty(ksi.TempFolder))
        //        throw new InvalidOperationException("Temp Folder can't be null or empty for saving to a new location.");

        //    var streamInfo = new StreamInfo
        //    {
        //        FileData = File.Open(Path.Combine(ksi.TempFolder, Path.GetFileName(kfi.StreamFileInfo.FileName)), FileMode.Create),
        //        FileName = Path.GetFileName(kfi.StreamFileInfo.FileName)
        //    };

        //    (kfi.Adapter as ISaveFiles).Save(streamInfo, ksi.Version);
        //}

        /// <summary>
        /// Closes an open file.
        /// </summary>
        /// <param name="kfi">The file to be closed.</param>
        /// <returns>True if file was closed, False otherwise.</returns>
        public bool CloseFile(KoreFileInfo kfi, bool leaveFileStreamOpen = false)
        {
            if (OpenFiles.Contains(kfi))
                OpenFiles.Remove(kfi);

            kfi.Adapter.Dispose();
            if (!leaveFileStreamOpen)
                kfi.StreamFileInfo.FileData.Close();
            if (kfi.Adapter is IMultipleFiles multFileAdapter)
                multFileAdapter.FileSystem.Dispose();

            return true;
        }

        /// <summary>
        /// Attempts to select a compatible adapter that is capable of identifying files.
        /// </summary>
        /// <param name="file">The file to be selected against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(KoreLoadInfo klf)
        {
            // Return an adapter that can Identify, whose extension matches that of our filename and successfully identifies the file.
            return _manager.GetAdapters<ILoadFiles>().
                Where(x => _manager.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.
                    ToLower().TrimEnd(';').Split(';').
                    Any(s => klf.FileName.ToLower().EndsWith(s.TrimStart('*')))
                    ).
                Select(x =>
                {
                    if (x is IMultipleFiles y)
                        y.FileSystem = klf.FileSystem;
                    return x;
                }).
                FirstOrDefault(adapter => CheckAdapter(adapter, klf));
        }

        /// <summary>
        /// Does the actual identification with IIdentifyFiles and checks the availablity of the date stream.
        /// </summary>
        /// <param name="adapter">Adapter to identify with.</param>
        /// <param name="klf">Kore information for identification.</param>
        /// <returns>Returns if the adapter was capable of identifying the file.</returns>
        private bool CheckAdapter(ILoadFiles adapter, KoreLoadInfo klf)
        {
            if (!(adapter is IIdentifyFiles))
                return false;
            adapter.LeaveOpen = true;

            klf.FileData.Position = 0;
            var info = new StreamInfo { FileData = klf.FileData, FileName = klf.FileName };
            var res = ((IIdentifyFiles)adapter).Identify(info);

            if (!klf.FileData.CanRead)
                throw new InvalidOperationException($"Plugin with ID {_manager.GetMetadata<PluginInfoAttribute>(adapter).ID} closed the streams while identifying the file.");

            return res;
        }

        /// <summary>
        /// Toggles an event for the UI to let the user select a blind adapter manually.
        /// </summary>
        /// <returns>The selected adapter or null.</returns>
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