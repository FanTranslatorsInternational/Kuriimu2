using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
using Kontract.Attributes;
using Kontract.FileSystem;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Intermediate;
using Kontract.Interfaces.Text;
using Kore;
using Kuriimu2_WinForms.Extensions;
using Kuriimu2_WinForms.FormatForms;
using Kuriimu2_WinForms.Interfaces;
using Kuriimu2_WinForms.Properties;

namespace Kuriimu2_WinForms
{
    public partial class Kuriimu2 : Form
    {
        private KoreManager _kore;
        private Random _rand = new Random();
        private string _tempFolder = "temp";

        private ToolStripMenuItem _cipherToolStrip;

        public Kuriimu2()
        {
            InitializeComponent();

            Icon = Resources.kuriimu2winforms;

            _kore = new KoreManager();

            tabCloseButtons.Images.Add(Resources.menu_delete);
            tabCloseButtons.Images.SetKeyName(0, "close-button");

            LoadExtensions();
        }

        private void LoadExtensions()
        {
            LoadCiphers();
        }

        private void LoadCiphers()
        {
            var ciphers = _kore.PluginLoader.GetAdapters<ICipherAdapter>();
            var cipherMenuBuilder = new ToolStripMenuBuilder<ICipherAdapter>(ciphers, AddCipherDelegates);

            _cipherToolStrip = new ToolStripMenuItem("Ciphers");
            cipherMenuBuilder.AddTreeToMenuStrip(_cipherToolStrip);
            mainMenuStrip.Items.Add(_cipherToolStrip);
        }

        #region Events

        private void Kuriimu2_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (openFiles.TabPages.Count > 0)
            {
                var firstForm = openFiles.TabPages[0].Controls[0] as IKuriimuForm;
                if (!CloseFile(firstForm.Kfi, false, true))
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        #region Ciphers
        private void Cipher_RequestData(object sender, RequestDataEventArgs e)
        {
            var input = new InputBox("Requesting data", e.RequestMessage);
            var ofd = new OpenFileDialog() { Title = e.RequestMessage };

            while (true)
            {
                if (e.IsRequestFile)
                {
                    if (ofd.ShowDialog() == DialogResult.OK && ofd.CheckFileExists)
                    {
                        e.Data = ofd.FileName;
                        return;
                    }

                    MessageBox.Show("No valid file selected. Please choose a valid file.", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    if (input.ShowDialog() == DialogResult.OK && input.Text.Length == e.DataSize)
                    {
                        e.Data = input.Text;
                        return;
                    }

                    MessageBox.Show("No valid data input. Please input valid data.", "Invalid data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EncItem_Click(object sender, EventArgs e)
        {
            var cipher = (sender as ToolStripItem).Tag as ICipherAdapter;
            DoCipher(new Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>>(cipher.Encrypt));
        }

        private void DecItem_Click(object sender, EventArgs e)
        {
            var cipher = (sender as ToolStripItem).Tag as ICipherAdapter;
            DoCipher(new Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>>(cipher.Decrypt));
        }
        #endregion

        #region Tab Item
        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabColor = (openFiles.TabPages[e.Index].Controls[0] as IKuriimuForm).TabColor;
            var textColor = (tabColor.GetBrightness() <= 0.49) ? Color.White : Color.Black;

            // Color the Tab Header
            e.Graphics.FillRectangle(new SolidBrush(tabColor), e.Bounds);

            // Format String
            var drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Far;
            drawFormat.LineAlignment = StringAlignment.Center;

            // Draw Header Text
            e.Graphics.DrawString(openFiles.TabPages[e.Index].Text, e.Font, new SolidBrush(textColor), new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 2, e.Bounds.Height), drawFormat);

            //Draw image
            var drawPoint = (openFiles.SelectedIndex == e.Index) ? new Point(e.Bounds.Left + 9, e.Bounds.Top + 4) : new Point(e.Bounds.Left + 3, e.Bounds.Top + 2);
            e.Graphics.DrawImage(tabCloseButtons.Images["close-button"], drawPoint);
        }

        private void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            Rectangle r = openFiles.GetTabRect(openFiles.SelectedIndex);
            Rectangle closeButton = new Rectangle(r.Left + 9, r.Top + 4, tabCloseButtons.Images["close-button"].Width, tabCloseButtons.Images["close-button"].Height);
            if (closeButton.Contains(e.Location))
            {
                foreach (Control control in openFiles.SelectedTab.Controls)
                    if (control is IKuriimuForm kuriimuTab)
                        kuriimuTab.Close();
            }
        }
        #endregion

        #region DragDrop
        private void Kuriimu2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void Kuriimu2_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (var file in files)
                OpenFile(file);
        }
        #endregion

        #region mainMenuStrip
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName);
        }
        #endregion

        #region Kuriimu Form
        private void Kuriimu2_OpenTab(object sender, OpenTabEventArgs e)
        {
            var openedTabPage = GetTabPageForKfi(GetKfiForFullPath(Path.Combine(e.Kfi.FullPath, e.Afi.FileName)));
            if (openedTabPage == null)
            {
                var newKfi = _kore.LoadFile(new KoreLoadInfo(e.Afi.FileData, e.Afi.FileName) { LeaveOpen = e.LeaveOpen, FileSystem = e.FileSystem });
                if (newKfi == null)
                    return;

                newKfi.ParentKfi = e.Kfi;
                var newTabPage = AddTabPage(newKfi, (sender as IKuriimuForm).TabColor, e.Kfi);

                e.OpenedTabPage = newTabPage;
            }
            else
                openFiles.SelectedTab = openedTabPage;

            e.EventResult = true;
        }

        private void TabControl_SaveTab(object sender, SaveTabEventArgs e)
        {
            SaveFile(e.Kfi, e.NewSaveFile, e.Version);
        }

        private void TabControl_CloseTab(object sender, CloseTabEventArgs e)
        {
            e.EventResult = CloseFile(e.Kfi, e.LeaveOpen);
        }
        #endregion

        #endregion

        #region Utilities

        #region Ciphers
        private async void DoCipher(Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>> cipherFunc)
        {
            // File to open
            var openFile = new OpenFileDialog();
            if (openFile.ShowDialog() != DialogResult.OK)
            {
                //MessageBox.Show("An error occured while selecting a file to open.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // File to save
            var saveFile = new SaveFileDialog();
            if (saveFile.ShowDialog() != DialogResult.OK)
            {
                //MessageBox.Show("An error occured while selecting a file to save to.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _cipherToolStrip.Enabled = false;
            var report = new Progress<ProgressReport>();
            await cipherFunc(openFile.OpenFile(), saveFile.OpenFile(), report);

            _cipherToolStrip.Enabled = true;
        }

        private void AddCipherDelegates(ToolStripMenuItem item, ICipherAdapter cipher)
        {
            cipher.RequestData += Cipher_RequestData;

            var decItem = new ToolStripMenuItem("Decrypt");
            decItem.Click += DecItem_Click;
            decItem.Tag = cipher;

            var encItem = new ToolStripMenuItem("Encrypt");
            encItem.Click += EncItem_Click;
            encItem.Tag = cipher;

            item?.DropDownItems.Add(decItem);
            item?.DropDownItems.Add(encItem);
        }
        #endregion

        #region Open File
        /// <summary>
        /// Opens a file with KoreManager and opens a corresponding tab
        /// </summary>
        /// <param name="filename"></param>
        private void OpenFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            var openedTabPage = GetTabPageForKfi(GetKfiForFullPath(filename));
            if (openedTabPage != null)
                openFiles.SelectedTab = openedTabPage;
            else
            {
                FileStream openFile = null;
                try
                {
                    openFile = File.Open(filename, FileMode.Open);
                }
                catch (IOException ioe)
                {
                    MessageBox.Show($"File {filename} couldn't be opened. Is it open somehwere?", "File locked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var kfi = _kore.LoadFile(new KoreLoadInfo(openFile, filename) { FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(filename)) });
                if (kfi == null)
                {
                    MessageBox.Show($"No plugin supports \"{filename}\".");
                    openFile.Dispose();
                    return;
                }

                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));
                AddTabPage(kfi, tabColor);
            }
        }

        private TabPage AddTabPage(KoreFileInfo kfi, Color tabColor, KoreFileInfo parentKfi = null)
        {
            var tabPage = new TabPage();

            IKuriimuForm tabControl = null;
            if (kfi.Adapter is ITextAdapter)
                tabControl = new TextForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi));
            else if (kfi.Adapter is IImageAdapter)
                tabControl = new ImageForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi));
            else if (kfi.Adapter is IArchiveAdapter)
            {
                tabControl = new ArchiveForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi), _tempFolder);
                (tabControl as IArchiveForm).OpenTab += Kuriimu2_OpenTab;
            }
            tabControl.TabColor = tabColor;

            if (tabControl is UserControl uc)
                uc.Dock = DockStyle.Fill;

            tabControl.SaveTab += TabControl_SaveTab;
            tabControl.CloseTab += TabControl_CloseTab;

            tabPage.Controls.Add(tabControl as UserControl);

            openFiles.TabPages.Add(tabPage);
            tabPage.ImageKey = "close-button";  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;

            return tabPage;
        }
        #endregion

        #region Save File
        /// <summary>
        /// Saves a Kfi
        /// </summary>
        private void SaveFile(KoreFileInfo kfi, string newSaveLocation = "", int version = 0)
        {
            if (!kfi.HasChanges && newSaveLocation == string.Empty)
                return;

            // Save files
            var ksi = new KoreSaveInfo(kfi, _tempFolder) { Version = version, NewSaveFile = newSaveLocation };
            var savedKfi = _kore.SaveFile(ksi);

            if (savedKfi.ParentKfi != null)
                savedKfi.ParentKfi.HasChanges = true;

            // Update all corresponsing tabs
            var kuriimuForm = GetTabPageForKfi(kfi).Controls[0] as IKuriimuForm;

            kuriimuForm.Kfi = savedKfi;
            if (kuriimuForm is IArchiveForm archiveForm)
            {
                archiveForm.UpdateChildTabs(savedKfi);
                archiveForm.UpdateParent();
            }
            kuriimuForm.UpdateForm();
        }
        #endregion

        #region Close File
        /// <summary>
        /// Close a Kfi and its corresponding tabs
        /// </summary>
        /// <param name="kfi">The initial Kfi to close</param>
        /// <param name="ignoreChildWarning">Ignore showing child close warning</param>
        /// <returns>If the closing was successful</returns>
        private bool CloseFile(KoreFileInfo kfi, bool leaveOpen = false, bool ignoreChildWarning = false)
        {
            // Security question, so the user knows that every sub file will be closed
            if (kfi.ChildKfi != null && kfi.ChildKfi.Count > 0 && !ignoreChildWarning)
            {
                var result = MessageBox.Show("Every file opened from this one and below will be closed too. Continue?", "Dependant files", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        break;
                    case DialogResult.No:
                    default:
                        return false;
                }
            }

            // Save unchanged saves, if wanted
            if (kfi.HasChanges)
            {
                var result = MessageBox.Show($"Changes were made to \"{kfi.FullPath}\" or its opened sub files. Do you want to save those changes?", "Unsaved changes", MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        TabControl_SaveTab(this, new SaveTabEventArgs(kfi));
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                    default:
                        return false;
                }
            }

            // Remove all tabs related to KFIs
            CloseOpenTabs(kfi);

            // Update parent, if existent
            if (kfi.ParentKfi != null)
            {
                var parentTab = GetTabPageForKfi(kfi.ParentKfi);
                (parentTab.Controls[0] as IArchiveForm).RemoveChildTab(GetTabPageForKfi(kfi));
            }

            // Close all KFIs
            return _kore.CloseFile(kfi, leaveOpen);
        }

        private void CloseOpenTabs(KoreFileInfo kfi)
        {
            if (kfi.ChildKfi != null)
                foreach (var child in kfi.ChildKfi)
                    CloseOpenTabs(child);

            foreach (TabPage page in openFiles.TabPages)
                if (page.Controls[0] is IKuriimuForm kuriimuForm)
                    if (kuriimuForm.Kfi == kfi)
                    {
                        openFiles.TabPages.Remove(page);
                        break;
                    }
        }
        #endregion

        #region Getter
        private KoreFileInfo GetKfiForFullPath(string fullPath)
        {
            return _kore.GetOpenedFile(fullPath);
        }

        private TabPage GetTabPageForKfi(KoreFileInfo kfi)
        {
            if (kfi == null)
                return null;

            foreach (TabPage page in openFiles.TabPages)
                if (page.Controls[0] is IKuriimuForm kuriimuForm)
                    if (kuriimuForm.Kfi == kfi)
                        return page;

            return null;
        }
        #endregion

        #endregion
    }
}
