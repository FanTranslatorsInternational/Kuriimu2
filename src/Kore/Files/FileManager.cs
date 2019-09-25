using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kore.Exceptions.FileManager;
using Kore.Files.Models;

namespace Kore.Files
{
    /// <summary>
    /// The file manager is the main class for all file related operations.
    /// </summary>
    public sealed class FileManager : IDisposable
    {
        private readonly PluginLoader _pluginLoader;

        /// <summary>
        /// The list of currently open files being tracked by Kore.
        /// </summary>
        public List<KoreFileInfo> OpenFiles { get; } = new List<KoreFileInfo>();

        /// <summary>
        /// Provides an event that the UI can handle to present a plugin list to the user.
        /// </summary>
        public event EventHandler<IdentificationFailedEventArgs> IdentificationFailed;

        /// <summary>
        /// Initializes a new instance of <see cref="FileManager"/>.
        /// </summary>
        public FileManager()
        {
            _pluginLoader = PluginLoader.Instance;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FileManager"/> with the given <see cref="PluginLoader"/>.
        /// </summary>
        /// <param name="pluginLoader">The plugin loader to use.</param>
        public FileManager(PluginLoader pluginLoader)
        {
            _pluginLoader = pluginLoader;
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
                // Select adapter automatically and if failed select it manually
                var adapter = SelectAdapter(kli) ?? SelectAdapterManually(kli);

                // If still no adapter was chosen, return
                if (adapter == null) return null;

                kli.Adapter = adapter;
            }

            // Instantiate a new instance of the adapter
            // ReSharper disable once SuspiciousTypeConversion.Global
            kli.Adapter = _pluginLoader.CreateNewAdapter<ILoadFiles>((IPlugin)kli.Adapter);

            // Load files(s)
            // TODO: Make KLI contain a StreamInfo of the given file
            kli.FileData.Position = 0;
            var streamInfo = new StreamInfo
            {
                FileData = kli.FileData,
                FileName = kli.FileName
            };

            try
            {
                // TODO: Subject to remove
                kli.Adapter.LeaveOpen = kli.LeaveOpen;
                // Try to load the file via adapter
                kli.Adapter.Load(streamInfo, kli.FileSystem);
            }
            catch (Exception ex)
            {
                // Catch any exception thrown by the plugin and expose it
                var pi = _pluginLoader.GetMetadata<PluginInfoAttribute>(kli.Adapter);
                var msg = $"The {pi?.Name} plugin failed to load \"{Path.GetFileName(kli.FileName)}\".";
                throw new LoadFileException(msg, ex);
            }

            // TODO: Subject to remove
            // Check if the stream still follows the LeaveOpen restriction
            if (!kli.FileData.CanRead && kli.LeaveOpen)
                throw new InvalidOperationException($"Plugin with ID {_pluginLoader.GetMetadata<PluginInfoAttribute>(kli.Adapter).ID} closed the streams while loading the file.");

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
        public KoreFileInfo SaveFile(KoreSaveInfo ksi) =>
            SaveFile(ksi, true);

        private KoreFileInfo SaveFile(KoreSaveInfo ksi, bool firstIteration)
        {
            var kfi = ksi.Kfi;
            var tempFolder = ksi.TempFolder;
            if (!string.IsNullOrEmpty(ksi.NewSaveFile) && File.Exists(ksi.NewSaveFile) && File.GetAttributes(ksi.NewSaveFile).HasFlag(FileAttributes.Directory))
                throw new InvalidOperationException($"{nameof(ksi.NewSaveFile)} needs to be a file path.");
            var guid = Guid.NewGuid().ToString();

            // Get FullPath tree
            FullPathNode fullPathTree = null;
            if (firstIteration)
                fullPathTree = CreateFullPathTree(ksi.Kfi);

            // Save all children first, if they exist
            SaveChildren(ksi);

            // Save data with the adapter
            var fs = new PhysicalDirectoryNode(guid, Path.GetFullPath(ksi.TempFolder));
            SaveWithAdapter(ksi, fs);

            // Close KFIs
            CloseFile(kfi, kfi.ParentKfi != null, firstIteration);

            // Replace data in parent KFI or physical folder
            fs = new PhysicalDirectoryNode("", Path.Combine(Path.GetFullPath(ksi.TempFolder), guid));
            if (kfi.ParentKfi != null)
                ReplaceFilesInAdapter(kfi.ParentKfi.Adapter as IArchiveAdapter, fs, fs.RootPath);
            else
            {
                var newLocation = string.IsNullOrEmpty(ksi.NewSaveFile) ? kfi.FullPath : ksi.NewSaveFile;
                var newSaveDir = Path.GetDirectoryName(newLocation);

                ReplaceFilesInFolder(newSaveDir, fs, fs.RootPath);
                UpdateFullPathTree(fullPathTree, Path.GetDirectoryName(kfi.FullPath), newSaveDir);
            }

            // Reopen files recursively from parent to child
            return firstIteration ?
                ReopenFiles(kfi.ParentKfi ?? kfi, fullPathTree, tempFolder, kfi.ParentKfi != null) :
                null;
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

        private void SaveWithAdapter(KoreSaveInfo ksi, PhysicalDirectoryNode fs)
        {
            var kfi = ksi.Kfi;

            kfi.Adapter.LeaveOpen = false;

            var newFilename = string.IsNullOrEmpty(ksi.NewSaveFile) ? Path.GetFileName(kfi.StreamFileInfo.FileName) : Path.GetFileName(ksi.NewSaveFile);
            fs.AddFile(newFilename);
            var fileNode = fs.GetFileNode(newFilename);
            var streamInfo = new StreamInfo
            {
                FileData = fileNode.Open(),
                FileName = newFilename
            };
            (kfi.Adapter as ISaveFiles)?.Save(streamInfo, fs, ksi.Version);

            if (streamInfo.FileData.CanRead)
                streamInfo.FileData.Dispose();
        }

        private void ReplaceFilesInAdapter(IArchiveAdapter parentAdapter, BaseReadOnlyDirectoryNode physicalFs, string root)
        {
            // Loop through all directories
            foreach (var dir in physicalFs.EnumerateDirectories())
                ReplaceFilesInAdapter(parentAdapter, physicalFs.GetDirectoryNode(dir.RelativePath), root);

            // Update files of this directory
            foreach (var file in physicalFs.EnumerateFiles())
            {
                var openedFile = file.Open();
                var afi = parentAdapter.Files.FirstOrDefault(x => Common.UnifyPath(x.FileName) == file.RelativePath);
                if (afi != null)
                {
                    afi.FileData.Dispose();
                    afi.FileData = openedFile;
                    afi.State = ArchiveFileState.Replaced;
                }
            }
        }

        private void ReplaceFilesInFolder(string newSaveLocation, PhysicalDirectoryNode physicalFs, string root)
        {
            // Loop through all directories
            foreach (var dir in physicalFs.EnumerateDirectories())
                ReplaceFilesInFolder(newSaveLocation, (PhysicalDirectoryNode)physicalFs.GetDirectoryNode(dir.RelativePath), root);

            // Update files of this directory
            foreach (var file in physicalFs.EnumerateFiles())
            {
                var relativeFileName = file.RelativePath;
                physicalFs.AddFile(relativeFileName);
                var openedFile = physicalFs.GetFileNode(relativeFileName).Open();

                if (!Directory.Exists(Path.Combine(newSaveLocation, Path.GetDirectoryName(relativeFileName) ?? string.Empty)))
                    Directory.CreateDirectory(Path.Combine(newSaveLocation, Path.GetDirectoryName(relativeFileName) ?? string.Empty));

                var createdFile = File.Create(Path.Combine(newSaveLocation, relativeFileName));
                openedFile.CopyTo(createdFile);

                createdFile.Close();
                openedFile.Close();
            }
        }

        private KoreFileInfo ReopenFiles(KoreFileInfo kfi, FullPathNode fullPathTree, string tempFolder, bool isChild)
        {
            var guid = Guid.NewGuid().ToString();

            // Open this file
            KoreFileInfo newKfi;
            if (isChild)
            {
                var parentFiles = (kfi.Adapter as IArchiveAdapter)?.Files;
                var foundAfi = parentFiles.FirstOrDefault(x => x.FileName == fullPathTree.FullPath.Remove(0, kfi.FullPath.Length + 1));
                if (foundAfi == null)
                    throw new InvalidOperationException($"While reopening files, the ArchiveFileInfo with FullPath \"{fullPathTree.FullPath}\" couldn't be found.");

                newKfi = LoadFile(new KoreLoadInfo(foundAfi.FileData, foundAfi.FileName)
                {
                    LeaveOpen = true,
                    Adapter = kfi.Adapter,
                    FileSystem = NodeFactory.FromArchiveFileInfos((kfi.Adapter as IArchiveAdapter).Files)
                });
                // new VirtualFileSystem(kfi.Adapter as IArchiveAdapter, tempFolder)

                newKfi.ParentKfi = kfi;
                newKfi.ChildKfi = new List<KoreFileInfo>();
            }
            else
            {
                var openedFile = File.Open(fullPathTree.FullPath, FileMode.Open);

                newKfi = LoadFile(new KoreLoadInfo(openedFile, fullPathTree.FullPath)
                {
                    LeaveOpen = true,
                    Adapter = kfi.Adapter,
                    FileSystem = NodeFactory.FromDirectory(Path.GetDirectoryName(fullPathTree.FullPath))
                });

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
        //    var fs = new PhysicalFileSystem(RelativePath.Combine(tempFolder, guid));
        //    if (kfi.Adapter is IMultipleFiles multFileAdapter)
        //        multFileAdapter.FileSystem = fs;
        //    kfi.Adapter.LeaveOpen = false;
        //    var streaminfo = new StreamInfo { FileData = fs.CreateFile(RelativePath.GetFileName(kfi.StreamFileInfo.FileName)), FileName = RelativePath.GetFileName(kfi.StreamFileInfo.FileName) };
        //    (kfi.Adapter as ISaveFiles).Save(streaminfo, ksi.Version);

        //    // Replace files in adapter
        //    if (kfi.ParentKfi != null)
        //    {
        //        var parentArchiveAdapter = kfi.ParentKfi.Adapter as IArchiveAdapter;
        //        RecursiveUpdate(parentArchiveAdapter, fs, RelativePath.Combine(RelativePath.GetFullPath(tempFolder), guid));
        //    }
        //    else
        //    {
        //        // TODO: Implement save if no parent is given
        //        // Get intial directory
        //        var initialDir = RelativePath.GetDirectoryName(ksi.Kfi.FullPath);

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
        /// <param name="leaveFileStreamOpen">If the file stream should be left open.</param>
        /// <returns>True if file was closed, False otherwise.</returns>
        public bool CloseFile(KoreFileInfo kfi, bool leaveFileStreamOpen = false)
        {
            return CloseFile(kfi, leaveFileStreamOpen, true);
        }

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

            if (firstIteration)
            {
                kfi.ParentKfi?.ChildKfi.Remove(kfi);
            }

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
        /// <param name="kli">The file to be selected against.</param>
        /// <returns>Returns a working ILoadFiles plugin or null.</returns>
        private ILoadFiles SelectAdapter(KoreLoadInfo kli)
        {
            // Return an adapter that can Identify, whose extension matches that of our filename and successfully identifies the file.
            return _pluginLoader.GetAdapters<ILoadFiles>().
                Where(x => _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x) != null).Where(x => _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.
                    ToLower().TrimEnd(';').Split(';').Any(s => kli.FileName.ToLower().EndsWith(s.TrimStart('*')))
                ).FirstOrDefault(adapter => CheckAdapter(adapter, kli));
        }

        /// <summary>
        /// Does the actual identification with IIdentifyFiles and checks the availablity of the data stream.
        /// </summary>
        /// <param name="adapter">Adapter to identify with.</param>
        /// <param name="kli">Kore information for identification.</param>
        /// <returns>Returns if the adapter was capable of identifying the file.</returns>
        private bool CheckAdapter(ILoadFiles adapter, KoreLoadInfo kli)
        {
            if (!(adapter is IIdentifyFiles iif))
                return false;
            adapter.LeaveOpen = true;

            kli.FileData.Position = 0;
            var info = new StreamInfo { FileData = kli.FileData, FileName = kli.FileName };
            var res = iif.Identify(info, kli.FileSystem);

            if (!kli.FileData.CanRead)
                throw new InvalidOperationException($"Plugin with ID '{_pluginLoader.GetMetadata<PluginInfoAttribute>(adapter).ID}' closed the stream(s) while identifying the file(s).");

            return res;
        }

        /// <summary>
        /// Toggles an event for the UI to let the user select a blind adapter manually.
        /// </summary>
        /// <returns>The selected adapter or null.</returns>
        private ILoadFiles SelectAdapterManually(KoreLoadInfo kli)
        {
            var blindAdapters = _pluginLoader.GetAdapters<ILoadFiles>().Where(a => !(a is IIdentifyFiles)).ToList();

            var args = new IdentificationFailedEventArgs(kli.FileName, blindAdapters);
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
                //// Add all of the adapter filters
                //var allTypes = _pluginLoader.
                //    GetAdapters<ILoadFiles>().
                //    Select(x => new
                //    {
                //        Name = _pluginLoader.GetMetadata<PluginInfoAttribute>(x)?.Name,
                //        Extension = _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x)?.Extension.ToLower()
                //    }).
                //    OrderBy(o => o.Name).ToList();

                //// Add the special all supported files filter
                //if (allTypes.Count > 0)
                //    allTypes.Insert(0, new { Name = "All Supported Files", Extension = string.Join(";", allTypes.Select(x => x.Extension).Distinct()) });

                //// Add the special all files filter
                //allTypes.Add(new { Name = "All Files", Extension = "*.*" });

                var allTypes = AvaloniaFileFilters;

                return string.Join("|", allTypes.Select(x => $"{x.Key} ({string.Join(";",x.Value)})|{string.Join(";", x.Value)}"));
            }
        }

        /// <summary>
        /// Provides a complete set of file format names and extensions for open file dialogs.
        /// </summary>
        // TODO: Maybe make an own return type or make it tuples after switching to Net Core
        public IList<KeyValuePair<string, List<string>>> AvaloniaFileFilters
        {
            get
            {
                // Add all of the adapter filters
                var allTypes = _pluginLoader.
                    GetAdapters<ILoadFiles>().
                    Select(x => new KeyValuePair<string, List<string>>(_pluginLoader.GetMetadata<PluginInfoAttribute>(x)?.Name, new List<string> { _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x)?.Extension.ToLower() })).
                    OrderBy(o => o.Key).ToList();

                // Add the special all supported files filter
                if (allTypes.Count > 0)
                    allTypes.Insert(0, new KeyValuePair<string, List<string>>("All Supported Files", allTypes.SelectMany(x => x.Value).ToList()));

                // Add the special all files filter
                allTypes.Add(new KeyValuePair<string, List<string>>("All Files", new List<string> { "*.*" }));

                return allTypes;
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
            var allTypes = _pluginLoader.GetAdapters<T>().Select(x => new { _pluginLoader.GetMetadata<PluginInfoAttribute>(x).Name, Extension = _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower() }).OrderBy(o => o.Name).ToList();

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
            return _pluginLoader.GetAdapters<T>().Select(x => _pluginLoader.GetMetadata<PluginExtensionInfoAttribute>(x).Extension.ToLower().TrimStart('*')).OrderBy(o => o);
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
            return _pluginLoader.GetAdapters<ILoadFiles>();
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
