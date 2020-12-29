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
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;
using Kore.Managers.Plugins;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    public partial class ArchiveForm : Panel, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly IArchiveFormCommunicator _communicator;
        private readonly PluginManager _pluginManager;
        private readonly IProgressContext _progress;

        private readonly IFileSystem _archiveFileSystem;
        private readonly IList<IArchiveFileInfo> _openingFiles;

        private readonly HashSet<UPath> _changedDirectories;
        private UPath _selectedPath;

        private readonly SearchTerm _searchTerm;
        private readonly AsyncOperation _asyncOperation;

        #region Constants

        private const string TreeArchiveResourceName = "Kuriimu2.EtoForms.Images.tree-archive-file.png";
        private const string TreeDirectoryResourceName = "Kuriimu2.EtoForms.Images.tree-directory.png";
        private const string TreeOpenDirectoryResourceName = "Kuriimu2.EtoForms.Images.tree-directory-open.png";

        private static readonly Color ColorDefaultState = KnownColors.Black;
        private static readonly Color ColorChangedState = KnownColors.Orange;

        #endregion

        #region Loaded image resources

        private readonly Image TreeArchiveResource = Bitmap.FromResource(TreeArchiveResourceName);
        private readonly Image TreeDirectoryResource = Bitmap.FromResource(TreeDirectoryResourceName);
        private readonly Image TreeOpenDirectoryResource = Bitmap.FromResource(TreeOpenDirectoryResourceName);

        #endregion

        public ArchiveForm(IStateInfo stateInfo, IArchiveFormCommunicator communicator, PluginManager pluginManager, IProgressContext progress)
        {
            InitializeComponent();

            _stateInfo = stateInfo;
            _communicator = communicator;
            _pluginManager = pluginManager;
            _progress = progress;

            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(stateInfo);
            _openingFiles = new List<IArchiveFileInfo>();

            _changedDirectories = new HashSet<UPath>();
            _selectedPath = UPath.Root;

            _searchTerm = new SearchTerm(searchTextBox);
            _searchTerm.TextChanged += searchTerm_TextChanged;

            _asyncOperation=new AsyncOperation();
            _asyncOperation.Toggled += asyncOperation_Toggled;

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

            // TODO: Attach file command events
            // TODO: Add file and folder actions to form (via context menu as well)

            UpdateProperties();
            LoadDirectories();
            UpdateFiles(_selectedPath);
        }

        #region Update

        public void UpdateForm()
        {
            Application.Instance.Invoke(() =>
            {
                // Update root name
                if (folders.Count > 0)
                    ((TreeGridItem)folders[0]).Values[1] = _stateInfo.FilePath.ToRelative().FullName;

                // Update form information
                UpdateProperties();
                UpdateDirectories();
                UpdateFiles(_selectedPath);
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateProperties()
        {
            var canSave = _stateInfo.PluginState is ISaveFiles;

            // Menu
            saveButton.Enabled = canSave && !_asyncOperation.IsRunning;
            saveAsButton.Enabled = canSave && _stateInfo.ParentStateInfo == null && !_asyncOperation.IsRunning;

            // Toolbar
            extractButton.Enabled = !_asyncOperation.IsRunning;
            replaceButton.Enabled = _stateInfo.PluginState is IReplaceFiles && canSave && !_asyncOperation.IsRunning;
            renameButton.Enabled = _stateInfo.PluginState is IRenameFiles && canSave && !_asyncOperation.IsRunning;
            deleteButton.Enabled = _stateInfo.PluginState is IRemoveFiles && canSave && !_asyncOperation.IsRunning;
        }

        private void UpdateDirectories()
        {
            if (folders.Count <= 0)
                return;

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
            var selectedItems = fileView.SelectedItems.ToArray();
            if (selectedItems.Length <= 0)
                return;

            // Get current state arguments
            var isLoadLocked = selectedItems.Any(x=>IsFileLocked(x.ArchiveFileInfo, true));
            var isStateLocked = selectedItems.Any(x => IsFileLocked(x.ArchiveFileInfo, false));

            var canExtractFiles = !isStateLocked && !_asyncOperation.IsRunning;
            var canReplaceFiles = _stateInfo.PluginState is IReplaceFiles && !isLoadLocked && !_asyncOperation.IsRunning;
            var canRenameFiles = _stateInfo.PluginState is IRenameFiles && !_asyncOperation.IsRunning;
            var canDeleteFiles = _stateInfo.PluginState is IRemoveFiles && !_asyncOperation.IsRunning;

            // Update context menu
            extractFileCommand.Enabled = canExtractFiles;
            replaceFileCommand.Enabled = canReplaceFiles;
            renameFileCommand.Enabled = canRenameFiles;
            deleteFileCommand.Enabled = canDeleteFiles;
        }

        private void UpdateDirectoryContextMenu()
        {
            // TODO: Make logic
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
            var rootItem = new TreeGridItem(TreeArchiveResource, _stateInfo.FilePath.ToRelative().FullName) { Expanded = true };
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

                    var directoryItem = new TreeGridItem(TreeDirectoryResource, currentDirectory);
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
            gridItem.Values[0] = TreeOpenDirectoryResource;

            folderView.ReloadItem(gridItem, false);
        }

        private void folderView_Collapsed(object sender, TreeGridViewItemEventArgs e)
        {
            var gridItem = (TreeGridItem)e.Item;
            gridItem.Values[0] = TreeDirectoryResource;

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
            var isChanged = element.ArchiveFileInfo.ContentChanged || _stateInfo.ArchiveChildren.Where(x => x.StateChanged).Any(x => x.FilePath == element.ArchiveFileInfo.FilePath);
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

        private void asyncOperation_Toggled(object sender, EventArgs e)
        {
            UpdateFileContextMenu();
            UpdateDirectoryContextMenu();
            UpdateProperties();
        }

        private void cancelCommand_Executed(object sender, EventArgs e)
        {
            // TODO: Implement Cancel
            //_asyncOperation.Cancel();
        }

        #endregion

        #region extractCommand

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
                            _communicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                            continue;
                        }

                        _openingFiles.Add(file);
                        if (await _communicator.Open(file, pluginId))
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
                    _communicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                    continue;
                }

                _openingFiles.Add(file);
                if (!await _communicator.Open(file))
                    _communicator.ReportStatus(false, "File couldn't be opened.");

                _openingFiles.Remove(file);
            }
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
            var wasSuccessful = await _communicator.Save(saveAs);
            if (!wasSuccessful)
                return;

            await Application.Instance.InvokeAsync(() =>
            {
                UpdateDirectories();
                UpdateFiles(UPath.Root);
                UpdateProperties();

                _communicator.Update(true, false);
            });
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
            var absolutePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath.ToRelative() / afi.FilePath.ToRelative();

            var isLoaded = _pluginManager.IsLoaded(absolutePath);
            if (!isLoaded)
                return false;

            if (lockOnLoaded)
                return true;

            var openedState = _pluginManager.GetLoadedFile(absolutePath);
            return openedState.StateChanged;
        }

        #endregion
    }
}
