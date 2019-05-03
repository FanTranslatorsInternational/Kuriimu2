using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Intermediate;
using Kontract.Interfaces.Text;
using Kontract.Models;
using Kontract.Models.Intermediate;
using Kore;
using Kuriimu2_WinForms.Extensions;
using Kuriimu2_WinForms.FormatForms;
using Kuriimu2_WinForms.Interfaces;
using Kuriimu2_WinForms.Properties;
using ExecutionEngineException = System.ExecutionEngineException;

namespace Kuriimu2_WinForms
{
    public partial class Kuriimu2 : Form
    {
        private KoreManager _kore;
        private Random _rand = new Random();
        private string _tempFolder = "temp";

        private Timer _timer;
        private Stopwatch _globalOperationWatch;

        private ToolStripMenuItem _cipherToolStrip;
        private ToolStripMenuItem _hashToolStrip;
        private ToolStripMenuItem _compToolStrip;

        private ToolStripMenuItem _imgDecToolStrip;
        private ToolStripMenuItem _imgTransToolStrip;

        private RawImageViewer _rawImgViewer;

        public Kuriimu2()
        {
            InitializeComponent();

            try
            {
                _kore = new KoreManager();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Load += (s, evt) => Close();
                return;
            }

            _timer = new Timer { Interval = 14 };
            _timer.Tick += _timer_Tick;
            _globalOperationWatch = new Stopwatch();

            Icon = Resources.kuriimu2winforms;

            _rawImgViewer = new RawImageViewer(_kore.PluginLoader);

            tabCloseButtons.Images.Add(Resources.menu_delete);
            tabCloseButtons.Images.SetKeyName(0, "close-button");

            LoadExtensions();
            LoadImageViews();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            operationTimer.Text = _globalOperationWatch.Elapsed.ToString();
        }

        private void LoadExtensions()
        {
            LoadCiphers();
            //LoadHashes();
            //LoadCompressions();
        }

        private void LoadImageViews()
        {
            LoadRawImageViewer();
            LoadImageTranscoder();
        }

        private void LoadCiphers()
        {
            var ciphers = _kore.PluginLoader.GetAdapters<ICipherAdapter>();
            var cipherMenuBuilder = new ToolStripMenuBuilder<ICipherAdapter>(ciphers, AddCipherDelegates);

            _cipherToolStrip = new ToolStripMenuItem("Ciphers");
            cipherMenuBuilder.AddTreeToMenuStrip(_cipherToolStrip);
            mnuMain.Items.Add(_cipherToolStrip);
            _cipherToolStrip.Enabled = _cipherToolStrip.DropDownItems.Count > 0;
        }

        //private void LoadHashes()
        //{
        //    var hashes = _kore.PluginLoader.GetAdapters<IHashAdapter>();
        //    var hashMenuBuilder = new ToolStripMenuBuilder<IHashAdapter>(hashes, AddHashDelegates);

        //    _hashToolStrip = new ToolStripMenuItem("Hashes");
        //    hashMenuBuilder.AddTreeToMenuStrip(_hashToolStrip);
        //mnuMain.Items.Add(_hashToolStrip);
        //_hashToolStrip.Enabled = _hashToolStrip.DropDownItems.Count > 0;
        //}

        //private void LoadCompressions()
        //{
        //    var compressions = _kore.PluginLoader.GetAdapters<ICompressionAdapter>();
        //    var compMenuBuilder = new ToolStripMenuBuilder<ICompressionAdapter>(compressions, AddCompressionDelegates);

        //    _compToolStrip = new ToolStripMenuItem("Compressions");
        //    compMenuBuilder.AddTreeToMenuStrip(_compToolStrip);
        //mnuMain.Items.Add(_compToolStrip);
        //_compToolStrip.Enabled = _compToolStrip.DropDownItems.Count > 0;
        //}

        private void LoadRawImageViewer()
        {
            var imgAdapters = _kore.PluginLoader.GetAdapters<IColorEncodingAdapter>();

            _imgDecToolStrip = new ToolStripMenuItem("Raw Image Viewer");
            _imgDecToolStrip.Click += _imgDecToolStrip_Click;
            _imgDecToolStrip.Enabled = imgAdapters.Any();

            mnuMain.Items.Add(_imgDecToolStrip);
        }

        private void _imgDecToolStrip_Click(object sender, EventArgs e)
        {
            _rawImgViewer.ShowDialog();
        }

        private void LoadImageTranscoder()
        {
            var imgAdapters = _kore.PluginLoader.GetAdapters<IColorEncodingAdapter>();

            _imgTransToolStrip = new ToolStripMenuItem("Image Transcoder");
            _imgTransToolStrip.Enabled = imgAdapters.Any();

            mnuMain.Items.Add(_imgTransToolStrip);
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
            _globalOperationWatch.Stop();

            var input = new InputBox("Requesting data", e.RequestMessage);
            var ofd = new OpenFileDialog() { Title = e.RequestMessage };

            while (true)
            {
                if (e.IsRequestFile)
                {
                    if (ofd.ShowDialog() == DialogResult.OK && ofd.CheckFileExists)
                    {
                        e.Data = ofd.FileName;
                        _globalOperationWatch.Start();
                        return;
                    }

                    MessageBox.Show("No valid file selected. Please choose a valid file.", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    if (input.ShowDialog() == DialogResult.OK && input.Text.Length == e.DataSize)
                    {
                        e.Data = input.Text;
                        _globalOperationWatch.Start();
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
                OpenFile(file, false);
        }
        #endregion

        #region mainMenuStrip
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName, false);
        }

        private void openWithPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName, true);
        }
        #endregion

        #region Kuriimu Form
        private void Kuriimu2_OpenTab(object sender, OpenTabEventArgs e)
        {
            var openedTabPage = GetTabPageForKfi(GetKfiForFullPath(Path.Combine(e.Kfi.FullPath, e.Afi.FileName)));
            if (openedTabPage == null)
            {
                var newKfi = _kore.LoadFile(new KoreLoadInfo(e.Afi.FileData, e.Afi.FileName)
                {
                    Adapter = e.PreselectedAdapter,
                    LeaveOpen = e.LeaveOpen,
                    FileSystem = e.FileSystem
                });
                if (newKfi == null)
                    return;

                newKfi.ParentKfi = e.Kfi;
                var newTabPage = AddTabPage(newKfi, (sender as IKuriimuForm).TabColor, e.Kfi);
                if (newTabPage == null)
                {
                    newKfi.ParentKfi = null;
                    _kore.CloseFile(newKfi, e.LeaveOpen);
                    return;
                }

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

        private void Report_ProgressChanged(object sender, ProgressReport e)
        {
            operationProgress.Text = $"{(e.HasMessage ? $"{e.Message} - " : string.Empty)}{e.Percentage}%";
            operationProgress.Value = Convert.ToInt32(e.Percentage);
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
            report.ProgressChanged += Report_ProgressChanged;

            var openFileStream = openFile.OpenFile();
            var saveFileStream = saveFile.OpenFile();

            _timer.Start();
            _globalOperationWatch.Reset();
            _globalOperationWatch.Start();
            await cipherFunc(openFileStream, saveFileStream, report);
            _globalOperationWatch.Stop();
            _timer.Stop();

            openFileStream.Close();
            saveFileStream.Close();

            _cipherToolStrip.Enabled = true;
        }

        private void AddCipherDelegates(ToolStripMenuItem item, ICipherAdapter cipher, bool ignoreDecrypt, bool ignoreEncrypt)
        {
            cipher.RequestData += Cipher_RequestData;

            if (!ignoreDecrypt)
            {
                var decItem = new ToolStripMenuItem("Decrypt");
                decItem.Click += DecItem_Click;
                decItem.Tag = cipher;
                item?.DropDownItems.Add(decItem);
            }

            if (!ignoreEncrypt)
            {
                var encItem = new ToolStripMenuItem("Encrypt");
                encItem.Click += EncItem_Click;
                encItem.Tag = cipher;
                item?.DropDownItems.Add(encItem);
            }
        }
        #endregion

        #region Open File
        /// <summary>
        /// Opens a file with KoreManager and opens a corresponding tab
        /// </summary>
        /// <param name="filename"></param>
        private void OpenFile(string filename, bool shouldChoosePlugin)
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

                KoreFileInfo kfi = null;
                if (shouldChoosePlugin)
                {
                    var pluginChooser = new ChoosePluginForm(_kore.PluginLoader);
                    if (pluginChooser.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show($"No plugin was selected.");
                        openFile.Dispose();
                        return;
                    }

                    try
                    {
                        kfi = _kore.LoadFile(new KoreLoadInfo(openFile, filename)
                        {
                            FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(filename)),
                            Adapter = pluginChooser.ChosenAdapter
                        });
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "Exception catched.");
                        openFile.Dispose();
                        return;
                    }
                }
                else
                {
                    try
                    {
                        kfi = _kore.LoadFile(new KoreLoadInfo(openFile, filename)
                        {
                            FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(filename))
                        });
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "Exception catched.");
                        openFile.Dispose();
                        return;
                    }

                    if (kfi == null)
                    {
                        MessageBox.Show($"No plugin supports \"{filename}\".");
                        openFile.Dispose();
                        return;
                    }
                }

                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));
                var newTabPage = AddTabPage(kfi, tabColor);
                if (newTabPage == null)
                    _kore.CloseFile(kfi);
            }
        }

        private TabPage AddTabPage(KoreFileInfo kfi, Color tabColor, KoreFileInfo parentKfi = null)
        {
            var tabPage = new TabPage
            {
                BackColor = SystemColors.Window,
                Padding = new Padding(0, 2, 2, 1)
            };

            IKuriimuForm tabControl = null;
            try
            {
                if (kfi.Adapter is ITextAdapter)
                    tabControl = new TextForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi), _kore.GetAdapters<IGameAdapter>());
                else if (kfi.Adapter is IImageAdapter)
                    tabControl = new ImageForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi));
                else if (kfi.Adapter is IArchiveAdapter)
                {
                    tabControl = new ArchiveForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi), _tempFolder, _kore.PluginLoader);
                    (tabControl as IArchiveForm).OpenTab += Kuriimu2_OpenTab;
                    (tabControl as IArchiveForm).GetAdapterById += Kuriimu2_GetAdapterById;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Exception catched.");
                return null;
            }

            tabControl.TabColor = tabColor;

            if (tabControl is UserControl uc)
                uc.Dock = DockStyle.Fill;

            tabControl.SaveTab += TabControl_SaveTab;
            tabControl.CloseTab += TabControl_CloseTab;
            tabControl.ReportProgress += Report_ProgressChanged;

            tabPage.Controls.Add(tabControl as UserControl);

            openFiles.TabPages.Add(tabPage);
            tabPage.ImageKey = "close-button";  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;

            return tabPage;
        }

        private void Kuriimu2_GetAdapterById(object sender, GetAdapterInformationByIdEventArgs e)
        {
            e.SelectedPlugin = _kore.PluginLoader.CreateNewAdapter<ILoadFiles>(e.PluginName);
            e.PluginMetaData = _kore.GetMetadata<PluginInfoAttribute>(e.SelectedPlugin);
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
