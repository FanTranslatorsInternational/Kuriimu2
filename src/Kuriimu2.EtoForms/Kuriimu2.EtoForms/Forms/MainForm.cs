using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Eto.Drawing;
using Eto.Forms;
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
using Kore.Managers;
using Kore.Managers.Plugins;
using Kore.Models.Update;
using Kore.Progress;
using Kore.Update;
using Kuriimu2.EtoForms.Exceptions;
using Kuriimu2.EtoForms.Forms.Dialogs;
using Kuriimu2.EtoForms.Forms.Dialogs.Batch;
using Kuriimu2.EtoForms.Forms.Dialogs.Extensions;
using Kuriimu2.EtoForms.Forms.Formats;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Progress;
using Kuriimu2.EtoForms.Support;
using Newtonsoft.Json;

namespace Kuriimu2.EtoForms.Forms
{
    public partial class MainForm : Form, IMainForm
    {
        private readonly Random _rand = new Random();

        private readonly IProgressContext _progress;
        private readonly PluginManager _pluginManager;

        private readonly IList<string> _openingFiles = new List<string>();
        private readonly IList<IStateInfo> _savingFiles = new List<IStateInfo>();

        private readonly IDictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)> _stateDictionary =
            new Dictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)>();
        private readonly IDictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)> _tabDictionary =
            new Dictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)>();

        private readonly Manifest _localManifest;

        private HashExtensionDialog _hashDialog;
        private DecryptExtensionDialog _decryptDialog;
        private EncryptExtensionsDialog _encryptDialog;
        private DecompressExtensionDialog _decompressDialog;
        private CompressExtensionDialog _compressDialog;
        private BatchExtractDialog _extractDialog;
        private BatchInjectDialog _injectDialog;
        private SequenceSearcher _searcherDialog;

        #region HotKeys

        private const Keys OpenHotKey = Keys.Control | Keys.O;
        private const Keys OpenWithHotKey = Keys.Control | Keys.Shift | Keys.O;
        private const Keys SaveAllHotKey = Keys.Control | Keys.Shift | Keys.S;

        #endregion

        #region Constants

        private const string MenuDeleteResourceName = "Kuriimu2.EtoForms.Images.menu-delete.png";
        private const string MenuSaveResourceName = "Kuriimu2.EtoForms.Images.menu-save.png";

        private const string ManifestUrl = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-EtoForms-Update/main/{0}/manifest.json";
        private const string ApplicationType = "EtoForms.{0}";

        private const string LoadError = "Load Error";
        private const string InvalidFile = "The selected file is invalid.";
        private const string NoPluginSelected = "No plugin was selected.";

        private const string SaveError = "Save Error";

        private const string ExceptionCatched = "Exception catched";
        private const string PluginsNotAvailable = "Plugins not available";

        private const string FormTitle = "Kuriimu2 {0}";
        private const string FormTitlePlugin = "Kuriimu2 {0} - {1} - {2} - {3}";

        private const string UnsavedChanges = "Unsaved changes";
        private const string UnsavedChangesText = "Changes were made to '{0}' or its opened sub files. Do you want to save those changes?";

        private const string DependantFiles = "Dependant files";
        private const string DependantFilesText = "Every file opened from this one and below will be closed too. Continue?";

        private const string ManifestResourceName = "Kuriimu2.EtoForms.Resources.version.json";

        #endregion

        #region Loaded image resources

        private readonly Bitmap MenuDeleteResource = Bitmap.FromResource(MenuDeleteResourceName);
        private readonly Bitmap MenuSaveResource = Bitmap.FromResource(MenuSaveResourceName);

        #endregion

        // ReSharper disable once UseObjectOrCollectionInitializer
        public MainForm()
        {
            InitializeComponent();

            _hashDialog = new HashExtensionDialog();
            _decryptDialog = new DecryptExtensionDialog();
            _encryptDialog = new EncryptExtensionsDialog();
            _decompressDialog = new DecompressExtensionDialog();
            _compressDialog = new CompressExtensionDialog();
            _searcherDialog=new SequenceSearcher();

            _localManifest = LoadLocalManifest();
            UpdateFormText();

            _progress = new ProgressContext(new ProgressBarExOutput(_progressBarEx, 300));
            _pluginManager = new PluginManager(_progress, new DialogManagerDialog(this), "plugins");
            _pluginManager.AllowManualSelection = true;
            _pluginManager.OnManualSelection += pluginManager_OnManualSelection;

            if (_pluginManager.LoadErrors.Any())
                DisplayPluginErrors(_pluginManager.LoadErrors);

            _extractDialog = new BatchExtractDialog(_pluginManager);
            _injectDialog = new BatchInjectDialog(_pluginManager);

            // HINT: The form cannot directly handle DragDrop for some reason and needs a catalyst (on every platform beside WinForms)
            // HINT: Some kind of form spanning control, which handles the drop action instead
            // HINT: https://github.com/picoe/Eto/issues/1852
            Content.DragEnter += mainForm_DragEnter;
            Content.DragDrop += mainForm_DragDrop;

            Content.Load += mainForm_Load;

            #region Commands

            openFileCommand.Executed += openFileCommand_Executed;
            openFileWithCommand.Executed += openFileWithCommand_Executed;
            saveAllFileCommand.Executed += saveAllFileCommand_Executed;

            openBatchExtractorCommand.Executed += openBatchExtractorCommand_Executed;
            openBatchInjectorCommand.Executed += openBatchInjectorCommand_Executed;
            openTextSequenceSearcherCommand.Executed += openTextSequenceSearcherCommand_Execute;

            openHashcommand.Executed += openHashCommand_Executed;
            openDecryptionCommand.Executed += openDecryptionCommand_Executed;
            openEncryptionCommand.Executed += openEncryptionCommand_Executed;
            openDecompressionCommand.Executed += openDecompressionCommand_Executed;
            openCompressionCommand.Executed += openCompressionCommand_Executed;

            #endregion
        }

        #region Open File

        private void openFileCommand_Executed(object sender, EventArgs e)
        {
            OpenPhysicalFile(false);
        }

        private void openFileWithCommand_Executed(object sender, EventArgs e)
        {
            OpenPhysicalFile(true);
        }

        private void OpenPhysicalFile(bool manualIdentification)
        {
            var fileToOpen = SelectFile();

            if (fileToOpen == null)
            {
                ReportStatus(false, "No file was selected.");
                return;
            }

            OpenPhysicalFiles(new[] { fileToOpen }, manualIdentification);
        }

        private async void OpenPhysicalFiles(IList<string> filesToOpen, bool manualIdentification)
        {
            foreach (var fileToOpen in filesToOpen)
            {
                if (_openingFiles.Contains(fileToOpen))
                {
                    ReportStatus(false, $"{fileToOpen} is already opening.");
                    continue;
                }
                _openingFiles.Add(fileToOpen);

                var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                    plugin == null ?
                        _pluginManager.LoadFile(fileToOpen) :
                        _pluginManager.LoadFile(fileToOpen, plugin.PluginId));
                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));

                await OpenFile(fileToOpen, manualIdentification, loadAction, tabColor);

                _openingFiles.Remove(fileToOpen);
            }
        }

        private async Task<bool> OpenFile(UPath filePath, bool manualIdentification, Func<IFilePlugin, Task<LoadResult>> loadFileFunc, Color tabColor)
        {
            ReportStatus(true, string.Empty);

            // Check if path is invalid
            if (filePath.IsNull || filePath.IsEmpty)
            {
                MessageBox.Show(InvalidFile, LoadError, MessageBoxButtons.OK, MessageBoxType.Error);
                return false;
            }

            // Check if file is already opened
            if (_pluginManager.IsLoaded(filePath))
            {
                var selectedTabPage = _stateDictionary[_pluginManager.GetLoadedFile(filePath)].TabPage;
                Application.Instance.Invoke(() => tabControl.SelectedPage = selectedTabPage);

                return true;
            }

            // Choose plugin
            IFilePlugin chosenPlugin = null;
            if (manualIdentification)
            {
                chosenPlugin = ChoosePlugin(_pluginManager.GetFilePlugins().ToArray());
                if (chosenPlugin == null)
                {
                    ReportStatus(false, NoPluginSelected);
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

                MessageBox.Show(message, LoadError, MessageBoxButtons.OK, MessageBoxType.Error);
                return false;
            }

            // Open tab page
            var wasAdded = Application.Instance.Invoke(() => AddTabPage(loadResult.LoadedState, tabColor));
            if (!wasAdded)
            {
                _pluginManager.Close(loadResult.LoadedState);
                return false;
            }

            // Update title if only one file is open
            if (tabControl.Pages.Count == 1)
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
                    // TODO: Implement other forms
                    //case ITextState _:
                    //    kuriimuForm = new TextForm(stateInfo, communicator, _pluginManager.GetGameAdapters().ToArray(),
                    //        _progressContext);
                    //    break;

                    //case IImageState _:
                    //    kuriimuForm = new ImageForm(stateInfo, communicator, _progressContext);
                    //    break;

                    case IArchiveState _:
                        kuriimuForm = new ArchiveForm(stateInfo, communicator, _pluginManager, _progress);
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
                MessageBox.Show(e.ToString(), ExceptionCatched);
#else
                MessageBox.Show(e.Message, ExceptionCatched);
#endif

                return false;
            }

            // Create new tab page
            var tabPage = new TabPage((Panel)kuriimuForm)
            {
                Padding = new Padding(0, 2, 2, 1),
                Text = stateInfo.FilePath.ToRelative().GetName()
            };

            // Add tab page to tab control
            Application.Instance.Invoke(() => tabControl.Pages.Add(tabPage));

            _stateDictionary[stateInfo] = (kuriimuForm, tabPage, tabColor);
            _tabDictionary[tabPage] = (kuriimuForm, stateInfo, tabColor);

            Application.Instance.Invoke(() =>
            {
                tabPage.Image = new Icon(new IconFrame(1, MenuDeleteResource));
                tabPage.MouseUp += tabPage_Click;
            });

            // Select tab page in tab control
            Application.Instance.Invoke(() => tabControl.SelectedPage = tabPage);

            UpdateTab(stateInfo);

            return true;
        }

        private string SelectFile()
        {
            var ofd = new OpenFileDialog();

            foreach (var filter in GetFileFilters(_pluginManager.GetFilePluginLoaders()))
                ofd.Filters.Add(filter);

            return ofd.ShowDialog(this) == DialogResult.Ok ?
                ofd.FileName :
                null;
        }

        private IList<FileFilter> GetFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<FileFilter>();

            foreach (var plugin in pluginLoaders.SelectMany(x => x.Plugins).Where(x => x.FileExtensions != null))
            {
                var pluginName = plugin.Metadata?.Name ?? plugin.GetType().Name;
                filters.Add(new FileFilter(pluginName, plugin.FileExtensions));
            }

            return filters;
        }

        #endregion

        #region Save File

        private async void saveAllFileCommand_Executed(object sender, EventArgs e)
        {
            await SaveAll(true);
        }

        private async Task SaveAll(bool invokeUpdateForm)
        {
            foreach (var entry in _tabDictionary.Values)
                await SaveFile(entry.StateInfo, true, invokeUpdateForm);
        }

        private async Task<bool> SaveFile(IStateInfo stateInfo, bool saveAs, bool invokeUpdateForm)
        {
            ReportStatus(true, string.Empty);

            // Check if file is already attempted to be saved
            if (_savingFiles.Contains(stateInfo))
            {
                ReportStatus(false, $"{stateInfo.FilePath.ToRelative()} is already saving.");
                return false;
            }

            _savingFiles.Add(stateInfo);

            // Select save path if necessary
            var savePath = UPath.Empty;
            if (saveAs)
            {
                savePath = SelectNewFile(stateInfo.FilePath.GetName());
                if (savePath.IsNull || savePath.IsEmpty)
                {
                    ReportStatus(false, "The selected file is invalid.");

                    _savingFiles.Remove(stateInfo);
                    return false;
                }
            }

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

                ReportStatus(false, "File not saved successfully.");
                MessageBox.Show(message, SaveError, MessageBoxButtons.OK, MessageBoxType.Error);

                _savingFiles.Remove(stateInfo);
                return false;
            }

            // Update current state form if enabled
            UpdateTab(stateInfo, invokeUpdateForm, false);

            // Update children
            UpdateChildrenTabs(stateInfo);

            // Update parents
            UpdateTab(stateInfo.ParentStateInfo, true);

            ReportStatus(true, "File saved successfully.");

            _savingFiles.Remove(stateInfo);
            return true;
        }

        private string SelectNewFile(string fileName)
        {
            return Application.Instance.Invoke(() =>
            {
                var sfd = new SaveFileDialog { FileName = fileName };
                return sfd.ShowDialog(this) == DialogResult.Ok ?
                    sfd.FileName :
                    null;
            });
        }


        #endregion

        #region Close File

        private async Task<bool> CloseFile(IStateInfo stateInfo, bool ignoreChildWarning = false)
        {
            ReportStatus(true, string.Empty);

            // Security question, so the user knows that every sub file will be closed
            if (stateInfo.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = MessageBox.Show(DependantFilesText, DependantFiles, MessageBoxButtons.YesNo);

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
                var text = string.Format(UnsavedChangesText, stateInfo.FilePath);
                var result = MessageBox.Show(text, UnsavedChanges, MessageBoxButtons.YesNoCancel);
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

        #region Update

        private void UpdateFormText()
        {
            if (tabControl.Pages.Count <= 0 || tabControl.SelectedIndex < 0)
            {
                Title = string.Format(FormTitle, _localManifest.BuildNumber);
                return;
            }

            var stateEntry = _tabDictionary[tabControl.SelectedPage];

            var pluginAssemblyName = ((UPath)stateEntry.StateInfo.PluginState.GetType().Assembly.Location).GetName();
            var pluginName = stateEntry.StateInfo.FilePlugin.Metadata.Name;
            var pluginId = stateEntry.StateInfo.FilePlugin.PluginId;

            Title = string.Format(FormTitlePlugin, _localManifest.BuildNumber, pluginAssemblyName, pluginName, pluginId.ToString("D"));
        }

        private void UpdateTab(IStateInfo stateInfo, bool invokeUpdateForm = false, bool iterateParents = true)
        {
            if (stateInfo == null || !_stateDictionary.ContainsKey(stateInfo))
                return;

            // Update this tab pages information
            var stateEntry = _stateDictionary[stateInfo];
            Application.Instance.Invoke(() => stateEntry.TabPage.Text = (stateInfo.StateChanged ? "* " : "") + stateInfo.FilePath.GetName());

            // If the call was not made by the requesting state, propagate an update action to it
            if (invokeUpdateForm)
                stateEntry.KuriimuForm.UpdateForm();

            // Update the information of the states parents
            if (iterateParents)
                UpdateTab(stateInfo.ParentStateInfo, true);
        }

        private void UpdateChildrenTabs(IStateInfo stateInfo)
        {
            // Iterate through children
            foreach (var child in stateInfo.ArchiveChildren)
                UpdateChildrenTabs(child);

            UpdateTab(stateInfo, true, false);
        }

        #endregion

        #region Events

        #region Main Form

        private void mainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragEffects.Copy;
        }

        private void mainForm_DragDrop(object sender, DragEventArgs e)
        {
            OpenPhysicalFiles(e.Data.Uris.Select(x => HttpUtility.UrlDecode(x.AbsolutePath)).ToArray(), false);
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
#if !DEBUG
            CheckForUpdate();
#endif
        }

        #endregion

        #region Tools

        private void openBatchExtractorCommand_Executed(object sender, EventArgs e)
        {
            _extractDialog.ShowModal();
        }

        private void openBatchInjectorCommand_Executed(object sender, EventArgs e)
        {
            _injectDialog.ShowModal();
        }

        private void openTextSequenceSearcherCommand_Execute(object sender, EventArgs e)
        {
            _searcherDialog.ShowModal();
        }

        #endregion

        #region Extensions

        private void openHashCommand_Executed(object sender, EventArgs e)
        {
            _hashDialog.ShowModal();
        }

        private void openCompressionCommand_Executed(object sender, EventArgs e)
        {
            _compressDialog.ShowModal();
        }

        private void openDecompressionCommand_Executed(object sender, EventArgs e)
        {
            _decompressDialog.ShowModal();
        }

        private void openEncryptionCommand_Executed(object sender, EventArgs e)
        {
            _encryptDialog.ShowModal();
        }

        private void openDecryptionCommand_Executed(object sender, EventArgs e)
        {
            _decryptDialog.ShowModal();
        }

        #endregion

        private void pluginManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            var selectedPlugin = ChoosePlugin(e.FilePlugins);
            if (selectedPlugin != null)
                e.Result = selectedPlugin;
        }

        private async void tabPage_Click(object sender, MouseEventArgs e)
        {
            if (!e.Buttons.HasFlag(MouseButtons.Primary) && !e.Buttons.HasFlag(MouseButtons.Middle))
                return;

            var page = (TabPage)sender;
            if (page == null)
                return;

            var tabEntry = _tabDictionary[page];
            var parentStateInfo = tabEntry.StateInfo.ParentStateInfo;

            if (e.Buttons.HasFlag(MouseButtons.Middle))
            {
                if (!page.Bounds.Contains((Point)e.Location))
                    return;
            }

            if (e.Buttons.HasFlag(MouseButtons.Primary))
            {
                var deleteImage = MenuDeleteResource;
                var closeButtonRect = new RectangleF(page.Bounds.Left + 9, page.Bounds.Top + 4, deleteImage.Width, deleteImage.Height);
                if (!closeButtonRect.Contains(e.Location))
                    return;
            }

            // Select parent tab
            if (parentStateInfo != null && _stateDictionary.ContainsKey(parentStateInfo))
                tabControl.SelectedPage = _stateDictionary[parentStateInfo].TabPage;

            // Close file
            await CloseFile(tabEntry.StateInfo);
        }

        #endregion

        #region Support

        private void CheckForUpdate()
        {
            if (_localManifest == null)
                return;

            var platform = GetCurrentPlatform();

            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest))
                return;

            var result =
                MessageBox.Show(
                    $"Do you want to update from '{_localManifest.BuildNumber}' to '{remoteManifest.BuildNumber}'?",
                    "Update available", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return;

            var executablePath = UpdateUtilities.DownloadUpdateExecutable();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{string.Format(ApplicationType, platform)} {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)}")
            };
            process.Start();

            Close();
        }

        private string GetCurrentPlatform()
        {
            if (Application.Instance.Platform.IsGtk)
                return "Gtk";

            if (Application.Instance.Platform.IsWpf)
                return "Wpf";

            if (Application.Instance.Platform.IsMac)
                return "Mac";

            throw new InvalidOperationException($"Platform {Application.Instance.Platform.ID} is not supported.");
        }

        private void DisplayPluginErrors(IReadOnlyList<PluginLoadError> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Following plugins could not be loaded:");
            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            MessageBox.Show(sb.ToString(), PluginsNotAvailable, MessageBoxButtons.OK, MessageBoxType.Error);
        }

        private Manifest LoadLocalManifest()
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ManifestResourceName);
            if (resourceStream == null)
                return null;

            return JsonConvert.DeserializeObject<Manifest>(new StreamReader(resourceStream).ReadToEnd());
        }

        private IFilePlugin ChoosePlugin(IReadOnlyList<IFilePlugin> filePlugins)
        {
            return Application.Instance.Invoke(() =>
            {
                var pluginDialog = new ChoosePluginDialog(filePlugins);
                return pluginDialog.ShowModal(this);
            });
        }

        private (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor) GetSelectedTabEntry()
        {
            var selectedTab = tabControl.SelectedPage;
            if (selectedTab == null)
                return default;

            if (!_tabDictionary.ContainsKey(selectedTab))
                return default;

            return _tabDictionary[selectedTab];
        }

        #endregion

        #region Form Communication

        private IArchiveFormCommunicator CreateFormCommunicator(IStateInfo stateInfo)
        {
            var communicator = new FormCommunicator(stateInfo, this);
            return communicator;
        }

        public Task<bool> OpenFile(IStateInfo stateInfo, IArchiveFileInfo file, Guid pluginId)
        {
            var absoluteFilePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / file.FilePath.ToRelative();
            var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                pluginId == Guid.Empty ?
                    _pluginManager.LoadFile(stateInfo, file) :
                    _pluginManager.LoadFile(stateInfo, file, pluginId));
            var tabColor = _stateDictionary[stateInfo].TabColor;

            return Task.Run(() => OpenFile(absoluteFilePath, false, loadAction, tabColor));
        }

        public Task<bool> SaveFile(IStateInfo stateInfo, bool saveAs)
        {
            return SaveFile(stateInfo, saveAs, false);
        }

        public Task<bool> CloseFile(IStateInfo stateInfo, IArchiveFileInfo file)
        {
            var absolutePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / file.FilePath.ToRelative();
            if (!_pluginManager.IsLoaded(absolutePath))
                return Task.FromResult(true);

            var loadedFile = _pluginManager.GetLoadedFile(absolutePath);
            return CloseFile(loadedFile);
        }

        public void RenameFile(IStateInfo stateInfo, IArchiveFileInfo file, UPath newPath)
        {
            var absolutePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / file.FilePath.ToRelative();
            if (!_pluginManager.IsLoaded(absolutePath))
                return;

            var loadedFile = _pluginManager.GetLoadedFile(absolutePath);
            loadedFile.RenameFilePath(newPath);

            UpdateTab(loadedFile, true, false);
        }

        public void Update(IStateInfo stateInfo, bool updateParents, bool updateChildren)
        {
            UpdateTab(stateInfo, false, updateParents);
            if (updateChildren)
                UpdateChildrenTabs(stateInfo);
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            if (message == null)
                return;

            var textColor = isSuccessful ? KnownColors.Black : KnownColors.DarkRed;

            Application.Instance.Invoke(() =>
            {
                statusMessage.Text = message;
                statusMessage.TextColor = textColor;
            });
        }

        #endregion
    }
}
