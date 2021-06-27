﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers.Plugins;
using Kuriimu2.WinForms.MainForms.Interfaces;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IKuriimuForm
    {
        private const string AllFilesFilter_ = "All Files (*.*)|*.*";
        private const string FileCount_ = "Files: {0}";

        private static readonly Color ColorDefaultState = Color.Black;
        private static readonly Color ColorChangedState = Color.Orange;

        private readonly IArchiveFormCommunicator _formCommunicator;
        private readonly IInternalPluginManager _pluginManager;
        private readonly IStateInfo _stateInfo;
        private readonly IProgressContext _progressContext;

        private readonly IList<UPath> _expandedPaths;
        private readonly IList<IArchiveFileInfo> _openingFiles;

        private bool _isSearchEmpty = true;
        private string _searchTerm;

        private bool _isOperationRunning;
        private CancellationTokenSource _operationToken;

        private IArchiveState ArchiveState => _stateInfo.PluginState as IArchiveState;

        public ArchiveForm(IStateInfo loadedState, IArchiveFormCommunicator formCommunicator, IInternalPluginManager pluginManager, IProgressContext progressContext)
        {
            InitializeComponent();

            // Populate image list
            imlFiles.Images.Add("tree-directory", Resources.tree_directory);
            imlFiles.Images.Add("tree-directory-open", Resources.tree_directory_open);
            imlFiles.Images.Add("tree-text-file", Resources.tree_text_file);
            imlFiles.Images.Add("tree-image-file", Resources.tree_image_file);
            imlFiles.Images.Add("tree-archive-file", Resources.tree_archive_file);
            imlFilesLarge.Images.Add("tree-directory", Resources.tree_directory_32);
            imlFilesLarge.Images.Add("tree-directory-open", Resources.tree_directory_open);
            imlFilesLarge.Images.Add("tree-text-file", Resources.tree_text_file_32);
            imlFilesLarge.Images.Add("tree-image-file", Resources.tree_image_file_32);
            imlFilesLarge.Images.Add("tree-archive-file", Resources.tree_archive_file_32);

            _stateInfo = loadedState;
            _progressContext = progressContext;

            _formCommunicator = formCommunicator;
            _pluginManager = pluginManager;

            _expandedPaths = new List<UPath>();
            _openingFiles = new List<IArchiveFileInfo>();

            UpdateDirectories();
            UpdateProperties();
        }

        #region Update Methods

        public void UpdateForm()
        {
            UpdateProperties();

            InvokeAction(() =>
            {
                var rootNode = GetRootNode();
                if (rootNode != null)
                    rootNode.Text = _stateInfo.FilePath.ToRelative().FullName;
            });

            UpdateDirectoryColors();
            UpdateFileColors();
        }

        #region Update File information

        private void UpdateFiles()
        {
            var files = EnumerateFilteredFiles(treDirectories.SelectedNode).ToArray();

            if (files.Length <= 0)
            {
                InvokeAction(() => lstFiles.Items.Clear());
                return;
            }

            // TODO: Review action to do most actual form updates at the end for less blocking
            UpdateListView(lstFiles, listView =>
            {
                listView.Items.Clear();

                imlFiles.Images.Add("0", Resources.menu_new);

                foreach (var file in files)
                {
                    var listViewItem = new ListViewItem(new[] { file.FilePath.GetName(), file.FileSize.ToString() },
                        "0", ColorDefaultState, Color.Transparent, listView.Font)
                    {
                        Tag = file
                    };

                    UpdateFileColor(listViewItem);
                    listView.Items.Add(listViewItem);
                }

                tslFileCount.Text = string.Format(FileCount_, files.Length);
            });
        }

        private void UpdateFileColors()
        {
            if (lstFiles.Items.Count <= 0)
                return;

            // TODO: Review action to do most actual form updates at the end for less blocking
            UpdateListView(lstFiles, listView =>
            {
                foreach (ListViewItem item in listView.Items)
                    UpdateFileColor(item);
            });
        }

        private void UpdateFileColor(ListViewItem item)
        {
            if (item == null)
                return;

            if (!(item.Tag is IArchiveFileInfo afi))
                return;

            var isChanged = afi.ContentChanged || _stateInfo.ArchiveChildren.Where(x => x.StateChanged).Any(x => x.FilePath == afi.FilePath);
            item.ForeColor = isChanged ? ColorChangedState : ColorDefaultState;
        }

        #endregion

        #region Update Directory information

        private void UpdateDirectories()
        {
            if (ArchiveState.Files == null || !ArchiveState.Files.Any())
            {
                InvokeAction(() => treDirectories.Nodes.Clear());
                return;
            }

            // TODO: Review action to do most actual form updates at the end for less blocking
            UpdateTreeView(treDirectories, treeView =>
            {
                treeView.Nodes.Clear();

                var archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo, UPath.Root, _stateInfo.StreamManager);
                var filePaths = EnumerateFilteredDirectories(archiveFileSystem);
                var lookup = ArchiveState.Files.OrderBy(f => f.FilePath).ToLookup(f => f.FilePath.GetDirectory());

                // 1. Build directory tree
                var root = treeView.Nodes.Add("root", _stateInfo.FilePath.ToRelative().FullName,
                    "tree-archive-file", "tree-archive-file");
                foreach (var path in filePaths)
                {
                    path.Split()
                        .Aggregate(root, (node, part) => node.Nodes[part] ?? node.Nodes.Add(part, part))
                        .Tag = lookup[path];
                }

                // 2. Expand tree
                ExpandDirectoryTree(_expandedPaths);

                // Always expand root
                root.Expand();
                treeView.SelectedNode = root;
            });
            InvokeAction(() => treDirectories.Focus());

            UpdateDirectoryColors();
        }

        private void UpdateDirectory(TreeNode treeNode)
        {
            var root = GetRootNode();
            if (root == null)
                return;

            // TODO: Review action to do most actual form updates at the end for less blocking
            UpdateTreeView(treDirectories, treeView =>
            {
                treeNode.Nodes.Clear();

                var nodePath = GetNodePath(root, treeNode).ToAbsolute();

                var archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo, UPath.Root, _stateInfo.StreamManager);
                var filePaths = EnumerateFilteredDirectories(archiveFileSystem).Where(x =>
                    x.IsInDirectory(nodePath, true));
                var lookup = ArchiveState.Files.OrderBy(f => f.FilePath).ToLookup(f => f.FilePath.GetDirectory());

                // 1. Build directory tree
                foreach (var path in filePaths)
                {
                    path.Split()
                        .Aggregate(root, (node, part) => node.Nodes[part] ?? node.Nodes.Add(part, part))
                        .Tag = lookup[path];
                }
            });
            InvokeAction(() => treDirectories.Focus());

            UpdateDirectoryColors();
        }

        private void ExpandDirectoryTree(IList<UPath> expandedPaths)
        {
            var root = GetRootNode();
            if (root == null)
                return;

            foreach (var expandedPath in expandedPaths.Select(x => x.Split()).ToArray())
            {
                var node = root;
                foreach (var pathPart in expandedPath)
                {
                    if (node.Nodes[pathPart] == null)
                        break;

                    node.Nodes[pathPart].Expand();
                    node = node.Nodes[pathPart];
                }
            }
        }

        private void UpdateDirectoryColors()
        {
            var root = GetRootNode();
            if (root == null)
                return;

            var changedFilePaths = ArchiveState.Files
                .Where(x => x.ContentChanged)
                .Select(x => x.FilePath.GetDirectory())
                .Concat(_stateInfo.ArchiveChildren.Where(x => x.StateChanged).Select(x => x.FilePath.GetDirectory()))
                .Distinct()
                .ToArray();
            if (!changedFilePaths.Any())
            {
                void Iterate(TreeNodeCollection nodes)
                {
                    if (nodes.Count <= 0)
                        return;

                    foreach (TreeNode node in nodes)
                    {
                        if (node == null)
                            break;

                        node.ForeColor = ColorDefaultState;
                        Iterate(node.Nodes);
                    }
                }

                InvokeAction(() =>
                {
                    root.ForeColor = ColorDefaultState;
                    Iterate(root.Nodes);
                });
                return;
            }

            UpdateTreeView(treDirectories, treeView =>
            {
                root.ForeColor = ColorChangedState;
                foreach (var filePath in changedFilePaths.Select(x => x.Split()))
                {
                    filePath.Aggregate(root, (node, part) =>
                    {
                        var newNode = node?.Nodes[part];
                        if (newNode == null)
                            return null;

                        newNode.ForeColor = ColorChangedState;
                        return newNode;
                    });
                }
            });
        }

        #endregion

        #region Update Property information

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateProperties()
        {
            // TODO: Remove ToolStripButtons

            InvokeAction(() =>
            {
                // Menu
                tsbSave.Enabled = ArchiveState is ISaveFiles;
                tsbSaveAs.Enabled = ArchiveState is ISaveFiles && _stateInfo.ParentStateInfo == null;

                // Toolbar
                tsbFileExtract.Enabled = true;
                tsbFileReplace.Enabled = ArchiveState is IReplaceFiles && ArchiveState is ISaveFiles;
                tsbFileRename.Enabled = ArchiveState is IRenameFiles && ArchiveState is ISaveFiles;
                tsbFileDelete.Enabled = ArchiveState is IRemoveFiles && ArchiveState is ISaveFiles;
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateFileContextMenu()
        {
            if (lstFiles.SelectedItems.Count <= 0)
                return;

            if (!(lstFiles.SelectedItems[0]?.Tag is IArchiveFileInfo afi))
                return;

            var isLoadLocked = IsFileLocked(afi, true);
            var isStateLocked = IsFileLocked(afi, false);

            var canExtractFiles = !isStateLocked && !_isOperationRunning;
            var canReplaceFiles = ArchiveState is IReplaceFiles && !isLoadLocked && !_isOperationRunning;
            var canRenameFiles = ArchiveState is IRenameFiles && !_isOperationRunning;
            var canDeleteFiles = ArchiveState is IRemoveFiles && !_isOperationRunning;

            // Update action menu items

            InvokeAction(() =>
            {
                extractFileToolStripMenuItem.Enabled = canExtractFiles;
                extractFileToolStripMenuItem.Text = canExtractFiles ? "&Extract..." : "Extract is not supported";
                extractFileToolStripMenuItem.Tag = afi;

                replaceFileToolStripMenuItem.Enabled = canReplaceFiles;
                replaceFileToolStripMenuItem.Text = canReplaceFiles ? "&Replace..." : "Replace is not supported";
                replaceFileToolStripMenuItem.Tag = afi;

                renameFileToolStripMenuItem.Enabled = canRenameFiles;
                renameFileToolStripMenuItem.Text = canRenameFiles ? "Re&name..." : "Rename is not supported";
                renameFileToolStripMenuItem.Tag = afi;

                deleteFileToolStripMenuItem.Enabled = canDeleteFiles;
                deleteFileToolStripMenuItem.Text = canDeleteFiles ? "&Delete" : "Delete is not supported";
                deleteFileToolStripMenuItem.Tag = afi;
            });

            // Update Open With context

            var items = new List<ToolStripItem>();
            foreach (var pluginId in afi.PluginIds ?? Array.Empty<Guid>())
            {
                var filePluginLoader = _pluginManager.GetFilePluginLoaders().FirstOrDefault(x => x.Exists(pluginId));
                var filePlugin = filePluginLoader?.GetPlugin(pluginId);

                if (filePlugin == null)
                    continue;

                var item = new ToolStripMenuItem(filePlugin.Metadata.Name)
                {
                    Tag = (pluginId, afi)
                };
                item.Click += fileMenuContextItem_Click;

                items.Add(item);
            }

            InvokeAction(() =>
            {
                openToolStripMenuItem.Enabled = !_isOperationRunning;

                openWithToolStripMenuItem.DropDownItems.AddRange(items.ToArray());
                openWithToolStripMenuItem.DropDownItems.Clear();
                openWithToolStripMenuItem.Enabled = openWithToolStripMenuItem.DropDownItems.Count > 0 && !_isOperationRunning;
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateDirectoryContextMenu()
        {
            var root = GetRootNode();
            if (root == null)
                return;

            var isRoot = treDirectories.SelectedNode == root;
            var canReplaceFiles = ArchiveState is IReplaceFiles && !_isOperationRunning;
            var canRenameFiles = ArchiveState is IRenameFiles && !isRoot && !_isOperationRunning;
            var canDeleteFiles = ArchiveState is IRemoveFiles && !isRoot && !_isOperationRunning;
            var canAddFiles = ArchiveState is IAddFiles && !_isOperationRunning;

            InvokeAction(() =>
            {
                extractDirectoryToolStripMenuItem.Enabled = !_isOperationRunning;
                extractDirectoryToolStripMenuItem.Text = !_isOperationRunning ? "&Extract..." : "Extract is not supported";

                replaceDirectoryToolStripMenuItem.Enabled = canReplaceFiles;
                replaceDirectoryToolStripMenuItem.Text = canReplaceFiles ? "&Replace..." : "Replace is not supported";

                renameDirectoryToolStripMenuItem.Enabled = canRenameFiles;
                renameDirectoryToolStripMenuItem.Text = canRenameFiles ? "Rename..." : "Rename is not supported";

                addDirectoryToolStripMenuItem.Enabled = canAddFiles;
                addDirectoryToolStripMenuItem.Text = canAddFiles ? "&Add..." : "Add is not supported";

                deleteDirectoryToolStripMenuItem.Enabled = canDeleteFiles;
                deleteDirectoryToolStripMenuItem.Text = canDeleteFiles ? "&Delete..." : "Delete is not supported";
            });
        }

        #endregion

        private void UpdateTreeView(TreeView treeView, Action<TreeView> updateAction)
        {
            InvokeAction(() =>
            {
                treeView.BeginUpdate();

                updateAction(treeView);

                treeView.EndUpdate();
            });
        }

        private void UpdateListView(ListView listView, Action<ListView> updateAction)
        {
            InvokeAction(() =>
            {
                listView.BeginUpdate();

                updateAction(listView);

                listView.EndUpdate();
            });
        }

        #endregion

        #region Events

        private void tsbFind_Click(object sender, EventArgs e)
        {
            Stub();
        }

        private void tsbProperties_Click(object sender, EventArgs e)
        {
            Stub();
        }

        private void TsbCancelOperation_Click(object sender, EventArgs e)
        {
            _operationToken.Cancel();
            tsbCancelOperation.Enabled = false;
        }

        #region tlsPreview

        private void tsbFileExtract_Click(object sender, EventArgs e)
        {
            ExtractSelectedFiles();
        }

        private void tsbFileReplace_Click(object sender, EventArgs e)
        {
            ReplaceSelectedFiles();
        }

        private void tsbFileRename_Click(object sender, EventArgs e)
        {
            RenameSelectedFiles();
        }

        private void tsbFileDelete_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        private void tsbFileOpen_Click(object sender, EventArgs e)
        {
            OpenSelectedFiles();
        }

        private void tsbFileProperties_Click(object sender, EventArgs e)
        {
            Stub();
        }

        #endregion

        #region mnuFiles

        private void mnuFiles_Opening(object sender, CancelEventArgs e)
        {
            UpdateFileContextMenu();
        }

        private void extractFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExtractSelectedFiles();
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceSelectedFiles();
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RenameSelectedFiles();
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSelectedFiles();
        }

        private async void fileMenuContextItem_Click(object sender, EventArgs e)
        {
            var tsi = (ToolStripMenuItem)sender;
            var (pluginId, afi) = ((Guid, IArchiveFileInfo))tsi.Tag;

            if (!await _formCommunicator.Open(afi, pluginId))
                _formCommunicator.ReportStatus(false, $"File could not be opened with plugin '{pluginId}'.");
        }

        #endregion

        #region mnuDirectories

        private void mnuDirectories_Opening(object sender, CancelEventArgs e)
        {
            UpdateDirectoryContextMenu();
        }

        private void extractDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExtractSelectedDirectory();
        }

        private void replaceDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceSelectedDirectory();
        }

        private void renameDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RenameSelectedDirectory();
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFilesToSelectedNode();
        }

        private void deleteDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedDirectory();
        }

        #endregion

        #region treDirectories

        private void treDirectories_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null)
                return;

            e.Node.ImageKey = "tree-directory";
            e.Node.SelectedImageKey = e.Node.ImageKey;

            var nodePath = GetNodePath(GetRootNode(), e.Node);
            if (_expandedPaths.Contains(nodePath))
                _expandedPaths.Remove(nodePath);
        }

        private void treDirectories_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null)
                return;

            e.Node.ImageKey = "tree-directory-open";
            e.Node.SelectedImageKey = e.Node.ImageKey;

            // Add expanded node
            var nodePath = GetNodePath(GetRootNode(), e.Node);
            if (nodePath != UPath.Empty)
                _expandedPaths.Add(nodePath);
        }

        private void treDirectories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateFiles();
            UpdateProperties();
        }

        #endregion

        #region lstFiles

        private void lstFiles_DoubleClick(object sender, EventArgs e)
        {
            var menuItem = lstFiles.SelectedItems[0];
            var afi = menuItem.Tag as IArchiveFileInfo;

            OpenFiles(new[] { afi });
        }

        #endregion

        #region txtSearch

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            txtSearch.TextChanged -= txtSearch_TextChanged;

            if (_isSearchEmpty)
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = SystemColors.WindowText;
            }

            txtSearch.TextChanged += txtSearch_TextChanged;
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            txtSearch.TextChanged -= txtSearch_TextChanged;

            if (_isSearchEmpty)
                AddSearchPlaceholder();

            txtSearch.TextChanged += txtSearch_TextChanged;
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _isSearchEmpty = string.IsNullOrEmpty(txtSearch.Text);
            _searchTerm = $"*{txtSearch.Text}*";

            UpdateDirectories();
            UpdateFiles();

            txtSearch.Focus();
        }

        private void btnSearchDelete_Click(object sender, EventArgs e)
        {
            txtSearch.TextChanged -= txtSearch_TextChanged;

            AddSearchPlaceholder();
            _isSearchEmpty = true;
            _searchTerm = string.Empty;

            UpdateDirectories();
            UpdateFiles();

            txtSearch.TextChanged += txtSearch_TextChanged;
        }

        #endregion

        #endregion

        #region Utilities

        #region General

        private void Stub()
        {
            MessageBox.Show("This method is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private IEnumerable<IArchiveFileInfo> CollectSelectedFiles()
        {
            foreach (ListViewItem item in lstFiles.SelectedItems)
                yield return item.Tag as IArchiveFileInfo;
        }

        private UPath GetNodePath(TreeNode node)
        {
            return GetNodePath(GetRootNode(), node);
        }

        private UPath GetNodePath(TreeNode rootNode, TreeNode node)
        {
            if (rootNode == null || node == null || rootNode == node)
                return UPath.Empty;

            var result = (UPath)node.Name;

            while (node.Parent != null && node.Parent != rootNode)
            {
                node = node.Parent;
                result = UPath.Combine(node.Name, result);
            }

            return result;
        }

        private void AddSearchPlaceholder()
        {
            txtSearch.Text = "Search archive...";
            txtSearch.ForeColor = SystemColors.ScrollBar;
        }

        #endregion

        #region Support

        private void InvokeAction(Action invokeAction)
        {
            if (InvokeRequired)
                Invoke(invokeAction);
            else
                invokeAction();
        }

        private TreeNode GetRootNode()
        {
            return treDirectories.Nodes["root"];
        }

        private IEnumerable<UPath> EnumerateFilteredDirectories(IFileSystem fileSystem)
        {
            var searchTerm = _isSearchEmpty ? "*" : _searchTerm;
            return fileSystem.EnumeratePaths(UPath.Root, searchTerm, SearchOption.AllDirectories,SearchTarget.Directory).Concat(
                fileSystem.EnumeratePaths(UPath.Root, searchTerm, SearchOption.AllDirectories, SearchTarget.File).Select(x=>x.GetDirectory()));
        }

        private IEnumerable<IArchiveFileInfo> EnumerateFilteredFiles(TreeNode treeNode)
        {
            if (!(treeNode?.Tag is IList<IArchiveFileInfo> files))
                yield break;

            var nodePath = GetNodePath(treeNode).ToAbsolute();
            var searchTerm = _isSearchEmpty ? "*" : _searchTerm;

            var fileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo);

            // Yield all files
            var enumeratedFiles = fileSystem.EnumeratePaths(nodePath, searchTerm, SearchOption.TopDirectoryOnly, SearchTarget.File);
            foreach (var file in enumeratedFiles.Intersect(files.Select(x => x.FilePath)))
                yield return files.First(x => x.FilePath == file);
        }

        private UPath SelectFolder()
        {
            var fbd = new FolderBrowserDialog
            {
                SelectedPath = Settings.Default.LastDirectory
            };

            return (UPath)Invoke(new Func<UPath>(() => fbd.ShowDialog() != DialogResult.OK ? UPath.Empty : fbd.SelectedPath));
        }

        private UPath SelectFile(string fileName)
        {
            var ofd = new OpenFileDialog
            {
                Filter = AllFilesFilter_,
                InitialDirectory = Settings.Default.LastDirectory,
                FileName = fileName
            };

            return (UPath)Invoke(new Func<UPath>(() => ofd.ShowDialog() != DialogResult.OK ? UPath.Empty : ofd.FileName));
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

        private IEnumerable<(UPath, IArchiveFileInfo)> EnumerateTreeNode(TreeNode treeNode, UPath nodePath = default)
        {
            nodePath = nodePath.IsNull || nodePath.IsEmpty ? UPath.Root : nodePath / treeNode.Name;

            // Yield all files
            if (treeNode.Tag is IList<IArchiveFileInfo> files)
            {
                foreach (var file in files)
                    yield return (nodePath / file.FilePath.GetName(), file);
            }

            // Yield files from directories
            foreach (TreeNode node in treeNode.Nodes)
                foreach (var file in EnumerateTreeNode(node, nodePath))
                    yield return file;
        }

        private IEnumerable<IArchiveFileInfo> EnumerateListView(ListView listView, bool onlySelected)
        {
            if (onlySelected)
            {
                foreach (ListViewItem item in listView.SelectedItems)
                    yield return item.Tag as IArchiveFileInfo;

                yield break;
            }

            foreach (ListViewItem item in listView.Items)
                yield return item.Tag as IArchiveFileInfo;
        }

        private void EnableOperation()
        {
            _isOperationRunning = true;
            tsbCancelOperation.Enabled = true;

            UpdateDirectoryContextMenu();
            UpdateFileContextMenu();
        }

        private void DisableOperation()
        {
            _isOperationRunning = false;
            tsbCancelOperation.Enabled = false;

            UpdateDirectoryContextMenu();
            UpdateFileContextMenu();
        }

        #endregion

        #region Extract Directories

        private void ExtractSelectedDirectory()
        {
            ExtractDirectory(treDirectories.SelectedNode);
        }

        private async void ExtractDirectory(TreeNode node)
        {
            EnableOperation();

            var elements = EnumerateTreeNode(node).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to extract.");
                DisableOperation();
                return;
            }

            // Select folder
            var extractPath = SelectFolder();
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                _formCommunicator.ReportStatus(false, "No folder selected.");
                DisableOperation();
                return;
            }

            // Extract elements
            var root = GetRootNode();
            var subFolder = node == root ? _stateInfo.FilePath.GetName() : node.Name;

            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(extractPath / subFolder, _stateInfo.StreamManager);

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                var count = 0;
                foreach (var (path, file) in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Extract files", count++, elements.Length);

                    if (IsFileLocked(file, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        newFileStream = destinationFileSystem.OpenFile(path, FileMode.Create, FileAccess.Write);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    var currentFileStream = await file.GetFileData();

                    currentFileStream.CopyTo(newFileStream);

                    newFileStream.Close();
                }
            }, _operationToken.Token);
            _progressContext.ReportProgress("Extract files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File extraction cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) extracted successfully.");

            DisableOperation();
        }

        #endregion

        #region Extract Files

        private void ExtractSelectedFiles()
        {
            ExtractFiles(true);
        }

        private async void ExtractFiles(bool onlySelected)
        {
            EnableOperation();

            var elements = EnumerateListView(lstFiles, onlySelected).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to extract.");
                DisableOperation();
                return;
            }

            // Select folder
            var extractPath = SelectFolder();
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                _formCommunicator.ReportStatus(false, "No folder selected.");
                DisableOperation();
                return;
            }

            // Extract elements
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(extractPath, _stateInfo.StreamManager);

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                var count = 0;
                foreach (var file in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Extract files", count++, elements.Length);

                    if (IsFileLocked(file, false))
                        continue;

                    Stream newFileStream;
                    try
                    {
                        newFileStream = destinationFileSystem.OpenFile(file.FilePath.GetName(), FileMode.Create, FileAccess.Write);
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
            _progressContext.ReportProgress("Extract files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File extraction cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) extracted successfully.");

            DisableOperation();
        }

        #endregion

        #region Replace Directories

        private void ReplaceSelectedDirectory()
        {
            ReplaceDirectory(treDirectories.SelectedNode);
        }

        private async void ReplaceDirectory(TreeNode node)
        {
            EnableOperation();

            var elements = EnumerateTreeNode(node).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to replace.");
                DisableOperation();
                return;
            }

            // Select folder
            var replacePath = SelectFolder();
            if (replacePath.IsNull || replacePath.IsEmpty)
            {
                _formCommunicator.ReportStatus(false, "No folder selected.");
                DisableOperation();
                return;
            }

            // Extract elements
            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(replacePath, _stateInfo.StreamManager);

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var replaceState = ArchiveState as IReplaceFiles;
                foreach (var (path, file) in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Replace files", count++, elements.Length);

                    if (IsFileLocked(file, true))
                        continue;

                    if (!sourceFileSystem.FileExists(path))
                        continue;

                    var currentFileStream = sourceFileSystem.OpenFile(path);
                    replaceState?.ReplaceFile(file, currentFileStream);
                }
            });
            _progressContext.ReportProgress("Replace files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File replacement cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) replaced successfully.");

            UpdateDirectoryColors();
            UpdateFileColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);

            DisableOperation();
        }

        #endregion

        #region Replace Files

        private void ReplaceSelectedFiles()
        {
            ReplaceFiles(true);
        }

        private async void ReplaceFiles(bool onlySelected)
        {
            EnableOperation();

            var elements = EnumerateListView(lstFiles, onlySelected).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to replace.");
                DisableOperation();
                return;
            }

            // Select destination
            UPath replaceDirectory;
            UPath replaceFileName;
            if (elements.Length == 1)
            {
                var selectedPath = SelectFile(elements[0].FilePath.GetName());
                if (selectedPath.IsNull || selectedPath.IsEmpty)
                {
                    _formCommunicator.ReportStatus(false, "No file selected.");
                    DisableOperation();
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
                    _formCommunicator.ReportStatus(false, "No folder selected.");
                    DisableOperation();
                    return;
                }

                replaceDirectory = selectedPath;
                replaceFileName = UPath.Empty;
            }

            // Extract elements
            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(replaceDirectory, _stateInfo.StreamManager);

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var replaceState = ArchiveState as IReplaceFiles;
                foreach (var file in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Replace files", count++, elements.Length);

                    if (IsFileLocked(file, true))
                        continue;

                    var filePath = replaceFileName.IsEmpty ? file.FilePath.GetName() : replaceFileName;
                    if (!sourceFileSystem.FileExists(filePath))
                        continue;

                    var currentFileStream = sourceFileSystem.OpenFile(filePath);
                    replaceState?.ReplaceFile(file, currentFileStream);
                }
            });
            _progressContext.ReportProgress("Replace files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File replacement cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) replaced successfully.");

            UpdateDirectoryColors();
            UpdateFileColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);

            DisableOperation();
        }

        #endregion

        #region Rename Directories

        private void RenameSelectedDirectory()
        {
            RenameDirectory(treDirectories.SelectedNode);
        }

        private async void RenameDirectory(TreeNode node)
        {
            EnableOperation();

            var elements = EnumerateTreeNode(node).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to rename.");
                DisableOperation();
                return;
            }

            // Select new directory name
            var inputBox = new InputBox($"Select a new name for '{node.Name}'",
                "Rename directory",
                node.Name);
            if (inputBox.ShowDialog() != DialogResult.OK)
            {
                _formCommunicator.ReportStatus(false, "No new name given.");
                DisableOperation();
                return;
            }

            var newDirectoryPath = GetNodePath(GetRootNode(), node).GetDirectory() / inputBox.InputText;

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var renameState = ArchiveState as IRenameFiles;
                foreach (var (path, file) in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Rename files", count++, elements.Length);

                    // Rename possibly open file in main form
                    _formCommunicator.Rename(file, newDirectoryPath / path.ToRelative());

                    // Rename file in archive
                    renameState?.Rename(file, newDirectoryPath / path.ToRelative());
                }
            });
            _progressContext.ReportProgress("Rename files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File renaming cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) renamed successfully.");

            node.Name = node.Text = inputBox.InputText;
            UpdateDirectory(node);
            UpdateFileColors();

            UpdateProperties();

            DisableOperation();
        }

        #endregion

        #region Rename Files

        private void RenameSelectedFiles()
        {
            RenameFiles(true);
        }

        private async void RenameFiles(bool onlySelected)
        {
            EnableOperation();

            var elements = EnumerateListView(lstFiles, onlySelected).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to rename.");
                DisableOperation();
                return;
            }

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var renameState = ArchiveState as IRenameFiles;
                foreach (var file in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Rename files", count++, elements.Length);

                    // Select new name
                    var inputBox = new InputBox($"Select a new name for '{file.FilePath.GetName()}'",
                        "Rename file",
                        file.FilePath.GetName());
                    if (inputBox.ShowDialog() != DialogResult.OK)
                        continue;

                    // Rename possibly open file in main form
                    _formCommunicator.Rename(file, file.FilePath.GetDirectory() / inputBox.InputText);

                    // Rename file in archive
                    renameState?.Rename(file, file.FilePath.GetDirectory() / inputBox.InputText);
                }
            });
            _progressContext.ReportProgress("Rename files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File renaming cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) renamed successfully.");

            UpdateFiles();
            UpdateDirectoryColors();

            UpdateProperties();

            DisableOperation();
        }

        #endregion

        #region Delete Directories

        private void DeleteSelectedDirectory()
        {
            DeleteDirectory(treDirectories.SelectedNode);
        }

        private async void DeleteDirectory(TreeNode node)
        {
            EnableOperation();

            var elements = EnumerateTreeNode(node).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to delete.");
                DisableOperation();
                return;
            }

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var removeState = ArchiveState as IRemoveFiles;
                foreach (var (_, file) in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Delete files", count++, elements.Length);

                    removeState?.RemoveFile(file);
                }
            });
            _progressContext.ReportProgress("Delete files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File deletion cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) deleted successfully.");

            node.Remove();

            UpdateDirectoryColors();
            UpdateProperties();

            _formCommunicator.Update(true, false);

            DisableOperation();
        }

        #endregion

        #region Delete Files

        private void DeleteSelectedFiles()
        {
            DeleteFiles(true);
        }

        private async void DeleteFiles(bool onlySelected)
        {
            EnableOperation();

            var elements = EnumerateListView(lstFiles, onlySelected).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(true, "No files to delete.");
                DisableOperation();
                return;
            }

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var removeState = ArchiveState as IRemoveFiles;
                foreach (var file in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Delete files", count++, elements.Length);

                    removeState?.RemoveFile(file);
                }
            });
            _progressContext.ReportProgress("Delete files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File deletion cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) deleted successfully.");

            UpdateDirectory(treDirectories.SelectedNode);
            UpdateFiles();

            UpdateProperties();
            _formCommunicator.Update(true, false);

            DisableOperation();
        }

        #endregion

        #region Add Files

        private void AddFilesToSelectedNode()
        {
            AddFiles(treDirectories.SelectedNode);
        }

        private async void AddFiles(TreeNode node)
        {
            EnableOperation();

            // Select folder
            var selectedPath = SelectFolder();
            if (selectedPath.IsNull || selectedPath.IsEmpty)
            {
                _formCommunicator.ReportStatus(false, "No folder selected.");
                DisableOperation();
                return;
            }

            // Add elements
            var subFolder = GetNodePath(GetRootNode(), node);
            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(selectedPath, _stateInfo.StreamManager);

            var elements = sourceFileSystem.EnumeratePaths(UPath.Root, "*", SearchOption.AllDirectories, SearchTarget.File).ToArray();
            if (elements.Length <= 0)
            {
                _formCommunicator.ReportStatus(false, "No files to add.");
                DisableOperation();
                return;
            }

            _formCommunicator.ReportStatus(true, string.Empty);

            _progressContext.StartProgress();
            _operationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var count = 0;
                var addState = _stateInfo.PluginState as IAddFiles;
                foreach (var filePath in elements)
                {
                    if (_operationToken.IsCancellationRequested)
                        break;

                    _progressContext.ReportProgress("Add files", count++, elements.Length);

                    addState?.AddFile(sourceFileSystem.OpenFile(filePath), subFolder / filePath.ToRelative());
                }
            }, _operationToken.Token);
            _progressContext.ReportProgress("Add files", 1, 1);
            _progressContext.FinishProgress();

            if (_operationToken.IsCancellationRequested)
                _formCommunicator.ReportStatus(false, "File adding cancelled.");
            else
                _formCommunicator.ReportStatus(true, "File(s) added successfully.");

            UpdateDirectory(node);
            UpdateProperties();
            _formCommunicator.Update(true, false);

            DisableOperation();
        }

        #endregion

        #region Open Files

        private void OpenSelectedFiles()
        {
            OpenFiles(CollectSelectedFiles().ToArray());
        }

        private async void OpenFiles(IList<IArchiveFileInfo> files)
        {
            if (files == null || !files.Any())
                return;

            foreach (var file in files)
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
                            _formCommunicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                            continue;
                        }

                        _openingFiles.Add(file);

                        if (await _formCommunicator.Open(file, pluginId))
                        {
                            opened = true;
                            _openingFiles.Remove(file);
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
                    _formCommunicator.ReportStatus(false, $"{file.FilePath.ToRelative()} is already opening.");
                    continue;
                }

                _openingFiles.Add(file);

                if (!await _formCommunicator.Open(file))
                    _formCommunicator.ReportStatus(false, "File couldn't be opened.");

                _openingFiles.Remove(file);
            }
        }

        #endregion

        #region Save File

        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save(false);
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            Save(true);
        }

        private async void Save(bool saveAs)
        {
            var wasSuccessful = await _formCommunicator.Save(saveAs);
            if (!wasSuccessful)
                return;

            SaveUpdate();
        }

        private void SaveUpdate()
        {
            InvokeAction(() =>
            {
                UpdateDirectories();
                UpdateFiles();

                UpdateProperties();
                _formCommunicator.Update(true, false);
            });
        }

        #endregion

        #endregion
    }
}