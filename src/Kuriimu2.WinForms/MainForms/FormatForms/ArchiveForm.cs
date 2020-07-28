using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
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
using Kuriimu2.WinForms.MainForms.Models.Contexts;
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
        private readonly IList<ArchiveFileInfo> _openingFiles;

        private bool _isSearchEmpty = true;
        private string _searchTerm;

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
            _openingFiles = new List<ArchiveFileInfo>();

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
            if (!(treDirectories.SelectedNode?.Tag is IList<ArchiveFileInfo> files))
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

                tslFileCount.Text = string.Format(FileCount_, files.Count);
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

            if (!(item.Tag is ArchiveFileInfo afi))
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
                var filePaths = archiveFileSystem.EnumeratePaths(UPath.Root, _isSearchEmpty ? "*" : _searchTerm,
                    SearchOption.AllDirectories, SearchTarget.Directory);
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

                var nodePath = GetNodePath(root, treeNode);

                var archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo, UPath.Root, _stateInfo.StreamManager);
                var filePaths = EnumerateFilteredPaths(archiveFileSystem).Where(x =>
                    x.ToRelative().IsInDirectory(nodePath, true));
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

            if (!(lstFiles.SelectedItems[0]?.Tag is ArchiveFileInfo afi))
                return;

            var isLoadLocked = IsFileLocked(afi, true);
            var isStateLocked = IsFileLocked(afi, false);

            var canExtractFiles = !isLoadLocked;
            var canReplaceFiles = ArchiveState is IReplaceFiles && !isStateLocked;
            var canRenameFiles = ArchiveState is IRenameFiles;
            var canDeleteFiles = ArchiveState is IRemoveFiles;

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
                openWithToolStripMenuItem.DropDownItems.AddRange(items.ToArray());
                openWithToolStripMenuItem.DropDownItems.Clear();

                openWithToolStripMenuItem.Enabled = openWithToolStripMenuItem.DropDownItems.Count > 0;
            });
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateDirectoryContextMenu()
        {
            var root = GetRootNode();
            if (root == null)
                return;

            var isRoot = treDirectories.SelectedNode == root;
            var canReplaceFiles = ArchiveState is IReplaceFiles;
            var canRenameFiles = ArchiveState is IRenameFiles && !isRoot;
            var canDeleteFiles = ArchiveState is IRemoveFiles && !isRoot;
            var canAddFiles = ArchiveState is IAddFiles;

            InvokeAction(() =>
            {
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
            var (pluginId, afi) = ((Guid, ArchiveFileInfo))tsi.Tag;

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
            if (!treDirectories.Focused)
                return;

            AddFiles();

            UpdateDirectories();
            UpdateProperties();
            _formCommunicator.Update(true, false);
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
            var afi = menuItem.Tag as ArchiveFileInfo;

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

        private IEnumerable<ArchiveFileInfo> CollectSelectedFiles()
        {
            foreach (ListViewItem item in lstFiles.SelectedItems)
                yield return item.Tag as ArchiveFileInfo;
        }

        private UPath GetNodePath(TreeNode rootNode, TreeNode node)
        {
            if (rootNode == null || node == null)
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

        private IEnumerable<UPath> EnumerateFilteredPaths(IFileSystem fileSystem)
        {
            return fileSystem.EnumeratePaths(UPath.Root, _isSearchEmpty ? "*" : _searchTerm,
                SearchOption.AllDirectories, SearchTarget.Directory);
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

        private bool IsFileLocked(ArchiveFileInfo afi, bool lockOnLoaded)
        {
            var absolutePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath.ToRelative() / afi.FilePath.ToRelative();

            var isLoaded = _pluginManager.IsLoaded(absolutePath);
            if (!isLoaded)
                return false;

            if (!lockOnLoaded)
                return true;

            var openedState = _pluginManager.GetLoadedFile(absolutePath);
            return openedState.StateChanged;
        }

        #endregion

        #region Extract Directories

        private void ExtractSelectedDirectory()
        {
            ExtractDirectory(treDirectories.SelectedNode);
        }

        private async void ExtractDirectory(TreeNode node)
        {
            var context = new ExtractContext();
            ExtractDirectory(node, context, true);
            await Task.Run(() => ExtractDirectory(node, context));

            if (context.IsSuccessful)
                _formCommunicator.ReportStatus(true, "Files extracted successfully.");
            else
                _formCommunicator.ReportStatus(false, context.Error);
        }

        private async void ExtractDirectory(TreeNode node, ExtractContext context, bool isCount = false)
        {
            if (node == null)
                return;

            if (!isCount && (context.ExtractPath.IsNull || context.ExtractPath.IsEmpty))
            {
                // Select folder to extract to
                context.ExtractPath = SelectFolder();
                if (context.ExtractPath.IsNull || context.ExtractPath.IsEmpty)
                {
                    context.IsSuccessful = false;
                    context.Error = "No folder was selected.";
                    return;
                }
            }

            var extractPath = UPath.Empty;
            if (!isCount)
            {
                extractPath = context.ExtractPath;
                extractPath /= node == GetRootNode() ? _stateInfo.FilePath.GetName() : node.Name;
            }

            // Extract files of directory
            if (node.Tag is IList<ArchiveFileInfo> files)
            {
                context.ExtractPath = extractPath;

                await ExtractFiles(files, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }

            // Extract sub directories
            foreach (TreeNode subNode in node.Nodes)
            {
                context.ExtractPath = extractPath;

                ExtractDirectory(subNode, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }
        }

        #endregion

        #region Extract Files

        private void ExtractSelectedFiles()
        {
            var selectedFiles = CollectSelectedFiles().ToArray();
            if (selectedFiles.Length <= 0)
                return;

            ExtractFiles(selectedFiles);
        }

        private async void ExtractFiles(IList<ArchiveFileInfo> files)
        {
            var context = new ExtractContext();
            await ExtractFiles(files, context, true);
            await Task.Run(() => ExtractFiles(files, context));

            if (context.IsSuccessful)
                _formCommunicator.ReportStatus(true, "File(s) extracted successfully.");
            else
                _formCommunicator.ReportStatus(false, context.Error);
        }

        private async Task ExtractFiles(IList<ArchiveFileInfo> files, ExtractContext context, bool isCount = false)
        {
            if (files == null || !files.Any())
                return;

            var directory = files[0].FilePath.GetDirectory();
            if (files.Any(x => x.FilePath.GetDirectory() != directory))
                return;

            // Select folder to extract to
            if (!isCount && (context.ExtractPath.IsNull || context.ExtractPath.IsEmpty))
            {
                context.ExtractPath = SelectFolder();
                if (context.ExtractPath.IsNull || context.ExtractPath.IsEmpty)
                {
                    context.IsSuccessful = false;
                    context.Error = "No folder was selected.";
                    return;
                }
            }

            var destinationFileSystem = isCount ? null : FileSystemFactory.CreatePhysicalFileSystem(context.ExtractPath, _stateInfo.StreamManager);
            foreach (var file in files)
            {
                if (IsFileLocked(file, true))
                    continue;

                if (isCount)
                {
                    context.MaxCount++;
                    continue;
                }

                var newFileStream = destinationFileSystem.OpenFile(file.FilePath.GetName(), FileMode.OpenOrCreate, FileAccess.Write);
                var fileStream = await file.GetFileData();

                fileStream.CopyTo(newFileStream);

                fileStream.Close();
                newFileStream.Close();

                _progressContext.ReportProgress("Extracting files", ++context.CurrentCount, context.MaxCount);
            }
        }

        #endregion

        #region Replace Directories

        private void ReplaceSelectedDirectory()
        {
            ReplaceDirectory(treDirectories.SelectedNode);
        }

        private async void ReplaceDirectory(TreeNode node)
        {
            var context = new ReplaceContext();
            ReplaceDirectory(node, context, true);
            await Task.Run(() => ReplaceDirectory(node, context));

            if (context.IsSuccessful)
                _formCommunicator.ReportStatus(true, "File(s) replaced successfully.");
            else
                _formCommunicator.ReportStatus(false, context.Error);

            UpdateDirectoryColors();
            UpdateFileColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        private void ReplaceDirectory(TreeNode node, ReplaceContext context, bool isCount = false)
        {
            if (node == null)
                return;

            if (!isCount && (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty))
            {
                // Select folder to extract to
                context.ReplacePath = SelectFolder();
                if (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty)
                {
                    context.IsSuccessful = false;
                    context.Error = "No folder was selected.";
                    return;
                }
            }

            // Extract files of directory
            if (node.Tag is IList<ArchiveFileInfo> files)
            {
                ReplaceFiles(files, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }

            var replacePath = context.ReplacePath;

            // Extract sub directories
            foreach (TreeNode subNode in node.Nodes)
            {
                context.ReplacePath = isCount ? UPath.Empty : replacePath / subNode.Name;

                ReplaceDirectory(subNode, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }
        }

        #endregion

        #region Replace Files

        private void ReplaceSelectedFiles()
        {
            var selectedFiles = CollectSelectedFiles().ToArray();
            if (selectedFiles.Length <= 0)
                return;

            ReplaceFiles(selectedFiles);
        }

        private async void ReplaceFiles(IList<ArchiveFileInfo> files)
        {
            if (files == null || !files.Any())
                return;

            var context = new ReplaceContext();

            if (files.Count > 1)
            {
                ReplaceFiles(files, context, true);
                await Task.Run(() => ReplaceFiles(files, context));
            }

            if (files.Count == 1)
            {
                ReplaceFile(files[0], context, true);
                await Task.Run(() => ReplaceFile(files[0], context));
            }

            if (context.IsSuccessful)
                _formCommunicator.ReportStatus(true, "File(s) replaced successfully.");
            else
                _formCommunicator.ReportStatus(false, context.Error);

            UpdateDirectoryColors();
            UpdateFileColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void ReplaceFiles(IList<ArchiveFileInfo> files, ReplaceContext context, bool isCount = false)
        {
            if (!(ArchiveState is IReplaceFiles replaceState))
            {
                context.IsSuccessful = false;
                context.Error = "The state does not support replacing files.";
                return;
            }

            if (files == null || !files.Any())
                return;

            var directoryPath = files[0].FilePath.GetDirectory();
            if (files.Any(x => x.FilePath.GetDirectory() != directoryPath))
                return;

            // Select folder to replace with
            if (!isCount && (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty))
            {
                context.ReplacePath = SelectFolder();
                if (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty)
                {
                    context.IsSuccessful = false;
                    context.Error = "No folder was selected.";
                    return;
                }
            }

            var sourceFileSystem = isCount ? null : FileSystemFactory.CreatePhysicalFileSystem(context.ReplacePath, _stateInfo.StreamManager);
            foreach (var file in files)
            {
                if (IsFileLocked(file, false))
                    continue;

                if (isCount)
                {
                    context.MaxCount++;
                    continue;
                }

                var rootedPath = file.FilePath.FullName.Substring(directoryPath.FullName.Length,
                    file.FilePath.FullName.Length - directoryPath.FullName.Length);
                if (!sourceFileSystem.FileExists(rootedPath))
                    continue;

                var newFileData = sourceFileSystem.OpenFile(rootedPath);

                replaceState.ReplaceFile(file, newFileData);

                _progressContext.ReportProgress("Replacing files", ++context.CurrentCount, context.MaxCount);
            }
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void ReplaceFile(ArchiveFileInfo file, ReplaceContext context, bool isCount = false)
        {
            if (!(ArchiveState is IReplaceFiles replaceState))
            {
                context.IsSuccessful = false;
                context.Error = "The state does not support replacing files.";
                return;
            }

            if (file == null || IsFileLocked(file, false))
                return;

            if (isCount)
            {
                context.MaxCount++;
                return;
            }

            // Select file to replace with
            if (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty)
            {
                context.ReplacePath = SelectFile(file.FilePath.GetName());
                if (context.ReplacePath.IsNull || context.ReplacePath.IsEmpty)
                {
                    context.IsSuccessful = false;
                    context.Error = "No file was selected.";
                    return;
                }
            }

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(context.ReplacePath.GetDirectory(), _stateInfo.StreamManager);

            var newFileData = sourceFileSystem.OpenFile(context.ReplacePath.GetName());
            replaceState.ReplaceFile(file, newFileData);

            UpdateDirectoryColors();
            UpdateFileColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        #endregion

        #region Rename Directories

        private void RenameSelectedDirectory()
        {
            RenameDirectory(treDirectories.SelectedNode);
        }

        private async void RenameDirectory(TreeNode node)
        {
            var context = new RenameContext();
            RenameDirectory(node, context, true);
            await Task.Run(() => RenameDirectory(node, context));

            _formCommunicator.ReportStatus(true, "Directory renamed successfully.");

            UpdateFileColors();
            UpdateDirectoryColors();

            UpdateProperties();
        }

        private void RenameDirectory(TreeNode node, RenameContext context, bool isCount = false)
        {
            if (node == null)
                return;

            if (node == GetRootNode())
                return;

            var nodePath = GetNodePath(GetRootNode(), node);

            // Select new directory name
            if (!isCount && (context.RenamedPath.IsNull || context.RenamedPath.IsEmpty))
            {
                var inputBox = new InputBox($"Select a new name for '{node.Text}'",
                    "Rename directory",
                    node.Text);

                if (inputBox.ShowDialog() != DialogResult.OK)
                {
                    context.IsSuccessful = false;
                    context.Error = "No valid name was entered.";
                    return;
                }

                node.Name = node.Text = inputBox.InputText;
                context.RenamedPath = nodePath.GetDirectory() / inputBox.InputText;
            }

            // Rename file paths of directory
            if (node.Tag is IList<ArchiveFileInfo> files)
            {
                RenameFiles(files, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }

            var renamedPath = context.RenamedPath;

            // Rename sub directories
            foreach (TreeNode subNode in node.Nodes)
            {
                context.RenamedPath = isCount ? UPath.Empty : renamedPath / subNode.Name;

                RenameDirectory(subNode, context, isCount);
                if (!context.IsSuccessful)
                    return;
            }
        }

        #endregion

        #region Rename Files

        private async void RenameSelectedFiles()
        {
            var renamingFiles = CollectSelectedFiles().ToArray();

            var context = new RenameContext();
            RenameFiles(renamingFiles, context, true);
            await Task.Run(() => RenameFiles(renamingFiles, context));

            if (context.IsSuccessful)
                _formCommunicator.ReportStatus(true, "File(s) renamed successfully.");
            else
                _formCommunicator.ReportStatus(false, context.Error);

            UpdateFiles();
            UpdateDirectoryColors();

            UpdateProperties();
        }

        private void RenameFiles(IList<ArchiveFileInfo> files, RenameContext context, bool isCount = false)
        {
            if (files == null || !files.Any())
                return;

            if (isCount)
            {
                context.MaxCount += files.Count;
                return;
            }

            foreach (var afi in files)
            {
                if (context.RenamedPath.IsNull || context.RenamedPath.IsEmpty)
                    RenameFile(afi);
                else
                    RenameFile(afi, context.RenamedPath / afi.FilePath.GetName());

                _progressContext.ReportProgress("Renaming files", ++context.CurrentCount, context.MaxCount);
            }
        }

        private void RenameFile(ArchiveFileInfo file)
        {
            RenameFile(file, UPath.Empty);
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void RenameFile(ArchiveFileInfo file, UPath renamedPath)
        {
            if (file == null)
                return;

            if (!(ArchiveState is IRenameFiles renameState))
                return;

            if (renamedPath.IsNull || renamedPath.IsEmpty)
            {
                var inputBox = new InputBox($"Select a new filename for '{file.FilePath.GetName()}'",
                    "Rename file",
                    file.FilePath.GetName());

                if (inputBox.ShowDialog() != DialogResult.OK)
                    return;

                renamedPath = file.FilePath.GetDirectory() / inputBox.InputText;
            }

            // Rename possibly open file in main form
            _formCommunicator.Rename(file, renamedPath);

            // Rename file in archive
            renameState.Rename(file, renamedPath);
        }

        #endregion

        #region Delete Directories

        private async void DeleteSelectedDirectory()
        {
            var context = new CountContext();
            DeleteDirectory(treDirectories.SelectedNode, context, true);
            await Task.Run(() => DeleteDirectory(treDirectories.SelectedNode, context));

            _formCommunicator.ReportStatus(true, "File(s) deleted successfully.");

            treDirectories.SelectedNode.Remove();

            UpdateDirectoryColors();

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        private void DeleteDirectory(TreeNode node, CountContext context, bool isCount = false)
        {
            if (node == null)
                return;

            // Delete files of directory
            if (node.Tag is IList<ArchiveFileInfo> files)
                DeleteFiles(files, context, isCount);

            // Delete sub directories
            foreach (TreeNode subNode in node.Nodes)
                DeleteDirectory(subNode, context, isCount);
        }

        #endregion

        #region Delete Files

        private async void DeleteSelectedFiles()
        {
            var filesToDelete = CollectSelectedFiles().ToArray();

            var context = new CountContext();
            DeleteFiles(filesToDelete, context, true);
            await Task.Run(() => DeleteFiles(filesToDelete, context));

            _formCommunicator.ReportStatus(true, "Files deleted successfully.");

            UpdateDirectory(treDirectories.SelectedNode);
            UpdateFiles();

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private async void DeleteFiles(IList<ArchiveFileInfo> files, CountContext context, bool isCount = false)
        {
            if (files == null || !files.Any())
                return;

            if (!(ArchiveState is IRemoveFiles removeState))
                return;

            if (isCount)
            {
                context.MaxCount += files.Count;
                return;
            }

            foreach (var afi in files)
            {
                // Close possibly open file in main form
                var isClosed = await _formCommunicator.Close(afi);

                // If file was successfully closed, remove it from the archive
                if (isClosed)
                    removeState.RemoveFile(afi);

                _progressContext.ReportProgress("Deleting files", ++context.CurrentCount, context.MaxCount);
            }
        }

        #endregion

        #region Open Files

        private void OpenSelectedFiles()
        {
            OpenFiles(CollectSelectedFiles().ToArray());
        }

        private async void OpenFiles(IList<ArchiveFileInfo> files)
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

        #region Add Files

        private void AddFiles()
        {
            var dlg = new FolderBrowserDialog
            {
                Description = $"Choose where you want to add from to {treDirectories.SelectedNode.FullPath}:"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            var fs = FileSystemFactory.CreatePhysicalFileSystem(dlg.SelectedPath, _stateInfo.StreamManager);
            AddRecursive(fs, UPath.Root);
        }

        private void AddRecursive(IFileSystem fileSystem, UPath currentPath)
        {
            foreach (var dir in fileSystem.EnumeratePaths(currentPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.Directory))
                AddRecursive(fileSystem, dir);

            foreach (var file in fileSystem.EnumeratePaths(currentPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                (ArchiveState as IAddFiles).AddFile(fileSystem, file);
        }

        #endregion

        #endregion
    }
}