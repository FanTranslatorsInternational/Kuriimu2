using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Extensions;
using Kore.Factories;
using Kore.Managers;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.WinForms.ExtensionForms;
using Kuriimu2.WinForms.MainForms.FormatForms;
using Kuriimu2.WinForms.MainForms.Interfaces;
using Kuriimu2.WinForms.Progress;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class Kuriimu2Form : Form
    {
        private HashTypeExtensionForm _hashForm;
        private CipherTypeExtensionForm _encryptForm;
        private CipherTypeExtensionForm _decryptForm;
        private DecompressTypeExtensionForm _decompressForm;
        private CompressTypeExtensionForm _compressForm;
        private RawImageViewer _rawImageViewer;

        private IDictionary<IStateInfo, TabPage> _stateTabDictionary;
        private IDictionary<TabPage, IStateInfo> _tabStateDictionary;
        private IDictionary<IKuriimuForm, Color> _formColorDictionary;
        private IDictionary<IKuriimuForm, TabPage> _formTabDictionary;
        private IDictionary<TabPage, Color> _tabColorDictionary;

        private IInternalPluginManager _pluginManager;
        private Random _rand = new Random();

        private Timer _timer;
        private Stopwatch _globalOperationWatch;

        private IProgressContext _progressContext;

        public Kuriimu2Form()
        {
            InitializeComponent();

            _progressContext = new ConcurrentProgress(new ToolStripProgressBarOutput(operationProgress, 50));

            _hashForm = new HashTypeExtensionForm();
            _encryptForm = new EncryptTypeExtensionForm();
            _decryptForm = new DecryptTypeExtensionForm();
            _decompressForm = new DecompressTypeExtensionForm();
            _compressForm = new CompressTypeExtensionForm();
            _rawImageViewer=new RawImageViewer();

            _stateTabDictionary = new Dictionary<IStateInfo, TabPage>();
            _tabStateDictionary = new Dictionary<TabPage, IStateInfo>();
            _formColorDictionary = new Dictionary<IKuriimuForm, Color>();
            _formTabDictionary = new Dictionary<IKuriimuForm, TabPage>();
            _tabColorDictionary = new Dictionary<TabPage, Color>();

            _pluginManager = new PluginManager(_progressContext, "plugins");
            if (_pluginManager.LoadErrors.Any())
                DisplayPluginErrors(_pluginManager.LoadErrors);

            _timer = new Timer { Interval = 14 };
            _timer.Tick += _timer_Tick;
            _globalOperationWatch = new Stopwatch();

            Icon = Resources.kuriimu2winforms;

            // TODO: Enable batch processing again
            batchProcessorToolStripMenuItem.Enabled = false;
            //batchProcessorToolStripMenuItem.Enabled = _pluginManager.GetAdapters<ICipherAdapter>().Any() ||
            //                                          _pluginManager.GetAdapters<ICompressionAdapter>().Any() ||
            //                                          _pluginManager.GetAdapters<IHashAdapter>().Any();

            tabCloseButtons.Images.Add(Resources.menu_delete);
            tabCloseButtons.Images.SetKeyName(0, "close-button");
        }

        private void DisplayPluginErrors(IReadOnlyList<PluginLoadError> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Following plugins could not be loaded:");
            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            MessageBox.Show(sb.ToString(), "Plugins not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            operationTimer.Text = _globalOperationWatch.Elapsed.ToString();
        }

        //private void LoadImageViews()
        //{
        //    LoadRawImageViewer();
        //    LoadImageTranscoder();
        //}

        //private void LoadCiphers()
        //{
        //    var ciphers = _pluginManager.GetAdapters<ICipherAdapter>();
        //    var cipherMenuBuilder = new CipherToolStripMenuBuilder(ciphers, AddCipherDelegates);

        //    cipherMenuBuilder.AddTreeToMenuStrip(ciphersToolStripMenuItem);
        //    ciphersToolStripMenuItem.Enabled = ciphersToolStripMenuItem.DropDownItems.Count > 0;
        //}

        //private void LoadCompressions()
        //{
        //    var compressions = _pluginManager.GetAdapters<ICompressionAdapter>();
        //    var compMenuBuilder = new CompressionToolStripMenuBuilder(compressions, AddCompressionDelegates);

        //    compMenuBuilder.AddTreeToMenuStrip(compressionsToolStripMenuItem);
        //    compressionsToolStripMenuItem.Enabled = compressionsToolStripMenuItem.DropDownItems.Count > 0;
        //}

        //private void LoadRawImageViewer()
        //{
        //    rawImageViewerToolStripMenuItem.Enabled = _pluginManager.GetAdapters<IColorEncodingAdapter>().Any();
        //}

        //private void LoadImageTranscoder()
        //{
        //    imageTranscoderToolStripMenuItem.Enabled = _pluginManager.GetAdapters<IColorEncodingAdapter>().Any();
        //}

        private void _imgTransToolStrip_Click(object sender, EventArgs e)
        {
            //new ImageTranscoder(_pluginManager).ShowDialog();
        }

        #region Events

        private void Kuriimu2_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (openFiles.TabPages.Count > 0)
            {
                var tabPage = openFiles.TabPages[0];
                if (!CloseFile(_tabStateDictionary[tabPage], true).Result)
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        //#region Ciphers
        //private void Cipher_RequestData(object sender, RequestDataEventArgs e)
        //{
        //    _globalOperationWatch.Stop();

        //    var input = new InputBox("Requesting data", e.RequestMessage);
        //    var ofd = new OpenFileDialog() { Title = e.RequestMessage };

        //    while (true)
        //    {
        //        if (e.IsRequestFile)
        //        {
        //            if (ofd.ShowDialog() == DialogResult.OK && ofd.CheckFileExists)
        //            {
        //                e.Data = ofd.FileName;
        //                _globalOperationWatch.Start();
        //                return;
        //            }

        //            MessageBox.Show("No valid file selected. Please choose a valid file.", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //        else
        //        {
        //            if (input.ShowDialog() == DialogResult.OK && input.InputText.Length == e.DataSize)
        //            {
        //                e.Data = input.InputText;
        //                _globalOperationWatch.Start();
        //                return;
        //            }

        //            MessageBox.Show("No valid data input. Please input valid data.", "Invalid data", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //    }
        //}

        //private void EncItem_Click(object sender, EventArgs e)
        //{
        //    var cipher = (sender as ToolStripItem).Tag as ICipherAdapter;
        //    cipher.RequestData += Cipher_RequestData;
        //    DoCipher(cipher.Encrypt);
        //    cipher.RequestData -= Cipher_RequestData;
        //}

        //private void DecItem_Click(object sender, EventArgs e)
        //{
        //    var cipher = (sender as ToolStripItem).Tag as ICipherAdapter;
        //    cipher.RequestData += Cipher_RequestData;
        //    DoCipher(cipher.Decrypt);
        //    cipher.RequestData -= Cipher_RequestData;
        //}
        //#endregion

        //#region Compression

        //private void CompressItem_Click(object sender, EventArgs e)
        //{
        //    var compressor = (sender as ToolStripItem).Tag as ICompressionAdapter;
        //    DoCompression(compressor.Compress);
        //}

        //private void DecompressItem_Click(object sender, EventArgs e)
        //{
        //    var compressor = (sender as ToolStripItem).Tag as ICompressionAdapter;
        //    DoCompression(compressor.Decompress);
        //}

        //#endregion

        //#region Hash

        //private void Hash_Click(object sender, EventArgs e)
        //{
        //    var hash = (sender as ToolStripItem).Tag as IHashAdapter;
        //    DoHash(hash.Compute);
        //}

        //#endregion

        #region Tab Item

        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            if(!_tabColorDictionary.ContainsKey(openFiles.TabPages[e.Index]))
                return;

            var tabColor = _tabColorDictionary[openFiles.TabPages[e.Index]];
            var textColor = tabColor.GetBrightness() <= 0.49 ? Color.White : Color.Black;

            // Color the Tab Header
            e.Graphics.FillRectangle(new SolidBrush(tabColor), e.Bounds);

            // Format String
            var drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };

            // Draw Header Text
            e.Graphics.DrawString(openFiles.TabPages[e.Index].Text, e.Font, new SolidBrush(textColor), new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 2, e.Bounds.Height), drawFormat);

            //Draw image
            var drawPoint = (openFiles.SelectedIndex == e.Index) ? new Point(e.Bounds.Left + 9, e.Bounds.Top + 4) : new Point(e.Bounds.Left + 3, e.Bounds.Top + 2);
            e.Graphics.DrawImage(tabCloseButtons.Images["close-button"], drawPoint);
        }

        private async void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            Rectangle r = openFiles.GetTabRect(openFiles.SelectedIndex);
            Rectangle closeButton = new Rectangle(r.Left + 9, r.Top + 4, tabCloseButtons.Images["close-button"].Width, tabCloseButtons.Images["close-button"].Height);
            if (closeButton.Contains(e.Location))
            {
                var selectedTab = openFiles.SelectedTab;
                await CloseFile(_tabStateDictionary[selectedTab]);
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
            var ofd = new OpenFileDialog
            {
                Filter = CreateFileFilters(_pluginManager.GetFilePluginLoaders())
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName, false);
        }

        private void openWithPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = CreateFileFilters(_pluginManager.GetFilePluginLoaders())
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName, true);
        }

        #endregion

        #region Kuriimu Form

        private async Task<bool> Kuriimu2_OpenTab(OpenFileEventArgs e)
        {
            // Check if file is already opened
            var absoluteFilePath = e.StateInfo.AbsoluteDirectory / e.StateInfo.FilePath / e.Afi.FilePath.ToRelative();
            if (_pluginManager.IsLoaded(absoluteFilePath))
            {
                openFiles.SelectedTab = _stateTabDictionary[_pluginManager.GetLoadedFile(absoluteFilePath)];

                return true;
            }

            var loadResult = await (e.PluginId == Guid.Empty ?
                _pluginManager.LoadFile(e.StateInfo, e.Afi) :
                _pluginManager.LoadFile(e.StateInfo, e.Afi, e.PluginId));

            var tabColor = _tabColorDictionary[_stateTabDictionary[e.StateInfo]];

            // Not loaded states are opened by the HexForm
            if (!loadResult.IsSuccessful)
            {
#if DEBUG
                var message = loadResult.Exception?.ToString() ?? loadResult.Message;
#else
                var message = loadResult.Message;
#endif

                MessageBox.Show(message, "File not loaded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var newTabPage = AddTabPage(loadResult.LoadedState, tabColor);
            if (newTabPage == null)
                _pluginManager.Close(loadResult.LoadedState);

            return true;
        }

        private async Task<bool> TabControl_SaveTab(SaveTabEventArgs e)
        {
            // TODO: Add version of file
            return await SaveFile(e.StateInfo, e.SavePath, 0);
        }

        private void TabControl_UpdateTab(IStateInfo stateInfo)
        {
            var tabPage = _stateTabDictionary[stateInfo];

            tabPage.Text = (stateInfo.StateChanged ? "* " : "") + stateInfo.FilePath.GetName();

            UpdateParentTabs(stateInfo);
        }

        #endregion

        #endregion

        #region Utilities

        //#region Ciphers

        //private async void DoCipher(Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>> cipherFunc)
        //{
        //    // File to open
        //    var openFile = new OpenFileDialog();
        //    if (openFile.ShowDialog() != DialogResult.OK)
        //    {
        //        //MessageBox.Show("An error occured while selecting a file to open.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    // File to save
        //    var saveFile = new SaveFileDialog();
        //    if (saveFile.ShowDialog() != DialogResult.OK)
        //    {
        //        //MessageBox.Show("An error occured while selecting a file to save to.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    ciphersToolStripMenuItem.Enabled = false;
        //    var report = new Progress<ProgressReport>();
        //    report.ProgressChanged += Report_ProgressChanged;

        //    var openFileStream = openFile.OpenFile();
        //    var saveFileStream = saveFile.OpenFile();

        //    _timer.Start();
        //    _globalOperationWatch.Reset();
        //    _globalOperationWatch.Start();
        //    await cipherFunc(openFileStream, saveFileStream, report);
        //    _globalOperationWatch.Stop();
        //    _timer.Stop();

        //    openFileStream.Close();
        //    saveFileStream.Close();

        //    ciphersToolStripMenuItem.Enabled = true;
        //}

        //private void AddCipherDelegates(ToolStripMenuItem item, ICipherAdapter cipher, bool ignoreDecrypt, bool ignoreEncrypt)
        //{
        //    if (!ignoreDecrypt)
        //    {
        //        var decItem = new ToolStripMenuItem("Decrypt");
        //        decItem.Click += DecItem_Click;
        //        decItem.Tag = cipher;
        //        item?.DropDownItems.Add(decItem);
        //    }

        //    if (!ignoreEncrypt)
        //    {
        //        var encItem = new ToolStripMenuItem("Encrypt");
        //        encItem.Click += EncItem_Click;
        //        encItem.Tag = cipher;
        //        item?.DropDownItems.Add(encItem);
        //    }
        //}

        //#endregion

        //#region Compression

        //private async void DoCompression(Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>> compFunc)
        //{
        //    // File to open
        //    var openFile = new OpenFileDialog();
        //    if (openFile.ShowDialog() != DialogResult.OK)
        //    {
        //        //MessageBox.Show("An error occured while selecting a file to open.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    // File to save
        //    var saveFile = new SaveFileDialog();
        //    if (saveFile.ShowDialog() != DialogResult.OK)
        //    {
        //        //MessageBox.Show("An error occured while selecting a file to save to.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    compressionsToolStripMenuItem.Enabled = false;
        //    var report = new Progress<ProgressReport>();
        //    report.ProgressChanged += Report_ProgressChanged;

        //    var openFileStream = openFile.OpenFile();
        //    var saveFileStream = saveFile.OpenFile();

        //    _timer.Start();
        //    _globalOperationWatch.Reset();
        //    _globalOperationWatch.Start();
        //    await compFunc(openFileStream, saveFileStream, report);
        //    _globalOperationWatch.Stop();
        //    _timer.Stop();

        //    openFileStream.Close();
        //    saveFileStream.Close();

        //    compressionsToolStripMenuItem.Enabled = true;
        //}

        //private void AddCompressionDelegates(ToolStripMenuItem item, ICompressionAdapter compressor, bool ignoreDecompression, bool ignoreCompression)
        //{
        //    if (!ignoreDecompression)
        //    {
        //        var decItem = new ToolStripMenuItem("Decompress");
        //        decItem.Click += DecompressItem_Click;
        //        decItem.Tag = compressor;
        //        item?.DropDownItems.Add(decItem);
        //    }

        //    if (!ignoreCompression)
        //    {
        //        var compItem = new ToolStripMenuItem("Compress");
        //        compItem.Click += CompressItem_Click;
        //        compItem.Tag = compressor;
        //        item?.DropDownItems.Add(compItem);
        //    }
        //}

        //#endregion

        //#region Hash

        //private async void DoHash(Func<Stream, IProgress<ProgressReport>, Task<HashResult>> hashFunc)
        //{
        //    // File to open
        //    var openFile = new OpenFileDialog();
        //    if (openFile.ShowDialog() != DialogResult.OK)
        //    {
        //        //MessageBox.Show("An error occured while selecting a file to open.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    hashesToolStripMenuItem.Enabled = false;
        //    var report = new Progress<ProgressReport>();
        //    report.ProgressChanged += Report_ProgressChanged;

        //    var openFileStream = openFile.OpenFile();

        //    _timer.Start();
        //    _globalOperationWatch.Reset();
        //    _globalOperationWatch.Start();
        //    var hashResult = await hashFunc(openFileStream, report);
        //    _globalOperationWatch.Stop();
        //    _timer.Stop();

        //    openFileStream.Close();

        //    if (hashResult.IsSuccessful)
        //    {
        //        return;
        //    }

        //    MessageBox.Show(
        //        $"The hash of {openFile.FileName} is:{Environment.NewLine}{hashResult.Result.Aggregate("", (a, b) => a + b.ToString("X2"))}");

        //    hashesToolStripMenuItem.Enabled = true;
        //}

        //private void AddHashDelegates(ToolStripMenuItem item, IHashAdapter hash)
        //{
        //    item.Click += Hash_Click;
        //    item.Tag = hash;
        //}

        //#endregion

        #region Open File

        /// <summary>
        /// Opens a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="manualIdentification"></param>
        private async void OpenFile(UPath filePath, bool manualIdentification)
        {
            // Check if file is already opened
            if (_pluginManager.IsLoaded(filePath))
            {
                if (openFiles.InvokeRequired)
                    openFiles.Invoke(new Action(() =>
                        openFiles.SelectedTab = _stateTabDictionary[_pluginManager.GetLoadedFile(filePath)]));
                else
                    openFiles.SelectedTab = _stateTabDictionary[_pluginManager.GetLoadedFile(filePath)];

                return;
            }

            LoadResult loadResult;
            if (!manualIdentification)
            {
                loadResult = await _pluginManager.LoadFile(filePath.FullName);
            }
            else
            {
                var pluginChooser = new ChoosePluginForm(_pluginManager.GetFilePlugins().ToArray());
                if (pluginChooser.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("No plugin was selected.");
                    return;
                }

                var pluginId = pluginChooser.SelectedPluginId;
                loadResult = await _pluginManager.LoadFile(filePath.FullName, pluginId);
            }

            if (!loadResult.IsSuccessful)
            {
#if DEBUG
                var message = loadResult.Exception?.ToString() ?? loadResult.Message;
#else
                var message = loadResult.Message;
#endif

                MessageBox.Show(message, "File not loaded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));
            var newTabPage = AddTabPage(loadResult.LoadedState, tabColor);
            if (newTabPage == null)
                _pluginManager.Close(loadResult.LoadedState);
        }

        private TabPage AddTabPage(IStateInfo stateInfo, Color tabColor)
        {
            IKuriimuForm kuriimuForm;
            try
            {
                switch (stateInfo.State)
                {
                    case ITextState textState:
                        kuriimuForm = new TextForm(stateInfo, _pluginManager.GetGameAdapters().ToArray(),
                            _progressContext);
                        break;

                    case IImageState imageState:
                        kuriimuForm = new ImageForm(stateInfo, _progressContext);
                        break;

                    case IArchiveState archiveState:
                        kuriimuForm = new ArchiveForm(stateInfo, _pluginManager, _progressContext);
                        ((IArchiveForm)kuriimuForm).OpenFilesDelegate = Kuriimu2_OpenTab;
                        break;

                    case IHexState hexState:
                        kuriimuForm = new HexForm(stateInfo);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unknown plugin state type {stateInfo.State.GetType().Name}.");
                }

                //if (stateInfo.State is ITextAdapter)
                //    tabControl = new TextForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi), _pluginManager.GetAdapters<IGameAdapter>());
                //else if (kfi.Adapter is ILayoutAdapter)
                //    tabControl = new LayoutForm(kfi, tabPage, parentKfi?.Adapter as IArchiveAdapter, GetTabPageForKfi(parentKfi));
                /*else*/
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(e.ToString(), "Exception catched.");
#else
                MessageBox.Show(e.Message, "Exception catched.");
#endif
                return null;
            }

            var userControl = (UserControl)kuriimuForm;
            userControl.Dock = DockStyle.Fill;

            kuriimuForm.SaveFilesDelegate = TabControl_SaveTab;
            kuriimuForm.UpdateTabDelegate = TabControl_UpdateTab;

            var tabPage = new TabPage
            {
                BackColor = SystemColors.Window,
                Padding = new Padding(0, 2, 2, 1),
                Name = stateInfo.FilePath.GetName()
            };

            tabPage.Controls.Add(kuriimuForm as UserControl);

            _stateTabDictionary[stateInfo] = tabPage;
            _tabStateDictionary[tabPage] = stateInfo;
            _formColorDictionary[tabPage.Controls[0] as IKuriimuForm] = tabColor;
            _tabColorDictionary[tabPage] = tabColor;
            _formTabDictionary[tabPage.Controls[0] as IKuriimuForm] = tabPage;

            openFiles.TabPages.Add(tabPage);
            tabPage.ImageKey = "close-button";  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;

            TabControl_UpdateTab(stateInfo);

            return tabPage;
        }

        #endregion

        #region Save File

        /// <summary>
        /// Saves a state.
        /// </summary>
        /// <param name="stateInfo"></param>
        /// <param name="savePath"></param>
        /// <param name="version"></param>
        private async Task<bool> SaveFile(IStateInfo stateInfo, UPath savePath, int version = 0)
        {
            var result = await _pluginManager.SaveFile(stateInfo, savePath);
            if (!result.IsSuccessful)
            {
#if DEBUG
                var message = result.Exception?.ToString() ?? result.Message;
#else
                var message = result.Message;
#endif
                MessageBox.Show(message, "File not saved.", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            // Update children
            UpdateChildrenTabs(stateInfo);

            // Update parents
            UpdateParentTabs(stateInfo);

            return true;
        }

        #endregion

        #region Close File

        /// <summary>
        /// Close a Kfi and its corresponding tabs
        /// </summary>
        /// <param name="stateInfo">The loaded state of a file.</param>
        /// <param name="ignoreChildWarning">Ignore showing child close warning.</param>
        /// <returns>If the closing was successful.</returns>
        private async Task<bool> CloseFile(IStateInfo stateInfo, bool ignoreChildWarning = false)
        {
            // Security question, so the user knows that every sub file will be closed
            if (stateInfo.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = MessageBox.Show("Every file opened from this one and below will be closed too. Continue?",
                    "Dependant files", MessageBoxButtons.YesNo);

                switch (result)
                {
                    case DialogResult.Yes:
                        break;

                    default:
                        return false;
                }
            }

            // Save unchanged files, if wanted
            if (stateInfo.StateChanged)
            {
                var result = MessageBox.Show($"Changes were made to \"{stateInfo.FilePath}\" or its opened sub files. Do you want to save those changes?",
                    "Unsaved changes", MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        var saveResult = await _pluginManager.SaveFile(stateInfo);
                        if (saveResult.IsSuccessful)
                            (stateInfo.State as ISaveFiles).ContentChanged = false;

                        // TODO: Somehow propagate save error to user?

                        break;

                    case DialogResult.No:
                        (stateInfo.State as ISaveFiles).ContentChanged = false;
                        break;

                    default:
                        return false;
                }
            }

            // Remove all tabs related to states
            CloseOpenTabs(stateInfo);

            // Update parents before state is disposed
            UpdateParentTabs(stateInfo);

            // Close all related states
            _pluginManager.Close(stateInfo);

            return true;
        }

        private void CloseOpenTabs(IStateInfo stateInfo)
        {
            foreach (var child in stateInfo.ArchiveChildren)
                CloseOpenTabs(child);

            if (_stateTabDictionary.ContainsKey(stateInfo))
            {
                var tabPage = _stateTabDictionary[stateInfo];
                _tabStateDictionary.Remove(tabPage);
                _tabColorDictionary.Remove(tabPage);
                _formColorDictionary.Remove(tabPage.Controls[0] as IKuriimuForm);
                _formTabDictionary.Remove(tabPage.Controls[0] as IKuriimuForm);

                _stateTabDictionary[stateInfo].Dispose();
                _stateTabDictionary.Remove(stateInfo);
            }
        }

        #endregion

        #endregion

        private void UpdateParentTabs(IStateInfo stateInfo)
        {
            if (stateInfo.ParentStateInfo != null)
            {
                var parentForm = _stateTabDictionary[stateInfo.ParentStateInfo].Controls[0] as IKuriimuForm;
                parentForm?.UpdateForm();

                UpdateParentTabs(stateInfo.ParentStateInfo);
            }
        }

        private void UpdateChildrenTabs(IStateInfo stateInfo)
        {
            if (stateInfo.ArchiveChildren.Any())
            {
                foreach (var child in stateInfo.ArchiveChildren)
                    UpdateChildrenTabs(child);
            }

            var form = _stateTabDictionary[stateInfo].Controls[0] as IKuriimuForm;
            form?.UpdateForm();
        }

        private string CreateFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<string> { "All files|*.*" };

            foreach (var plugin in pluginLoaders.SelectMany(x => x.Plugins))
            {
                filters.Add($"{plugin.Metadata?.Name ?? plugin.GetType().Name}|{string.Join(";", plugin.FileExtensions)}");
            }

            return string.Join("|", filters);
        }

        private void TextSequenceSearcherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SequenceSearcher().ShowDialog();
        }

        private void BatchProcessorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //new Batch(_pluginManager).ShowDialog();
        }

        private void hashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _hashForm.ShowDialog();
        }

        private void decryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _decryptForm.ShowDialog();
        }

        private void encryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _encryptForm.ShowDialog();
        }

        private void decompressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _decompressForm.ShowDialog();
        }

        private void compressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _compressForm.ShowDialog();
        }

        private void rawImageViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _rawImageViewer.ShowDialog();
        }
    }
}
