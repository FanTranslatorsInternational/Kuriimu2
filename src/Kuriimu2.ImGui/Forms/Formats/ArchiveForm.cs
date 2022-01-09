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
using ImGui.Forms.Models;
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
using Veldrid;

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
            UpdateFileView(_treeView.SelectedNode.Data);
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
            var canExtractFiles = !_asyncOperation.IsRunning;
            var canReplaceFiles = _formInfo.CanReplaceFiles && !_asyncOperation.IsRunning;
            var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;
            var canAddFiles = _formInfo.CanAddFiles && !_asyncOperation.IsRunning;

            _extractDirectoryButton.Enabled = canExtractFiles;
            _replaceDirectoryButton.Enabled = canReplaceFiles;
            _renameDirectoryButton.Enabled = canRenameFiles;
            _deleteDirectoryButton.Enabled = canDeleteFiles;
            _addDirectoryButton.Enabled = canAddFiles;
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

        private async Task ExtractDirectory(TreeNode<IList<IArchiveFileInfo>> node)
        {
            var nodePath = GetAbsolutePath(node);
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
            var subFolder = _treeView.Nodes.Contains(node) ? node.Caption : GetAbsolutePath(node).ToRelative();
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

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ReplaceFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ReplaceFileSuccessfulStatusResource());

            UpdateFileView(_treeView.SelectedNode.Data);
            // TODO: Update file tree to show changed directories
            //UpdateFileTree();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task ReplaceDirectory(TreeNode<IList<IArchiveFileInfo>> node)
        {
            var nodePath = GetAbsolutePath(node);
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

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(afi.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.ReplaceFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.ReplaceFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.ReplaceFileSuccessfulStatusResource());

            UpdateFileView(_treeView.SelectedNode.Data);
            // TODO: Update file tree to show changed directories
            //UpdateDirectories();

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

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(file.FilePath.GetDirectory());
                }
            });
            _formInfo.Progress.ReportProgress(LocalizationResources.RenameFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.RenameFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.RenameFileSuccessfulStatusResource());
        }

        private async Task RenameDirectory(TreeNode<IList<IArchiveFileInfo>> node)
        {
            var nodePath = GetAbsolutePath(node);
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

            var newDirectoryPath = GetAbsolutePath(node).GetDirectory() / newName;

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

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(afi.FilePath.GetDirectory());
                }

                return Task.CompletedTask;
            });

            node.Caption = newName;

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

        private async Task AddFiles(TreeNode<IList<IArchiveFileInfo>> node)
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
            var subFolder = GetAbsolutePath(node);
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

                    // TODO: This will currently copy files to memory, instead of just using a reference to any more memory efficient stream (like FileStream)
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
                }
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.AddFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.AddFileCancelledStatusResource());
            else if (filesNotAdded)
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.UnableToAddFilesStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.AddFileSuccessfulStatusResource());

            // TODO: Update UI properly
            //UpdateFileView(_treeView.SelectedNode.Data);
            // TODO: Update affected file tree node

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Delete methods

        private Task DeleteSelectedFiles()
        {
            return DeleteFiles(_fileView.SelectedRows.Select(x => x.ArchiveFileInfo).ToArray());
        }

        private Task DeleteSelectedDirectory()
        {
            return DeleteDirectory(_treeView.SelectedNode);
        }

        private async Task DeleteFiles(IList<IArchiveFileInfo> files)
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

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(file.FilePath.GetDirectory());
                }

                return Task.CompletedTask;
            });

            _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.DeleteFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.DeleteFileSuccessfulStatusResource());

            // TODO: Update UI properly

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async Task DeleteDirectory(TreeNode<IList<IArchiveFileInfo>> node)
        {
            var nodePath = GetAbsolutePath(node);
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
            await _asyncOperation.StartAsync(async cts =>
            {
                var count = 0;
                foreach (var filePath in filePaths)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), count++, filePaths.Length);

                    afiFileSystem.DeleteFile(nodePath / filePath);

                    // TODO: Mark directory or set correct directory nodes as changed
                    //AddChangedDirectory(nodePath);
                }
            });

            afiFileSystem.DeleteDirectory(nodePath, true);

            _formInfo.Progress.ReportProgress(LocalizationResources.DeleteFileProgressResource(), 1, 1);
            _formInfo.Progress.FinishProgress();

            if (_asyncOperation.WasCancelled)
                _formInfo.FormCommunicator.ReportStatus(false, LocalizationResources.DeleteFileCancelledStatusResource());
            else
                _formInfo.FormCommunicator.ReportStatus(true, LocalizationResources.DeleteFileSuccessfulStatusResource());

            // TODO: Update UI properly

            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Update methods

        private void UpdateFileTree(DirectoryEntry entry, TreeNode<IList<IArchiveFileInfo>> currentNode = null)
        {
            // Create node for entry
            var node = new TreeNode<IList<IArchiveFileInfo>>
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
            node.Data = entry.Files;
        }

        private void UpdateFileView(IList<IArchiveFileInfo> files)
        {
            _fileView.Rows = files.Select(afi => new ArchiveFile(afi)).ToArray();
            UpdateFileCount(files.Count);
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

        private UPath GetAbsolutePath(TreeNode<IList<IArchiveFileInfo>> node)
        {
            // If a root node, return root path
            if (_treeView.Nodes.Contains(node))
                return UPath.Root;

            return GetAbsolutePath(node.Parent) / node.Caption;
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

    //partial class ArchiveForm : Component, IKuriimuForm
    //{
    //    private readonly ArchiveFormInfo _formInfo;
    //    private readonly FileManager _fileManager;

    //    private readonly IList<IArchiveFileInfo> _openingFiles;
    //    private readonly HashSet<UPath> _changedDirectories;

    //    private readonly SearchTerm _searchTerm;
    //    private readonly AsyncOperation _asyncOperation;

    //    private IFileSystem _archiveFileSystem;
    //    private UPath _selectedPath;

    //    private SortingScheme _sortingScheme = SortingScheme.NameAsc;

    //    #region Hot Keys

    //    private const Keys SaveHotKey = Keys.Control | Keys.S;
    //    private const Keys SaveAsHotKey = Keys.F12;

    //    #endregion

    //    #region Constants

    //    private static readonly Color ColorDefaultState = Themer.Instance.GetTheme().AltColor;
    //    private static readonly Color ColorChangedState = Themer.Instance.GetTheme().ArchiveChangedColor;

    //    #endregion

    //    #region Localization Keys

    //    private const string FileNotSuccessfullyLoadedKey_ = "FileNotSuccessfullyLoaded";
    //    private const string FileNotSuccessfullyLoadedWithPluginKey_ = "FileNotSuccessfullyLoadedWithPlugin";
    //    private const string FileAlreadyOpeningStatusKey_ = "FileAlreadyOpeningStatus";

    //    private const string NoTargetSelectedStatusKey_ = "NoTargetSelectedStatus";

    //    private const string NoFilesToExtractStatusKey_ = "NoFilesToExtractStatus";
    //    private const string NoFilesToReplaceStatusKey_ = "NoFilesToReplaceStatus";
    //    private const string NoFilesToRenameStatusKey_ = "NoFilesToRenameStatus";
    //    private const string NoFilesToDeleteStatusKey_ = "NoFilesToDeleteStatus";
    //    private const string NoFilesToAddStatusKey_ = "NoFilesToAddStatus";

    //    private const string ExtractFileProgressKey_ = "ExtractFileProgress";
    //    private const string ReplaceFileProgressKey_ = "ReplaceFileProgress";
    //    private const string RenameFileProgressKey_ = "RenameFileProgress";
    //    private const string DeleteFileProgressKey_ = "DeleteFileProgress";
    //    private const string AddFileProgressKey_ = "AddFileProgress";

    //    private const string ExtractFileCancelledStatusKey_ = "ExtractFileCancelledStatus";
    //    private const string ReplaceFileCancelledStatusKey_ = "ReplaceFileCancelledStatus";
    //    private const string RenameFileCancelledStatusKey_ = "RenameFileCancelledStatus";
    //    private const string DeleteFileCancelledStatusKey_ = "DeleteFileCancelledStatus";
    //    private const string AddFileCancelledStatusKey_ = "AddFileCancelledStatus";

    //    private const string ExtractFileSuccessfulStatusKy_ = "ExtractFileSuccessfulStatus";
    //    private const string ReplaceFileSuccessfulStatusKy_ = "ReplaceFileSuccessfulStatus";
    //    private const string RenameFileSuccessfulStatusKy_ = "RenameFileSuccessfulStatus";
    //    private const string DeleteFileSuccessfulStatusKy_ = "DeleteFileSuccessfulStatus";
    //    private const string AddFileSuccessfulStatusKy_ = "AddFileSuccessfulStatus";

    //    private const string UnableToAddFilesStatusKey_ = "UnableToAddFilesStatus";

    //    private const string NoNameGivenStatusKey_ = "NoNameGivenStatus";

    //    private const string RenameFileTitleKey_ = "RenameFileTitle";
    //    private const string RenameDirectoryTitleKey_ = "RenameDirectoryTitle";

    //    private const string RenameItemCaptionKey_ = "RenameItemCaption";

    //    #endregion

    //    #region Log messages

    //    private const string CouldNotAddFileLog_ = "Could not add file: {0}";

    //    #endregion

    //    public ArchiveForm(ArchiveFormInfo formInfo, FileManager fileManager)
    //    {
    //        InitializeComponent();

    //        _formInfo = formInfo;
    //        _fileManager = fileManager;

    //        _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
    //        _openingFiles = new List<IArchiveFileInfo>();

    //        _changedDirectories = new HashSet<UPath>();
    //        _selectedPath = UPath.Root;

    //        _searchTerm = new SearchTerm(searchTextBox);
    //        _searchTerm.TextChanged += searchTerm_TextChanged;

    //        _asyncOperation = new AsyncOperation();
    //        _asyncOperation.Started += asyncOperation_Started;
    //        _asyncOperation.Finished += asyncOperation_Finished;

    //        folderView.Expanded += folderView_Expanded;
    //        folderView.Collapsed += folderView_Collapsed;
    //        folderView.CellFormatting += folderView_CellFormatting;
    //        folderView.SelectedItemChanged += folderView_SelectedItemChanged;

    //        fileView.SelectedItemsChanged += fileView_SelectedItemsChanged;
    //        fileView.CellDoubleClick += fileView_CellDoubleClick;
    //        fileView.CellFormatting += fileView_CellFormatting;
    //        fileView.ColumnHeaderClick += fileView_ColumnHeaderClick;

    //        searchClearCommand.Executed += searchClearCommand_Executed;
    //        cancelCommand.Executed += cancelCommand_Executed;

    //        saveCommand.Executed += SaveCommand_Executed;
    //        saveAsCommand.Executed += SaveAsCommand_Executed;

    //        openCommand.Executed += openCommand_Executed;
    //        extractFileCommand.Executed += extractFileCommand_Executed;
    //        replaceFileCommand.Executed += replaceFileCommand_Executed;
    //        renameFileCommand.Executed += RenameFileCommand_Executed;
    //        deleteFileCommand.Executed += DeleteFileCommand_Executed;

    //        extractDirectoryCommand.Executed += extractDirectoryCommand_Executed;
    //        replaceDirectoryCommand.Executed += replaceDirectoryCommand_Executed;
    //        renameDirectoryCommand.Executed += renameDirectoryCommand_Executed;
    //        addDirectoryCommand.Executed += addDirectoryCommand_Executed;
    //        deleteDirectoryCommand.Executed += deleteDirectoryCommand_Executed;

    //        UpdateProperties();
    //        LoadDirectories();
    //        UpdateFiles(_selectedPath);
    //    }

    //    #region Forminterface methods

    //    public bool HasRunningOperations()
    //    {
    //        return _asyncOperation.IsRunning;
    //    }

    //    public void CancelOperations()
    //    {
    //        if (HasRunningOperations())
    //            _asyncOperation.Cancel();
    //    }

    //    #endregion

    //    #region Update

    //    public void UpdateForm()
    //    {
    //        Application.Instance.Invoke(() =>
    //        {
    //            // Update root name
    //            if (folders.Count > 0)
    //                ((TreeGridItem)folders[0]).Values[1] = _formInfo.FileState.FilePath.ToRelative().FullName;

    //            // Update form information
    //            UpdateProperties();
    //            UpdateDirectories();
    //            UpdateFiles(_selectedPath);
    //        });
    //    }

    //    private void UpdateProperties()
    //    {
    //        var canSave = _formInfo.FileState.PluginState.CanSave;

    //        // Menu
    //        saveButton.Enabled = canSave && !_asyncOperation.IsRunning;
    //        saveAsButton.Enabled = canSave && _formInfo.FileState.ParentFileState == null && !_asyncOperation.IsRunning;

    //        // Toolbar
    //        extractButton.Enabled = !_asyncOperation.IsRunning;
    //        replaceButton.Enabled = _formInfo.CanReplaceFiles && canSave && !_asyncOperation.IsRunning;
    //        renameButton.Enabled = _formInfo.CanRenameFiles && canSave && !_asyncOperation.IsRunning;
    //        deleteButton.Enabled = _formInfo.CanDeleteFiles && canSave && !_asyncOperation.IsRunning;
    //    }

    //    private void UpdateDirectories()
    //    {
    //        if (folders.Count <= 0)
    //            return;

    //        // If an update is triggered ba a parent, and therefore got this instance saved
    //        // We need to clear the changed directories
    //        if (!_formInfo.FileState.StateChanged)
    //            _changedDirectories.Clear();

    //        folderView.ReloadItem(folders[0]);
    //    }

    //    private void UpdateFiles(UPath path)
    //    {
    //        // Get all files by a given search term in the given path
    //        var enumeratedFiles = _archiveFileSystem.EnumerateFiles(path, _searchTerm.Get());

    //        // Sort all files by the selected sorting scheme
    //        switch (_sortingScheme)
    //        {
    //            case SortingScheme.SizeAsc:
    //                enumeratedFiles = enumeratedFiles.OrderBy(f => _archiveFileSystem.GetFileLength(f));
    //                break;

    //            case SortingScheme.SizeDes:
    //                enumeratedFiles = enumeratedFiles.OrderByDescending(f => _archiveFileSystem.GetFileLength(f));
    //                break;

    //            case SortingScheme.NameAsc:
    //                enumeratedFiles = enumeratedFiles.OrderBy(f => f.GetName());
    //                break;

    //            case SortingScheme.NameDes:
    //                enumeratedFiles = enumeratedFiles.OrderByDescending(f => f.GetName());
    //                break;
    //        }

    //        // Add enumeration of files to the DataStore
    //        fileView.DataStore = enumeratedFiles
    //            .Select(x => (AfiFileEntry)_archiveFileSystem.GetFileEntry(x))
    //            .Select(x => new FileElement(x.ArchiveFileInfo));
    //    }

    //    private void UpdateFileContextMenu()
    //    {
    //        var selectedItem = fileView.SelectedItems.FirstOrDefault();
    //        if (selectedItem == null)
    //            return;

    //        // Get current state arguments
    //        var isLoadLocked = IsFileLocked(selectedItem.ArchiveFileInfo, true);
    //        var isStateLocked = IsFileLocked(selectedItem.ArchiveFileInfo, false);

    //        var canExtractFiles = !isStateLocked && !_asyncOperation.IsRunning;
    //        var canReplaceFiles = _formInfo.CanReplaceFiles && !isLoadLocked && !_asyncOperation.IsRunning;
    //        var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
    //        var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;

    //        Application.Instance.Invoke(() =>
    //        {
    //            // Update Open with menu item
    //            openWithMenuItem.Items.Clear();
    //            foreach (var pluginId in selectedItem.ArchiveFileInfo.PluginIds ?? Array.Empty<Guid>())
    //            {
    //                var filePluginLoader = _fileManager.GetFilePluginLoaders().FirstOrDefault(x => x.Exists(pluginId));
    //                var filePlugin = filePluginLoader?.GetPlugin(pluginId);

    //                if (filePlugin == null)
    //                    continue;

    //                openWithCommand = new Command { MenuText = filePlugin.Metadata.Name, Tag = (selectedItem, pluginId) };
    //                openWithCommand.Executed += openWithCommand_Executed;

    //                openWithMenuItem.Items.Add(openWithCommand);
    //            }

    //            // Update context menu
    //            openCommand.Enabled = !_asyncOperation.IsRunning;
    //            openWithMenuItem.Enabled = openWithMenuItem.Items.Count > 0 && !_asyncOperation.IsRunning;

    //            extractFileCommand.Enabled = canExtractFiles;
    //            replaceFileCommand.Enabled = canReplaceFiles;
    //            renameFileCommand.Enabled = canRenameFiles;
    //            deleteFileCommand.Enabled = canDeleteFiles;
    //        });
    //    }

    //    private void UpdateDirectoryContextMenu()
    //    {
    //        var canExtractFiles = !_asyncOperation.IsRunning;
    //        var canReplaceFiles = _formInfo.CanReplaceFiles && !_asyncOperation.IsRunning;
    //        var canRenameFiles = _formInfo.CanRenameFiles && !_asyncOperation.IsRunning;
    //        var canDeleteFiles = _formInfo.CanDeleteFiles && !_asyncOperation.IsRunning;
    //        var canAddFiles = _formInfo.CanAddFiles && !_asyncOperation.IsRunning;

    //        Application.Instance.Invoke(() =>
    //        {
    //            extractDirectoryCommand.Enabled = canExtractFiles;
    //            replaceDirectoryCommand.Enabled = canReplaceFiles;
    //            renameDirectoryCommand.Enabled = canRenameFiles;
    //            deleteDirectoryCommand.Enabled = canDeleteFiles;
    //            addDirectoryCommand.Enabled = canAddFiles;
    //        });
    //    }

    //    #endregion

    //    #region Load

    //    /// <summary>
    //    /// Loads the whole directory tree and add it to the folder view.
    //    /// </summary>
    //    private void LoadDirectories()
    //    {
    //        // Clear tree view
    //        folders.Clear();

    //        // Load tree into tree items
    //        var rootItem = new TreeGridItem(ImageResources.Misc.Archive, _formInfo.FileState.FilePath.ToRelative().FullName) { Expanded = true };
    //        foreach (var directory in _archiveFileSystem.EnumerateAllDirectories(UPath.Root, _searchTerm.Get())
    //            .Concat(_archiveFileSystem.EnumerateAllFiles(UPath.Root, _searchTerm.Get()).Select(x => x.GetDirectory()))
    //            .Distinct())
    //        {
    //            directory.Split().Aggregate(rootItem, (node, currentDirectory) =>
    //            {
    //                var existingDirectory = (TreeGridItem)node.Children.FirstOrDefault(x =>
    //                   (string)((TreeGridItem)x).Values[1] == currentDirectory);

    //                if (existingDirectory != null)
    //                    return existingDirectory;

    //                var directoryItem = new TreeGridItem(ImageResources.Misc.Directory, currentDirectory);
    //                node.Children.Add(directoryItem);

    //                return directoryItem;
    //            });
    //        }

    //        // Add root item to tree view
    //        folders.Add(rootItem);
    //        folderView.ReloadItem(rootItem);
    //    }

    //    #endregion

    //    #region Events

    //    #region folderView

    //    private void folderView_SelectedItemChanged(object sender, EventArgs e)
    //    {
    //        if (folderView.SelectedItem == null)
    //            return;

    //        var gridItem = (TreeGridItem)folderView.SelectedItem;
    //        _selectedPath = GetAbsolutePath(gridItem);

    //        UpdateDirectoryContextMenu();
    //        UpdateFiles(_selectedPath);
    //    }

    //    private void folderView_Expanded(object sender, TreeGridViewItemEventArgs e)
    //    {
    //        var gridItem = (TreeGridItem)e.Item;
    //        gridItem.Values[0] = ImageResources.Misc.DirectoryOpen;

    //        folderView.ReloadItem(gridItem, false);
    //    }

    //    private void folderView_Collapsed(object sender, TreeGridViewItemEventArgs e)
    //    {
    //        var gridItem = (TreeGridItem)e.Item;
    //        gridItem.Values[0] = ImageResources.Misc.Directory;

    //        folderView.ReloadItem(gridItem, false);
    //    }

    //    private void folderView_CellFormatting(object sender, GridCellFormatEventArgs e)
    //    {
    //        if (e.Item == null)
    //            return;

    //        var gridItem = (TreeGridItem)e.Item;
    //        var path = GetAbsolutePath(gridItem);
    //        e.ForegroundColor = _changedDirectories.Contains(path) ? ColorChangedState : ColorDefaultState;
    //    }

    //    #endregion

    //    #region fileView

    //    private void fileView_SelectedItemsChanged(object sender, EventArgs e)
    //    {
    //        UpdateFileContextMenu();
    //    }

    //    private async void fileView_CellDoubleClick(object sender, GridCellMouseEventArgs e)
    //    {
    //        if (e.Item == null)
    //            return;

    //        await OpenSelectedFiles();
    //    }

    //    private void fileView_CellFormatting(object sender, GridCellFormatEventArgs e)
    //    {
    //        if (e.Item == null)
    //            return;

    //        var element = (FileElement)e.Item;
    //        var isChanged = element.ArchiveFileInfo.ContentChanged || _formInfo.FileState.ArchiveChildren.Where(x => x.StateChanged).Any(x => x.FilePath == element.ArchiveFileInfo.FilePath);
    //        e.ForegroundColor = isChanged ? ColorChangedState : ColorDefaultState;
    //    }

    //    private void fileView_ColumnHeaderClick(object sender, GridColumnEventArgs e)
    //    {
    //        if (e.Column.ID == "Size")
    //        {
    //            if (_sortingScheme == SortingScheme.SizeAsc)
    //                _sortingScheme = SortingScheme.SizeDes;
    //            else
    //                _sortingScheme = SortingScheme.SizeAsc;
    //        }
    //        else if (e.Column.ID == "Name")
    //        {
    //            if (_sortingScheme == SortingScheme.NameAsc)
    //                _sortingScheme = SortingScheme.NameDes;
    //            else
    //                _sortingScheme = SortingScheme.NameAsc;
    //        }
    //        UpdateFiles(_selectedPath);
    //    }

    //    #endregion

    //    #region searchTerm

    //    private void searchClearCommand_Executed(object sender, EventArgs e)
    //    {
    //        _searchTerm.Clear();
    //    }

    //    private void searchTerm_TextChanged(object sender, EventArgs e)
    //    {
    //        LoadDirectories();
    //        UpdateFiles(UPath.Root);
    //    }

    //    #endregion

    //    #region asyncOperation

    //    private void asyncOperation_Started(object sender, EventArgs e)
    //    {
    //        UpdateFileContextMenu();
    //        UpdateDirectoryContextMenu();
    //        UpdateProperties();
    //    }

    //    private void asyncOperation_Finished(object sender, EventArgs e)
    //    {
    //        UpdateFileContextMenu();
    //        UpdateDirectoryContextMenu();
    //        UpdateProperties();
    //    }

    //    private void cancelCommand_Executed(object sender, EventArgs e)
    //    {
    //        _asyncOperation.Cancel();
    //    }

    //    #endregion

    //    #region fileContextMenu

    //    private async void openCommand_Executed(object sender, EventArgs e)
    //    {
    //        await OpenSelectedFiles();
    //    }

    //    private async void openWithCommand_Executed(object sender, EventArgs e)
    //    {
    //        var (fileElement, pluginId) = ((FileElement, Guid))((Command)sender).Tag;
    //        if (!await OpenFile(fileElement.ArchiveFileInfo, pluginId))
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(FileNotSuccessfullyLoadedWithPluginKey_, pluginId));
    //    }

    //    private async void extractFileCommand_Executed(object sender, EventArgs e)
    //    {
    //        await ExtractSelectedFiles();
    //    }

    //    private async void replaceFileCommand_Executed(object sender, EventArgs e)
    //    {
    //        await ReplaceSelectedFiles();
    //    }

    //    private async void RenameFileCommand_Executed(object sender, EventArgs e)
    //    {
    //        await RenameSelectedFiles();
    //    }

    //    private async void DeleteFileCommand_Executed(object sender, EventArgs e)
    //    {
    //        await DeleteSelectedFiles();
    //    }

    //    #endregion

    //    #region directoryContextMenu

    //    private async void extractDirectoryCommand_Executed(object sender, EventArgs e)
    //    {
    //        await ExtractSelectedDirectory();
    //    }

    //    private async void replaceDirectoryCommand_Executed(object sender, EventArgs e)
    //    {
    //        await ReplaceSelectedDirectory();
    //    }

    //    private async void renameDirectoryCommand_Executed(object sender, EventArgs e)
    //    {
    //        await RenameSelectedDirectory();
    //    }

    //    private async void addDirectoryCommand_Executed(object sender, EventArgs e)
    //    {
    //        await AddFilesToSelectedItem();
    //    }

    //    private async void deleteDirectoryCommand_Executed(object sender, EventArgs e)
    //    {
    //        await DeleteSelectedDirectory();
    //    }

    //    #endregion

    //    #endregion

    //    #region Open Files

    //    private Task OpenSelectedFiles()
    //    {
    //        return OpenFiles(fileView.SelectedItems.ToArray());
    //    }

    //    private async Task OpenFiles(IList<FileElement> fileElements)
    //    {
    //        if (fileElements == null)
    //            return;

    //        foreach (var file in fileElements.Select(x => x.ArchiveFileInfo))
    //        {
    //            var pluginIds = file.PluginIds ?? Array.Empty<Guid>();

    //            if (pluginIds.Any())
    //            {
    //                // Opening by plugin id
    //                var opened = false;
    //                foreach (var pluginId in pluginIds)
    //                {
    //                    if (_openingFiles.Contains(file))
    //                    {
    //                        _formInfo.FormCommunicator.ReportStatus(false, Localize(FileAlreadyOpeningStatusKey_, file.FilePath.ToRelative()));
    //                        continue;
    //                    }

    //                    _openingFiles.Add(file);
    //                    if (await OpenFile(file, pluginId))
    //                    {
    //                        _openingFiles.Remove(file);

    //                        opened = true;
    //                        break;
    //                    }

    //                    _openingFiles.Remove(file);
    //                }

    //                if (opened)
    //                    continue;
    //            }

    //            // Use automatic identification if no preset plugin could open the file
    //            if (_openingFiles.Contains(file))
    //            {
    //                _formInfo.FormCommunicator.ReportStatus(false, Localize(FileAlreadyOpeningStatusKey_, file.FilePath.ToRelative()));
    //                continue;
    //            }

    //            _openingFiles.Add(file);
    //            if (!await OpenFile(file))
    //                _formInfo.FormCommunicator.ReportStatus(false, Localize(FileNotSuccessfullyLoadedKey_));

    //            _openingFiles.Remove(file);
    //        }
    //    }

    //    private Task<bool> OpenFile(IArchiveFileInfo afi, Guid pluginId = default)
    //    {
    //        return pluginId == default ? _formInfo.FormCommunicator.Open(afi) : _formInfo.FormCommunicator.Open(afi, pluginId);
    //    }

    //    #endregion

    //    #region Save Files

    //    private void SaveCommand_Executed(object sender, EventArgs e)
    //    {
    //        Save(false);
    //    }

    //    private void SaveAsCommand_Executed(object sender, EventArgs e)
    //    {
    //        Save(true);
    //    }

    //    private async void Save(bool saveAs)
    //    {
    //        var wasSuccessful = await _formInfo.FormCommunicator.Save(saveAs);
    //        if (!wasSuccessful)
    //            return;

    //        _changedDirectories.Clear();
    //        _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_formInfo.FileState);
    //        _selectedPath = UPath.Root;

    //        await Application.Instance.InvokeAsync(() =>
    //        {
    //            LoadDirectories();
    //            UpdateFiles(UPath.Root);
    //            UpdateProperties();

    //            _formInfo.FormCommunicator.Update(true, false);
    //        });
    //    }

    //    #endregion

    //    #region Extraction

    //    #region Extract Files

    //    private Task ExtractSelectedFiles()
    //    {
    //        return ExtractFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
    //    }

    //    private async Task ExtractFiles(IList<IArchiveFileInfo> files)
    //    {
    //        if (files.Count <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToExtractStatusKey_));
    //            return;
    //        }

    //        // Select folder or file
    //        var selectedPath = files.Count > 1 ? SelectFolder() : SaveFile(files[0].FilePath.GetName());
    //        if (selectedPath.IsNull || selectedPath.IsEmpty)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //            return;
    //        }

    //        // Use containing directory as root if a file was selected
    //        var extractRoot = files.Count > 1 ? selectedPath : selectedPath.GetDirectory();

    //        // Extract elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(extractRoot.FullName, _formInfo.FileState.StreamManager);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(async cts =>
    //        {
    //            var count = 0;
    //            foreach (var file in files)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(ExtractFileProgressKey_), count++, files.Count);

    //                if (IsFileLocked(file, false))
    //                    continue;

    //                Stream newFileStream;
    //                try
    //                {
    //                    // Use in-archive filename if a folder was selected, use selected filename if a file was selected
    //                    var extractName = files.Count > 1 ? file.FilePath.GetName() : selectedPath.GetName();
    //                    newFileStream = destinationFileSystem.OpenFile(extractName, FileMode.Create, FileAccess.Write);
    //                }
    //                catch (IOException)
    //                {
    //                    continue;
    //                }
    //                var currentFileStream = file.GetFileData().Result;

    //                currentFileStream.CopyTo(newFileStream);

    //                newFileStream.Close();
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(ExtractFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(ExtractFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(ExtractFileSuccessfulStatusKy_));
    //    }

    //    #endregion

    //    #region Extract Directories

    //    private Task ExtractSelectedDirectory()
    //    {
    //        return ExtractDirectory((TreeGridItem)folderView.SelectedItem);
    //    }

    //    private async Task ExtractDirectory(TreeGridItem item)
    //    {
    //        var itemPath = GetAbsolutePath(item);
    //        var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath, _searchTerm.Get()).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

    //        if (filePaths.Length <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToExtractStatusKey_));
    //            return;
    //        }

    //        // Select folder
    //        var extractPath = SelectFolder();
    //        if (extractPath.IsNull || extractPath.IsEmpty)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //            return;
    //        }

    //        // Extract elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        var subFolder = item == folders[0] ? GetRootName() : GetAbsolutePath(item).ToRelative();
    //        var destinationFileSystem = FileSystemFactory.CreateSubFileSystem((extractPath / subFolder).FullName, _formInfo.FileState.StreamManager);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var filePath in filePaths)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(ExtractFileProgressKey_), count++, filePaths.Length);

    //                var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
    //                if (IsFileLocked(afi, false))
    //                    continue;

    //                Stream newFileStream;
    //                try
    //                {
    //                    newFileStream = destinationFileSystem.OpenFile(filePath, FileMode.Create, FileAccess.Write);
    //                }
    //                catch (IOException)
    //                {
    //                    continue;
    //                }

    //                var currentFileStream = afi.GetFileData().Result;

    //                currentFileStream.CopyTo(newFileStream);

    //                newFileStream.Close();
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(ExtractFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(ExtractFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(ExtractFileSuccessfulStatusKy_));
    //    }

    //    #endregion

    //    #endregion

    //    #region Replacement

    //    #region Replace Files

    //    private Task ReplaceSelectedFiles()
    //    {
    //        return ReplaceFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
    //    }

    //    private async Task ReplaceFiles(IList<IArchiveFileInfo> files)
    //    {
    //        if (files.Count <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToReplaceStatusKey_));
    //            return;
    //        }

    //        // Select destination
    //        UPath replaceDirectory;
    //        UPath replaceFileName;
    //        if (files.Count == 1)
    //        {
    //            var selectedPath = OpenFile(files[0].FilePath.GetName());
    //            if (selectedPath.IsNull || selectedPath.IsEmpty)
    //            {
    //                _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //                return;
    //            }

    //            replaceDirectory = selectedPath.GetDirectory();
    //            replaceFileName = selectedPath.GetName();
    //        }
    //        else
    //        {
    //            var selectedPath = SelectFolder();
    //            if (selectedPath.IsNull || selectedPath.IsEmpty)
    //            {
    //                _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //                return;
    //            }

    //            replaceDirectory = selectedPath;
    //            replaceFileName = UPath.Empty;
    //        }

    //        // Extract elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replaceDirectory.FullName, _formInfo.FileState.StreamManager);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var file in files)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(ReplaceFileProgressKey_), count++, files.Count);

    //                if (IsFileLocked(file, true))
    //                    continue;

    //                var filePath = replaceFileName.IsEmpty ? file.FilePath.GetName() : replaceFileName;
    //                if (!sourceFileSystem.FileExists(filePath))
    //                    continue;

    //                var currentFileStream = sourceFileSystem.OpenFile(filePath);
    //                //TODO this cast smells, should IFileState/IPluginState be generified?
    //                ((IArchiveState)_formInfo.FileState.PluginState).AttemptReplaceFile(file, currentFileStream);

    //                AddChangedDirectory(file.FilePath.GetDirectory());
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(ReplaceFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(ReplaceFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(ReplaceFileSuccessfulStatusKy_));

    //        UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
    //        UpdateDirectories();

    //        _formInfo.FormCommunicator.Update(true, false);
    //    }

    //    #endregion

    //    #region Replace Directories

    //    private Task ReplaceSelectedDirectory()
    //    {
    //        return ReplaceDirectory((TreeGridItem)folderView.SelectedItem);
    //    }

    //    private async Task ReplaceDirectory(TreeGridItem item)
    //    {
    //        var itemPath = GetAbsolutePath(item);
    //        var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

    //        if (filePaths.Length <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToReplaceStatusKey_));
    //            return;
    //        }

    //        // Select folder
    //        var replacePath = SelectFolder();
    //        if (replacePath.IsNull || replacePath.IsEmpty)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //            return;
    //        }

    //        // Extract elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(replacePath.FullName, _formInfo.FileState.StreamManager);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var filePath in filePaths)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(ReplaceFileProgressKey_), count++, filePaths.Length);

    //                var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
    //                if (IsFileLocked(afi, true))
    //                    continue;

    //                if (!sourceFileSystem.FileExists(filePath))
    //                    continue;

    //                var currentFileStream = sourceFileSystem.OpenFile(filePath);
    //                //TODO this cast smells, should IFileState/IPluginState be generified?
    //                ((IArchiveState)_formInfo.FileState.PluginState).AttemptReplaceFile(afi, currentFileStream);

    //                AddChangedDirectory(afi.FilePath.GetDirectory());
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(ReplaceFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(ReplaceFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(ReplaceFileSuccessfulStatusKy_));

    //        UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
    //        UpdateDirectories();

    //        _formInfo.FormCommunicator.Update(true, false);
    //    }

    //    #endregion

    //    #endregion

    //    #region Renaming

    //    #region Rename Files

    //    private Task RenameSelectedFiles()
    //    {
    //        return RenameFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
    //    }

    //    private async Task RenameFiles(IList<IArchiveFileInfo> files)
    //    {
    //        if (files.Count <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToRenameStatusKey_));
    //            return;
    //        }

    //        // Rename elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var file in files)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(RenameFileProgressKey_), count++, files.Count);

    //                // Select new name
    //                var newName = Application.Instance.Invoke(() =>
    //                {
    //                    var inputBox = new InputBoxDialog(Localize(RenameItemCaptionKey_, file.FilePath.GetName()),
    //                        Localize(RenameFileTitleKey_), file.FilePath.GetName());
    //                    return inputBox.ShowModal(this);
    //                });

    //                if (string.IsNullOrEmpty(newName))
    //                    continue;

    //                // Rename possibly open file in main form
    //                _formInfo.FormCommunicator.Rename(file, file.FilePath.GetDirectory() / newName);

    //                // Rename file in archive
    //                _archiveFileSystem.MoveFile(file.FilePath, file.FilePath.GetDirectory() / newName);

    //                AddChangedDirectory(file.FilePath.GetDirectory());
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(RenameFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(RenameFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(RenameFileSuccessfulStatusKy_));

    //        UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
    //        UpdateDirectories();
    //    }

    //    #endregion

    //    #region Rename Directories

    //    private Task RenameSelectedDirectory()
    //    {
    //        return RenameDirectory((TreeGridItem)folderView.SelectedItem);
    //    }

    //    private async Task RenameDirectory(TreeGridItem item)
    //    {
    //        var itemPath = GetAbsolutePath(item);
    //        var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

    //        if (filePaths.Length <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToRenameStatusKey_));
    //            return;
    //        }

    //        // Select new directory name
    //        var inputBox = new InputBoxDialog(Localize(RenameItemCaptionKey_, GetItemName(item)),
    //            Localize(RenameDirectoryTitleKey_), GetItemName(item));
    //        var newName = inputBox.ShowModal(this);

    //        if (string.IsNullOrEmpty(newName))
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoNameGivenStatusKey_));
    //            return;
    //        }

    //        // Rename elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        var newDirectoryPath = GetAbsolutePath(item).GetDirectory() / newName;

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var filePath in filePaths)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(RenameFileProgressKey_), count++, filePaths.Length);

    //                // Rename possibly open file in main form
    //                var afi = ((AfiFileEntry)_archiveFileSystem.GetFileEntry(itemPath / filePath)).ArchiveFileInfo;
    //                _formInfo.FormCommunicator.Rename(afi, newDirectoryPath / filePath.ToRelative());

    //                // Rename file in archive
    //                _archiveFileSystem.MoveFile(afi.FilePath, newDirectoryPath / filePath.ToRelative());

    //                AddChangedDirectory(afi.FilePath.GetDirectory());
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(RenameFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(RenameFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(RenameFileSuccessfulStatusKy_));

    //        SetItemName(item, newName);

    //        UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));
    //        UpdateDirectories();
    //    }

    //    #endregion

    //    #endregion

    //    #region Deletion

    //    #region Delete Files

    //    private Task DeleteSelectedFiles()
    //    {
    //        return DeleteFiles(fileView.SelectedItems.Select(x => x.ArchiveFileInfo).ToArray());
    //    }

    //    private async Task DeleteFiles(IList<IArchiveFileInfo> files)
    //    {
    //        if (files.Count <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToDeleteStatusKey_));
    //            return;
    //        }

    //        // Delete elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var file in files)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(DeleteFileProgressKey_), count++, files.Count);

    //                _archiveFileSystem.DeleteFile(file.FilePath);

    //                AddChangedDirectory(file.FilePath.GetDirectory());
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(DeleteFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(DeleteFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(DeleteFileSuccessfulStatusKy_));

    //        UpdateDirectories();
    //        UpdateFiles(_selectedPath);

    //        _formInfo.FormCommunicator.Update(true, false);
    //    }

    //    #endregion

    //    #region Delete Directories

    //    private Task DeleteSelectedDirectory()
    //    {
    //        return DeleteDirectory((TreeGridItem)folderView.SelectedItem);
    //    }

    //    private async Task DeleteDirectory(TreeGridItem item)
    //    {
    //        var itemPath = GetAbsolutePath(item);
    //        var filePaths = _archiveFileSystem.EnumerateAllFiles(itemPath).Select(x => x.GetSubDirectory(itemPath).ToRelative()).ToArray();

    //        if (filePaths.Length <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(NoFilesToDeleteStatusKey_));
    //            return;
    //        }

    //        // Delete elements
    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        _formInfo.Progress.StartProgress();
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var filePath in filePaths)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(DeleteFileProgressKey_), count++, filePaths.Length);

    //                _archiveFileSystem.DeleteFile(itemPath / filePath);

    //                AddChangedDirectory(itemPath);
    //            }
    //        });

    //        _archiveFileSystem.DeleteDirectory(itemPath, true);

    //        _formInfo.Progress.ReportProgress(Localize(DeleteFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(DeleteFileCancelledStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(DeleteFileSuccessfulStatusKy_));

    //        LoadDirectories();
    //        UpdateFiles(UPath.Root);

    //        _formInfo.FormCommunicator.Update(true, false);
    //    }

    //    #endregion

    //    #endregion

    //    #region Adding

    //    private Task AddFilesToSelectedItem()
    //    {
    //        return AddFiles((TreeGridItem)folderView.SelectedItem);
    //    }

    //    private async Task AddFiles(TreeGridItem item)
    //    {
    //        // Select folder
    //        var selectedPath = SelectFolder();
    //        if (selectedPath.IsNull || selectedPath.IsEmpty)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoTargetSelectedStatusKey_));
    //            return;
    //        }

    //        // Add elements
    //        var subFolder = GetAbsolutePath(item);
    //        var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(selectedPath.FullName, _formInfo.FileState.StreamManager);

    //        var elements = sourceFileSystem.EnumerateAllFiles(UPath.Root).ToArray();
    //        if (elements.Length <= 0)
    //        {
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(NoFilesToAddStatusKey_));
    //            return;
    //        }

    //        _formInfo.FormCommunicator.ReportStatus(true, string.Empty);

    //        _formInfo.Progress.StartProgress();
    //        var filesNotAdded = false;
    //        await _asyncOperation.StartAsync(cts =>
    //        {
    //            var count = 0;
    //            foreach (var filePath in elements)
    //            {
    //                if (cts.IsCancellationRequested)
    //                    break;

    //                _formInfo.Progress.ReportProgress(Localize(AddFileProgressKey_), count++, elements.Length);

    //                // Do not add file if it already exists
    //                // This would be replacement and is not part of this operation
    //                if (_archiveFileSystem.FileExists(subFolder / filePath.ToRelative()))
    //                    continue;

    //                // TODO: This will currently copy files to memory, instead of just using a reference to any more memory efficient stream (like FileStream)
    //                Stream createdFile;
    //                try
    //                {
    //                    // The plugin can throw if a file is not addable
    //                    createdFile = _archiveFileSystem.OpenFile(subFolder / filePath.ToRelative(), FileMode.Create, FileAccess.Write);
    //                }
    //                catch (Exception e)
    //                {
    //                    // HINT: Log messages are not localized
    //                    _formInfo.Logger.Fatal(e, CouldNotAddFileLog_, filePath);
    //                    filesNotAdded = true;

    //                    continue;
    //                }
    //                var sourceFile = sourceFileSystem.OpenFile(filePath);
    //                sourceFile.CopyTo(createdFile);

    //                sourceFile.Close();
    //            }
    //        });
    //        _formInfo.Progress.ReportProgress(Localize(AddFileProgressKey_), 1, 1);
    //        _formInfo.Progress.FinishProgress();

    //        if (_asyncOperation.WasCancelled)
    //            _formInfo.FormCommunicator.ReportStatus(false, Localize(AddFileCancelledStatusKey_));
    //        else if (filesNotAdded)
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(UnableToAddFilesStatusKey_));
    //        else
    //            _formInfo.FormCommunicator.ReportStatus(true, Localize(AddFileSuccessfulStatusKy_));

    //        UpdateFiles(GetAbsolutePath((TreeGridItem)folderView.SelectedItem));

    //        _formInfo.FormCommunicator.Update(true, false);
    //    }

    //    #endregion

    //    #region Support

    //    private string Localize(string name, params object[] args)
    //    {
    //        return string.Format(Application.Instance.Localize(this, name), args);
    //    }

    //    private UPath GetAbsolutePath(TreeGridItem item)
    //    {
    //        if (item?.Parent == null)
    //            return UPath.Root;

    //        return GetAbsolutePath((TreeGridItem)item.Parent) / (string)item.Values[1];
    //    }

    //    private bool IsFileLocked(IArchiveFileInfo afi, bool lockOnLoaded)
    //    {
    //        var absolutePath = _formInfo.FileState.AbsoluteDirectory / _formInfo.FileState.FilePath.ToRelative() / afi.FilePath.ToRelative();

    //        var isLoaded = _fileManager.IsLoaded(absolutePath);
    //        if (!isLoaded)
    //            return false;

    //        if (lockOnLoaded)
    //            return true;

    //        var openedState = _fileManager.GetLoadedFile(absolutePath);
    //        return openedState.StateChanged;
    //    }

    //    private UPath OpenFile(string fileName)
    //    {
    //        return SelectFile<OpenFileDialog>(fileName);
    //    }

    //    private UPath SaveFile(string fileName)
    //    {
    //        return SelectFile<SaveFileDialog>(fileName);
    //    }

    //    private UPath SelectFile<DialogType>(string fileName) where DialogType : FileDialog, new()
    //    {
    //        var ofd = new DialogType
    //        {
    //            Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
    //            FileName = fileName
    //        };

    //        var result = ofd.ShowDialog(this) == DialogResult.Ok ? ofd.FileName : UPath.Empty;

    //        if (result != UPath.Empty)
    //        {
    //            Settings.Default.LastDirectory = result.FullName;
    //            Settings.Default.Save();
    //        }

    //        return result;
    //    }

    //    private UPath SelectFolder()
    //    {
    //        var sfd = new SelectFolderDialog
    //        {
    //            Directory = Settings.Default.LastDirectory
    //        };
    //        var result = sfd.ShowDialog(this) == DialogResult.Ok ? sfd.Directory : UPath.Empty;

    //        if (result != UPath.Empty)
    //        {
    //            Settings.Default.LastDirectory = result.FullName;
    //            Settings.Default.Save();
    //        }

    //        return result;
    //    }

    //    private void AddChangedDirectory(UPath path)
    //    {
    //        while (path != UPath.Root && !path.IsEmpty)
    //        {
    //            _changedDirectories.Add(path);
    //            path = path.GetDirectory();
    //        }
    //    }

    //    private string GetRootName()
    //    {
    //        return GetItemName(folders.FirstOrDefault() as TreeGridItem);
    //    }

    //    private string GetItemName(TreeGridItem item)
    //    {
    //        return (string)item?.Values[1];
    //    }

    //    private void SetItemName(TreeGridItem item, string name)
    //    {
    //        item.Values[1] = name;
    //    }

    //    #endregion
    //}
}
