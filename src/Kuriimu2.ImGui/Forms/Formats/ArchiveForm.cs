using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using Komponent.Extensions;
using Kontract.Extensions;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;
using Kore.Managers.Plugins;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ArchiveForm : Component, IKuriimuForm
    {
        private readonly ArchiveFormInfo _formInfo;
        private readonly FileManager _fileManager;

        private readonly IList<IArchiveFileInfo> _openingFiles;
        private readonly AsyncOperation _asyncOperation;
        private readonly SearchTerm _searchTerm;

        private const string CouldNotAddFileLog_ = "Could not add file: {0}";

        public ArchiveForm(ArchiveFormInfo formInfo, FileManager fileManager)
        {
            InitializeComponent();

            _formInfo = formInfo;
            _fileManager = fileManager;

            _openingFiles = new List<IArchiveFileInfo>();
            _asyncOperation = new AsyncOperation();
            _searchTerm = new SearchTerm(_searchBox);

            #region Events

            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;

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

            #endregion

            #region Updates

            UpdateFileTree(formInfo.PluginState.Files.ToTree());
            UpdateFileCount(0);

            #endregion
        }

        #region Events

        #region TreeView

        private void _treeView_SelectedNodeChanged(object sender, EventArgs e)
        {
            UpdateFileView(_treeView.SelectedNode?.Data);
        }

        #endregion

        #region ContextMenu

        private void _fileContext_Show(object sender, EventArgs e)
        {
            var selectedItem = _fileView.SelectedRows.FirstOrDefault();
            if (selectedItem == null)
                return;

            // Get current state arguments
            var isLoadLocked = IsFileLocked(selectedItem.ArchiveFileInfo, true);
            var isStateLocked = IsFileLocked(selectedItem.ArchiveFileInfo, false);

            var canExtractFiles = !isStateLocked && !_asyncOperation.IsRunning;
            var canReplaceFiles = _formInfo.CanReplaceFiles && !isLoadLocked && !_asyncOperation.IsRunning;
            var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;

            // Update Open With menu node
            _openWithFileMenu.Items.Clear();

            foreach (var pluginId in selectedItem.ArchiveFileInfo.PluginIds ?? Array.Empty<Guid>())
            {
                var filePluginLoader = _fileManager.GetFilePluginLoaders().FirstOrDefault(x => x.Exists(pluginId));
                var filePlugin = filePluginLoader?.GetPlugin(pluginId);

                if (filePlugin == null)
                    continue;

                var pluginButton = new MenuBarButton { Caption = filePlugin.Metadata.Name };
                pluginButton.Clicked += async (s, ev) =>
                {
                    if (!await OpenFile(selectedItem.ArchiveFileInfo, pluginId))
                        _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.FileNotSuccessfullyLoadedWithPluginCaptionResource(pluginId));
                };

                _openWithFileMenu.Items.Add(pluginButton);
            }

            // Update context menu
            _openFileButton.Enabled = !_asyncOperation.IsRunning;
            _openWithFileMenu.Enabled = _openWithFileMenu.Items.Count > 0 && !_asyncOperation.IsRunning;

            _extractFileButton.Enabled = canExtractFiles;
            _replaceFileButton.Enabled = canReplaceFiles;
            _renameFileButton.Enabled = canRenameFiles;
            _deleteFileButton.Enabled = canDeleteFiles;
        }

        private void _directoryContext_Show(object sender, EventArgs e)
        {
            var canExtractDirectories = !_asyncOperation.IsRunning;
            var canReplaceDirectories = _formInfo.CanReplaceFiles && !_asyncOperation.IsRunning;
            var canRenameDirectories = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteDirectories = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning && _treeView.SelectedNode != _treeView.Nodes[0];
            var canAddDirectories = _formInfo.CanAddFiles && !_asyncOperation.IsRunning;

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
            await OpenFiles(_fileView.SelectedRows.ToArray());
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
            await AddFilesToSelectedNode();
        }

        private async void _deleteDirectoryButton_Clicked(object sender, EventArgs e)
        {
            await DeleteSelectedDirectory();
        }

        #endregion

        #endregion

        #region Open methods

        private async Task OpenFiles(IList<ArchiveFile> fileElements)
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
                            _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.FileAlreadyOpeningStatusResource(file.FilePath.ToRelative()));
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
                    _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.FileAlreadyOpeningStatusResource(file.FilePath.ToRelative()));
                    continue;
                }

                _openingFiles.Add(file);
                if (!await OpenFile(file))
                    _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.FileNotSuccessfullyLoadedCaptionResource());

                _openingFiles.Remove(file);
            }
        }

        private Task<bool> OpenFile(IArchiveFileInfo afi, Guid pluginId = default)
        {
            return pluginId == default ?
                _formInfo.FormCommunicator.Open(afi) :
                _formInfo.FormCommunicator.Open(afi, pluginId);
        }

        #endregion

        #region Extract methods

        private Task ExtractSelectedFiles()
        {
            return ExtractFiles(_fileView.SelectedRows.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private Task ExtractSelectedDirectory()
        {
            return ExtractDirectory(_treeView.SelectedNode);
        }

        private async Task ExtractFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToExtractStatusResource());
                return;
            }

            // Select folder or file
            var selectedPath = await (files.Count > 1 ? SelectFolder() : SaveFile(files[0].FilePath.GetName()));
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
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

                    _formInfo.Progress.ReportProgress(LocalizationResources.ExtractFileProgressResource(), count++, files.Count);

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
            _formInfo.Progress.ReportProgress(LocalizationResources.ExtractFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ExtractFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ExtractFileSuccessfulStatusResource());
        }

        private async Task ExtractDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            var filePaths = afiFileSystem.EnumerateAllFiles(nodePath, _searchTerm.Get()).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToExtractStatusResource());
                return;
            }

            // Select folder
            var extractPath = await SelectFolder();
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            // TODO: Get root properly
            var subFolder = _treeView.Nodes.Contains(node) ? node.Caption : nodePath.ToRelative();
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem((extractPath / subFolder).FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ExtractFileProgressResource(), count++, filePaths.Length);

                    var afi = ((AfiFileEntry)afiFileSystem.GetFileEntry(nodePath / filePath)).ArchiveFileInfo;
                    if (IsFileLocked(afi, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        newFileStream = await destinationFileSystem.OpenFileAsync(filePath, FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    var currentFileStream = afi.GetFileData().Result;

                    await currentFileStream.CopyToAsync(newFileStream, cts.Token);

                    newFileStream.Close();
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ExtractFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ExtractFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ExtractFileSuccessfulStatusResource());
        }

        #endregion

        #region Replace methods

        private Task ReplaceSelectedFiles()
        {
            return ReplaceFiles(_fileView.SelectedRows.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private Task ReplaceSelectedDirectory()
        {
            return ReplaceDirectory(_treeView.SelectedNode);
        }

        private async Task ReplaceFiles(IList<IArchiveFileInfo> files)
        {
            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToReplaceStatusResource());
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
                    _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
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
                    _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
                    return;
                }

                replaceDirectory = selectedPath;
                replaceFileName = UPath.Empty;
            }

            // Replace elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replaceDirectory.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), count++, files.Count);

                    if (IsFileLocked(file, true))
                        continue;

                    var filePath = replaceFileName.IsEmpty ? file.FilePath.GetName() : replaceFileName;
                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = await sourceFileSystem.OpenFileAsync(filePath);
                    //TODO this cast smells, should IFileState/IPluginState be generified?
                    ((IArchiveState)_formInfo.FileState.PluginState).AttemptReplaceFile(file, currentFileStream);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ReplaceFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ReplaceFileSuccessfulStatusResource());

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task ReplaceDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            var filePaths = afiFileSystem.EnumerateAllFiles(nodePath).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToReplaceStatusResource());
                return;
            }

            // Select folder
            var replacePath = await SelectFolder();
            if (replacePath.IsNull || replacePath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
                return;
            }

            // Extract elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replacePath.FullName, _formInfo.FileState.StreamManager);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), count++, filePaths.Length);

                    var afi = ((AfiFileEntry)afiFileSystem.GetFileEntry(nodePath / filePath)).ArchiveFileInfo;
                    if (IsFileLocked(afi, true))
                        continue;

                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = await sourceFileSystem.OpenFileAsync(filePath);
                    //TODO this cast smells, should IFileState/IPluginState be generified?
                    ((IArchiveState)_formInfo.FileState.PluginState).AttemptReplaceFile(afi, currentFileStream);

                    AddChangedDirectory(afi.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ReplaceFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ReplaceFileSuccessfulStatusResource());

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Rename methods

        private Task RenameSelectedFiles()
        {
            return RenameFiles(_fileView.SelectedRows.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private Task RenameSelectedDirectory()
        {
            return RenameDirectory(_treeView.SelectedNode);
        }

        private async Task RenameFiles(IList<IArchiveFileInfo> files)
        {
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);

            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToRenameStatusResource());
                return;
            }

            // Rename elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var file in files)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.RenameFileProgressResource(), count++, files.Count);

                    // Select new name
                    var newName = await InputBox.ShowAsync(LocalizationResources.RenameFileTitleResource(),
                        LocalizationResources.RenameItemCaptionResource(file.FilePath.GetName()),
                        file.FilePath.GetName());

                    if (string.IsNullOrEmpty(newName))
                        continue;

                    // Rename possibly open file in main form
                    _formInfo.FormCommunicator.Rename(file, file.FilePath.GetDirectory() / newName);

                    // Rename file in archive
                    afiFileSystem.MoveFile(file.FilePath, file.FilePath.GetDirectory() / newName);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.RenameFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.RenameFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.RenameFileSuccessfulStatusResource());
        }

        private async Task RenameDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            var filePaths = afiFileSystem.EnumerateAllFiles(nodePath).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToRenameStatusResource());
                return;
            }

            // Select new directory name
            var newName = await InputBox.ShowAsync(LocalizationResources.RenameDirectoryTitleResource(),
                LocalizationResources.RenameItemCaptionResource(node.Caption), node.Caption);

            if (string.IsNullOrEmpty(newName))
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoNameGivenStatusResource());
                return;
            }

            // Rename elements
            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            var newDirectoryPath = nodePath.GetDirectory() / newName;

            _formInfo.Progress.StartProgress();
            await _asyncOperation.StartAsync(cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.RenameFileProgressResource(), count++, filePaths.Length);

                    // Rename possibly open file in main form
                    var afi = ((AfiFileEntry)afiFileSystem.GetFileEntry(nodePath / filePath)).ArchiveFileInfo;
                    _formInfo.FormCommunicator.Rename(afi, newDirectoryPath / filePath.ToRelative());

                    // Rename file in archive
                    afiFileSystem.MoveFile(afi.FilePath, newDirectoryPath / filePath.ToRelative());
                }

                return Task.CompletedTask;
            });

            node.Caption = newName;
            node.Data.Name = newName;

            AddChangedDirectory(node.Data.AbsolutePath.ToAbsolute());

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.RenameFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.RenameFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.RenameFileSuccessfulStatusResource());
        }

        #endregion

        #region Add methods

        private Task AddFilesToSelectedNode()
        {
            return AddFiles(_treeView.SelectedNode);
        }

        private async Task AddFiles(TreeNode<DirectoryEntry> node)
        {
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);

            // Select folder
            var selectedPath = await SelectFolder();
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoTargetSelectedStatusResource());
                return;
            }

            // Add elements
            var subFolder = node.Data.AbsolutePath.ToAbsolute();
            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(selectedPath.FullName, _formInfo.FileState.StreamManager);

            var elements = sourceFileSystem.EnumerateAllFiles(UPath.Root).ToArray();
            if (elements.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.NoFilesToAddStatusResource());
                return;
            }

            _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

            _formInfo.Progress.StartProgress();
            var filesNotAdded = false;
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in elements)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.AddFileProgressResource(), count++, elements.Length);

                    // Do not add file if it already exists
                    // This would be replacement and is not part of this operation
                    if (afiFileSystem.FileExists(subFolder / filePath.ToRelative()))
                        continue;

                    Stream createdFile;
                    try
                    {
                        // The plugin can throw if a file is not addable
                        createdFile = await afiFileSystem.OpenFileAsync(subFolder / filePath.ToRelative(), FileMode.Create, FileAccess.Write);
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
                    var afi = ((AfiFileEntry)afiFileSystem.GetFileEntry(subFolder / filePath.ToRelative())).ArchiveFileInfo;
                    AddTreeFile(node, filePath.ToRelative(), afi);
                }
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.AddFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            AddChangedDirectory(subFolder);

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.AddFileCancelledStatusResource());
            else if (filesNotAdded)
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.UnableToAddFilesStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.AddFileSuccessfulStatusResource());

            _formInfo.FormCommunicator.Update(true, false);
        }

        private void AddTreeFile(TreeNode<DirectoryEntry> node, UPath relativePath, IArchiveFileInfo afi)
        {
            var localNode = node;
            foreach (var part in relativePath.GetDirectory().ToRelative().Split())
            {
                var localNodeTmp = localNode.Nodes.FirstOrDefault(x => x.Caption == part);
                if (localNodeTmp == null)
                {
                    localNodeTmp = new TreeNode<DirectoryEntry> { Caption = part, Data = new DirectoryEntry(part) };

                    localNode.Nodes.Add(localNodeTmp);
                    localNode.Data.AddDirectory(localNodeTmp.Data);
                }

                localNode = localNodeTmp;
            }

            localNode.Data.Files.Add(afi);
        }

        #endregion

        #region Delete methods

        private Task DeleteSelectedFiles()
        {
            return DeleteFiles(_treeView.SelectedNode.Data, _fileView.SelectedRows.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private Task DeleteSelectedDirectory()
        {
            return DeleteDirectory(_treeView.SelectedNode);
        }

        private async Task DeleteFiles(DirectoryEntry entry, IList<IArchiveFileInfo> files)
        {
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);

            if (files.Count <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToDeleteStatusResource());
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

                    _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), count++, files.Count);

                    afiFileSystem.DeleteFile(file.FilePath);
                    entry.Files.Remove(file);

                    AddChangedDirectory(file.FilePath.GetDirectory());
                }

                return Task.CompletedTask;
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.DeleteFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.DeleteFileSuccessfulStatusResource());

            UpdateFileView(_treeView.SelectedNode.Data);

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task DeleteDirectory(TreeNode<DirectoryEntry> node)
        {
            var nodePath = node.Data.AbsolutePath.ToAbsolute();
            var afiFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
            var filePaths = afiFileSystem.EnumerateAllFiles(nodePath).Select(x => x.GetSubDirectory(nodePath).ToRelative()).ToArray();

            if (filePaths.Length <= 0)
            {
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.NoFilesToDeleteStatusResource());
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

                    _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), count++, filePaths.Length);

                    afiFileSystem.DeleteFile(nodePath / filePath);
                }

                return Task.CompletedTask;
            });

            // Execute final deletions
            AddChangedDirectory(nodePath);
            afiFileSystem.DeleteDirectory(nodePath, true);

            node.Data.Remove();
            node.Remove();

            // Update progress
            _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.DeleteFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.DeleteFileSuccessfulStatusResource());

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Update methods

        private void UpdateFileTree(DirectoryEntry entry, TreeNode<DirectoryEntry> currentNode = null)
        {
            // Create node for entry
            var node = new TreeNode<DirectoryEntry>
            {
                Caption = string.IsNullOrEmpty(entry.Name) ? _formInfo.FileState.FilePath.GetName() : entry.Name
            };

            // Add node to tree
            currentNode?.Nodes.Add(node);

            if (currentNode == null)
            {
                _treeView.Nodes.Clear();
                _treeView.Nodes.Add(node);
            }

            // Add directories
            foreach (var dir in entry.Directories)
                UpdateFileTree(dir, node);

            // Add files
            node.Data = entry;
        }

        private void UpdateFileView(DirectoryEntry entry = null)
        {
            if (entry == null)
            {
                _fileView.Rows = new List<ArchiveFile>();
                UpdateFileCount(0);

                return;
            }

            _fileView.Rows = entry.Files.Select(afi => new ArchiveFile(afi)).ToArray();
            UpdateFileCount(entry.Files.Count);
        }

        private void UpdateFileCount(int fileCount)
        {
            _fileCount.Caption = LocalizationResources.FileCountCaptionResource(fileCount);
        }

        #endregion

        #region Support methods

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

        private async Task<UPath> OpenFile(string fileName)
        {
            var ofd = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.LastDirectory == string.Empty ? Path.GetFullPath(".") : Settings.Default.LastDirectory,
                InitialFileName = fileName
            };

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.SelectedPath : UPath.Empty;

            if (result != UPath.Empty)
            {
                Settings.Default.LastDirectory = result.FullName;
                Settings.Default.Save();
            }

            return result;
        }

        private async Task<UPath> SaveFile(string fileName)
        {
            var dir = Settings.Default.LastDirectory == string.Empty ? Path.GetFullPath(".") : Settings.Default.LastDirectory;
            var ofd = new SaveFileDialog(Path.Combine(dir, fileName));

            var result = await ofd.ShowAsync() == DialogResult.Ok ? ofd.SelectedPath : UPath.Empty;

            if (result != UPath.Empty)
            {
                Settings.Default.LastDirectory = result.FullName;
                Settings.Default.Save();
            }

            return result;
        }

        private async Task<UPath> SelectFolder()
        {
            var sfd = new SelectFolderDialog
            {
                Directory = Settings.Default.LastDirectory
            };
            var result = await sfd.ShowAsync() == DialogResult.Ok ? sfd.Directory : UPath.Empty;

            if (result != UPath.Empty)
            {
                Settings.Default.LastDirectory = result.FullName;
                Settings.Default.Save();
            }

            return result;
        }

        private void AddChangedDirectory(UPath path)
        {
            var node = _treeView.Nodes[0];
            node.TextColor = ColorResources.ArchiveChanged;

            foreach (var part in path.ToRelative().Split())
            {
                node = node.Nodes.FirstOrDefault(x => x.Caption == part);
                if (node == null)
                    break;

                node.TextColor = ColorResources.ArchiveChanged;
            }
        }

        #endregion

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);
        }

        #endregion

        #region IKuriimuForm implementation

        public void UpdateForm()
        {
            // TODO
        }

        public bool HasRunningOperations()
        {
            // TODO
            return false;
        }

        public void CancelOperations()
        {
            // TODO
        }

        #endregion
    }
}
