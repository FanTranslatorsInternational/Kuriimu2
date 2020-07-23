using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
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
using MoreLinq;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IArchiveForm
    {
        private const string AllFilesFilter_ = "All Files (*.*)|*.*";
        private const string FileCount_ = "Files: {0}";

        private static readonly Color _colorDefaultState = Color.Black;
        private static readonly Color _colorChangedState = Color.Orange;

        private readonly IInternalPluginManager _pluginManager;
        private readonly IStateInfo _stateInfo;
        private readonly IProgressContext _progressContext;

        private IList<UPath> _expandedPaths;

        private bool _isSearchEmpty = true;
        private string _searchTerm;

        private ISaveFiles SaveState => _stateInfo.PluginState as ISaveFiles;
        private IArchiveState ArchiveState => _stateInfo.PluginState as IArchiveState;

        public Func<OpenFileEventArgs, Task<bool>> OpenFilesDelegate { get; set; }
        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        public ArchiveForm(IStateInfo loadedState, IInternalPluginManager pluginManager, IProgressContext progressContext)
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

            _pluginManager = pluginManager;

            _expandedPaths = new List<UPath>();

            UpdateDirectories();
            UpdateFormInternal();
        }

        #region Save File

        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void SaveAs()
        {
            var sfd = new SaveFileDialog
            {
                FileName = _stateInfo.FilePath.GetName(),
                Filter = AllFilesFilter_
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
        }

        private void Save()
        {
            Save(UPath.Empty);
        }

        private async void Save(UPath savePath)
        {
            await SaveFilesDelegate(new SaveTabEventArgs(_stateInfo, savePath));

            UpdateDirectories();
            UpdateFiles();

            UpdateFormInternal();
        }

        #endregion

        #region Update Methods

        public void UpdateForm()
        {
            UpdateProperties();

            UpdateDirectoryColors();
            UpdateFiles();
        }

        private void UpdateFormInternal()
        {
            UpdateProperties();
            UpdateTabDelegate?.Invoke(_stateInfo);
        }

        private void UpdateProperties()
        {
            // Menu
            tsbSave.Enabled = ArchiveState is ISaveFiles;
            tsbSaveAs.Enabled = ArchiveState is ISaveFiles && _stateInfo.ParentStateInfo == null;
            // TODO: Property implementation
            //tsbProperties.Enabled = _archiveAdapter.FileHasExtendedProperties;

            // Toolbar
            tsbFileExtract.Enabled = true;
            tsbFileReplace.Enabled = ArchiveState is IReplaceFiles && ArchiveState is ISaveFiles;
            tsbFileRename.Enabled = ArchiveState is IRenameFiles && ArchiveState is ISaveFiles;
            tsbFileDelete.Enabled = ArchiveState is IRemoveFiles && ArchiveState is ISaveFiles;
        }

        private void UpdateFiles()
        {
            if (!(treDirectories.SelectedNode?.Tag is IList<ArchiveFileInfo> files))
            {
                lstFiles.Items.Clear();
                return;
            }

            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();

            imlFiles.Images.Add("0", Resources.menu_new);

            foreach (var file in files)
            {
                var isChanged = file.ContentChanged || _stateInfo.ArchiveChildren.Any(x => x.FilePath == file.FilePath);

                var foreColor = isChanged ? _colorChangedState : _colorDefaultState;
                var listViewItem = new ListViewItem(new[] { file.FilePath.GetName(), file.FileSize.ToString() },
                    "0", foreColor, Color.Transparent, lstFiles.Font)
                {
                    Tag = file
                };

                lstFiles.Items.Add(listViewItem);
            }

            tslFileCount.Text = string.Format(FileCount_, files.Count);

            lstFiles.EndUpdate();
        }

        private void UpdateDirectories()
        {
            if (ArchiveState.Files == null || !ArchiveState.Files.Any())
            {
                treDirectories.Nodes.Clear();
                return;
            }

            treDirectories.BeginUpdate();
            treDirectories.Nodes.Clear();

            var archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo, UPath.Root, _stateInfo.StreamManager);
            var filePaths = archiveFileSystem.EnumeratePaths(UPath.Root, _isSearchEmpty ? "*" : _searchTerm,
                SearchOption.AllDirectories, SearchTarget.Directory);
            var lookup = ArchiveState.Files.OrderBy(f => f.FilePath).ToLookup(f => f.FilePath.GetDirectory());

            // 1. Build directory tree
            var root = treDirectories.Nodes.Add("root", _stateInfo.FilePath.ToRelative().FullName,
                "tree-archive-file", "tree-archive-file");
            foreach (var path in filePaths)
            {
                path.Split()
                    .Aggregate(root, (node, part) => node.Nodes[part] ?? node.Nodes.Add(part, part))
                    .Tag = lookup[path];
            }

            // 2. Expand nodes
            foreach (var expandedPath in _expandedPaths.Select(x => x.Split()).ToArray())
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

            // Always expand root
            root.Expand();
            treDirectories.SelectedNode = root;

            treDirectories.EndUpdate();
            treDirectories.Focus();

            UpdateDirectoryColors();
        }

        private void UpdateDirectoryColors()
        {
            var root = treDirectories.Nodes["root"];
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

                        node.ForeColor = _colorDefaultState;
                        Iterate(node.Nodes);
                    }
                }

                root.ForeColor = _colorDefaultState;
                Iterate(root.Nodes);
                return;
            }

            treDirectories.BeginUpdate();

            root.ForeColor = _colorChangedState;
            foreach (var filePath in changedFilePaths.Select(x => x.Split()))
            {
                filePath.Aggregate(root, (node, part) =>
                {
                    var newNode = node?.Nodes[part];
                    if (newNode == null)
                        return null;

                    newNode.ForeColor = _colorChangedState;
                    return newNode;
                });
            }

            treDirectories.EndUpdate();
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateFileContextMenu()
        {
            if (lstFiles.SelectedItems.Count <= 0)
                return;

            if (!(lstFiles.SelectedItems[0]?.Tag is ArchiveFileInfo afi))
                return;

            var selectedFilePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath / afi.FilePath.ToRelative();
            var isLocked = _pluginManager.IsLoaded(selectedFilePath);

            var canExtractFiles = !isLocked;
            var canReplaceFiles = ArchiveState is IReplaceFiles && !isLocked;
            var canRenameFiles = ArchiveState is IRenameFiles && !isLocked;
            var canDeleteFiles = ArchiveState is IRemoveFiles && !isLocked;

            // Update action menu items

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

            // Update Open With context

            openWithToolStripMenuItem.DropDownItems.Clear();

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
                openWithToolStripMenuItem.DropDownItems.Add(item);
            }

            openWithToolStripMenuItem.Enabled = openWithToolStripMenuItem.DropDownItems.Count > 0;
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private void UpdateDirectoryContextMenu()
        {
            var fileName = ((UPath)treDirectories.SelectedNode.Text).GetName().Replace('.', '_');

            var canReplaceFiles = ArchiveState is IReplaceFiles;
            var canDeleteFiles = ArchiveState is IRemoveFiles;
            var canAddFiles = ArchiveState is IAddFiles;

            replaceDirectoryToolStripMenuItem.Enabled = canReplaceFiles;
            replaceDirectoryToolStripMenuItem.Text = canReplaceFiles ? $"&Replace {fileName}..." : "Replace is not supported";

            addDirectoryToolStripMenuItem.Enabled = canAddFiles;
            addDirectoryToolStripMenuItem.Text = canAddFiles ? $"&Add to {fileName}..." : "Add is not supported";

            deleteDirectoryToolStripMenuItem.Enabled = canDeleteFiles;
            deleteDirectoryToolStripMenuItem.Text = canDeleteFiles ? $"&Delete {fileName}..." : "Delete is not supported";
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

            if (!await OpenAfi(afi, pluginId))
                MessageBox.Show($"File could not be opened with plugin '{pluginId}'.",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var node = treDirectories.SelectedNode;
            var selectedPath = UPath.Empty;

            while (node.Parent != null)
            {
                selectedPath = node.Text / selectedPath;
                node = node.Parent;
            }

            var treeFiles = CollectFilesFromTreeNode(treDirectories.SelectedNode).ToList();
            ReplaceFiles(treeFiles, selectedPath);

            UpdateFormInternal();
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!treDirectories.Focused)
                return;

            AddFiles();

            UpdateDirectories();
            UpdateFormInternal();
        }

        private void deleteDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treDirectories.SelectedNode?.Tag is IEnumerable<ArchiveFileInfo>)
                DeleteFiles(treDirectories.SelectedNode?.Tag as IList<ArchiveFileInfo>);

            UpdateDirectories();
            UpdateFormInternal();
        }

        #endregion

        #region treDirectories

        private void treDirectories_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory";
                e.Node.SelectedImageKey = e.Node.ImageKey;

                var nodePath = GetNodePath(treDirectories.Nodes["root"], e.Node);
                if (_expandedPaths.Contains(nodePath))
                    _expandedPaths.Remove(nodePath);
            }
        }

        private void treDirectories_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory-open";
                e.Node.SelectedImageKey = e.Node.ImageKey;

                // Add expanded node
                var nodePath = GetNodePath(treDirectories.Nodes["root"], e.Node);
                if (nodePath != UPath.Empty)
                    _expandedPaths.Add(nodePath);
            }
        }

        private void treDirectories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateFiles();
            UpdateFormInternal();
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

        private IEnumerable<ArchiveFileInfo> CollectFilesFromTreeNode(TreeNode node)
        {
            if (node.Tag is IList<ArchiveFileInfo> files)
                foreach (var file in files)
                    yield return file;

            foreach (TreeNode childNode in node.Nodes)
                foreach (var file in CollectFilesFromTreeNode(childNode))
                    yield return file;
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

        private IEnumerable<UPath> CollectExpandedDirectories(TreeNodeCollection nodeCollection)
        {
            if (nodeCollection == null)
                yield break;

            foreach (TreeNode node in nodeCollection)
            {
                if (!node.IsExpanded)
                    continue;

                var nodeName = node.Name;
                yield return nodeName;

                foreach (var collectedPath in CollectExpandedDirectories(node.Nodes))
                    yield return UPath.Combine(nodeName, collectedPath);
            }
        }

        private void AddSearchPlaceholder()
        {
            txtSearch.Text = "Search archive...";
            txtSearch.ForeColor = SystemColors.ScrollBar;
        }

        #endregion

        #region Extract Directories

        private async void ExtractSelectedDirectory()
        {
            await ExtractDirectory(treDirectories.SelectedNode);
        }

        private Task<bool> ExtractDirectory(TreeNode node)
        {
            return ExtractDirectory(node, UPath.Empty);
        }

        private async Task<bool> ExtractDirectory(TreeNode node, UPath extractPath)
        {
            if (node == null)
                return true;

            if (!(node.Tag is IList<ArchiveFileInfo> files))
            {
                // Extract sub directories
                foreach (TreeNode subNode in node.Nodes)
                    if (!await ExtractDirectory(subNode, extractPath))
                        return false;

                return true;
            }

            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                // Select folder to extract to
                var fbd = new FolderBrowserDialog
                {
                    SelectedPath = Settings.Default.LastDirectory,
                    Description = $"Select where you want to extract '{node.FullPath}' to..."
                };

                if (fbd.ShowDialog() != DialogResult.OK)
                    return false;

                extractPath = fbd.SelectedPath;
            }

            // Extract files of directory
            extractPath /= node == treDirectories.Nodes["root"] ? _stateInfo.FilePath.GetName() : node.Name;
            if (!await ExtractFiles(files, extractPath))
                return false;

            // Extract sub directories
            foreach (TreeNode subNode in node.Nodes)
                if (!await ExtractDirectory(subNode, extractPath))
                    return false;

            return true;
        }

        #endregion

        #region Extract Files

        private async void ExtractSelectedFiles()
        {
            var selectedFiles = CollectSelectedFiles().ToArray();

            if (selectedFiles.Length <= 0)
                return;

            await ExtractFiles(selectedFiles);
        }

        private Task<bool> ExtractFiles(IList<ArchiveFileInfo> files)
        {
            return ExtractFiles(files, UPath.Empty);
        }

        private async Task<bool> ExtractFiles(IList<ArchiveFileInfo> files, UPath extractPath)
        {
            if (files == null || !files.Any())
                return true;

            var directory = files[0].FilePath.GetDirectory();
            if (files.Any(x => x.FilePath.GetDirectory() != directory))
                return true;

            // Select folder to extract to
            if (extractPath.IsNull || extractPath.IsEmpty)
            {
                var fbd = new FolderBrowserDialog
                {
                    SelectedPath = Settings.Default.LastDirectory,
                    Description = $"Select where you want to extract '{directory.ToRelative()}' to..."
                };

                if (fbd.ShowDialog() != DialogResult.OK)
                    return false;

                extractPath = fbd.SelectedPath;
            }

            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(extractPath, _stateInfo.StreamManager);
            foreach (var file in files)
            {
                var newFileStream = destinationFileSystem.OpenFile(file.FilePath.GetName(), FileMode.OpenOrCreate, FileAccess.Write);
                var fileStream = await file.GetFileData();

                fileStream.CopyTo(newFileStream);

                fileStream.Close();
                newFileStream.Close();
            }

            return true;
        }

        #endregion

        #region Replace

        private void ReplaceSelectedFiles()
        {

            var selectedFiles = CollectSelectedFiles().ToList();

            ReplaceFiles(selectedFiles);

            UpdateFormInternal();
        }

        private void ReplaceFiles(IList<ArchiveFileInfo> files)
        {
            if (files.Count > 1)
                ReplaceFiles(files, files[0].FilePath.GetDirectory());
            else
                ReplaceFile(files[0]);
        }

        private void ReplaceFiles(IList<ArchiveFileInfo> files, UPath rootPath)
        {
            var fbd = new FolderBrowserDialog
            {
                SelectedPath = Settings.Default.LastDirectory,
                Description = $"Select where you want to replace {rootPath} from..."
            };

            if (fbd.ShowDialog() != DialogResult.OK)
                return;

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(fbd.SelectedPath, _stateInfo.StreamManager);
            var destinationFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo, rootPath.ToAbsolute(), _stateInfo.StreamManager);

            var replaceCount = 0;
            foreach (var sourcePath in sourceFileSystem.EnumeratePaths(UPath.Root, "*", SearchOption.AllDirectories, SearchTarget.File))
            {
                if (!destinationFileSystem.FileExists(sourcePath))
                    continue;

                var newFileData = sourceFileSystem.OpenFile(sourcePath);
                if (!(ArchiveState is IReplaceFiles replaceState))
                    continue;

                var afi = files.First(f => f.FilePath == rootPath.ToAbsolute() / sourcePath.ToRelative());
                replaceState.ReplaceFile(afi, newFileData);
                replaceCount++;
            }

            MessageBox.Show($"Replaced {replaceCount} files in \"{rootPath}\" successfully.", "Replacement Result",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateDirectoryColors();
            UpdateFiles();

            UpdateFormInternal();
        }

        private void ReplaceFile(ArchiveFileInfo file)
        {
            ContractAssertions.IsNotNull(file, nameof(file));

            var fileName = file.FilePath.GetName();

            var sfd = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.LastDirectory,
                FileName = fileName,
                Filter = AllFilesFilter_
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(((UPath)sfd.FileName).GetDirectory(), _stateInfo.StreamManager);
            var newFileData = sourceFileSystem.OpenFile(((UPath)sfd.FileName).GetName());

            if (!(ArchiveState is IReplaceFiles replaceState))
                return;

            replaceState.ReplaceFile(file, newFileData);

            MessageBox.Show($"Replaced {file.FilePath} with \"{sfd.FileName}\" successfully.", "Replacement Result",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateDirectoryColors();
            UpdateFiles();

            UpdateFormInternal();
        }

        #endregion

        #region Rename

        private void RenameSelectedFiles()
        {
            RenameFiles(CollectSelectedFiles().ToList());

            UpdateFormInternal();
        }

        private void RenameFiles(IList<ArchiveFileInfo> files)
        {
            ContractAssertions.IsNotNull(files, nameof(files));

            var canceledRenames = new List<ArchiveFileInfo>();
            foreach (var afi in files)
            {
                var inputBox = new InputBox($"Select a new filename for '{afi.FilePath.GetName()}'.",
                    "Rename file",
                    afi.FilePath.GetName());

                if (inputBox.ShowDialog() == DialogResult.OK)
                    (ArchiveState as IRenameFiles).Rename(afi, ((UPath)inputBox.InputText).GetName());
                else
                    canceledRenames.Add(afi);
            }

            if (canceledRenames.Count > 0)
            {
                var canceledRenameFiles = string.Join(Environment.NewLine, canceledRenames.Select(x => x.FilePath.GetName()));
                MessageBox.Show($"Following files were not renamed:{canceledRenameFiles}",
                    "Renaming error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateFiles();
        }

        #endregion

        #region Delete

        private void DeleteSelectedFiles()
        {
            DeleteFiles(CollectSelectedFiles().ToList());

            UpdateFiles();
            UpdateFormInternal();
        }

        private void DeleteFiles(IList<ArchiveFileInfo> files)
        {
            ContractAssertions.IsNotNull(files, nameof(files));

            if (files.Count <= 0)
                return;

            foreach (var afi in files)
                (ArchiveState as IRemoveFiles)?.RemoveFile(afi);
        }

        #endregion

        #region Open

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

                if (!pluginIds.Any())
                {
                    // Use automatic identification
                    if (!await OpenAfi(file))
                    {
                        MessageBox.Show("File couldn't be opened.",
                            "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                }

                // Opening by plugin id
                foreach (var pluginId in pluginIds)
                    await OpenAfi(file, pluginId);

                MessageBox.Show("File could not be loaded by any preset plugin.", "LoadError", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Task<bool> OpenAfi(ArchiveFileInfo afi) => OpenAfi(afi, Guid.Empty);

        private Task<bool> OpenAfi(ArchiveFileInfo afi, Guid plugin)
        {
            var args = new OpenFileEventArgs(_stateInfo, afi, plugin);
            return OpenFilesDelegate?.Invoke(args) ?? Task.FromResult(false);
        }

        #endregion

        #region Add

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