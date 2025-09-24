using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Models.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.FileSystem;
using Konnect.Management.Streams;
using Kuriimu2.ImGui.Extensions;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using Veldrid;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ArchiveForm : Component, IKuriimuForm
    {
        private static readonly KeyCommand DeleteCommand = new(Key.Delete);

        private readonly ArchiveFormInfo _formInfo;
        private readonly IPluginManager _pluginManager;
        private readonly IFileManager _fileManager;
        private IFileSystem _fileSystem;

        private readonly IList<IArchiveFile> _openingFiles;

        private readonly HashSet<IArchiveFile> _changedFiles;
        private readonly HashSet<UPath> _changedDirectories;
        private readonly HashSet<UPath> _openedDirectories;
        private UPath _selectedPath;

        private readonly AsyncOperation _asyncOperation;
        private readonly SearchTerm _searchTerm;
        private bool _saveLock;

        private Component? _lastSelectedComponent;

        private const string CouldNotAddFileLog_ = "Could not add file: {0}";

        public ArchiveForm(ArchiveFormInfo formInfo, IPluginManager pluginManager, IFileManager fileManager)
        {
            InitializeComponent();

            _formInfo = formInfo;
            _pluginManager = pluginManager;
            _fileManager = fileManager;
            _fileSystem = FileSystemFactory.CreateArchivePluginFileSystem(_formInfo.FileState);

            _openingFiles = new System.Collections.Generic.List<IArchiveFile>();

            _changedFiles = new HashSet<IArchiveFile>();
            _changedDirectories = new HashSet<UPath>();
            _openedDirectories = new HashSet<UPath>();

            _asyncOperation = new AsyncOperation();
            _searchTerm = new SearchTerm(_searchBox);

            #region Events

            _saveBtn.Clicked += _saveBtn_Clicked;
            _saveAsBtn.Clicked += _saveAsBtn_Clicked;

            _searchTerm.TextChanged += _searchTerm_TextChanged;
            _clearButton.Clicked += _clearButton_Clicked;

            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;
            _treeView.NodeExpanded += _treeView_NodeExpanded;
            _treeView.NodeCollapsed += _treeView_NodeCollapsed;

            _fileView.SelectedRowsChanged += _fileView_SelectedRowsChanged;
            _fileView.DoubleClicked += _fileView_DoubleClicked;

            _directoryContext.Show += _directoryContext_Show;
            _fileContext.Show += _fileContext_Show;

            _openFileButton.Clicked += _openFileButton_Clicked;

            _extractFileButton.Clicked += _extractFileButton_Clicked;
            _replaceFileButton.Clicked += _replaceFileButton_Clicked;
            _renameFileButton.Clicked += _renameFileButton_Clicked;
            _deleteFileButton.Clicked += _deleteFileButton_Clicked;

            _extractDirectoryButton.Clicked += _extractDirectoryButton_Clicked;
            _replaceDirectoryButton.Clicked += _replaceDirectoryButton_Clicked;
            _renameDirectoryButton.Clicked += _renameDirectoryButton_Clicked;
            _addDirectoryButton.Clicked += _addDirectoryButton_Clicked;
            _deleteDirectoryButton.Clicked += _deleteDirectoryButton_Clicked;

            _cancelBtn.Clicked += _cancelBtn_Clicked;

            _asyncOperation.Started += _asyncOperation_Started;
            _asyncOperation.Finished += _asyncOperation_Finished;

            #endregion

            #region Updates

            UpdateFileTree();
            _treeView.SelectedNode = _treeView.Nodes.FirstOrDefault();

            UpdateForm();

            #endregion
        }

        private void _fileView_SelectedRowsChanged(object? sender, EventArgs e)
        {
            _lastSelectedComponent = _fileView;
        }

        #region Events

        #region Save

        private void _saveBtn_Clicked(object sender, EventArgs e)
        {
            Save(false);
        }

        private void _saveAsBtn_Clicked(object sender, EventArgs e)
        {
            Save(true);
        }

        #endregion

        #region Search

        private void _searchTerm_TextChanged(object sender, EventArgs e)
        {
            UpdateFileTree();
        }

        private void _clearButton_Clicked(object sender, EventArgs e)
        {
            _searchTerm.Clear();

            UpdateFileTree();
        }

        #endregion

        #region TreeView

        private void _treeView_SelectedNodeChanged(object sender, EventArgs e)
        {
            if (_treeView.SelectedNode != null)
                _selectedPath = _treeView.SelectedNode.Data.AbsolutePath;

            UpdateFileView(_treeView.SelectedNode?.Data);

            _lastSelectedComponent = _treeView;
        }

        private void _treeView_NodeCollapsed(object sender, NodeEventArgs<DirectoryEntry> e)
        {
            _openedDirectories.Remove(e.Node.Data.AbsolutePath);
        }

        private void _treeView_NodeExpanded(object sender, NodeEventArgs<DirectoryEntry> e)
        {
            _openedDirectories.Add(e.Node.Data.AbsolutePath);
        }

        #endregion

        #region FileView

        private async void _fileView_DoubleClicked(object? sender, EventArgs e)
        {
            if (_fileView.SelectedRows.Count <= 0)
                return;

            await OpenFiles([_fileView.SelectedRows[0].Data]);
        }

        #endregion

        #region ContextMenu

        private void _fileContext_Show(object sender, EventArgs e)
        {
            var selectedItem = _fileView.SelectedRows.FirstOrDefault();
            if (selectedItem == null)
                return;

            _lastSelectedComponent = _fileView;

            // Get current state arguments
            var isLoadLocked = IsFileLocked(selectedItem.Data.File, true);
            var isStateLocked = IsFileLocked(selectedItem.Data.File, false);

            var canExtractFiles = !isStateLocked && !_asyncOperation.IsRunning && !_saveLock;
            var canReplaceFiles = _formInfo.CanReplaceFiles && !_saveLock && !isLoadLocked && !_asyncOperation.IsRunning;
            var canRenameFiles = _formInfo.CanRenameFiles && !_saveLock && !_asyncOperation.IsRunning;
            var canDeleteFiles = CanDeleteFiles();

            // Update Open With menu node
            _openWithFileMenu.Items.Clear();

            foreach (var pluginId in selectedItem.Data.File.PluginIds ?? [])
            {
                var filePlugin = _pluginManager.GetPlugin<IFilePlugin>(pluginId);

                if (filePlugin == null)
                    continue;

                var pluginButton = new MenuBarButton { Text = filePlugin.Metadata.Name };
                pluginButton.Clicked += async (s, ev) =>
                {
                    if (!await OpenFile(selectedItem.Data.File, pluginId))
                        _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadErrorPlugin(pluginId));
                };

                _openWithFileMenu.Items.Add(pluginButton);
            }

            // Update context menu
            _openFileButton.Enabled = !_asyncOperation.IsRunning && !_saveLock;
            _openWithFileMenu.Enabled = _openWithFileMenu.Items.Count > 0 && !_asyncOperation.IsRunning && !_saveLock;

            _extractFileButton.Enabled = canExtractFiles;
            _replaceFileButton.Enabled = canReplaceFiles;
            _renameFileButton.Enabled = canRenameFiles;
            _deleteFileButton.Enabled = canDeleteFiles;
        }

        private void _directoryContext_Show(object sender, EventArgs e)
        {
            _lastSelectedComponent = _treeView;

            var canExtractDirectories = !_asyncOperation.IsRunning && !_saveLock;
            var canReplaceDirectories = _formInfo.CanReplaceFiles && !_saveLock && !_asyncOperation.IsRunning;
            var canRenameDirectories = _formInfo.CanRenameFiles && !_saveLock && !_asyncOperation.IsRunning;
            var canDeleteDirectories = CanDeleteDirectories();
            var canAddDirectories = _formInfo.CanAddFiles && !_saveLock && !_asyncOperation.IsRunning;

            _extractDirectoryButton.Enabled = canExtractDirectories;
            _replaceDirectoryButton.Enabled = canReplaceDirectories;
            _renameDirectoryButton.Enabled = canRenameDirectories;
            _deleteDirectoryButton.Enabled = canDeleteDirectories;
            _addDirectoryButton.Enabled = canAddDirectories;
        }

        #endregion

        #region ContextMenu Items

        private async void _openFileButton_Clicked(object sender, EventArgs e)
        {
            await OpenFiles(_fileView.SelectedRows.Select(x => x.Data).ToArray());
        }

        private async void _extractFileButton_Clicked(object sender, EventArgs e)
        {
            await ExtractSelectedFiles();
        }

        private async void _replaceFileButton_Clicked(object sender, EventArgs e)
        {
            await ReplaceSelectedFiles();
        }

        private async void _renameFileButton_Clicked(object sender, EventArgs e)
        {
            await RenameSelectedFiles();
        }

        private async void _deleteFileButton_Clicked(object sender, EventArgs e)
        {
            if (CanDeleteFiles() && _lastSelectedComponent == _fileView)
                await DeleteSelectedFiles();
        }

        private async void _extractDirectoryButton_Clicked(object sender, EventArgs e)
        {
            await ExtractSelectedDirectory();
        }

        private async void _replaceDirectoryButton_Clicked(object sender, EventArgs e)
        {
            await ReplaceSelectedDirectory();
        }

        private async void _renameDirectoryButton_Clicked(object sender, EventArgs e)
        {
            await RenameSelectedDirectory();
        }

        private async void _addDirectoryButton_Clicked(object sender, EventArgs e)
        {
            await AddFolderToSelectedNode();
        }

        private async void _deleteDirectoryButton_Clicked(object sender, EventArgs e)
        {
            if (CanDeleteDirectories() && _lastSelectedComponent == _treeView)
                await DeleteSelectedDirectory();
        }

        #endregion

        #region AsyncOperation

        private void _cancelBtn_Clicked(object sender, EventArgs e)
        {
            _asyncOperation.Cancel();
        }

        private void _asyncOperation_Started(object sender, EventArgs e)
        {
            _cancelBtn.Enabled = true;

            _clearButton.Enabled = false;
            _searchBox.IsReadOnly = true;
        }

        private void _asyncOperation_Finished(object sender, EventArgs e)
        {
            _cancelBtn.Enabled = false;

            _clearButton.Enabled = true;
            _searchBox.IsReadOnly = false;
        }

        #endregion

        #endregion

        #region Open methods

        private async Task OpenFiles(IList<ArchiveFile> fileElements)
        {
            if (fileElements == null)
                return;

            foreach (var file in fileElements.Select(x => x.File))
            {
                var pluginIds = file.PluginIds ?? [];

                if (pluginIds.Any())
                {
                    // Opening by plugin id
                    var opened = false;
                    foreach (var pluginId in pluginIds)
                    {
                        if (_openingFiles.Contains(file))
                        {
                            _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadOpening(file.FilePath.ToRelative()));
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
                    _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadOpening(file.FilePath.ToRelative()));
                    continue;
                }

                _openingFiles.Add(file);
                if (!await OpenFile(file))
                    _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadError);

                _openingFiles.Remove(file);
            }
        }

        private async Task<bool> OpenFile(IArchiveFile afi, Guid pluginId = default)
        {
            return pluginId == default ?
                await _formInfo.FormCommunicator.Open(afi) :
                await _formInfo.FormCommunicator.Open(afi, pluginId);
        }

        #endregion

        #region Save methods

        private async void Save(bool saveAs)
        {
            _saveLock = true;
            UpdateSaveButtons();

            // Execute save operation
            var wasSuccessful = await _formInfo.FormCommunicator.Save(saveAs);
            if (!wasSuccessful)
            {
                _saveLock = false;
                UpdateSaveButtons();

                return;
            }

            _fileSystem = FileSystemFactory.CreateArchivePluginFileSystem(_formInfo.FileState);

            // Call update methods
            _saveLock = false;

            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Extract methods

        private Task ExtractSelectedFiles()
        {
            return ExtractFiles(_fileView.SelectedRows.Select(x => x.Data.File).ToArray());
        }

        private Task ExtractSelectedDirectory()
        {
            return ExtractDirectory(_treeView.SelectedNode);
        }

        private async Task ExtractFiles(IList<IArchiveFile> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractNone);
                return;
            }

            // Select folder or file
            var selectedPath = await (files.Count > 1 ? SelectFolder() : SaveFile(files[0].FilePath.GetName()));
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Use containing directory as root if a file was selected
            var extractRoot = files.Count > 1 ? selectedPath : selectedPath.GetDirectory();

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var sm = new StreamManager();
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(extractRoot.FullName, sm);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressExtract, count++, files.Count);

                    if (IsFileLocked(file, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        // Use in-archive filename if a folder was selected, use selected filename if a file was selected
                        var extractName = files.Count > 1 ? file.FilePath.GetName() : selectedPath.GetName();
                        newFileStream = await destinationFileSystem.OpenFileAsync(extractName, FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    var currentFileStream = file.GetFileData().Result;

                    await currentFileStream.CopyToAsync(newFileStream, cts.Token);
                    newFileStream.Close();
                }
            });
            sm.ReleaseAll();

            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressExtract, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusExtractSuccess);
        }

        private async Task ExtractDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var fileEntries = _fileSystem.EnumerateAllFileEntries(nodePath, _searchTerm.Get()).ToArray();

            if (fileEntries.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractNone);
                return;
            }

            // Select folder
            var extractPath = await SelectFolder();
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var sm = new StreamManager();
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem((extractPath / (string)node.Text).FullName, sm);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressExtract, count++, fileEntries.Length);

                    if (IsFileLocked(fileEntry.ArchiveFile, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        destinationFileSystem.CreateDirectory(fileEntry.Path.GetSubDirectory(nodePath).GetDirectory());
                        newFileStream = await destinationFileSystem.OpenFileAsync(fileEntry.Path.GetSubDirectory(nodePath), FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    var currentFileStream = fileEntry.ArchiveFile.GetFileData().Result;

                    await currentFileStream.CopyToAsync(newFileStream, cts.Token);

                    newFileStream.Close();
                }
            });
            sm.ReleaseAll();

            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressExtract, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusExtractSuccess);
        }

        #endregion

        #region Replace methods

        private Task ReplaceSelectedFiles()
        {
            return ReplaceFiles(_fileView.SelectedRows.Select(x => x.Data.File).ToArray());
        }

        private Task ReplaceSelectedDirectory()
        {
            return ReplaceDirectory(_treeView.SelectedNode);
        }

        private async Task ReplaceFiles(IList<IArchiveFile> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusReplaceNone);
                return;
            }

            // Select destination
            UPath replaceDirectory;
            UPath replaceFileName;
            if (files.Count == 1)
            {
                var selectedPath = await OpenFile(files[0].FilePath.GetName());
                if (selectedPath.IsNull || selectedPath.IsEmpty)
                {
                    _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                    return;
                }

                replaceDirectory = selectedPath.GetDirectory();
                replaceFileName = selectedPath.GetName();
            }
            else
            {
                var selectedPath = await SelectFolder();
                if (selectedPath.IsNull || selectedPath.IsEmpty)
                {
                    _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                    return;
                }

                replaceDirectory = selectedPath;
                replaceFileName = UPath.Empty;
            }

            // Replace elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replaceDirectory.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressReplace, count++, files.Count);

                    if (IsFileLocked(file, true))
                        continue;

                    var filePath = replaceFileName.IsEmpty ? file.FilePath.GetName() : replaceFileName;
                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = await sourceFileSystem.OpenFileAsync(filePath);
                    _formInfo.PluginState.AttemptReplaceFile(file, currentFileStream);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                    _changedFiles.Add(file);
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressReplace, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusReplaceCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusReplaceSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task ReplaceDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var fileEntries = _fileSystem.EnumerateAllFileEntries(nodePath, _searchTerm.Get()).ToArray();

            if (fileEntries.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusReplaceNone);
                return;
            }

            // Select folder
            var replacePath = await SelectFolder();
            if (replacePath.IsNull || replacePath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replacePath.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressReplace, count++, fileEntries.Length);

                    if (IsFileLocked(fileEntry.ArchiveFile, true))
                        continue;

                    var path = fileEntry.Path.GetSubDirectory(nodePath);
                    if (!sourceFileSystem.FileExists(path))
                        continue;

                    var currentFileStream = await sourceFileSystem.OpenFileAsync(path);
                    _formInfo.PluginState.AttemptReplaceFile(fileEntry.ArchiveFile, currentFileStream);

                    AddChangedDirectory(fileEntry.Path.GetDirectory());
                    _changedFiles.Add(fileEntry.ArchiveFile);
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressReplace, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusReplaceCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusReplaceSuccess);

            UpdateFileView(node.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Rename methods

        private Task RenameSelectedFiles()
        {
            return RenameFiles(_fileView.SelectedRows.Select(x => x.Data.File).ToArray());
        }

        private Task RenameSelectedDirectory()
        {
            return RenameDirectory(_treeView.SelectedNode);
        }

        private async Task RenameFiles(IList<IArchiveFile> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameNone);
                return;
            }

            // RenameFile elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressRename, count++, files.Count);

                    // Select new name
                    var newName = await InputBox.ShowAsync(LocalizationResources.ArchiveDialogRenameFileCaption,
                        LocalizationResources.ArchiveDialogRenameText(file.FilePath.GetName()),
                        file.FilePath.GetName());

                    if (string.IsNullOrEmpty(newName))
                        continue;

                    // Rename possibly open file in main form
                    _formInfo.FormCommunicator.Rename(file, file.FilePath.GetDirectory() / newName);

                    // Rename file in archive
                    _fileSystem.MoveFile(file.FilePath, file.FilePath.GetDirectory() / newName);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                    _changedFiles.Add(file);
                }
            });

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressRename, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusRenameSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();
        }

        private async Task RenameDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var fileEntries = _fileSystem.EnumerateAllFileEntries(nodePath).ToArray();

            if (fileEntries.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameNone);
                return;
            }

            // Select new directory name
            var newName = await InputBox.ShowAsync(LocalizationResources.ArchiveDialogRenameDirectoryCaption,
                LocalizationResources.ArchiveDialogRenameText(node.Text), node.Text);

            if (string.IsNullOrEmpty(newName))
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameErrorNoName);
                return;
            }

            // RenameFile elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var newDirectoryPath = nodePath.GetDirectory() / newName;

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressRename, count++, fileEntries.Length);

                    // Move file to new directory
                    var newPath = newDirectoryPath / fileEntry.Path.GetSubDirectory(nodePath).ToRelative();

                    _formInfo.FormCommunicator.Rename(fileEntry.ArchiveFile, newPath);
                    _fileSystem.MoveFile(fileEntry.Path, newPath);
                }
            });

            node.Text = newName;
            node.Data.Name = newName;

            AddRenamedDirectory(nodePath.ToRelative(), node.Data.AbsolutePath);
            AddChangedDirectory(node.Data.AbsolutePath);

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressRename, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusRenameSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();
        }

        #endregion

        #region Add methods

        private Task AddFilesToSelectedNode()
        {
            //return AddFiles(_treeView.SelectedNode);
            return Task.CompletedTask;
        }

        private Task AddFolderToSelectedNode()
        {
            return AddFolder(_treeView.SelectedNode);
        }

        private async Task AddFolder(TreeNode<DirectoryEntry> node)
        {
            // Select folder
            var selectedFolder = await SelectFolder();
            if (selectedFolder.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Add files
            var subFolder = node.Data.AbsolutePath.ToAbsolute();
            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(selectedFolder.FullName, _formInfo.FileState.StreamManager);

            var files = sourceFileSystem.EnumerateAllFiles(UPath.Root).ToArray();
            if (files.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusAddNone);
                return;
            }

            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.Progress.StartProgress();
            var filesNotAdded = false;
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressAdd, count++, files.Length);

                    // Do not add file if it already exists
                    // This would be replacement and is not part of this operation
                    if (_fileSystem.FileExists(subFolder / filePath.ToRelative()))
                        continue;

                    Stream createdFile;
                    try
                    {
                        // The plugin can throw if a file is not addable
                        createdFile = await _fileSystem.OpenFileAsync(subFolder / filePath.ToRelative(), FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception e)
                    {
                        // HINT: Log messages are not localized
                        _formInfo.Logger.Fatal(e, CouldNotAddFileLog_, filePath);
                        filesNotAdded = true;

                        continue;
                    }

                    var sourceFile = await sourceFileSystem.OpenFileAsync(filePath);
                    await sourceFile.CopyToAsync(createdFile, cts.Token);

                    sourceFile.Close();

                    // Add file to directory entries and tree
                    var afi = ((AfiFileEntry)_fileSystem.GetFileEntry(subFolder / filePath.ToRelative())).ArchiveFile;
                    AddTreeFile(node, filePath.ToRelative(), afi);

                    _changedFiles.Add(afi);
                }
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressAdd, 1, 1);
            _formInfo.Progress.FinishProgress();

            AddChangedDirectory(subFolder);

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusAddCancel);
            else if (filesNotAdded)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusAddError);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusAddSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private void AddTreeFile(TreeNode<DirectoryEntry> node, UPath relativePath, IArchiveFile afi)
        {
            var localNode = node;
            foreach (var part in relativePath.GetDirectory().ToRelative().Split())
            {
                var localNodeTmp = localNode.Nodes.FirstOrDefault(x => x.Text == part);
                if (localNodeTmp == null)
                {
                    localNodeTmp = new TreeNode<DirectoryEntry> { Text = part, Data = new DirectoryEntry(part) };

                    localNode.Nodes.Add(localNodeTmp);
                    localNode.Data.AddDirectory(localNodeTmp.Data);
                }

                localNode = localNodeTmp;
                AddChangedDirectory(localNode.Data.AbsolutePath);
            }

            localNode.Data.Files.Add(afi);
            _changedFiles.Add(afi);
        }

        #endregion

        #region Delete methods

        private bool CanDeleteDirectories()
        {
            return _formInfo.CanDeleteFiles && !_saveLock && !_asyncOperation.IsRunning && _treeView.SelectedNode != _treeView.Nodes[0];
        }

        private bool CanDeleteFiles()
        {
            return _formInfo.CanDeleteFiles && !_saveLock && !_asyncOperation.IsRunning;
        }

        private Task DeleteSelectedFiles()
        {
            return DeleteFiles(_treeView.SelectedNode.Data, _fileView.SelectedRows.Select(x => x.Data.File).ToArray());
        }

        private Task DeleteSelectedDirectory()
        {
            return DeleteDirectory(_treeView.SelectedNode);
        }

        private async Task DeleteFiles(DirectoryEntry entry, IList<IArchiveFile> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteNone);
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressDelete, count++, files.Count);

                    _fileSystem.DeleteFile(file.FilePath);
                    entry.Files.Remove(file);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressDelete, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusDeleteSuccess);

            UpdateFileView(_treeView.SelectedNode.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task DeleteDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var filePaths = _fileSystem.EnumerateAllFiles(nodePath).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteNone);
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressDelete, count++, filePaths.Length);

                    _fileSystem.DeleteFile(nodePath / filePath);
                }
            });

            // Execute final deletions
            AddChangedDirectory(nodePath);
            _fileSystem.DeleteDirectory(nodePath, true);

            node.Data.Remove();
            node.Remove();

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.ArchiveProgressDelete, 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusDeleteSuccess);

            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Update methods

        private void UpdateFileTree()
        {
            var files = _fileSystem.EnumerateAllFileEntries(UPath.Root, _searchTerm.Get()).Select(x => ((AfiFileEntry)x).ArchiveFile).ToArray();

            TreeNode<DirectoryEntry> selected = null;
            UpdateFileTree(files.ToTree(), ref selected);
        }

        private void UpdateFileTree(DirectoryEntry entry, ref TreeNode<DirectoryEntry> selected, TreeNode<DirectoryEntry> currentNode = null)
        {
            // Create node for entry
            var node = new TreeNode<DirectoryEntry>
            {
                Text = string.IsNullOrEmpty(entry.Name) ? _formInfo.FileState.FilePath.GetName() : entry.Name,
                TextColor = _changedDirectories.Contains(entry.AbsolutePath) ? ColorResources.Changed : Color.Transparent,
                IsExpanded = currentNode == null || _openedDirectories.Contains(entry.AbsolutePath)
            };

            if (entry.AbsolutePath == _selectedPath)
                selected = node;

            // Add node to tree
            currentNode?.Nodes.Add(node);

            // Add directories
            foreach (var dir in entry.Directories)
                UpdateFileTree(dir, ref selected, node);

            // Add files
            node.Data = entry;

            // Set tree to TreeView component
            if (currentNode == null)
            {
                _treeView.Nodes.Clear();
                _treeView.Nodes.Add(node);

                _treeView.SelectedNode = selected;
            }
        }

        private void UpdateFileView(DirectoryEntry entry = null)
        {
            if (entry == null)
            {
                _fileView.Rows = new System.Collections.Generic.List<DataTableRow<ArchiveFile>>();
                UpdateFileCount(0);

                return;
            }

            _fileView.Rows = entry.Files.Select(afi => new DataTableRow<ArchiveFile>(new ArchiveFile(afi))
            {
                TextColor = _changedFiles.Contains(afi) ? ColorResources.Changed : Color.Transparent
            }).ToArray();

            UpdateFileCount(entry.Files.Count);
        }

        private void UpdateFileCount(int fileCount)
        {
            _fileCount.Text = LocalizationResources.ArchiveFileCount(fileCount);
        }

        #endregion

        #region Support methods

        private bool IsFileLocked(IArchiveFile afi, bool lockOnLoaded)
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

        private async Task<UPath> OpenFile(string fileName)
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = string.IsNullOrEmpty(SettingsResources.LastDirectory) ? Path.GetFullPath(".") : SettingsResources.LastDirectory,
                InitialFileName = fileName
            };

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.Files[0] : UPath.Empty;

            if (result != UPath.Empty)
            {
                SettingsResources.LastDirectory = Path.GetDirectoryName(result.FullName);
            }

            return result;
        }

        private async Task<UPath> SaveFile(string fileName)
        {
            var dir = string.IsNullOrEmpty(SettingsResources.LastDirectory) ? Path.GetFullPath(".") : SettingsResources.LastDirectory;
            var ofd = new WindowsSaveFileDialog
            {
                InitialDirectory = dir,
                InitialFileName = fileName
            };

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.Files[0] : UPath.Empty;

            if (result != UPath.Empty)
            {
                SettingsResources.LastDirectory = Path.GetDirectoryName(result.FullName);
            }

            return result;
        }

        private async Task<UPath> SelectFolder()
        {
            var sfd = new SelectFolderDialog
            {
                Directory = SettingsResources.LastDirectory
            };
            var result = await sfd.ShowAsync() == DialogResult.Ok ? sfd.Directory : UPath.Empty;

            if (result != UPath.Empty)
            {
                SettingsResources.LastDirectory = result.FullName;
            }

            return result;
        }

        private void AddChangedDirectory(UPath path)
        {
            // Add paths to global store for changed directories, for complete updates of the tree
            var full = UPath.Empty;
            _changedDirectories.Add(full);

            foreach (var part in path.ToRelative().Split())
            {
                full /= part;
                _changedDirectories.Add(full);
            }

            // Color nodes directly for quicker updates
            var node = _treeView.Nodes[0];
            node.TextColor = ColorResources.Changed;

            foreach (var part in path.ToRelative().Split())
            {
                node = node.Nodes.FirstOrDefault(x => x.Text == part);
                if (node == null)
                    break;

                node.TextColor = ColorResources.Changed;
            }
        }

        private void AddRenamedDirectory(UPath oldDirectory, UPath newDirectory)
        {
            // RenameFile elements in opened directories
            foreach (var opened in _openedDirectories.ToArray())
            {
                if (!opened.FullName.StartsWith(oldDirectory.FullName))
                    continue;

                _openedDirectories.Remove(opened);
                _openedDirectories.Add(newDirectory / opened.GetSubDirectory(oldDirectory).ToRelative());
            }

            // RenameFile elements in changed directories
            foreach (var changed in _changedDirectories.ToArray())
            {
                if (!changed.FullName.StartsWith(oldDirectory.FullName))
                    continue;

                _changedDirectories.Remove(changed);
                _changedDirectories.Add(newDirectory / changed.GetSubDirectory(oldDirectory).ToRelative());
            }
        }

        #endregion

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override async void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);

            if (DeleteCommand.IsPressed())
            {
                if (CanDeleteDirectories() && _lastSelectedComponent == _treeView)
                    await DeleteSelectedDirectory();
                else if (CanDeleteFiles() && _lastSelectedComponent == _fileView)
                    await DeleteSelectedFiles();
            }
        }

        #endregion

        #region IKuriimuForm implementation

        public void UpdateForm()
        {
            // Update changed directories and files
            if (!_formInfo.FileState.StateChanged)
                ClearChangedItems();
            else
                UpdateChildrenFiles();

            // Update root name, if changed
            var rootName = _formInfo.FileState.FilePath.GetName();
            if (_treeView.Nodes[0].Text != rootName)
                _treeView.Nodes[0].Text = rootName;

            // Update save button enablement
            UpdateSaveButtons();
        }

        private void ClearChangedItems()
        {
            ClearChangedDirectories();
            ClearChangedFiles();
        }

        private void ClearChangedDirectories()
        {
            if (_changedDirectories.Count <= 0)
                return;

            _changedDirectories.Clear();
            UpdateFileTree();
        }

        private void ClearChangedFiles()
        {
            if (_changedFiles.Count <= 0)
                return;

            _changedFiles.Clear();
            UpdateFileView(_treeView.SelectedNode.Data);
        }

        private void UpdateChildrenFiles()
        {
            foreach (IFileState child in _formInfo.FileState.ArchiveChildren)
            {
                if (!child.StateChanged)
                    continue;

                var changedFile = (AfiFileEntry)_fileSystem.GetFileEntry(child.FilePath);

                _changedFiles.Add(changedFile.ArchiveFile);
                AddChangedDirectory(child.FilePath.GetDirectory());
            }

            UpdateFileView(_treeView.SelectedNode.Data);
        }

        private void UpdateSaveButtons()
        {
            var canSave = _formInfo.FileState.PluginState.CanSave && !_saveLock;

            _saveBtn.Enabled = canSave && _formInfo.FileState.StateChanged && !_asyncOperation.IsRunning;
            _saveAsBtn.Enabled = canSave && _formInfo.FileState is { StateChanged: true, ParentFileState: null } && !_asyncOperation.IsRunning;
        }

        public bool HasRunningOperations()
        {
            return _asyncOperation.IsRunning;
        }

        public void CancelOperations()
        {
            _asyncOperation.Cancel();
        }

        #endregion
    }
}
