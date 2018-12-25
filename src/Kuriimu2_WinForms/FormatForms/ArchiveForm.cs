using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kuriimu2_WinForms.Interfaces;
using Kore;
using Kontract.Interfaces.Common;
using System.IO;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.VirtualFS;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.FileSystem;
using Kuriimu2_WinForms.Tools;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IKuriimuForm
    {
        public KoreFileInfo Kfi { get; set; }

        private Kore.Kore _kore;
        private TabControl _tabControl;
        private string _tempFolder;
        private string _subFolder;
        private IArchiveAdapter _archiveAdapter;

        public ArchiveForm(KoreFileInfo kfi, TabControl tabControl, string tempFolder, string subFolder)
        {
            InitializeComponent();

            // Overwrite window themes
            Win32.SetWindowTheme(treDirectories.Handle, "explorer", null);
            Win32.SetWindowTheme(lstFiles.Handle, "explorer", null);

            Kfi = kfi;

            _kore = new Kore.Kore();
            _tabControl = tabControl;
            _tempFolder = tempFolder;
            _subFolder = subFolder;
            _archiveAdapter = kfi.Adapter as IArchiveAdapter;

            LoadDirectories();
        }

        private void LoadDirectories()
        {
            treDirectories.BeginUpdate();
            treDirectories.Nodes.Clear();

            if (_archiveAdapter.Files != null)
            {
                var lookup = _archiveAdapter.Files.OrderBy(f => f.FileName.TrimStart('/', '\\')).ToLookup(f => Path.GetDirectoryName(f.FileName.TrimStart('/', '\\')));

                // Build directory tree
                var root = treDirectories.Nodes.Add("root", Kfi.FileInfo.Name, "tree-archive-file", "tree-archive-file");
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

                    //var shfi = new Win32.SHFILEINFO();
                    //try
                    //{
                    //    if (!imlFiles.Images.ContainsKey(ext) && !string.IsNullOrEmpty(ext))
                    //    {
                    //        Win32.SHGetFileInfo(ext, 0, out shfi, Marshal.SizeOf(shfi), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_USEFILEATTRIBUTES);
                    //        imlFiles.Images.Add(ext, Icon.FromHandle(shfi.hIcon));
                    //    }
                    //}
                    //finally
                    //{
                    //    if (shfi.hIcon != IntPtr.Zero)
                    //        Win32.DestroyIcon(shfi.hIcon);
                    //}
                    //try
                    //{
                    //    if (!imlFilesLarge.Images.ContainsKey(ext) && !string.IsNullOrEmpty(ext))
                    //    {
                    //        Win32.SHGetFileInfo(ext, 0, out shfi, Marshal.SizeOf(shfi), Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON | Win32.SHGFI_USEFILEATTRIBUTES);
                    //        imlFilesLarge.Images.Add(ext, Icon.FromHandle(shfi.hIcon));
                    //    }
                    //}
                    //finally
                    //{
                    //    if (shfi.hIcon != IntPtr.Zero)
                    //        Win32.DestroyIcon(shfi.hIcon);
                    //}

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

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    var files = (Kfi.Adapter as IArchiveAdapter).Files;

        //    MessageBox.Show(files.Aggregate("", (a, b) => a + Environment.NewLine + b.FileName), "Files", MessageBoxButtons.OK);
        //}

        //private void button2_Click(object sender, EventArgs e)
        //{
        //    var files = (Kfi.Adapter as IArchiveAdapter).Files;
        //    var vfs = new VirtualFileSystem(Kfi.Adapter as IArchiveAdapter, Path.Combine(_tempFolder, _subFolder));

        //    vfs.ExtractFile(files[3].FileName);

        //    var kfi = _kore.LoadFile(Path.Combine(_tempFolder, _subFolder, Path.GetFileName(files[3].FileName)), vfs);
        //    AddTabPage(kfi);
        //}

        private void AddTabPage(KoreFileInfo kfi)
        {
            var tabPage = new TabPage();

            if (kfi.Adapter is ITextAdapter)
                tabPage.Controls.Add(new TextForm(kfi));
            else if (kfi.Adapter is IImageAdapter)
                tabPage.Controls.Add(new ImageForm(kfi));
            else if (kfi.Adapter is IArchiveAdapter)
                tabPage.Controls.Add(new ArchiveForm(kfi, _tabControl, _tempFolder, Guid.NewGuid().ToString()));

            _tabControl.TabPages.Add(tabPage);
        }

        private void ArchiveForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }

        private void treDirectories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            LoadFiles();
        }
    }
}
