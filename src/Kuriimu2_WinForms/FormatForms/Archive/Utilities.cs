using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kore;
using Kuriimu2_WinForms.Interfaces;
using Kuriimu2_WinForms.Properties;
using Kuriimu2_WinForms.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.FormatForms.Archive
{
    public partial class ArchiveForm
    {
        #region General
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
                foreach (var file in files)
                {
                    // Get the items from the file system, and add each of them to the ListView,
                    // complete with their corresponding name and icon indices.
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    var textFile = ext.Length > 0 && _kore.FileExtensionsByType<ITextAdapter>().Contains(ext);
                    var imageFile = ext.Length > 0 && _kore.FileExtensionsByType<IImageAdapter>().Contains(ext);
                    var archiveFile = ext.Length > 0 && _kore.FileExtensionsByType<IArchiveAdapter>().Contains(ext);

                    var shfi = new Win32.SHFILEINFO();
                    try
                    {
                        if (!imlFiles.Images.ContainsKey(ext) && !string.IsNullOrEmpty(ext))
                        {
                            Win32.SHGetFileInfo(ext, 0, out shfi, Marshal.SizeOf(shfi), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_USEFILEATTRIBUTES);
                            imlFiles.Images.Add(ext, Icon.FromHandle(shfi.hIcon));
                        }
                    }
                    finally
                    {
                        if (shfi.hIcon != IntPtr.Zero)
                            Win32.DestroyIcon(shfi.hIcon);
                    }
                    try
                    {
                        if (!imlFilesLarge.Images.ContainsKey(ext) && !string.IsNullOrEmpty(ext))
                        {
                            Win32.SHGetFileInfo(ext, 0, out shfi, Marshal.SizeOf(shfi), Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON | Win32.SHGFI_USEFILEATTRIBUTES);
                            imlFilesLarge.Images.Add(ext, Icon.FromHandle(shfi.hIcon));
                        }
                    }
                    finally
                    {
                        if (shfi.hIcon != IntPtr.Zero)
                            Win32.DestroyIcon(shfi.hIcon);
                    }

                    if (textFile) ext = "tree-text-file";
                    if (imageFile) ext = "tree-image-file";
                    if (archiveFile) ext = "tree-archive-file";

                    var sb = new StringBuilder(16);
                    Win32.StrFormatByteSize((long)file.FileSize, sb, 16);
                    lstFiles.Items.Add(new ListViewItem(new[] { Path.GetFileName(file.FileName), sb.ToString(), file.State.ToString() }, ext, StateToColor(file.State), Color.Transparent, lstFiles.Font) { Tag = file });
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

        private void UpdateForm()
        {
            _tabPage.Text = Kfi.DisplayName;

            //Text = $"{Settings.Default.ApplicationName} v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion}" + (FileName() != string.Empty ? " - " + FileName() : string.Empty) + (_hasChanges ? "*" : string.Empty) + (_archiveManager != null ? " - " + _archiveManager.Description + " Manager (" + _archiveManager.Name + ")" : string.Empty);

            //openToolStripMenuItem.Enabled = _archiveManagers.Count > 0;
            //tsbOpen.Enabled = _archiveManagers.Count > 0;

            var selectedItem = lstFiles.SelectedItems.Count > 0 ? lstFiles.SelectedItems[0] : null;
            var afi = selectedItem?.Tag as ArchiveFileInfo;

            bool nodeSelected = treDirectories.SelectedNode != null;

            _canExtractDirectories = nodeSelected;
            _canReplaceDirectories = nodeSelected;

            bool itemSelected = lstFiles.SelectedItems.Count > 0;

            _canAddFiles = _archiveAdapter is IArchiveAddFile;
            _canExtractFiles = itemSelected && (bool)afi?.FileSize.HasValue;
            _canReplaceFiles = itemSelected && _archiveAdapter.CanReplaceFiles;
            _canRenameFiles = itemSelected && _archiveAdapter.CanRenameFiles;
            _canDeleteFiles = itemSelected && _archiveAdapter is IArchiveDeleteFile;

            //splMain.Enabled = _fileOpen;

            // Menu
            //saveToolStripMenuItem.Enabled = _fileOpen && (bool)_archiveManager?.CanSave;
            tsbSave.Enabled = _archiveAdapter is ISaveFiles;
            //saveAsToolStripMenuItem.Enabled = _fileOpen && (bool)_archiveManager?.CanSave;
            tsbSaveAs.Enabled = _archiveAdapter is ISaveFiles && _parentAdapter == null;
            //closeToolStripMenuItem.Enabled = _fileOpen;
            //findToolStripMenuItem.Enabled = _fileOpen;
            //tsbFind.Enabled = _fileOpen;
            //propertiesToolStripMenuItem.Enabled = _fileOpen && _archiveManager.FileHasExtendedProperties;
            tsbProperties.Enabled = _archiveAdapter.FileHasExtendedProperties;

            // Toolbar
            tsbFileExtract.Enabled = _canExtractFiles;
            tsbFileReplace.Enabled = _canReplaceFiles;
            tsbFileRename.Enabled = _canRenameFiles;
            tsbFileDelete.Enabled = _canDeleteFiles;
            //addFileToolStripMenuItem.Enabled = canAdd && treEntries.Focused;
            //renameFileToolStripMenuItem.Enabled = canRename && treEntries.Focused;
            //deleteFileToolStripMenuItem.Enabled = canDelete && treEntries.Focused;
            //filePropertiesToolStripMenuItem.Enabled = itemSelected && _archiveManager.EntriesHaveExtendedProperties;
            //tsbFileProperties.Enabled = itemSelected && _archiveManager.EntriesHaveExtendedProperties;

            //treDirectories.Enabled = _fileOpen;

            // Shortcuts
            //tsbKuriimu.Enabled = File.Exists(Path.Combine(Application.StartupPath, "kuriimu.exe"));
            //tsbKukkii.Enabled = File.Exists(Path.Combine(Application.StartupPath, "kukkii.exe"));
        }

        public void Save(string filename = "")
        {
            SaveTab?.Invoke(this, new SaveTabEventArgs(Kfi) { NewSaveLocation = filename });
        }

        public void Close()
        {
            CloseTab?.Invoke(this, new CloseTabEventArgs(Kfi, _parentTabPage) { LeaveOpen = Kfi.ParentKfi != null });
        }

        public void UpdateForm2()
        {
            LoadDirectories();
            LoadFiles();

            if (_parentTabPage != null)
                (_parentTabPage.Controls[0] as IKuriimuForm).UpdateForm2();
        }

        public void RemoveChildTab(ArchiveForm form)
        {
            var toRemove = _openedTabs.FirstOrDefault(x => (x.Controls[0] as ArchiveForm) == form);
            if (toRemove != null) _openedTabs.Remove(toRemove);
        }

        private void Stub()
        {
            MessageBox.Show("This method is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static IEnumerable<ArchiveFileInfo> CollectFiles(TreeNode node)
        {
            if (node.Tag is IEnumerable<ArchiveFileInfo> files)
                foreach (var file in files)
                    yield return file;

            foreach (TreeNode childNode in node.Nodes)
                foreach (var file in CollectFiles(childNode))
                    yield return file;
        }

        #region Directory ContextToolStrip
        private void ExtractFiles(List<ArchiveFileInfo> files, string selectedNode = "", string selectedPath = "")
        {
            var selectedPathRegex = "^" + selectedPath.Replace("\\", @"[\\/]") + @"[\\/]?";

            if (files?.Count > 1)
            {
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

        private void ReplaceFiles(List<ArchiveFileInfo> files, string selectedNode = "", string selectedPath = "")
        {
            var selectedPathRegex = "^" + selectedPath.Replace("\\", @"[\\/]") + @"[\\/]?";

            if (files?.Count > 1)
            {
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
                var afi = files.First();
                var filename = Path.GetFileName(afi.FileName);

                var ofd = new OpenFileDialog();
                ofd.Title = $"Select a file to replace {filename} with...";
                ofd.InitialDirectory = Settings.Default.LastDirectory;

                // TODO: Implement file type filtering if replacement filetype matters
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
            (this as Control).Text = Kfi.DisplayName;

            UpdateForm();
            LoadFiles();
        }

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

        private void DeleteFiles(IEnumerable<ArchiveFileInfo> toDelete)
        {
            if (toDelete?.Count() > 0)
                foreach (var d in toDelete)
                    (_archiveAdapter as IArchiveDeleteFile).DeleteFile(d);
        }
        #endregion

        #region File ContextToolStrip

        #endregion
    }
}
