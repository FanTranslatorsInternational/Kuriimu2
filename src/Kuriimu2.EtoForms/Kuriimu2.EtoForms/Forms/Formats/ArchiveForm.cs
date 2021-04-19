using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;
using Kore.Managers.Plugins;
using Kuriimu2.EtoForms.Forms.Dialogs;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Resources;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    public partial class ArchiveForm : Panel, IKuriimuForm
    {
        private readonly ArchiveFormInfo _formInfo;
        private readonly FileManager _fileManager;

        private readonly IList<IArchiveFileInfo> _openingFiles;
        private readonly HashSet<UPath> _changedDirectories;

        private readonly SearchTerm _searchTerm;
        private readonly AsyncOperation _asyncOperation;

        private IFileSystem _archiveFileSystem;
        private UPath _selectedPath;

        #region Hot Keys

        private const Keys SaveHotKey = Keys.Control | Keys.S;
        private const Keys SaveAsHotKey = Keys.F12;

        #endregion

        #region Constants

        private static readonly Color ColorDefaultState = KnownColors.Black;
        private static readonly Color ColorChangedState = KnownColors.Orange;

        #endregion

        public ArchiveForm(ArchiveFormInfo formInfo, FileManager fileManager)
        {
            InitializeComponent();

            _formInfo = formInfo;
            _fileManager = fileManager;

            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            _openingFiles = new List<IArchiveFileInfo>();

            _changedDirectories = new HashSet<UPath>();
            _selectedPath = UPath.Root;

            _searchTerm = new SearchTerm(searchTextBox);
            _searchTerm.TextChanged += searchTerm_TextChanged;

            _asyncOperation = new AsyncOperation();
            _asyncOperation.Started += asyncOperation_Started;
            _asyncOperation.Finished += asyncOperation_Finished;

            folderView.Expanded += folderView_Expanded;
            folderView.Collapsed += folderView_Collapsed;
            folderView.CellFormatting += folderView_CellFormatting;
            folderView.SelectedItemChanged += folderView_SelectedItemChanged;

            fileView.SelectedItemsChanged += fileView_SelectedItemsChanged;
            fileView.CellDoubleClick += fileView_CellDoubleClick;
            fileView.CellFormatting += fileView_CellFormatting;

            searchClearCommand.Executed += searchClearCommand_Executed;
            cancelCommand.Executed += cancelCommand_Executed;

            saveCommand.Executed += SaveCommand_Executed;
            saveAsCommand.Executed += SaveAsCommand_Executed;

            openCommand.Executed += openCommand_Executed;
            extractFileCommand.Executed += extractFileCommand_Executed;
            replaceFileCommand.Executed += replaceFileCommand_Executed;
            renameFileCommand.Executed += RenameFileCommand_Executed;
            deleteFileCommand.Executed += DeleteFileCommand_Executed;

            extractDirectoryCommand.Executed += extractDirectoryCommand_Executed;
            replaceDirectoryCommand.Executed += replaceDirectoryCommand_Executed;
            renameDirectoryCommand.Executed += renameDirectoryCommand_Executed;
            addDirectoryCommand.Executed += addDirectoryCommand_Executed;
            deleteDirectoryCommand.Executed += deleteDirectoryCommand_Executed;

            UpdateProperties();
            LoadDirectories();
            UpdateFiles(_selectedPath);
        }

        #region Forminterface methods

        public bool HasRunningOperations()
        {
            return _asyncOperation.IsRunning;
        }

        #endregion

        #region Update

        public void UpdateForm()
        {
            Application.Instance.Invoke(() =>
            {
                // Update root name
                if (folders.Count > 0)
                    ((TreeGridItem)folders[0]).Values[1] = _formInfo.FileState.FilePath.ToRelative().FullName;

                // Update form information
                UpdateProperties();
                UpdateDirectories();
                UpdateFiles(_selectedPath);
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateProperties()
        {
            var canSave = _formInfo.CanSave;

            // Menu
            saveButton.Enabled = canSave && !_asyncOperation.IsRunning;
            saveAsButton.Enabled = canSave && _formInfo.FileState.ParentFileState == null && !_asyncOperation.IsRunning;

            // Toolbar
            extractButton.Enabled = !_asyncOperation.IsRunning;
            replaceButton.Enabled = _formInfo.CanReplaceFiles && canSave && !_asyncOperation.IsRunning;
            renameButton.Enabled = _formInfo.CanRenameFiles && canSave && !_asyncOperation.IsRunning;
            deleteButton.Enabled = _formInfo.CanDeleteFiles && canSave && !_asyncOperation.IsRunning;
        }

        private void UpdateDirectories()
        {
            if (folders.Count <= 0)
                return;

            // If an update is triggered ba a parent, and therefore got this instance saved
            // We need to clear the changed directories
            if (!_formInfo.FileState.StateChanged)
                _changedDirectories.Clear();

            folderView.ReloadItem(folders[0]);
        }

        private void UpdateFiles(UPath path)
        {
            files.Clear();

            foreach (var file in _archiveFileSystem.EnumerateFiles(path, _searchTerm.Get()))
            {
                var fileEntry = (AfiFileEntry)_archiveFileSystem.GetFileEntry(file);
                files.Add(new FileElement(fileEntry.ArchiveFileInfo));
            }
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateFileContextMenu()
        {
            var selectedItem = fileView.SelectedItems.FirstOrDefault();
            if (selectedItem == null)
                return;

            // Get current state arguments
            var isLoadLocked = IsFileLocked(selectedItem.ArchiveFileInfo, true);
            var isStateLocked = IsFileLocked(selectedItem.ArchiveFileInfo, false);

            var canExtractFiles = !isStateLocked && !_asyncOperation.IsRunning;
            var canReplaceFiles = _formInfo.CanReplaceFiles && !isLoadLocked && !_asyncOperation.IsRunning;
            var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;

            Application.Instance.Invoke(() =>
            {
                // Update Open with menu item
                openWithMenuItem.Items.Clear();
                foreach (var pluginId in selectedItem.ArchiveFileInfo.PluginIds ?? Array.Empty<Guid>())
                {
                    var filePluginLoader = _fileManager.GetFilePluginLoaders().FirstOrDefault(x => x.Exists(pluginId));
                    var filePlugin = filePluginLoader?.GetPlugin(pluginId);

                    if (filePlugin == null)
                        continue;

                    openWithCommand = new Command { MenuText = filePlugin.Metadata.Name, Tag = (selectedItem, pluginId) };
                    openWithCommand.Executed += openWithCommand_Executed;

                    openWithMenuItem.Items.Add(openWithCommand);
                }

                // Update context menu
                openCommand.Enabled = !_asyncOperation.IsRunning;
                openWithMenuItem.Enabled = openWithMenuItem.Items.Count > 0 && !_asyncOperation.IsRunning;

                extractFileCommand.Enabled = canExtractFiles;
                replaceFileCommand.Enabled = canReplaceFiles;
                renameFileCommand.Enabled = canRenameFiles;
                deleteFileCommand.Enabled = canDeleteFiles;
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateDirectoryContextMenu()
        {
            var canExtractFiles = !_asyncOperation.IsRunning;
            var canReplaceFiles = _formInfo.CanReplaceFiles && !_asyncOperation.IsRunning;
            var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;
            var canAddFiles = _formInfo.CanAddFiles && !_asyncOperation.IsRunning;

            Application.Instance.Invoke(() =>
            {
                extractDirectoryCommand.Enabled = canExtractFiles;
                replaceDirectoryCommand.Enabled = canReplaceFiles;
                renameDirectoryCommand.Enabled = canRenameFiles;
                deleteDirectoryCommand.Enabled = canDeleteFiles;
                addDirectoryCommand.Enabled = canAddFiles;
            });
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads the whole directory tree and add it to the folder view.
        /// </summary>
        private void LoadDirectories()
        {
            // Clear tree view
            folders.Clear();

            // Load tree into tree items
            var rootItem = new TreeGridItem(ImageResources.Misc.Archive, _formInfo.FileState.FilePath.ToRelative().FullName) { Expanded = true };
            foreach (var directory in _archiveFileSystem.EnumerateAllDirectories(UPath.Root, _searchTerm.Get())
                .Concat(_archiveFileSystem.EnumerateAllFiles(UPath.Root, _searchTerm.Get()).Select(x => x.GetDirectory()))
                .Distinct())
            {
                directory.Split().Aggregate(rootItem, (node, currentDirectory) =>
                {
                    var existingDirectory = (TreeGridItem)node.Children.FirstOrDefault(x =>
                       (string)((TreeGridItem)x).Values[1] == currentDirectory);

                    if (existingDirectory != null)
                        return existingDirectory;

                    var directoryItem = new TreeGridItem(ImageResources.Misc.Directory, currentDirectory);
                    node.Children.Add(directoryItem);

                    return directoryItem;
                });
            }

            // Add root item to tree view
            folders.Add(rootItem);
            folderView.ReloadItem(rootItem);
        }

        #endregion

        #region Events

        #region folderView

        private void folderView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (folderView.SelectedItem == null)
                return;

            var gridItem = (TreeGridItem)folderView.SelectedItem;
            _selectedPath = GetAbsolutePath(gridItem);

            UpdateDirectoryContextMenu();
            UpdateFiles(_selectedPath);
        }

        private void folderView_Expanded(object sender, TreeGridViewItemEventArgs e)
        {
            var gridItem = (TreeGridItem)e.Item;
            gridItem.Values[0] = ImageResources.Misc.DirectoryOpen;

            folderView.ReloadItem(gridItem, false);
        }

        private void folderView_Collapsed(object sender, TreeGridViewItemEventArgs e)
        {
            var gridItem = (TreeGridItem)e.Item;
            gridItem.Values[0] = ImageResources.Misc.Directory;

            folderView.ReloadItem(gridItem, false);
        }

        private void folderView_CellFormatting(object sender, GridCellFormatEventArgs e)
        {
            if (e.Item == null)
                return;

            var gridItem = (TreeGridItem)e.Item;
            var path = GetAbsolutePath(gridItem);
            e.ForegroundColor = _changedDirectories.Contains(path) ? ColorChangedState : ColorDefaultState;
        }

        #endregion

        #region fileView

        private void fileView_SelectedItemsChanged(object sender, EventArgs e)
        {
            UpdateFileContextMenu();
        }

        private async void fileView_CellDoubleClick(object sender, GridCellMouseEventArgs e)
        {
            if (e.Item == null)
                return;

            await OpenSelectedFiles();
        }

        private void fileView_CellFormatting(object sender, GridCellFormatEventArgs e)
        {
            if (e.Item == null)
                return;

            var element = (FileElement)e.Item;
            var isChanged = element.ArchiveFileInfo.ContentChanged || _formInfo.FileState.ArchiveChildren.Where(x => x.StateChanged).Any(x => x.FilePath == element.ArchiveFileInfo.FilePath);
            e.ForegroundColor = isChanged ? ColorChangedState : ColorDefaultState;
        }

        #endregion

        #region searchTerm

        private void searchClearCommand_Executed(object sender, EventArgs e)
        {
            _searchTerm.Clear();
        }

        private void searchTerm_TextChanged(object sender, EventArgs e)
        {
            LoadDirectories();
            UpdateFiles(UPath.Root);
        }

        #endregion

        #region asyncOperation

        private void asyncOperation_Started(object sender, EventArgs e)
        {
            UpdateFileContextMenu();
            UpdateDirectoryContextMenu();
            UpdateProperties();
        }

        private void asyncOperation_Finished(object sender, EventArgs e)
        {
            UpdateFileContextMenu();
            UpdateDirectoryContextMenu();
            UpdateProperties();
        }

        private void cancelCommand_Executed(object sender, EventArgs e)
        {
            _asyncOperation.Cancel();
        }

        #endregion

        #region fileContextMenu

        private async void openCommand_Executed(object sender, EventArgs e)
        {
            await OpenSelectedFiles();
        }

        private async void openWithCommand_Executed(object sender, EventArgs e)
        {
            var (fileElement, pluginId) = ((FileElement, Guid))((Command)sender).Tag;
            if (!await OpenFile(fileElement.ArchiveFileInfo, pluginId))
                _formInfo.FormCommunicator.ReportStatus(false, $"File could not be opened with plugin '{pluginId}'.");
        }

        private async void extractFileCommand_Executed(object sender, EventArgs e)
        {
            await ExtractSelectedFiles();
        }

        private async void replaceFileCommand_Executed(object sender, EventArgs e)
        {
            await ReplaceSelectedFiles();
        }

        private async void RenameFileCommand_Executed(object sender, EventArgs e)
        {
            await RenameSelectedFiles();
        }

        private async void DeleteFileCommand_Executed(object sender, EventArgs e)
        {
            await DeleteSelectedFiles();
        }

        #endregion

        #region directoryContextMenu

        private async void extractDirectoryCommand_Executed(object sender, EventArgs e)
        {
            await ExtractSelectedDirectory();
        }

        private async void replaceDirectoryCommand_Executed(object sender, EventArgs e)
        {
            await ReplaceSelectedDirectory();
        }

        private async void renameDirectoryCommand_Executed(object sender, EventArgs e)
        {
            await RenameSelectedDirectory();
        }

        private async void addDirectoryCommand_Executed(object sender, EventArgs e)
        {
            await AddFilesToSelectedItem();
        }

        private async void deleteDirectoryCommand_Executed(object sender, EventArgs e)
        {
            await DeleteSelectedDirectory();
        }

        #endregion

        #endregion

        #region Open Files

        private Task OpenSelectedFiles()
        {
            return OpenFiles(fileView.SelectedItems.ToArray());
        }

        private async Task OpenFiles(IList<FileElement> fileElements)
        {
            if (fileElements == null)
                return;

            foreach (var file in fileElements.Select(x => x.ArchiveFileInfo))
            {
                var pluginIds = file.PluginIds ?? Array.Empty<Guid>();

                if (pluginIds.Any())
                {
                    // Opening by plugin id
                    var opened = false;
                    foreach (var pluginId in pluginIds)
                    {
                        if (_openingFiles.Contains(file))
                        {
                            _formInfo.FormCommunicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                            continue;
                        }

                        _openingFiles.Add(file);
                        if (await OpenFile(file, pluginId))
                        {
                            _openingFiles.Remove(file);

                            opened = true;
                            break;
                        }

                        _openingFiles.Remove(file);
                    }

                    if (opened)
                        continue;
                }

                // Use automatic identification if no preset plugin could open the file
                if (_openingFiles.Contains(file))
                {
                    _formInfo.FormCommunicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                    continue;
                }

                _openingFiles.Add(file);
                if (!await OpenFile(file))
                    _formInfo.FormCommunicator.ReportStatus(false, "File couldn't be opened.");

                _openingFiles.Remove(file);
            }
        }

        private Task<bool> OpenFile(IArchiveFileInfo afi, Guid pluginId = default)
        {
            return pluginId == default ? _formInfo.FormCommunicator.Open(afi) : _formInfo.FormCommunicator.Open(afi, pluginId);
        }

        #endregion

        #region Save Files

        private void SaveCommand_Executed(object sender, EventArgs e)
        {
            Save(false);
        }

        private void SaveAsCommand_Executed(object sender, EventArgs e)
        {
            Save(true);
        }

        private async void Save(bool saveAs)
        {
            var wasSuccessful = await _formInfo.FormCommunicator.Save(saveAs);
            if (!wasSuccessful)
                return;

            _changedDirectories.Clear();
            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            _selectedPath = UPath.Root;

            await Application.Instance.InvokeAsync(() =>
            {
                LoadDirectories();
                UpdateFiles(UPath.Root);
                UpdateProperties();

                _formInfo.FormCommunicator.Update(true, false);
            });
        }

        #endregion

        #region Extraction

        #region Extract Files

        private Task ExtractSelectedFiles()
        {
            return ExtractFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private async Task ExtractFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to extract.");
                return;
            }

            // Select folder or file
            var selectedPath = files.Count > 1 ? SelectFolder() : SaveFile(files[0].FilePath.GetName());
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No target selected.");
                return;
            }
            
            // Use containing directory as root if a file was selected
            var extractRoot = files.Count > 1 ? selectedPath : selectedPath.GetDirectory();

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(extractRoot.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Extract files", count++, files.Count);

                    if (IsFileLocked(file, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        // Use in-archive filename if a folder was selected, use selected filename if a file was selected
                        var extractName = files.Count > 1 ? file.FilePath.GetName() : selectedPath.GetName();
                        newFileStream = destinationFileSystem.OpenFile(extractName, FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    var currentFileStream = await file.GetFileData();

                    currentFileStream.CopyTo(newFileStream);

                    newFileStream.Close();
                }
            });
            _formInfo.Progress.ReportProgress("Extract files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File extraction cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) extracted successfully.");
        }

        #endregion

        #region Extract Directories

        private Task ExtractSelectedDirectory()
        {
            return ExtractDirectory((TreeGridItem)folderView.SelectedItem);
        }

        private async Task ExtractDirectory(TreeGridItem item)
        {
            var itemPath = GetAbsolutePath(item);
            var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath, _searchTerm.Get()).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to extract.");
                return;
            }

            // Select folder
            var extractPath = SelectFolder();
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No folder selected.");
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var subFolder = item == folders[0] ? GetRootName() : GetAbsolutePath(item).ToRelative();
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem((extractPath / subFolder).FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Extract files", count++, filePaths.Length);

                    var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
                    if (IsFileLocked(afi, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        newFileStream = destinationFileSystem.OpenFile(filePath, FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    var currentFileStream = afi.GetFileData().Result;

                    currentFileStream.CopyTo(newFileStream);

                    newFileStream.Close();
                }
            });
            _formInfo.Progress.ReportProgress("Extract files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File extraction cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) extracted successfully.");
        }

        #endregion

        #endregion

        #region Replacement

        #region Replace Files

        private Task ReplaceSelectedFiles()
        {
            return ReplaceFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private async Task ReplaceFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to replace.");
                return;
            }

            // Select destination
            UPath replaceDirectory;
            UPath replaceFileName;
            if (files.Count == 1)
            {
                var selectedPath = OpenFile(files[0].FilePath.GetName());
                if (selectedPath.IsNull || selectedPath.IsEmpty)
                {
                    _formInfo.FormCommunicator.ReportStatus(false, "No file selected.");
                    return;
                }

                replaceDirectory = selectedPath.GetDirectory();
                replaceFileName = selectedPath.GetName();
            }
            else
            {
                var selectedPath = SelectFolder();
                if (selectedPath.IsNull || selectedPath.IsEmpty)
                {
                    _formInfo.FormCommunicator.ReportStatus(false, "No folder selected.");
                    return;
                }

                replaceDirectory = selectedPath;
                replaceFileName = UPath.Empty;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replaceDirectory.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Replace files", count++, files.Count);

                    if (IsFileLocked(file, true))
                        continue;

                    var filePath = replaceFileName.IsEmpty ? file.FilePath.GetName() : replaceFileName;
                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = sourceFileSystem.OpenFile(filePath);

                    if (_formInfo.CanReplaceFiles)
                    {
                        //XXX this check is the equivalent of the 'as' cast here previously
                        //  However, the 'unsupported' case doesn't seem to be handled anywhere
                        // TODO this cast smells, should IFileState/IPluginState be generified?
                        ((IArchiveState)_formInfo.FileState.PluginState).ReplaceFile(file, currentFileStream);
                    }

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress("Replace files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File replacement cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) replaced successfully.");

            UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
            UpdateDirectories();

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Replace Directories

        private Task ReplaceSelectedDirectory()
        {
            return ReplaceDirectory((TreeGridItem)folderView.SelectedItem);
        }

        private async Task ReplaceDirectory(TreeGridItem item)
        {
            var itemPath = GetAbsolutePath(item);
            var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to replace.");
                return;
            }

            // Select folder
            var replacePath = SelectFolder();
            if (replacePath.IsNull || replacePath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No folder selected.");
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replacePath.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Replace files", count++, filePaths.Length);

                    var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
                    if (IsFileLocked(afi, true))
                        continue;

                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = sourceFileSystem.OpenFile(filePath);
                    if (_formInfo.CanReplaceFiles)
                    {
                        //XXX this check is the equivalent of the 'as' cast here previously
                        //  However, the 'unsupported' case doesn't seem to be handled anywhere
                        // TODO this cast smells, should IFileState/IPluginState be generified?
                        ((IArchiveState)_formInfo.FileState.PluginState).ReplaceFile(afi, currentFileStream);
                    }

                    AddChangedDirectory(afi.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress("Replace files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File replacement cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) replaced successfully.");

            UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
            UpdateDirectories();

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #endregion

        #region Renaming

        #region Rename Files

        private Task RenameSelectedFiles()
        {
            return RenameFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private async Task RenameFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to rename.");
                return;
            }

            // Rename elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Rename files", count++, files.Count);

                    // Select new name
                    var newName = Application.Instance.Invoke(() =>
                    {
                        var inputBox = new InputBoxDialog($"Select a new name for '{file.FilePath.GetName()}'",
                            "Rename file", file.FilePath.GetName());
                        return inputBox.ShowModal(this);
                    });

                    if (string.IsNullOrEmpty(newName))
                        continue;

                    // Rename possibly open file in main form
                    _formInfo.FormCommunicator.Rename(file, file.FilePath.GetDirectory() / newName);

                    // Rename file in archive
                    _archiveFileSystem.MoveFile(file.FilePath, file.FilePath.GetDirectory() / newName);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress("Rename files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File renaming cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) renamed successfully.");

            UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
            UpdateDirectories();
        }

        #endregion

        #region Rename Directories

        private Task RenameSelectedDirectory()
        {
            return RenameDirectory((TreeGridItem)folderView.SelectedItem);
        }

        private async Task RenameDirectory(TreeGridItem item)
        {
            var itemPath = GetAbsolutePath(item);
            var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to rename.");
                return;
            }

            // Select new directory name
            var inputBox = new InputBoxDialog($"Select a new name for '{GetItemName(item)}'",
                "Rename directory", GetItemName(item));
            var newName = inputBox.ShowModal(this);

            if (string.IsNullOrEmpty(newName))
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No new name given.");
                return;
            }

            // Rename elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var newDirectoryPath = GetAbsolutePath(item).GetDirectory() / newName;

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Rename files", count++, filePaths.Length);

                    // Rename possibly open file in main form
                    var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
                    _formInfo.FormCommunicator.Rename(afi, newDirectoryPath / filePath.ToRelative());

                    // Rename file in archive
                    _archiveFileSystem.MoveFile(afi.FilePath, newDirectoryPath / filePath.ToRelative());

                    AddChangedDirectory(afi.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress("Rename files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File renaming cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) renamed successfully.");

            SetItemName(item, newName);

            UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
            UpdateDirectories();
        }

        #endregion

        #endregion

        #region Deletion

        #region Delete Files

        private Task DeleteSelectedFiles()
        {
            return DeleteFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private async Task DeleteFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to delete.");
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Delete files", count++, files.Count);

                    _archiveFileSystem.DeleteFile(file.FilePath);
                }
            });
            _formInfo.Progress.ReportProgress("Delete files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File deletion cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) deleted successfully.");

            UpdateDirectories();
            UpdateFiles(UPath.Root);

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Delete Directories

        private Task DeleteSelectedDirectory()
        {
            return DeleteDirectory((TreeGridItem)folderView.SelectedItem);
        }

        private async Task DeleteDirectory(TreeGridItem item)
        {
            var itemPath = GetAbsolutePath(item);
            var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, "No files to delete.");
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Delete files", count++, filePaths.Length);

                    _archiveFileSystem.DeleteFile(itemPath / filePath);
                }
            });

            _archiveFileSystem.DeleteDirectory(itemPath, true);

            _formInfo.Progress.ReportProgress("Delete files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File deletion cancelled.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) deleted successfully.");

            LoadDirectories();
            UpdateFiles(UPath.Root);

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #endregion

        #region Adding

        private Task AddFilesToSelectedItem()
        {
            return AddFiles((TreeGridItem)folderView.SelectedItem);
        }

        private async Task AddFiles(TreeGridItem item)
        {
            // Select folder
            var selectedPath = SelectFolder();
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No folder selected.");
                return;
            }

            // Add elements
            var subFolder = GetAbsolutePath(item);
            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(selectedPath.FullName, _formInfo.FileState.StreamManager);

            var elements = sourceFileSystem.EnumerateAllFiles(UPath.Root).ToArray();
            if (elements.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(false, "No files to add.");
                return;
            }

            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            var filesNotAdded = false;
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in elements)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress("Add files", count++, elements.Length);

                    // Do not add file if it already exists
                    // This would be replacement and is not part of this operation
                    if(_archiveFileSystem.FileExists(subFolder / filePath.ToRelative()))
                        continue;

                    // TODO: This will currently copy files to memory, instead of just using a reference to any more memory efficient stream (like FileStream)
                    Stream createdFile;
                    try
                    {
                        // The plugin can throw if a file is not addable
                        createdFile = _archiveFileSystem.OpenFile(subFolder / filePath.ToRelative(), FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception e)
                    {
                        _formInfo.Logger.Fatal(e, "Could not add the file {0}", filePath);
                        filesNotAdded = true;

                        continue;
                    }
                    var sourceFile = sourceFileSystem.OpenFile(filePath);
                    sourceFile.CopyTo(createdFile);

                    sourceFile.Close();
                }
            });
            _formInfo.Progress.ReportProgress("Add files", 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, "File adding cancelled.");
            else if (filesNotAdded)
                _formInfo.FormCommunicator.ReportStatus(true, "Some file(s) could not be added successfully. Refer to the log for more information.");
            else
                _formInfo.FormCommunicator.ReportStatus(true, "File(s) added successfully.");

            UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Support

        private UPath GetAbsolutePath(TreeGridItem item)
        {
            if (item?.Parent == null)
                return UPath.Root;

            return GetAbsolutePath((TreeGridItem)item.Parent) / (string)item.Values[1];
        }

        private bool IsFileLocked(IArchiveFileInfo afi, bool lockOnLoaded)
        {
            var absolutePath = _formInfo.FileState.AbsoluteDirectory / _formInfo.FileState.FilePath.ToRelative() / afi.FilePath.ToRelative();

            var isLoaded = _fileManager.IsLoaded(absolutePath);
            if (!isLoaded)
                return false;

            if (lockOnLoaded)
                return true;

            var openedState = _fileManager.GetLoadedFile(absolutePath);
            return openedState.StateChanged;
        }

        private UPath OpenFile(string fileName)
        {
            return SelectFile<OpenFileDialog>(fileName);
        }
        
        private UPath SaveFile(string fileName)
        {
            return SelectFile<SaveFileDialog>(fileName);
        }
        
        private UPath SelectFile<DialogType>(string fileName) where DialogType : FileDialog, new()
        {
            var ofd = new DialogType
            {
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                FileName = fileName
            };
            
            var result = ofd.ShowDialog(this) == DialogResult.Ok ? ofd.FileName : UPath.Empty;

            if (result != UPath.Empty)
            {
                Settings.Default.LastDirectory = result.FullName;
                Settings.Default.Save();
            }

            return result;
        }

        private UPath SelectFolder()
        {
            var sfd = new SelectFolderDialog
            {
                Directory = Settings.Default.LastDirectory
            };
            var result = sfd.ShowDialog(this) == DialogResult.Ok ? sfd.Directory : UPath.Empty;

            if (result != UPath.Empty)
            {
                Settings.Default.LastDirectory = result.FullName;
                Settings.Default.Save();
            }

            return result;
        }

        private void AddChangedDirectory(UPath path)
        {
            while (path != UPath.Root && !path.IsEmpty)
            {
                _changedDirectories.Add(path);
                path = path.GetDirectory();
            }
        }

        private string GetRootName()
        {
            return GetItemName(folders.FirstOrDefault() as TreeGridItem);
        }

        private string GetItemName(TreeGridItem item)
        {
            return (string)item?.Values[1];
        }

        private void SetItemName(TreeGridItem item, string name)
        {
            item.Values[1] = name;
        }

        #endregion
    }
}
