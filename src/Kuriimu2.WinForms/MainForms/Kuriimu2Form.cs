using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Extensions;
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
        private readonly HashTypeExtensionForm _hashForm;
        private readonly CipherTypeExtensionForm _encryptForm;
        private readonly CipherTypeExtensionForm _decryptForm;
        private readonly DecompressTypeExtensionForm _decompressForm;
        private readonly CompressTypeExtensionForm _compressForm;
        private readonly RawImageViewer _rawImageViewer;

        private readonly IDictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)> _stateDictionary;
        private readonly IDictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)> _tabDictionary;

        private readonly IInternalPluginManager _pluginManager;
        private readonly IProgressContext _progressContext;
        private readonly IDialogManager _dialogManager;

        private readonly Random _rand = new Random();

        private Timer _timer;
        private Stopwatch _globalOperationWatch;

        public Kuriimu2Form()
        {
            InitializeComponent();

            _progressContext = new ConcurrentProgress(new NullProgressOutput());
            _dialogManager = new DialogManagerForm();

            _hashForm = new HashTypeExtensionForm();
            _encryptForm = new EncryptTypeExtensionForm();
            _decryptForm = new DecryptTypeExtensionForm();
            _decompressForm = new DecompressTypeExtensionForm();
            _compressForm = new CompressTypeExtensionForm();
            _rawImageViewer = new RawImageViewer();

            _stateDictionary = new Dictionary<IStateInfo, (IKuriimuForm, TabPage, Color)>();
            _tabDictionary = new Dictionary<TabPage, (IKuriimuForm, IStateInfo, Color)>();

            _pluginManager = new PluginManager(_progressContext, _dialogManager, "plugins")
            {
                AllowManualSelection = Settings.Default.AllowManualSelection
            };
            _pluginManager.OnManualSelection += PluginManager_OnManualSelection;

            if (_pluginManager.LoadErrors.Any())
                DisplayPluginErrors(_pluginManager.LoadErrors);

            _timer = new Timer
            {
                Interval = 14
            };
            _timer.Tick += Timer_Tick;
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

        #region Events

        private void Timer_Tick(object sender, EventArgs e)
        {
            operationTimer.Text = _globalOperationWatch.Elapsed.ToString();
        }

        private void Kuriimu2_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (_stateDictionary.Keys.Count > 0)
            {
                var stateInfo = _stateDictionary.Keys.First();
                if (!CloseFile(stateInfo, true).Result)
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        private void PluginManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            // Display form for manual selection
            var chooseForm = new ChoosePluginForm(e.FilePlugins);
            var result = chooseForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                e.Result = chooseForm.SelectedFilePlugin;
            }
        }

        #region Tab Item

        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (!_tabDictionary.ContainsKey(openFiles.TabPages[e.Index]))
                return;

            var tabEntry = _tabDictionary[openFiles.TabPages[e.Index]];
            var textColor = tabEntry.TabColor.GetBrightness() <= 0.49 ? Color.White : Color.Black;

            // Color the Tab Header
            e.Graphics.FillRectangle(new SolidBrush(tabEntry.TabColor), e.Bounds);

            // Format String
            var drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };

            // Draw Header Text
            e.Graphics.DrawString(openFiles.TabPages[e.Index].Text, e.Font, new SolidBrush(textColor), new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 2, e.Bounds.Height), drawFormat);

            //Draw image
            var drawPoint = openFiles.SelectedIndex == e.Index ? new Point(e.Bounds.Left + 9, e.Bounds.Top + 4) : new Point(e.Bounds.Left + 3, e.Bounds.Top + 2);
            e.Graphics.DrawImage(tabCloseButtons.Images["close-button"], drawPoint);
        }

        private async void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            var r = openFiles.GetTabRect(openFiles.SelectedIndex);
            var closeButton = new Rectangle(r.Left + 9, r.Top + 4, tabCloseButtons.Images["close-button"].Width, tabCloseButtons.Images["close-button"].Height);
            if (closeButton.Contains(e.Location))
            {
                var tabEntry = _tabDictionary[openFiles.SelectedTab];

                // Select parent
                var parentStateInfo = tabEntry.StateInfo.ParentStateInfo;
                if (parentStateInfo != null && _stateDictionary.ContainsKey(parentStateInfo))
                    openFiles.SelectedTab = _stateDictionary[parentStateInfo].TabPage;

                await CloseFile(tabEntry.StateInfo);
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
                var stateEntry = _stateDictionary[_pluginManager.GetLoadedFile(absoluteFilePath)];
                openFiles.SelectedTab = stateEntry.TabPage;

                return true;
            }

            var loadResult = await (e.PluginId == Guid.Empty ?
                _pluginManager.LoadFile(e.StateInfo, e.Afi) :
                _pluginManager.LoadFile(e.StateInfo, e.Afi, e.PluginId));

            var tabColor = _stateDictionary[e.StateInfo].TabColor;

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
            var stateEntry = _stateDictionary[stateInfo];

            stateEntry.TabPage.Text = (stateInfo.StateChanged ? "* " : "") + stateInfo.FilePath.GetName();

            UpdateParentTabs(stateInfo);
        }

        #endregion

        #endregion

        #region Utilities

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
                        openFiles.SelectedTab = _stateDictionary[_pluginManager.GetLoadedFile(filePath)].TabPage));
                else
                    openFiles.SelectedTab = _stateDictionary[_pluginManager.GetLoadedFile(filePath)].TabPage;

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

                var selectedPlugin = pluginChooser.SelectedFilePlugin;
                loadResult = await _pluginManager.LoadFile(filePath.FullName, selectedPlugin.PluginId);
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

            _stateDictionary[stateInfo] = (tabPage.Controls[0] as IKuriimuForm, tabPage, tabColor);
            _tabDictionary[tabPage] = (tabPage.Controls[0] as IKuriimuForm, stateInfo, tabColor);

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

                        // TODO: Somehow propagate save error to user?

                        break;

                    case DialogResult.No:
                        // Close state and tabs without doing anything
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

            if (!_stateDictionary.ContainsKey(stateInfo))
                return;

            var stateEntry = _stateDictionary[stateInfo];
            _tabDictionary.Remove(stateEntry.TabPage);
            //_tabStateDictionary.Remove(tabPage);
            //_tabColorDictionary.Remove(tabPage);
            //_formColorDictionary.Remove(tabPage.Controls[0] as IKuriimuForm);
            //_formTabDictionary.Remove(tabPage.Controls[0] as IKuriimuForm);

            stateEntry.TabPage.Dispose();
            _stateDictionary.Remove(stateInfo);
        }

        #endregion

        #endregion

        private void UpdateParentTabs(IStateInfo stateInfo)
        {
            if (stateInfo.ParentStateInfo == null)
                return;

            var parentForm = _stateDictionary[stateInfo.ParentStateInfo].KuriimuForm;
            parentForm?.UpdateForm();

            UpdateParentTabs(stateInfo.ParentStateInfo);
        }

        private void UpdateChildrenTabs(IStateInfo stateInfo)
        {
            if (stateInfo.ArchiveChildren.Any())
            {
                foreach (var child in stateInfo.ArchiveChildren)
                    UpdateChildrenTabs(child);
            }

            _stateDictionary[stateInfo].KuriimuForm?.UpdateForm();
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
