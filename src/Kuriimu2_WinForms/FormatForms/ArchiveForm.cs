using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kore;
using Kuriimu2_WinForms.Interfaces;
using Kuriimu2_WinForms.Properties;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IArchiveForm
    {
        private bool _canAddFiles;
        private bool _canDeleteFiles;
        private bool _canExtractDirectories;
        private bool _canExtractFiles;
        private bool _canRenameFiles;
        private bool _canReplaceDirectories;
        private bool _canReplaceFiles;
        private List<TabPage> _childTabs;
        private TabPage _parentTab;
        private string _subFolder;
        private TabPage _currentTab;
        private string _tempFolder;
        public ArchiveForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage, string tempFolder)
        {
            InitializeComponent();

            // Overwrite window themes
            //Win32.SetWindowTheme(treDirectories.Handle, "explorer", null);
            //Win32.SetWindowTheme(lstFiles.Handle, "explorer", null);

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

            Kfi = kfi;
            Kfi.PropertyChanged += Kfi_PropertyChanged;

            _currentTab = tabPage;
            _parentTab = parentTabPage;

            _tempFolder = tempFolder;
            _subFolder = Guid.NewGuid().ToString();
            _childTabs = new List<TabPage>();

            if (!Directory.Exists(Path.Combine(tempFolder, _subFolder)))
                Directory.CreateDirectory(Path.Combine(tempFolder, _subFolder));

            LoadDirectories();
            UpdateForm();
        }

        #region EventHandler
        public event EventHandler<CloseTabEventArgs> CloseTab;

        public event EventHandler<OpenTabEventArgs> OpenTab;

        public event EventHandler<SaveTabEventArgs> SaveTab;
        #endregion

        public KoreFileInfo Kfi { get; set; }
        public Color TabColor { get; set; }
        private IArchiveAdapter _archiveAdapter { get => Kfi.Adapter as IArchiveAdapter; }

        #region Events
        private void Kfi_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KoreFileInfo.DisplayName))
                _currentTab.Text = Kfi.DisplayName;
        }

        #region tlsMain
        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void tsbFind_Click(object sender, EventArgs e)
        {
            Stub();
        }

        private void tsbProperties_Click(object sender, EventArgs e)
        {
            Stub();
        }
        #endregion

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
            var selectedItem = lstFiles.SelectedItems.Count > 0 ? lstFiles.SelectedItems[0] : null;
            var afi = selectedItem?.Tag as ArchiveFileInfo;
            var ext = Path.GetExtension(afi?.FileName);

            extractFileToolStripMenuItem.Enabled = _canExtractFiles;
            extractFileToolStripMenuItem.Text = _canExtractFiles ? "E&xtract..." : "Extract is not supported";
            extractFileToolStripMenuItem.Tag = afi;

            replaceFileToolStripMenuItem.Enabled = _canReplaceFiles;
            replaceFileToolStripMenuItem.Text = _canReplaceFiles ? "&Replace..." : "Replace is not supported";
            replaceFileToolStripMenuItem.Tag = afi;

            renameFileToolStripMenuItem.Enabled = _canRenameFiles;
            renameFileToolStripMenuItem.Text = _canRenameFiles ? "Re&name..." : "Rename is not supported";
            renameFileToolStripMenuItem.Tag = afi;

            deleteFileToolStripMenuItem.Enabled = _canDeleteFiles;
            deleteFileToolStripMenuItem.Text = _canDeleteFiles ? "&Delete" : "Delete is not supported";
            deleteFileToolStripMenuItem.Tag = afi;

            // Generate supported application menu items
            var kuriimuVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<ITextAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var kukkiiVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IImageAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var karameruVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IArchiveAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());

            openFileToolStripMenuItem.Enabled = kuriimuVisible || kukkiiVisible || karameruVisible;
            openFileToolStripMenuItem.Text = openFileToolStripMenuItem.Enabled ? "Open" : "No plugins support this file";
            openFileToolStripMenuItem.Tag = afi;
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

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSelectedFiles();
        }
        #endregion

        #region mnuDirectories
        private void mnuDirectories_Opening(object sender, CancelEventArgs e)
        {
            extractDirectoryToolStripMenuItem.Enabled = _canExtractDirectories;
            extractDirectoryToolStripMenuItem.Text = _canExtractDirectories ? $"E&xtract {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Extract is not supported";

            replaceDirectoryToolStripMenuItem.Enabled = _canReplaceDirectories;
            replaceDirectoryToolStripMenuItem.Text = _canReplaceDirectories ? $"&Replace {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Replace is not supported";

            addDirectoryToolStripMenuItem.Enabled = _canAddFiles;
            addDirectoryToolStripMenuItem.Text = _canAddFiles ? $"&Add to {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Add is not supported";

            deleteDirectoryToolStripMenuItem.Enabled = _canAddFiles;
            deleteDirectoryToolStripMenuItem.Text = _canAddFiles ? $"&Delete {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Delete is not supported";
        }

        private void extractDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treDirectories.SelectedNode;
            var selectedPath = string.Empty;

            while (node.Parent != null)
            {
                selectedPath = node.Text + "\\" + selectedPath;
                node = node.Parent;
            }

            ExtractFiles(CollectFilesFromTreeNode(treDirectories.SelectedNode).ToList(), Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_'), selectedPath.TrimEnd('\\'));
        }

        private void replaceDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treDirectories.SelectedNode;
            var selectedPath = string.Empty;

            while (node.Parent != null)
            {
                selectedPath = node.Text + "\\" + selectedPath;
                node = node.Parent;
            }

            ReplaceFiles(CollectFilesFromTreeNode(treDirectories.SelectedNode).ToList(), Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_'), selectedPath.TrimEnd('\\'));

            UpdateParent();
            UpdateForm();
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!treDirectories.Focused)
                return;

            AddFiles();

            LoadDirectories();

            UpdateParent();
            UpdateForm();
        }

        private void deleteDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treDirectories.SelectedNode?.Tag is IEnumerable<ArchiveFileInfo>)
                DeleteFiles(treDirectories.SelectedNode?.Tag as IEnumerable<ArchiveFileInfo>);

            LoadDirectories();

            UpdateParent();
            UpdateForm();
        }
        #endregion

        #region treDirectories
        private void treDirectories_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory";
                e.Node.SelectedImageKey = e.Node.ImageKey;
            }
        }

        private void treDirectories_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory-open";
                e.Node.SelectedImageKey = e.Node.ImageKey;
            }
        }

        private void treDirectories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            LoadFiles();

            UpdateForm();
        }
        private void treDirectories_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            UpdateForm();
        }
        #endregion

        #region lstFiles
        private void lstFiles_DoubleClick(object sender, EventArgs e)
        {
            var menuItem = lstFiles.SelectedItems[0];
            var afi = menuItem.Tag as ArchiveFileInfo;
            var ext = Path.GetExtension(afi?.FileName);

            var kuriimuVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<ITextAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var kukkiiVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IImageAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var karameruVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IArchiveAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());

            if (kuriimuVisible || kukkiiVisible || karameruVisible)
            {
                if (!OpenAfi(afi))
                    MessageBox.Show("File couldn't be opened.", "Opening error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("This file is not supported by any plugin.", "Not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateForm();
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
            if (node.Tag is IEnumerable<ArchiveFileInfo> files)
                foreach (var file in files)
                    yield return file;

            foreach (TreeNode childNode in node.Nodes)
                foreach (var file in CollectFilesFromTreeNode(childNode))
                    yield return file;
        }

        private void LoadDirectories()
        {
            treDirectories.BeginUpdate();
            treDirectories.Nodes.Clear();

            if (_archiveAdapter.Files != null)
            {
                var lookup = _archiveAdapter.Files.OrderBy(f => f.FileName.TrimStart('/', '\\')).ToLookup(f => Path.GetDirectoryName(f.FileName.TrimStart('/', '\\')));

                // Build directory tree
                var root = treDirectories.Nodes.Add("root", Kfi.StreamFileInfo.FileName, "tree-archive-file", "tree-archive-file");
                foreach (var dir in lookup.Select(g => g.Key))
                {
                    dir.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                        .Aggregate(root, (node, part) => node.Nodes[part] ?? node.Nodes.Add(part, part))
                        .Tag = lookup[dir];
                }

                root.Expand();
                treDirectories.SelectedNode = root;
            }
            else
                LoadFiles();

            treDirectories.EndUpdate();
            treDirectories.Focus();
        }

        private void LoadFiles()
        {
            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();

            if (treDirectories.SelectedNode?.Tag is IEnumerable<ArchiveFileInfo> files)
            {
                imlFiles.Images.Add("0", Resources.menu_new);

                foreach (var file in files)
                {
                    // Get the items from the file system, and add each of them to the ListView,
                    // complete with their corresponding name and icon indices.
                    //var ext = Path.GetExtension(file.FileName).ToLower();
                    //TODO
                    //var textFile = ext.Length > 0 && PluginLoader.Global.FileExtensionsByType<ITextAdapter>().Contains(ext);
                    //var imageFile = ext.Length > 0 && PluginLoader.Global.FileExtensionsByType<IImageAdapter>().Contains(ext);
                    //var archiveFile = ext.Length > 0 && PluginLoader.Global.FileExtensionsByType<IArchiveAdapter>().Contains(ext);

                    ////TODO
                    //if (false) ext = "tree-text-file";
                    //if (false) ext = "tree-image-file";
                    //if (false) ext = "tree-archive-file";

                    //var sb = new StringBuilder(16);
                    //Win32.StrFormatByteSize((long)file.FileSize, sb, 16);
                    lstFiles.Items.Add(new ListViewItem(new[] { Path.GetFileName(file.FileName), file.FileSize.ToString(), file.State.ToString() }, "0", StateToColor(file.State), Color.Transparent, lstFiles.Font) { Tag = file });
                }

                tslFileCount.Text = $"Files: {files.Count()}";
            }

            lstFiles.EndUpdate();
        }

        private Color StateToColor(ArchiveFileState state)
        {
            Color result = Color.Black;

            switch (state)
            {
                case ArchiveFileState.Empty:
                    result = Color.DarkGray;
                    break;
                case ArchiveFileState.Added:
                    result = Color.Green;
                    break;
                case ArchiveFileState.Replaced:
                    result = Color.Orange;
                    break;
                case ArchiveFileState.Renamed:
                    result = Color.Blue;
                    break;
                case ArchiveFileState.Deleted:
                    result = Color.Red;
                    break;
            }

            return result;
        }
        #endregion

        #region Updates
        public void UpdateForm()
        {
            _currentTab.Text = Kfi.DisplayName;

            var selectedItem = lstFiles.SelectedItems.Count > 0 ? lstFiles.SelectedItems[0] : null;
            var afi = selectedItem?.Tag as ArchiveFileInfo;

            bool nodeSelected = treDirectories.SelectedNode != null;

            _canExtractDirectories = nodeSelected;
            _canReplaceDirectories = nodeSelected;

            bool itemSelected = lstFiles.SelectedItems.Count > 0;

            _canAddFiles = _archiveAdapter is IArchiveAddFile;
            _canExtractFiles = itemSelected && (bool)afi?.FileSize.HasValue;
            _canReplaceFiles = itemSelected && _archiveAdapter is IArchiveReplaceFiles;
            _canRenameFiles = itemSelected && _archiveAdapter is IArchiveRenameFiles;
            _canDeleteFiles = itemSelected && _archiveAdapter is IArchiveDeleteFile;

            var ext = Path.GetExtension(afi?.FileName);

            var kuriimuVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<ITextAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var kukkiiVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IImageAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var karameruVisible = ext?.Length > 0 && PluginLoader.Instance.GetAdapters<IArchiveAdapter>().Select(x => PluginLoader.Instance.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());

            // Menu
            tsbSave.Enabled = _archiveAdapter is ISaveFiles;
            tsbSaveAs.Enabled = _archiveAdapter is ISaveFiles && Kfi.ParentKfi == null;
            tsbProperties.Enabled = _archiveAdapter.FileHasExtendedProperties;

            // Toolbar
            tsbFileExtract.Enabled = _canExtractFiles;
            tsbFileReplace.Enabled = _canReplaceFiles;
            tsbFileRename.Enabled = _canRenameFiles;
            tsbFileDelete.Enabled = _canDeleteFiles;
            tsbFileOpen.Enabled = kuriimuVisible || kukkiiVisible || karameruVisible;
        }

        public void UpdateParent()
        {
            if (_parentTab != null)
                if (_parentTab.Controls[0] is IArchiveForm archiveForm)
                {
                    archiveForm.UpdateForm();
                    archiveForm.UpdateParent();
                }
        }
        #endregion

        #region Child Tabs
        public void RemoveChildTab(TabPage tabPage)
        {
            if (_childTabs.Contains(tabPage))
                _childTabs.Remove(tabPage);
        }

        public void UpdateChildTabs(KoreFileInfo kfi)
        {
            if (kfi.ChildKfi != null)
                foreach (var child in kfi.ChildKfi.Zip(_childTabs, (x, y) => new { Kfi = x, Tab = y }))
                {
                    (child.Tab.Controls[0] as IKuriimuForm).Kfi = child.Kfi;
                    if (child.Tab.Controls[0] is IArchiveForm archiveForm) archiveForm.UpdateChildTabs(child.Kfi);
                }

            LoadDirectories();

            UpdateForm();
        }
        #endregion

        #region Save
        private void SaveAs()
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(Kfi.StreamFileInfo.FileName);
            sfd.Filter = "All Files (*.*)|*.*";

            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
            else
                MessageBox.Show("No save location was chosen", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void Save(string filename = "")
        {
            SaveTab?.Invoke(this, new SaveTabEventArgs(Kfi) { NewSaveFile = filename });

            UpdateParent();
            UpdateForm();
        }
        #endregion

        #region Extract
        private void ExtractSelectedFiles()
        {
            ExtractFiles(CollectSelectedFiles().ToList());

            UpdateParent();
            UpdateForm();
        }

        private void ExtractFiles(List<ArchiveFileInfo> files, string selectedNode = "", string selectedPath = "")
        {
            var selectedPathRegex = "^" + selectedPath.Replace("\\", @"[\\/]") + @"[\\/]?";

            if (files?.Count > 1)
            {
                // Extracting more than one file should choose a folder to extract to

                var fbd = new FolderBrowserDialog
                {
                    SelectedPath = Settings.Default.LastDirectory,
                    Description = $"Select where you want to extract {selectedNode} to..."
                };

                if (fbd.ShowDialog() != DialogResult.OK) return;
                foreach (var afi in files)
                {
                    var stream = afi.FileData;
                    if (stream == null) continue;

                    var path = Path.Combine(fbd.SelectedPath, Regex.Replace(Path.GetDirectoryName(afi.FileName).TrimStart('/', '\\').TrimEnd('\\') + "\\", selectedPathRegex, selectedNode + "\\"));

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    using (var fs = File.Create(Path.Combine(fbd.SelectedPath, path, Path.GetFileName(afi.FileName))))
                    {
                        if (stream.CanSeek)
                            stream.Position = 0;

                        try
                        {
                            if (afi.FileSize > 0)
                            {
                                stream.CopyTo(fs);
                                fs.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "Partial Extraction Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                MessageBox.Show($"\"{selectedNode}\" extracted successfully.", "Extraction Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (files?.Count == 1)
            {
                // Extracting just one file should choose a folder and filename

                var afi = files.First();
                var stream = afi?.FileData;
                var filename = Path.GetFileName(afi?.FileName);

                if (stream == null)
                {
                    MessageBox.Show($"Uninitialized file stream. Unable to extract \"{filename}\".", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var extension = Path.GetExtension(filename).ToLower();
                var sfd = new SaveFileDialog
                {
                    InitialDirectory = Settings.Default.LastDirectory,
                    FileName = filename,
                    Filter = $"{extension.ToUpper().TrimStart('.')} File (*{extension})|*{extension}"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;
                using (var fs = File.Create(sfd.FileName))
                {
                    if (stream.CanSeek)
                        stream.Position = 0;

                    try
                    {
                        if (afi.FileSize > 0)
                        {
                            stream.CopyTo(fs);
                            fs.Close();
                        }

                        MessageBox.Show($"\"{filename}\" extracted successfully.", "Extraction Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion

        #region Replace
        private void ReplaceSelectedFiles()
        {
            ReplaceFiles(CollectSelectedFiles().ToList());

            UpdateParent();
            UpdateForm();
        }

        private void ReplaceFiles(List<ArchiveFileInfo> files, string selectedNode = "", string selectedPath = "")
        {
            var selectedPathRegex = "^" + selectedPath.Replace("\\", @"[\\/]") + @"[\\/]?";

            if (files?.Count > 1)
            {
                // Extracting more than one file should choose a folder to extract to

                var fbd = new FolderBrowserDialog
                {
                    SelectedPath = Directory.Exists(Path.Combine(Settings.Default.LastDirectory, selectedNode)) ? Path.Combine(Settings.Default.LastDirectory, selectedNode) : Settings.Default.LastDirectory,
                    Description = $"Select where you want to replace {selectedNode} from..."
                };

                if (fbd.ShowDialog() != DialogResult.OK) return;
                var replaceCount = 0;
                foreach (var afi in files)
                {
                    var path = Path.Combine(fbd.SelectedPath, Regex.Replace(Path.GetDirectoryName(afi.FileName).TrimStart('/', '\\').TrimEnd('\\') + "\\", selectedPathRegex, string.Empty));
                    var file = Path.Combine(fbd.SelectedPath, path, Path.GetFileName(afi.FileName));

                    if (!File.Exists(file)) continue;

                    if (afi.FileData is FileStream)
                        afi.FileData.Close();

                    try
                    {
                        afi.FileData = File.OpenRead(file);
                        afi.State = ArchiveFileState.Replaced;
                        replaceCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Partial Replacement Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                MessageBox.Show($"Replaced {replaceCount} files in \"{selectedNode}\" successfully.", "Replacement Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (files?.Count == 1)
            {
                // Extracting just one file should choose a folder and filename

                var afi = files.First();
                var filename = Path.GetFileName(afi.FileName);

                var ofd = new OpenFileDialog();
                ofd.Title = $"Select a file to replace {filename} with...";
                ofd.InitialDirectory = Settings.Default.LastDirectory;

                ofd.Filter = "All Files (*.*)|*.*";

                if (ofd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    afi.FileData = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    afi.State = ArchiveFileState.Replaced;
                    lstFiles.SelectedItems[0].ForeColor = StateToColor(afi.State);
                    MessageBox.Show($"{filename} has been replaced with {Path.GetFileName(ofd.FileName)}.", "File Replaced", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Replace Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            Kfi.HasChanges = true;

            LoadFiles();
        }
        #endregion

        #region Rename
        private void RenameSelectedFiles()
        {
            RenameFiles(CollectSelectedFiles().ToList());

            UpdateParent();
            UpdateForm();
        }

        private void RenameFiles(List<ArchiveFileInfo> afis)
        {
            if (afis == null || afis.Count <= 0)
                return;

            List<ArchiveFileInfo> canceledRenames = new List<ArchiveFileInfo>();
            foreach (var afi in afis)
            {
                var inputBox = new InputBox($"Select a new filename for \"{Path.GetFileName(afi.FileName)}\"", "Rename file", Path.GetFileName(afi.FileName));
                if (inputBox.ShowDialog() == DialogResult.Cancel)
                    canceledRenames.Add(afi);
                else
                    (_archiveAdapter as IArchiveRenameFiles).RenameFile(afi, Path.GetFileName(inputBox.InputText));
            }

            if (canceledRenames.Count > 0)
                MessageBox.Show($"Following files were not renamed:" + canceledRenames.Aggregate("", (a, b) => a + Environment.NewLine + Path.GetFileName(b.FileName)), "Renaming error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (canceledRenames.Count != afis.Count)
                Kfi.HasChanges = true;

            LoadFiles();
        }
        #endregion

        #region Delete
        private void DeleteSelectedFiles()
        {
            DeleteFiles(CollectSelectedFiles());

            UpdateParent();
            UpdateForm();
        }

        private void DeleteFiles(IEnumerable<ArchiveFileInfo> toDelete)
        {
            if (toDelete?.Count() > 0)
                foreach (var d in toDelete)
                    (_archiveAdapter as IArchiveDeleteFile).DeleteFile(d);
        }
        #endregion

        #region Open
        private void OpenSelectedFiles()
        {
            OpenFiles(CollectSelectedFiles().ToList());
        }

        private void OpenFiles(List<ArchiveFileInfo> afis)
        {
            if (afis == null || afis.Count <= 0)
                return;

            var notOpened = new List<ArchiveFileInfo>();
            foreach (var afi in afis)
            {
                if (!OpenAfi(afi))
                    notOpened.Add(afi);
            }

            if (notOpened.Count > 0)
                MessageBox.Show($"Following files were not opened:" + notOpened.Aggregate("", (a, b) => a + Environment.NewLine + Path.GetFileName(b.FileName)), "Opening error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool OpenAfi(ArchiveFileInfo afi)
        {
            var fs = new VirtualFileSystem(_archiveAdapter, Path.Combine(_tempFolder, _subFolder));

            var args = new OpenTabEventArgs(afi, Kfi, fs) { LeaveOpen = true };
            OpenTab?.Invoke(this, args);

            if (args.EventResult && args.OpenedTabPage != null)
            {
                if (Kfi.ChildKfi == null)
                    Kfi.ChildKfi = new List<KoreFileInfo>();
                Kfi.ChildKfi.Add((args.OpenedTabPage.Controls[0] as IKuriimuForm).Kfi);

                _childTabs.Add(args.OpenedTabPage);
            }

            return args.EventResult;
        }
        #endregion

        #region Add
        private void AddFiles()
        {
            var dlg = new FolderBrowserDialog
            {
                Description = $"Choose where you want to add from to {treDirectories.SelectedNode.FullPath}:"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                void AddRecursive(string root, string currentPath = "")
                {
                    foreach (var dir in Directory.GetDirectories(Path.Combine(root, currentPath)))
                        AddRecursive(root, dir.Replace(root + "\\", ""));

                    foreach (var file in Directory.GetFiles(Path.Combine(root, currentPath)))
                        AddFile(root, file.Replace(root + "\\", ""));
                }
                void AddFile(string root, string currentPath)
                {
                    (_archiveAdapter as IArchiveAddFile).AddFile(new ArchiveFileInfo
                    {
                        State = ArchiveFileState.Added,
                        FileName = Path.Combine(GetFilePath(treDirectories.SelectedNode, treDirectories.TopNode.Name), currentPath),
                        FileData = File.OpenRead(Path.Combine(root, currentPath))
                    });
                }
                string GetFilePath(TreeNode node, string limit)
                {
                    var res = "";
                    if (node.Name != limit)
                        res = GetFilePath(node.Parent, limit);
                    else
                        return String.Empty;

                    return Path.Combine(res, node.Name);
                }

                AddRecursive(dlg.SelectedPath);
            }
        }
        #endregion

        #region Close
        public void Close()
        {
            CloseTab?.Invoke(this, new CloseTabEventArgs(Kfi) { LeaveOpen = Kfi.ParentKfi != null });
        }
        #endregion

        #endregion
    }
}