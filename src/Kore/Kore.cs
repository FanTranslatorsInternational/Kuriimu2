using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.Interfaces.FileSystem;

namespace Kore
{
    /// <summary>
    /// Kore is the main brain library of Kuriimu. It performs all of the important and UI agnostic functions of Kuriimu.
    /// </summary>
    public sealed class KoreManager : IDisposable
    {
        /// <summary>
        /// Stores the plugin directory that was set at construction time.
        /// </summary>
        private readonly string _pluginDirectory;

        /// <summary>
        /// Retrieves the PluginLoader of this Kore instance
        /// </summary>
        /// <returns></returns>
        public PluginLoader PluginLoader { get; }

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
            PluginLoader = PluginLoader.Instance;
            _pluginDirectory = PluginLoader.PluginFolder;
        }

        /// <summary>
        /// Initializes a new Kore instance with the given plugin directory.
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public KoreManager(string pluginDirectory)
        {
            PluginLoader = new PluginLoader(pluginDirectory);
            _pluginDirectory = PluginLoader.PluginFolder;
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
        public List<T> GetAdapters<T>() => PluginLoader.GetAdapters<T>();

        /// <summary>
        /// Returns the metadata with the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adapter">Adapter to get metadata from</param>
        /// <returns></returns>
        public T GetMetadata<T>(object adapter) where T : Attribute, IPluginMetadata => PluginLoader.GetMetadata<T>(adapter);

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
        // TODO: We want to somehow reflect these names and also possibly return a class holding the Type and a DisplayName vs. the plain interface name.

        /// <summary>
        /// Load a file using Kore.
        /// </summary>
        /// <param name="kli"></param>
        /// <returns></returns>
        public KoreFileInfo LoadFile(KoreLoadInfo kli)
        {
            if (kli.Adapter == null)
            {
                // Select adapter automatically
                var adapter = SelectAdapter(kli);

                // Ask the user to select a plugin directly.
                adapter = adapter ?? SelectAdapterManually();

                if (adapter == null) return null;
                kli.Adapter = adapter;
            }

            // Instantiate a new instance of the adapter
            kli.Adapter = PluginLoader.CreateAdapter<ILoadFiles>(PluginLoader.GetMetadata<PluginInfoAttribute>(kli.Adapter).ID);

            // Load files(s)
            kli.FileData.Position = 0;
            var streamInfo = new StreamInfo { FileData = kli.FileData, FileName = kli.FileName };

            try
            {
                if (kli.Adapter is IMultipleFiles multiFileAdapter) multiFileAdapter.FileSystem = kli.FileSystem;

                kli.Adapter.LeaveOpen = kli.LeaveOpen;
                kli.Adapter.Load(streamInfo);
            }
            catch (Exception ex)
            {
                var pi = PluginLoader.GetMetadata<PluginInfoAttribute>(kli.Adapter);
                throw new LoadFileException($"The {pi?.Name} plugin failed to load \"{Path.GetFileName(kli.FileName)}\".{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.StackTrace}");
            }

            // Check if the stream still follows the LeaveOpen restriction
            if (!kli.FileData.CanRead && kli.LeaveOpen)
                throw new InvalidOperationException($"Plugin with ID {PluginLoader.GetMetadata<PluginInfoAttribute>(kli.Adapter).ID} closed the streams while loading the file.");

            // Create a KoreFileInfo to keep track of the now open file.
            var kfi = new KoreFileInfo
            {
                StreamFileInfo = streamInfo,
                HasChanges = false,
                Adapter = kli.Adapter
            };

            if (kli.TrackFile)
                OpenFiles.Add(kfi);

            return kfi;
        }

        /* - 1. Create filename tree
           - 2. Save all files from child to parent
           - 3. Close all KFI Streams
           - 4. Reset KFI.StreamFileInfo
           - 5. Execute LoadFile of KFI.Adapter on new KFI.StreamFileInfo
           - 6. Reopen dependent files from parent to child
        */
        public void SaveFile(KoreSaveInfo ksi)
        {
            SaveFile(ksi, true);
        }

        private void SaveFile(KoreSaveInfo ksi, bool firstIteration)
        {
            var kfi = ksi.Kfi;
            var tempFolder = ksi.TempFolder;
            var guid = Guid.NewGuid().ToString();

            // Get FullPath tree
            FullPathNode fullPathTree = null;
            if (firstIteration)
                fullPathTree = CreateFullPathTree(ksi.Kfi);

            // Save all childs first, if existent
            SaveChildren(ksi);

            // Save data with the adapter
            var fs = new PhysicalFileSystem(Path.Combine(Path.GetFullPath(ksi.TempFolder), guid));
            SaveWithAdapter(ksi, fs);

            // Close KFIs
            CloseFile(kfi, kfi.ParentKfi != null, firstIteration);

            // Replace data in parent KFI or physical folder
            if (kfi.ParentKfi != null)
                ReplaceFilesInAdapter(kfi.ParentKfi.Adapter as IArchiveAdapter, fs, fs.RootDirectory);
            else
            {
                var newSaveDir = string.IsNullOrEmpty(ksi.NewSaveLocation) ? Path.GetDirectoryName(kfi.FullPath) : Path.GetDirectoryName(ksi.NewSaveLocation);

                ReplaceFilesInFolder(newSaveDir, fs, fs.RootDirectory);
                UpdateFullPathTree(fullPathTree, Path.GetDirectoryName(kfi.FullPath), newSaveDir);
            }

            // Reopen files recursively from parent to child
            if (firstIteration)
                ksi.SavedKfi = ReopenFiles(kfi.ParentKfi != null ? kfi.ParentKfi : kfi, fullPathTree, tempFolder, kfi.ParentKfi != null);
        }

        private FullPathNode CreateFullPathTree(KoreFileInfo kfi)
        {
            var newNode = new FullPathNode(kfi.FullPath);
            if (kfi.ChildKfi != null)
                foreach (var child in kfi.ChildKfi)
                    newNode.Nodes.Add(CreateFullPathTree(child));
            return newNode;
        }

        private void UpdateFullPathTree(FullPathNode fullPathTree, string oldSaveLocation, string newSaveLocation)
        {
            fullPathTree.FullPath = fullPathTree.FullPath.Replace(oldSaveLocation, newSaveLocation);

            foreach (var child in fullPathTree.Nodes)
                UpdateFullPathTree(child, oldSaveLocation, newSaveLocation);
        }

        private class FullPathNode
        {
            public FullPathNode(string fullPath)
            {
                FullPath = fullPath;
                Nodes = new List<FullPathNode>();
            }

            public string FullPath { get; set; }
            public List<FullPathNode> Nodes { get; }
        }

        private void SaveChildren(KoreSaveInfo ksi)
        {
            var kfi = ksi.Kfi;

            if (kfi.ChildKfi != null && kfi.ChildKfi.Count > 0 && kfi.HasChanges)
                foreach (var child in kfi.ChildKfi)
                    if (child.HasChanges)
                        SaveFile(new KoreSaveInfo(child, ksi.TempFolder) { Version = ksi.Version }, false);
        }

        private void SaveWithAdapter(KoreSaveInfo ksi, IFileSystem fs)
        {
            var kfi = ksi.Kfi;

            if (kfi.Adapter is IMultipleFiles multFileAdapter)
                multFileAdapter.FileSystem = fs;
            kfi.Adapter.LeaveOpen = false;

            var streaminfo = new StreamInfo
            {
                FileData = fs.CreateFile(Path.GetFileName(kfi.StreamFileInfo.FileName)),
                FileName = Path.GetFileName(kfi.StreamFileInfo.FileName)
            };
            (kfi.Adapter as ISaveFiles).Save(streaminfo, ksi.Version);

            if (streaminfo.FileData.CanRead)
                streaminfo.FileData.Dispose();
        }

        private void ReplaceFilesInAdapter(IArchiveAdapter parentAdapter, IFileSystem physicalFS, string root)
        {
            // Loop through all directories
            foreach (var dir in physicalFS.EnumerateDirectories(true))
                ReplaceFilesInAdapter(parentAdapter, physicalFS.GetDirectory(dir), root);

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

        private void ReplaceFilesInFolder(string newSaveLocation, IFileSystem physicalFS, string root)
        {
            // Loop through all directories
            foreach (var dir in physicalFS.EnumerateDirectories(true))
                ReplaceFilesInFolder(newSaveLocation, physicalFS.GetDirectory(dir), root);

            // Update files of this directory
            foreach (var file in physicalFS.EnumerateFiles())
            {
                var relativeFileName = file.Remove(0, root.Length + 1);
                var openedFile = physicalFS.OpenFile(relativeFileName);

                if (!Directory.Exists(Path.Combine(newSaveLocation, Path.GetDirectoryName(relativeFileName))))
                    Directory.CreateDirectory(Path.Combine(newSaveLocation, Path.GetDirectoryName(relativeFileName)));

                var createdFile = File.Create(Path.Combine(newSaveLocation, relativeFileName));
                openedFile.CopyTo(createdFile);

                createdFile.Close();
                openedFile.Close();
            }
        }

        private string UnifyPathDelimiters(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        }

        private KoreFileInfo ReopenFiles(KoreFileInfo kfi, FullPathNode fullPathTree, string tempFolder, bool isChild)
        {
            var guid = Guid.NewGuid().ToString();

            // Open this file
            KoreFileInfo newKfi = null;
            if (isChild)
            {
                var parentFiles = (kfi.Adapter as IArchiveAdapter).Files;
                var foundAfi = parentFiles.FirstOrDefault(x => x.FileName == fullPathTree.FullPath.Remove(0, kfi.FullPath.Length + 1));
                if (foundAfi == null)
                    throw new InvalidOperationException($"While reopening files, the ArchiveFileInfo with FullPath \"{fullPathTree.FullPath}\" couldn't be found.");

                newKfi = LoadFile(new KoreLoadInfo(foundAfi.FileData, foundAfi.FileName) { LeaveOpen = true, Adapter = kfi.Adapter, FileSystem = new VirtualFileSystem(kfi.Adapter as IArchiveAdapter, tempFolder) });

                newKfi.ParentKfi = kfi;
                newKfi.ChildKfi = new List<KoreFileInfo>();
            }
            else
            {
                var openedFile = File.Open(fullPathTree.FullPath, FileMode.Open);

                newKfi = LoadFile(new KoreLoadInfo(openedFile, fullPathTree.FullPath) { LeaveOpen = true, Adapter = kfi.Adapter, FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(fullPathTree.FullPath)) });

                newKfi.ChildKfi = new List<KoreFileInfo>();
            }

            // Open Childs
            foreach (var child in fullPathTree.Nodes)
                newKfi.ChildKfi.Add(ReopenFiles(newKfi, child, tempFolder, true));

            return newKfi;
        }

        //public void SaveFile(KoreSaveInfo ksi)
        //{
        //    // Save data with the adapter
        //    var guid = Guid.NewGuid().ToString();
        //    var fs = new PhysicalFileSystem(Path.Combine(tempFolder, guid));
        //    if (kfi.Adapter is IMultipleFiles multFileAdapter)
        //        multFileAdapter.FileSystem = fs;
        //    kfi.Adapter.LeaveOpen = false;
        //    var streaminfo = new StreamInfo { FileData = fs.CreateFile(Path.GetFileName(kfi.StreamFileInfo.FileName)), FileName = Path.GetFileName(kfi.StreamFileInfo.FileName) };
        //    (kfi.Adapter as ISaveFiles).Save(streaminfo, ksi.Version);

        //    // Replace files in adapter
        //    if (kfi.ParentKfi != null)
        //    {
        //        var parentArchiveAdapter = kfi.ParentKfi.Adapter as IArchiveAdapter;
        //        RecursiveUpdate(parentArchiveAdapter, fs, Path.Combine(Path.GetFullPath(tempFolder), guid));
        //    }
        //    else
        //    {
        //        // TODO: Implement save if no parent is given
        //        // Get intial directory
        //        var initialDir = Path.GetDirectoryName(ksi.Kfi.FullPath);

        //        // Close current initial file
        //        ksi.Kfi.StreamFileInfo.FileData.Close();

        //        // Close current FileSystem, if set
        //        if (ksi.Kfi is IMultipleFiles multFileAdapter2 && multFileAdapter2.FileSystem != null)
        //            multFileAdapter2.FileSystem.Dispose();

        //        // All open filestreams of the initial file should be closed by now
        //        if (string.IsNullOrEmpty(ksi.NewSaveLocation))
        //        {
        //            // Move saved files to intial location
        //            SaveFileSystem(fs, initialDir);
        //        }
        //        else
        //        {

        //        }

        //        // Reopen
        //    }

        //    // Update archive file states in this level
        //    if (kfi.Adapter is IArchiveAdapter archiveAdapter)
        //        foreach (var afi in archiveAdapter.Files)
        //            afi.State = ArchiveFileState.Archived;

        //    // Update archive file states up the parent tree
        //    if (kfi.ParentKfi != null)
        //        kfi.UpdateState(ArchiveFileState.Replaced);
        //}

        /// <summary>
        /// Closes an open file.
        /// </summary>
        /// <param name="kfi">The file to be closed.</param>
        /// <returns>True if file was closed, False otherwise.</returns>
        public bool CloseFile(KoreFileInfo kfi, bool leaveFileStreamOpen = false)
        {
            return CloseFile(kfi, leaveFileStreamOpen, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kfi"></param>
        /// <param name="leaveFileStreamOpen"></param>
        /// <param name="firstIteration"></param>
        /// <returns></returns>
        private bool CloseFile(KoreFileInfo kfi, bool leaveFileStreamOpen, bool firstIteration)
        {
            if (kfi.ChildKfi != null && kfi.ChildKfi.Count > 0)
                foreach (var child in kfi.ChildKfi)
                    CloseFile(child, false, false);

            if (OpenFiles.Contains(kfi))
                OpenFiles.Remove(kfi);

            kfi.Adapter.Dispose();
            if (!leaveFileStreamOpen)
                kfi.StreamFileInfo.FileData.Close();
            if (kfi.Adapter is IMultipleFiles multFileAdapter)
                multFileAdapter.FileSystem.Dispose();

            if (firstIteration)
                if (kfi.ParentKfi != null)
                    kfi.ParentKfi.ChildKfi.Remove(kfi);

            return true;
        }

        /// <summary>
        /// Get a KFI by its FullPath property
        /// </summary>
        /// <param name="fullpath">the full qualified path</param>
        /// <returns>KFI or null if not found</returns>
        public KoreFileInfo GetOpenedFile(string fullpath)
        {
            if (OpenFiles.Any(x => x.FullPath == fullpath))
                return OpenFiles.First(x => x.FullPath == fullpath);

            return null;
        }

        /// <summary>
        /// Attempts to select a compatible adapter that is capable of identifying files.
        /// </summary>
        /// <param name="file">The file to be selected against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(KoreLoadInfo kli)
        {
            // Return an adapter that can Identify, whose extension matches that of our filename and successfully identifies the file.
            return PluginLoader.GetAdapters<ILoadFiles>().
                Where(x => PluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.
                    ToLower().TrimEnd(';').Split(';').Any(s => kli.FileName.ToLower().EndsWith(s.TrimStart('*')))
                ).Select(x =>
                {
                    if (x is IMultipleFiles y)
                        y.FileSystem = kli.FileSystem;
                    return x;
                }).FirstOrDefault(adapter => CheckAdapter(adapter, kli));
        }

        /// <summary>
        /// Does the actual identification with IIdentifyFiles and checks the availablity of the data stream.
        /// </summary>
        /// <param name="adapter">Adapter to identify with.</param>
        /// <param name="kli">Kore information for identification.</param>
        /// <returns>Returns if the adapter was capable of identifying the file.</returns>
        private bool CheckAdapter(ILoadFiles adapter, KoreLoadInfo kli)
        {
            if (!(adapter is IIdentifyFiles))
                return false;
            adapter.LeaveOpen = true;

            kli.FileData.Position = 0;
            var info = new StreamInfo { FileData = kli.FileData, FileName = kli.FileName };
            var res = ((IIdentifyFiles)adapter).Identify(info);

            if (!kli.FileData.CanRead)
                throw new InvalidOperationException($"Plugin with ID '{PluginLoader.GetMetadata<PluginInfoAttribute>(adapter).ID}' closed the stream(s) while identifying the file(s).");

            return res;
        }

        /// <summary>
        /// Toggles an event for the UI to let the user select a blind adapter manually.
        /// </summary>
        /// <returns>The selected adapter or null.</returns>
        private ILoadFiles SelectAdapterManually()
        {
            var blindAdapters = PluginLoader.GetAdapters<ILoadFiles>().Where(a => !(a is IIdentifyFiles)).ToList();

            var args = new IdentificationFailedEventArgs { BlindAdapters = blindAdapters };
            IdentificationFailed?.Invoke(this, args);

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
                var allTypes = PluginLoader.GetAdapters<ILoadFiles>().Select(x => new { PluginLoader.GetMetadata<PluginInfoAttribute>(x).Name, Extension = PluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

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
            var allTypes = PluginLoader.GetAdapters<T>().Select(x => new { PluginLoader.GetMetadata<PluginInfoAttribute>(x).Name, Extension = PluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

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
            return PluginLoader.GetAdapters<T>().Select(x => PluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower().TrimStart('*')).OrderBy(o => o);
        }

        /// <inheritdoc />
        /// <summary>
        /// Shuts down Kore and closes all plugins and open files.
        /// </summary>
        public void Dispose()
        {
            foreach (var kfi in OpenFiles.Select(f => f))
                CloseFile(kfi);
        }

        private List<ILoadFiles> Debug()
        {
            return PluginLoader.GetAdapters<ILoadFiles>();
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
