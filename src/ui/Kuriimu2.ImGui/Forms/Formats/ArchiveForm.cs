using Hexa.NET.ImGui;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Rectangle = ImGui.Forms.Support.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    internal partial class ArchiveForm : Component, IKuriimuForm
    {
        private static readonly KeyCommand DeleteCommand = new(ImGuiKey.Delete);

        private readonly ArchiveFormInfo _formInfo;
        private readonly IPluginManager _pluginManager;
        private readonly IFileManager _fileManager;
        private IFileSystem _fileSystem;

        private readonly System.Collections.Generic.List<IArchiveFile> _openingFiles = [];

        private readonly HashSet<IArchiveFile> _changedFiles = [];
        private readonly HashSet<UPath> _changedDirectories = [];
        private readonly HashSet<UPath> _openedDirectories = [];
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

            _asyncOperation = new AsyncOperation();
            _searchTerm = new SearchTerm(_searchBox);

            #region Events

            _saveBtn.Clicked += SaveBtn_Clicked;
            _saveAsBtn.Clicked += SaveAsBtn_Clicked;

            _searchTerm.TextChanged += SearchTerm_TextChanged;
            _clearButton.Clicked += ClearButton_Clicked;

            _treeView.SelectedNodeChanged += TreeView_SelectedNodeChanged;
            _treeView.NodeExpanded += TreeView_NodeExpanded;
            _treeView.NodeCollapsed += TreeView_NodeCollapsed;

            _fileView.SelectedRowsChanged += FileView_SelectedRowsChanged;
            _fileView.DoubleClicked += FileView_DoubleClicked;

            _directoryContext.Show += DirectoryContext_Show;
            _fileContext.Show += FileContext_Show;

            _openFileButton.Clicked += OpenFileButton_Clicked;

            _extractFileButton.Clicked += ExtractFileButton_Clicked;
            _replaceFileButton.Clicked += ReplaceFileButton_Clicked;
            _renameFileButton.Clicked += RenameFileButton_Clicked;
            _deleteFileButton.Clicked += DeleteFileButton_Clicked;

            _extractDirectoryButton.Clicked += ExtractDirectoryButton_Clicked;
            _replaceDirectoryButton.Clicked += ReplaceDirectoryButton_Clicked;
            _renameDirectoryButton.Clicked += RenameDirectoryButton_Clicked;
            _addFileButton.Clicked += AddFileButton_Clicked;
            _addDirectoryButton.Clicked += AddDirectoryButton_Clicked;
            _deleteDirectoryButton.Clicked += DeleteDirectoryButton_Clicked;

            _cancelBtn.Clicked += CancelBtn_Clicked;

            _asyncOperation.Started += AsyncOperation_Started;
            _asyncOperation.Finished += AsyncOperation_Finished;

            #endregion

            #region Updates

            UpdateFileTree();
            _treeView.SelectedNode = _treeView.Nodes.FirstOrDefault();

            UpdateForm();

            #endregion
        }

        private void FileView_SelectedRowsChanged(object? sender, EventArgs e)
        {
            _lastSelectedComponent = _fileView;
        }

        #region Events

        #region Save

        private void SaveBtn_Clicked(object? sender, EventArgs e)
        {
            Save(false);
        }

        private void SaveAsBtn_Clicked(object? sender, EventArgs e)
        {
            Save(true);
        }

        #endregion

        #region Search

        private void SearchTerm_TextChanged(object? sender, EventArgs e)
        {
            UpdateFileTree();
        }

        private void ClearButton_Clicked(object? sender, EventArgs e)
        {
            _searchTerm.Clear();

            UpdateFileTree();
        }

        #endregion

        #region TreeView

        private void TreeView_SelectedNodeChanged(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Data != null)
                _selectedPath = _treeView.SelectedNode.Data.AbsolutePath;

            UpdateFileView(_treeView.SelectedNode?.Data);

            _lastSelectedComponent = _treeView;
        }

        private void TreeView_NodeCollapsed(object? sender, NodeEventArgs<DirectoryEntry> e)
        {
            if (e.Node.Data is null)
                return;

            _openedDirectories.Remove(e.Node.Data.AbsolutePath);
        }

        private void TreeView_NodeExpanded(object? sender, NodeEventArgs<DirectoryEntry> e)
        {
            if (e.Node.Data is null)
                return;

            _openedDirectories.Add(e.Node.Data.AbsolutePath);
        }

        #endregion

        #region FileView

        private async void FileView_DoubleClicked(object? sender, EventArgs e)
        {
            if (_fileView.SelectedRows.Count <= 0)
                return;

            await OpenFiles([_fileView.SelectedRows[0].Data]);
        }

        #endregion

        #region ContextMenu

        private void FileContext_Show(object? sender, EventArgs e)
        {
            if (_fileView.SelectedRows.Count <= 0)
                return;

            var selectedItem = _fileView.SelectedRows[0];

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
                pluginButton.Clicked += async (_, _) =>
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

        private void DirectoryContext_Show(object? sender, EventArgs e)
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
            _addFileButton.Enabled = canAddDirectories;
        }

        #endregion

        #region ContextMenu Items

        private async void OpenFileButton_Clicked(object? sender, EventArgs e)
        {
            await OpenFiles([.. _fileView.SelectedRows.Select(x => x.Data)]);
        }

        private async void ExtractFileButton_Clicked(object? sender, EventArgs e)
        {
            await ExtractSelectedFiles();
        }

        private async void ReplaceFileButton_Clicked(object? sender, EventArgs e)
        {
            await ReplaceSelectedFiles();
        }

        private async void RenameFileButton_Clicked(object? sender, EventArgs e)
        {
            await RenameSelectedFiles();
        }

        private async void DeleteFileButton_Clicked(object? sender, EventArgs e)
        {
            if (CanDeleteFiles() && _lastSelectedComponent == _fileView)
                await DeleteSelectedFiles();
        }

        private async void ExtractDirectoryButton_Clicked(object? sender, EventArgs e)
        {
            await ExtractSelectedDirectory();
        }

        private async void ReplaceDirectoryButton_Clicked(object? sender, EventArgs e)
        {
            await ReplaceSelectedDirectory();
        }

        private async void RenameDirectoryButton_Clicked(object? sender, EventArgs e)
        {
            await RenameSelectedDirectory();
        }

        private async void AddFileButton_Clicked(object? sender, EventArgs e)
        {
            await AddSelectedFiles();
        }

        private async void AddDirectoryButton_Clicked(object? sender, EventArgs e)
        {
            await AddSelectedFolder();
        }

        private async void DeleteDirectoryButton_Clicked(object? sender, EventArgs e)
        {
            if (CanDeleteDirectories() && _lastSelectedComponent == _treeView)
                await DeleteSelectedDirectory();
        }

        #endregion

        #region AsyncOperation

        private void CancelBtn_Clicked(object? sender, EventArgs e)
        {
            _asyncOperation.Cancel();
        }

        private void AsyncOperation_Started(object? sender, EventArgs e)
        {
            _cancelBtn.Enabled = true;

            _clearButton.Enabled = false;
            _searchBox.IsReadOnly = true;
        }

        private void AsyncOperation_Finished(object? sender, EventArgs e)
        {
            _cancelBtn.Enabled = false;

            _clearButton.Enabled = true;
            _searchBox.IsReadOnly = false;
        }

        #endregion

        #endregion

        #region Open methods

        private async Task OpenFiles(ArchiveFile[] fileElements)
        {
            if (fileElements.Length <= 0)
                return;

            foreach (var file in fileElements.Select(x => x.File))
            {
                var pluginIds = file.PluginIds ?? [];

                if (pluginIds.Length > 0)
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
            return pluginId == Guid.Empty ?
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
            return ExtractFiles([.. _fileView.SelectedRows.Select(x => x.Data.File)]);
        }

        private Task ExtractSelectedDirectory()
        {
            return ExtractDirectory(_treeView.SelectedNode);
        }

        private async Task ExtractFiles(IArchiveFile[] files)
        {
            if (files.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractNone);
                return;
            }

            // Select folder or file
            var selectedPath = await (files.Length > 1 ? SelectFolder() : SelectFile(files[0].FilePath.GetName()));
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Use containing directory as root if a file was selected
            var extractRoot = files.Length > 1 ? selectedPath : selectedPath.GetDirectory();

            var outputPath = extractRoot.FullName ?? string.Empty;
            if (File.Exists(outputPath))
                outputPath = outputPath.Replace('.', '_');

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            var sm = new StreamManager();
            var destinationFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputPath, sm);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressExtract);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, files.Length);

                    if (IsFileLocked(file, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        // Use in-archive filename if a folder was selected, use selected filename if a file was selected
                        var extractName = files.Length > 1 ? file.FilePath.GetName() : selectedPath.GetName();
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

            _formInfo.Progress.ReportProgress(1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusExtractCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusExtractSuccess);
        }

        private async Task ExtractDirectory(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

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

            var outputPath = (extractPath / (string)node.Text).FullName ?? string.Empty;
            if (File.Exists(outputPath))
                outputPath = (extractPath / ((string)node.Text).Replace('.', '_')).FullName ?? string.Empty;

            var sm = new StreamManager();
            var destinationFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(outputPath, sm);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressExtract);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, fileEntries.Length);

                    if (IsFileLocked(fileEntry.ArchiveFile, false))
                        continue;

                    try
                    {
                        destinationFileSystem.CreateDirectory(fileEntry.Path.GetSubDirectory(nodePath).GetDirectory());
                        var newFileStream = await destinationFileSystem.OpenFileAsync(fileEntry.Path.GetSubDirectory(nodePath), FileMode.Create, FileAccess.Write);

                        var currentFileStream = await fileEntry.ArchiveFile.GetFileData();

                        await currentFileStream.CopyToAsync(newFileStream, cts.Token);

                        newFileStream.Close();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            });
            sm.ReleaseAll();

            _formInfo.Progress.ReportProgress(1, 1);
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
            return ReplaceFiles([.. _fileView.SelectedRows.Select(x => x.Data.File)]);
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

            var sourceFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(replaceDirectory.FullName ?? string.Empty, _formInfo.FileState.StreamManager);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressReplace);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, files.Count);

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
            _formInfo.Progress.ReportProgress(1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusReplaceCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusReplaceSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task ReplaceDirectory(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

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

            var sourceFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(replacePath.FullName ?? string.Empty, _formInfo.FileState.StreamManager);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressReplace);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, fileEntries.Length);

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
            _formInfo.Progress.ReportProgress(1, 1);
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
            return RenameFiles([.. _fileView.SelectedRows.Select(x => x.Data.File)]);
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

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressRename);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, files.Count);

                    // Select new name
                    var fileName = file.FilePath.GetName() ?? string.Empty;
                    var newName = await InputBox.ShowAsync(LocalizationResources.ArchiveDialogRenameFileCaption,
                        LocalizationResources.ArchiveDialogRenameText(fileName), fileName);

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
            _formInfo.Progress.ReportProgress(1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusRenameCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusRenameSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();
        }

        private async Task RenameDirectory(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

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

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressRename);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var fileEntry in fileEntries.Cast<AfiFileEntry>())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, fileEntries.Length);

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
            _formInfo.Progress.ReportProgress(1, 1);
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

        private Task AddSelectedFiles()
        {
            return AddFiles(_treeView.SelectedNode);
        }

        private async Task AddFiles(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

            // Select files
            var selectedFiles = await SelectFiles();
            if (selectedFiles.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Add files
            var subFolder = node.Data.AbsolutePath.ToAbsolute();

            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressAdd);
            _formInfo.Progress.StartProgress();

            var filesNotAdded = false;
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in selectedFiles)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    var fileName = filePath.GetName();
                    _formInfo.Progress.ReportProgress(count++, selectedFiles.Length);

                    // Do not add file if it already exists
                    // This would be replacement and is not part of this operation
                    if (_fileSystem.FileExists(subFolder / fileName))
                        continue;

                    Stream createdFile;
                    try
                    {
                        // The plugin can throw if a file is not addable
                        createdFile = await _fileSystem.OpenFileAsync(subFolder / fileName, FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception e)
                    {
                        // HINT: Log messages are not localized
                        _formInfo.Logger.Fatal(e, CouldNotAddFileLog_, filePath);
                        filesNotAdded = true;

                        continue;
                    }

                    if (string.IsNullOrEmpty(filePath.FullName))
                        continue;

                    var sourceFile = File.OpenRead(filePath.FullName);
                    await sourceFile.CopyToAsync(createdFile, cts.Token);

                    sourceFile.Close();

                    // Add file to directory entries and tree
                    var afi = ((AfiFileEntry)_fileSystem.GetFileEntry(subFolder / fileName)).ArchiveFile;
                    AddTreeFile(node, fileName, afi);

                    _changedFiles.Add(afi);
                }
            });

            _formInfo.Progress.ReportProgress(1, 1);
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

        private Task AddSelectedFolder()
        {
            return AddFolder(_treeView.SelectedNode);
        }

        private async Task AddFolder(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

            // Select folder
            var selectedFolder = await SelectFolder();
            if (selectedFolder.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusSelectNone);
                return;
            }

            // Add files
            var subFolder = node.Data.AbsolutePath.ToAbsolute();
            var sourceFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(selectedFolder.FullName ?? string.Empty, _formInfo.FileState.StreamManager);

            var files = sourceFileSystem.EnumerateAllFiles(UPath.Root).ToArray();
            if (files.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusAddNone);
                return;
            }

            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressAdd);
            _formInfo.Progress.StartProgress();

            var filesNotAdded = false;
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, files.Length);

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

            _formInfo.Progress.ReportProgress(1, 1);
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
                    localNode.Data?.AddDirectory(localNodeTmp.Data);
                }

                localNode = localNodeTmp;
                if (localNode.Data is not null)
                    AddChangedDirectory(localNode.Data.AbsolutePath);
            }

            localNode.Data?.Files.Add(afi);
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
            return DeleteFiles(_treeView.SelectedNode?.Data, [.. _fileView.SelectedRows.Select(x => x.Data.File)]);
        }

        private Task DeleteSelectedDirectory()
        {
            return DeleteDirectory(_treeView.SelectedNode);
        }

        private async Task DeleteFiles(DirectoryEntry? entry, IArchiveFile[] files)
        {
            if (entry is null)
                return;

            if (files.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteNone);
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressDelete);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, files.Length);

                    _fileSystem.DeleteFile(file.FilePath);
                    entry.Files.Remove(file);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });

            _formInfo.Progress.ReportProgress(1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteCancel);
            else
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.ArchiveStatusDeleteSuccess);

            UpdateFileView(_treeView.SelectedNode?.Data);
            UpdateForm();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task DeleteDirectory(TreeNode<DirectoryEntry>? node)
        {
            if (node?.Data is null)
                return;

            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var filePaths = _fileSystem.EnumerateAllFiles(nodePath).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.ArchiveStatusDeleteNone);
                return;
            }

            // Delete elements
            _formInfo.FormCommunicator.ReportStatus(StatusKind.Info, string.Empty);

            _formInfo.ProgressOutput.SetMessage(LocalizationResources.ArchiveProgressDelete);
            _formInfo.Progress.StartProgress();

            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(count++, filePaths.Length);

                    _fileSystem.DeleteFile(nodePath / filePath);
                }
            });

            // Execute final deletions
            AddChangedDirectory(nodePath);
            _fileSystem.DeleteDirectory(nodePath, true);

            node.Data.Remove();
            node.Remove();

            // Update progress
            _formInfo.Progress.ReportProgress(1, 1);
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

            TreeNode<DirectoryEntry>? selected = null;
            UpdateFileTree(files.ToTree(), ref selected);
        }

        private void UpdateFileTree(DirectoryEntry entry, ref TreeNode<DirectoryEntry>? selected, TreeNode<DirectoryEntry>? currentNode = null)
        {
            // Create node for entry
            var node = new TreeNode<DirectoryEntry>
            {
                Text = string.IsNullOrEmpty(entry.Name) ? _formInfo.FileState.FilePath.GetName() ?? string.Empty : entry.Name,
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

        private void UpdateFileView(DirectoryEntry? entry = null)
        {
            if (entry == null)
            {
                _fileView.Rows = [];
                UpdateFileCount(0);

                return;
            }

            _fileView.Rows = [..entry.Files.Select(afi => new DataTableRow<ArchiveFile>(new ArchiveFile(afi))
            {
                TextColor = _changedFiles.Contains(afi) ? ColorResources.Changed : Color.Transparent
            })];

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
            return openedState!.StateChanged;
        }

        private static async Task<UPath> OpenFile(string? fileName)
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = string.IsNullOrEmpty(SettingsResources.LastDirectory) ? Path.GetFullPath(".") : SettingsResources.LastDirectory,
                InitialFileName = fileName
            };

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.Files[0] : UPath.Empty;

            if (result != UPath.Empty)
            {
                SettingsResources.LastDirectory = Path.GetDirectoryName(result.FullName) ?? string.Empty;
            }

            return result;
        }

        private static async Task<UPath> SelectFile(string? fileName)
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
                SettingsResources.LastDirectory = Path.GetDirectoryName(result.FullName) ?? string.Empty;
            }

            return result;
        }

        private static async Task<UPath[]> SelectFiles()
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = string.IsNullOrEmpty(SettingsResources.LastDirectory) ? Path.GetFullPath(".") : SettingsResources.LastDirectory,
                Multiselect = true
            };

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.Files.Select(x => (UPath)x).ToArray() : [];

            return result;
        }

        private static async Task<UPath> SelectFolder()
        {
            var sfd = new WindowsSelectFolderDialog
            {
                InitialDirectory = SettingsResources.LastDirectory
            };
            var result = await sfd.ShowAsync() == DialogResult.Ok ? sfd.Directory : UPath.Empty;

            if (result != UPath.Empty)
            {
                SettingsResources.LastDirectory = result.FullName ?? string.Empty;
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
                if (string.IsNullOrEmpty(opened.FullName) || string.IsNullOrEmpty(oldDirectory.FullName))
                    continue;

                if (!opened.FullName.StartsWith(oldDirectory.FullName, StringComparison.Ordinal))
                    continue;

                _openedDirectories.Remove(opened);
                _openedDirectories.Add(newDirectory / opened.GetSubDirectory(oldDirectory).ToRelative());
            }

            // RenameFile elements in changed directories
            foreach (var changed in _changedDirectories.ToArray())
            {
                if (string.IsNullOrEmpty(changed.FullName) || string.IsNullOrEmpty(oldDirectory.FullName))
                    continue;

                if (!changed.FullName.StartsWith(oldDirectory.FullName, StringComparison.Ordinal))
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
            UpdateFileView(_treeView.SelectedNode?.Data);
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

            UpdateFileView(_treeView.SelectedNode?.Data);
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
