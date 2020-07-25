using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Extensions;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.WinForms.Exceptions;
using Kuriimu2.WinForms.ExtensionForms;
using Kuriimu2.WinForms.MainForms.FormatForms;
using Kuriimu2.WinForms.MainForms.Interfaces;
using Kuriimu2.WinForms.MainForms.Models;
using Kuriimu2.WinForms.Progress;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class Kuriimu2Form : Form
    {
        private const string LoadError_ = "Load Error";
        private const string InvalidFile_ = "The selected file is invalid.";
        private const string NoPluginSelected_ = "No plugin was selected.";

        private const string SaveError_ = "Save Error";
        private const string ExceptionCatched_ = "Exception catched";
        private const string PluginsNotAvailable_ = "Plugins not available";

        private const string UnsavedChanges_ = "Unsaved changes";
        private const string UnsavedChangesText_ = "Changes were made to '{0}' or its opened sub files. Do you want to save those changes?";

        private const string DependantFiles_ = "Dependant files";
        private const string DependantFilesText_ = "Every file opened from this one and below will be closed too. Continue?";

        private const string FormTitle_ = "Kuriimu2";
        private const string FormTitlePlugin_ = "Kuriimu2 - {0} - {1}";

        private const string CloseButton_ = "close-button";

        private const Keys OpenHotKey_ = Keys.Control | Keys.O;
        private const Keys OpenWithHotKey_ = Keys.Control | Keys.Shift | Keys.O;
        private const Keys SaveHotKey_ = Keys.Control | Keys.S;
        private const Keys SaveAllHotKey_ = Keys.Control | Keys.Shift | Keys.S;
        private const Keys SaveAsHotKey_ = Keys.F12;

        private readonly HashTypeExtensionForm _hashForm;
        private readonly CipherTypeExtensionForm _encryptForm;
        private readonly CipherTypeExtensionForm _decryptForm;
        private readonly DecompressTypeExtensionForm _decompressForm;
        private readonly CompressTypeExtensionForm _compressForm;
        private readonly RawImageViewer _rawImageViewer;
        private readonly SequenceSearcher _sequenceSearcher;

        private readonly IDictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)> _stateDictionary;
        private readonly IDictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)> _tabDictionary;

        private readonly IInternalPluginManager _pluginManager;
        private readonly IProgressContext _progressContext;

        private readonly Random _rand = new Random();

        public Kuriimu2Form()
        {
            InitializeComponent();

            KeyPreview = true;

            _progressContext = new ConcurrentProgress(new ToolStripProgressBarOutput(progressBarToolStrip, 14));
            var dialogManager = new DialogManagerForm();

            _hashForm = new HashTypeExtensionForm();
            _encryptForm = new EncryptTypeExtensionForm();
            _decryptForm = new DecryptTypeExtensionForm();
            _decompressForm = new DecompressTypeExtensionForm();
            _compressForm = new CompressTypeExtensionForm();
            _rawImageViewer = new RawImageViewer();
            _sequenceSearcher = new SequenceSearcher();

            _stateDictionary = new Dictionary<IStateInfo, (IKuriimuForm, TabPage, Color)>();
            _tabDictionary = new Dictionary<TabPage, (IKuriimuForm, IStateInfo, Color)>();

            _pluginManager = new PluginManager(_progressContext, dialogManager, "plugins")
            {
                AllowManualSelection = Settings.Default.AllowManualSelection
            };
            _pluginManager.OnManualSelection += PluginManager_OnManualSelection;

            if (_pluginManager.LoadErrors.Any())
                DisplayPluginErrors(_pluginManager.LoadErrors);

            Icon = Resources.kuriimu2winforms;

            // TODO: Enable batch processing again
            batchProcessorToolStripMenuItem.Enabled = false;

            tabCloseButtons.Images.Add(Resources.menu_delete);
            tabCloseButtons.Images.SetKeyName(0, CloseButton_);
        }

        #region Open File

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenPhysicalFile(false);
        }

        private void openWithPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenPhysicalFile(true);
        }

        private void OpenPhysicalFile(bool manualIdentification)
        {
            var fileToOpen = SelectFile();
            OpenPhysicalFiles(new[] { fileToOpen }, manualIdentification);
        }

        private async void OpenPhysicalFiles(IList<string> filesToOpen, bool manualIdentification)
        {
            foreach (var fileToOpen in filesToOpen)
            {
                var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                    plugin == null ?
                        _pluginManager.LoadFile(fileToOpen) :
                        _pluginManager.LoadFile(fileToOpen, plugin.PluginId));
                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));

                await OpenFile(fileToOpen, manualIdentification, loadAction, tabColor);
            }
        }

        private Task<bool> Kuriimu2_OpenFile(IStateInfo stateInfo, ArchiveFileInfo file, Guid pluginId)
        {
            var absoluteFilePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / file.FilePath.ToRelative();
            var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                pluginId == Guid.Empty ?
                    _pluginManager.LoadFile(stateInfo, file) :
                    _pluginManager.LoadFile(stateInfo, file, pluginId));
            var tabColor = _stateDictionary[stateInfo].TabColor;

            return OpenFile(absoluteFilePath, false, loadAction, tabColor);
        }

        private async Task<bool> OpenFile(UPath filePath, bool manualIdentification, Func<IFilePlugin, Task<LoadResult>> loadFileFunc, Color tabColor)
        {
            // Check if path is invalid
            if (filePath.IsNull || filePath.IsEmpty)
            {
                MessageBox.Show(InvalidFile_, LoadError_, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            // Check if file is already opened
            if (_pluginManager.IsLoaded(filePath))
            {
                var selectedTabPage = _stateDictionary[_pluginManager.GetLoadedFile(filePath)].TabPage;
                if (openFiles.InvokeRequired)
                    openFiles.Invoke(new Action(() => openFiles.SelectedTab = selectedTabPage));
                else
                    openFiles.SelectedTab = selectedTabPage;

                return true;
            }

            // Choose plugin
            IFilePlugin chosenPlugin = null;
            if (manualIdentification)
            {
                chosenPlugin = ChoosePlugin(_pluginManager.GetFilePlugins().ToArray());
                if (chosenPlugin == null)
                {
                    MessageBox.Show(NoPluginSelected_, LoadError_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Load file
            var loadResult = await loadFileFunc(chosenPlugin);
            if (!loadResult.IsSuccessful)
            {
#if DEBUG
                var message = loadResult.Exception?.ToString() ?? loadResult.Message;
#else
                var message = loadResult.Message;
#endif

                MessageBox.Show(message, LoadError_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Open tab page
            var wasAdded = AddTabPage(loadResult.LoadedState, tabColor);
            if (!wasAdded)
            {
                _pluginManager.Close(loadResult.LoadedState);
                return false;
            }

            // Update title if only one file is open
            if (openFiles.TabCount == 1)
                UpdateFormText();

            return true;
        }

        private bool AddTabPage(IStateInfo stateInfo, Color tabColor)
        {
            var communicator = CreateFormCommunicator(stateInfo);

            IKuriimuForm kuriimuForm;
            try
            {
                switch (stateInfo.PluginState)
                {
                    case ITextState _:
                        kuriimuForm = new TextForm(stateInfo, communicator, _pluginManager.GetGameAdapters().ToArray(),
                            _progressContext);
                        break;

                    case IImageState _:
                        kuriimuForm = new ImageForm(stateInfo, communicator, _progressContext);
                        break;

                    case IArchiveState _:
                        kuriimuForm = new ArchiveForm(stateInfo, communicator, _pluginManager, _progressContext);
                        break;

                    case IHexState _:
                        kuriimuForm = new HexForm(stateInfo, communicator);
                        break;

                    default:
                        throw new UnknownPluginStateException(stateInfo.PluginState);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(e.ToString(), ExceptionCatched_);
#else
                MessageBox.Show(e.Message, "Exception catched.");
#endif
                return false;
            }

            var tabPage = new TabPage
            {
                BackColor = SystemColors.Window,
                Padding = new Padding(0, 2, 2, 1),
                Name = stateInfo.FilePath.ToRelative().GetName(),
                Controls = { (UserControl)kuriimuForm }
            };
            openFiles.TabPages.Add(tabPage);

            _stateDictionary[stateInfo] = (kuriimuForm, tabPage, tabColor);
            _tabDictionary[tabPage] = (kuriimuForm, stateInfo, tabColor);

            tabPage.ImageKey = CloseButton_;  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;

            UpdateTab(stateInfo);

            return true;
        }

        private string SelectFile()
        {
            var ofd = new OpenFileDialog
            {
                Filter = CreateFileFilters(_pluginManager.GetFilePluginLoaders())
            };

            return ofd.ShowDialog() == DialogResult.OK ?
                ofd.FileName :
                null;
        }

        #endregion

        #region Save File

        private Task<bool> TabControl_SaveTab(IStateInfo stateInfo, bool saveAs)
        {
            return saveAs ? SaveFileAs(stateInfo) : SaveFile(stateInfo);
        }

        private async Task SaveAll(bool invokeUpdateForm)
        {
            foreach (var entry in _tabDictionary.Values)
                await SaveFile(entry.StateInfo, invokeUpdateForm);
        }

        private Task<bool> SaveFile(IStateInfo stateInfo, bool invokeUpdateForm = false)
        {
            return SaveFile(stateInfo, UPath.Empty, invokeUpdateForm);
        }

        private Task<bool> SaveFileAs(IStateInfo stateInfo, bool invokeUpdateForm = false)
        {
            var sfd = new SaveFileDialog
            {
                FileName = stateInfo.FilePath.GetName(),
                Filter = "All Files (*.*)|*.*"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(InvalidFile_, SaveError_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Task.FromResult(false);
            }

            return SaveFile(stateInfo, sfd.FileName, invokeUpdateForm);
        }

        private async Task<bool> SaveFile(IStateInfo stateInfo, UPath savePath, bool invokeUpdateForm = false)
        {
            var saveResult = savePath.IsEmpty ?
                await _pluginManager.SaveFile(stateInfo) :
                await _pluginManager.SaveFile(stateInfo, savePath);

            if (!saveResult.IsSuccessful)
            {
#if DEBUG
                var message = saveResult.Exception?.ToString() ?? saveResult.Message;
#else
                var message = saveResult.Message;
#endif

                MessageBox.Show(message, SaveError_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Update current state form if enabled
            UpdateTab(stateInfo, invokeUpdateForm, false);

            // Update children
            UpdateChildrenTabs(stateInfo);

            // Update parents
            UpdateTab(stateInfo.ParentStateInfo, true);

            return true;
        }

        #endregion

        #region Close File

        private Task<bool> Kuriimu2_CloseFile(IStateInfo stateInfo, ArchiveFileInfo afi)
        {
            var absolutePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / afi.FilePath.ToRelative();
            if (!_pluginManager.IsLoaded(absolutePath))
                return Task.FromResult(true);

            var loadedFile = _pluginManager.GetLoadedFile(absolutePath);
            return CloseFile(loadedFile);
        }

        private async Task<bool> CloseFile(IStateInfo stateInfo, bool ignoreChildWarning = false)
        {
            // Security question, so the user knows that every sub file will be closed
            if (stateInfo.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = MessageBox.Show(DependantFilesText_, DependantFiles_, MessageBoxButtons.YesNo);

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
                var text = string.Format(UnsavedChangesText_, stateInfo.FilePath);
                var result = MessageBox.Show(text, UnsavedChanges_, MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        await _pluginManager.SaveFile(stateInfo);

                        // TODO: Somehow propagate save error to user?

                        break;

                    case DialogResult.No:
                        // Close state and tabs without doing anything
                        break;

                    default:
                        return false;
                }
            }

            // Remove all tabs related to the state
            CloseTab(stateInfo);

            // Close all related states
            var parentState = stateInfo.ParentStateInfo;
            _pluginManager.Close(stateInfo);

            // Update parents before state is disposed
            UpdateTab(parentState, true);

            return true;
        }

        private void CloseTab(IStateInfo stateInfo)
        {
            foreach (var child in stateInfo.ArchiveChildren)
                CloseTab(child);

            if (!_stateDictionary.ContainsKey(stateInfo))
                return;

            var stateEntry = _stateDictionary[stateInfo];
            _tabDictionary.Remove(stateEntry.TabPage);

            stateEntry.TabPage.Dispose();
            _stateDictionary.Remove(stateInfo);
        }

        #endregion

        #region Report Status

        private void Kuriimu2_ReportStatus(string message, Color textColor)
        {
            statusLabelToolStrip.Text = message;
            statusLabelToolStrip.ForeColor = textColor;
        }

        #endregion

        #region Update Methods

        private void TabControl_UpdateTab(IStateInfo stateInfo, bool updateParents, bool updateChildren)
        {
            UpdateTab(stateInfo, false, updateParents);
            if (updateChildren)
                UpdateChildrenTabs(stateInfo);
        }

        private void UpdateChildrenTabs(IStateInfo stateInfo)
        {
            // Iterate through the children
            foreach (var child in stateInfo.ArchiveChildren)
                UpdateChildrenTabs(child);

            UpdateTab(stateInfo, true, false);
        }

        private void UpdateTab(IStateInfo stateInfo, bool invokeUpdateForm = false, bool iterateParents = true)
        {
            if (stateInfo == null || !_stateDictionary.ContainsKey(stateInfo))
                return;

            // Update this tab pages information
            var stateEntry = _stateDictionary[stateInfo];
            stateEntry.TabPage.Text = (stateInfo.StateChanged ? "* " : "") + stateInfo.FilePath.GetName();

            // If the call was not made by the requesting state, propagate an update action to it
            if (invokeUpdateForm)
                stateEntry.KuriimuForm.UpdateForm();

            // Update the information of the states parents
            if (iterateParents)
                UpdateTab(stateInfo.ParentStateInfo, true);
        }

        private void UpdateFormText()
        {
            if (openFiles.SelectedIndex < 0)
            {
                Text = FormTitle_;
                return;
            }

            var stateEntry = _tabDictionary[openFiles.SelectedTab];

            var pluginAssemblyName = ((UPath)stateEntry.StateInfo.PluginState.GetType().Assembly.Location).GetName();
            var pluginId = stateEntry.StateInfo.FilePlugin.PluginId;

            Text = string.Format(FormTitlePlugin_, pluginAssemblyName, pluginId.ToString("D"));
        }

        #endregion

        #region Events

        private void PluginManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            var selectedPlugin = ChoosePlugin(e.FilePlugins);
            if (selectedPlugin != null)
                e.Result = selectedPlugin;
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private async void Kuriimu2_KeyDown(object sender, KeyEventArgs e)
        {
            var tabEntry = GetSelectedTabEntry();

            switch (e.KeyData)
            {
                case OpenHotKey_:
                    OpenPhysicalFile(false);
                    break;

                case OpenWithHotKey_:
                    OpenPhysicalFile(true);
                    break;

                case SaveHotKey_:
                    if (tabEntry.StateInfo.PluginState is ISaveFiles)
                        await SaveFile(tabEntry.StateInfo, true);
                    break;

                case SaveAllHotKey_:
                    if (tabEntry.StateInfo.PluginState is ISaveFiles)
                        await SaveAll(true);
                    break;

                case SaveAsHotKey_:
                    if (tabEntry.StateInfo.PluginState is ISaveFiles)
                        await SaveFileAs(tabEntry.StateInfo, true);
                    break;
            }
        }

        private void Kuriimu2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void Kuriimu2_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            OpenPhysicalFiles(files, false);
        }

        private void Kuriimu2_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (_stateDictionary.Keys.Count > 0)
            {
                var stateInfo = _stateDictionary.Keys.First();
                if (CloseFile(stateInfo, true).Result)
                    continue;

                e.Cancel = true;
                break;
            }
        }

        private void openFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFormText();
        }

        private void textSequenceSearcherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _sequenceSearcher.ShowDialog();
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

        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            var selectedTabPage = openFiles.TabPages[e.Index];
            if (!_tabDictionary.ContainsKey(selectedTabPage))
                return;

            var tabEntry = _tabDictionary[selectedTabPage];
            var textColor = tabEntry.TabColor.GetBrightness() <= 0.49 ? Color.White : Color.Black;

            // Color the tab header
            e.Graphics.FillRectangle(new SolidBrush(tabEntry.TabColor), e.Bounds);

            // Format string
            var drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };

            // Draw header text
            e.Graphics.DrawString(selectedTabPage.Text, e.Font, new SolidBrush(textColor), new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 2, e.Bounds.Height), drawFormat);

            // Draw image
            var drawPoint = openFiles.SelectedIndex == e.Index ? new Point(e.Bounds.Left + 9, e.Bounds.Top + 4) : new Point(e.Bounds.Left + 3, e.Bounds.Top + 2);
            e.Graphics.DrawImage(tabCloseButtons.Images[CloseButton_], drawPoint);
        }

        private async void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            var tabImage = tabCloseButtons.Images[CloseButton_];
            if (tabImage == null)
                return;

            var selectedRect = openFiles.GetTabRect(openFiles.SelectedIndex);
            var closeButtonRect = new Rectangle(selectedRect.Left + 9, selectedRect.Top + 4, tabImage.Width, tabImage.Height);
            if (!closeButtonRect.Contains(e.Location))
                return;

            var tabEntry = _tabDictionary[openFiles.SelectedTab];
            var parentStateInfo = tabEntry.StateInfo.ParentStateInfo;

            // Close file
            var wasClosed = await CloseFile(tabEntry.StateInfo);
            if (!wasClosed)
                return;

            // Select parent tab
            if (parentStateInfo != null && _stateDictionary.ContainsKey(parentStateInfo))
                openFiles.SelectedTab = _stateDictionary[parentStateInfo].TabPage;
        }

        #endregion

        #region Support

        private void DisplayPluginErrors(IReadOnlyList<PluginLoadError> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Following plugins could not be loaded:");
            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            MessageBox.Show(sb.ToString(), PluginsNotAvailable_, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor) GetSelectedTabEntry()
        {
            var selectedTab = openFiles.SelectedTab;
            if (selectedTab == null)
                return default;

            if (_tabDictionary.ContainsKey(selectedTab))
                return _tabDictionary[selectedTab];

            return default;
        }

        private IFilePlugin ChoosePlugin(IReadOnlyList<IFilePlugin> filePlugins)
        {
            var pluginChooser = new ChoosePluginForm(filePlugins);
            return pluginChooser.ShowDialog() == DialogResult.OK ?
                pluginChooser.SelectedFilePlugin :
                null;
        }

        private string CreateFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<string> { "All files|*.*" };

            foreach (var plugin in pluginLoaders.SelectMany(x => x.Plugins))
            {
                var pluginName = plugin.Metadata?.Name ?? plugin.GetType().Name;
                var extensions = string.Join(";", plugin.FileExtensions);

                filters.Add($"{pluginName}|{extensions}");
            }

            return string.Join("|", filters);
        }

        private IArchiveFormCommunicator CreateFormCommunicator(IStateInfo stateInfo)
        {
            var communicator = new FormCommunicator(stateInfo)
            {
                OpenFileDelegate = Kuriimu2_OpenFile,
                SaveFileDelegate = TabControl_SaveTab,
                CloseFileDelegate = Kuriimu2_CloseFile,
                UpdateTabDelegate = TabControl_UpdateTab,
                ReportStatusDelegate = Kuriimu2_ReportStatus
            };


            return communicator;
        }

        #endregion

        private void BatchProcessorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //new Batch(_pluginManager).ShowDialog();
        }
    }
}
