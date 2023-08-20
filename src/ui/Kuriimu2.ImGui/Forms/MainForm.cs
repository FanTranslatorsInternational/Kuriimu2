using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Localization;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Models;
using Kontract.Extensions;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.Loaders;
using Kore.Extensions;
using Kore.Implementation.Managers.Files;
using Kore.Implementation.Progress;
using Kore.Models.Managers.Files;
using Kore.Models.Update;
using Kore.Update;
using Kuriimu2.ImGui.Forms.Dialogs;
using Kuriimu2.ImGui.Forms.Formats;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Progress;
using Kuriimu2.ImGui.Resources;
using Newtonsoft.Json;
using Serilog;

namespace Kuriimu2.ImGui.Forms
{
    partial class MainForm : Form, IMainForm
    {
        private readonly Random _rand = new Random();

        private readonly Manifest _localManifest;

        private readonly ILogger _logger;
        private readonly ProgressContext _progress;
        private readonly KoreFileManager _koreFileManager;

        private readonly IDictionary<IFileState, OpenedFile> _stateDictionary = new Dictionary<IFileState, OpenedFile>();
        private readonly IDictionary<TabPage, OpenedFile> _tabDictionary = new Dictionary<TabPage, OpenedFile>();

        #region Constants

        private const string ManifestUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-ImGuiForms-Update/main/{0}/manifest.json";
        private const string ApplicationType_ = "ImGui";

        private const string FormTitle_ = "Kuriimu2 {0}-{1}";
        private const string FormTitlePlugin_ = "Kuriimu2 {0}-{1} - {2} - {3} - {4}";

        #endregion

        public MainForm()
        {
            InitializeComponent();

            #region Initialization

            _localManifest = LoadLocalManifest();

            _logger = LoadLogger();
            _progress = LoadProgressContext(_progressBar);
            _koreFileManager = LoadFileManager(_logger, _progress);

            #endregion

            #region Events

            Load += MainForm_Load;
            Closing += MainForm_Closing;
            DragDrop += MainForm_DragDrop;

            _koreFileManager.OnManualSelection += KoreFileManagerOnManualSelection;

            _includeDevBuildsButton.CheckChanged += _includeDevBuildsButton_CheckChanged;
            _changeLanguageMenu.SelectedItemChanged += _changeLanguageMenu_SelectedItemChanged;
            _changeThemeMenu.SelectedItemChanged += _changeThemeMenu_SelectedItemChanged;

            _openButton.Clicked += _openButton_Clicked;
            _openWithButton.Clicked += _openWithButton_Clicked;
            _saveAllButton.Clicked += _saveAllButton_Clicked;

            _tabControl.PageRemoving += _tabControl_PageRemoving;
            _tabControl.PageRemoved += _tabControl_PageRemoved;

            _aboutButton.Clicked += _aboutButton_Clicked;

            #endregion

            UpdateFormTitle();
        }

        #region Events

        #region Form

        private async void MainForm_Load(object sender, EventArgs e)
        {
#if !DEBUG
            // Check if updates are available
            await CheckForUpdate();
#endif

            // Set stored theme
            if (Enum.TryParse<Theme>(Settings.Default.Theme, out var parsedTheme))
                Style.ChangeTheme(parsedTheme);

            // Set stored localization
            LocalizationResources.Instance.ChangeLocale(Settings.Default.Locale);

            // Show plugin load errors
            // TODO: Change to small label at bottom of window "Some plugins couldn't be loaded."
            // TODO: Add menu bar button to show which plugins were loaded successfully and which did not and why
            await DisplayPluginErrors(_koreFileManager.LoadErrors);
        }

        private async Task MainForm_Closing(object sender, ClosingEventArgs e)
        {
            // Cancel all operations on the forms first
            foreach (var tabEntry in _tabDictionary)
                tabEntry.Value.Form.CancelOperations();

            if (_stateDictionary.Keys.Any(x => x.StateChanged))
            {
                var result = await ConfirmSavingChanges();
                switch (result)
                {
                    case DialogResult.Yes:
                        // Save all files in place that need saving
                        await SaveAll(false);
                        break;

                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;

                        // DialogResult.No means to not save changes and just close all open files
                }
            }

            while (_stateDictionary.Keys.Count > 0)
            {
                var stateInfo = _stateDictionary.Keys.First();

                // Ignore changes warning for closing a single file, because we already made a check if all files, unrelated to each other, should be saved
                // The warning is therefore meaningless and should be ignored for every subsequent closing operation
                if (await CloseFile(stateInfo, true, true, true))
                    continue;

                e.Cancel = true;
                break;
            }
        }

        private async void MainForm_DragDrop(object sender, Veldrid.Sdl2.DragDropEvent e)
        {
            await OpenPhysicalFiles(new List<string> { e.File }, false);
        }

        #endregion

        #region File Management

        private async void _openButton_Clicked(object sender, EventArgs e)
        {
            await OpenPhysicalFile(false);
        }

        private async void _openWithButton_Clicked(object sender, EventArgs e)
        {
            await OpenPhysicalFile(true);
        }

        private async void _saveAllButton_Clicked(object sender, EventArgs e)
        {
            await SaveAll(true);
        }

        #endregion

        #region Change application settings

        private void _includeDevBuildsButton_CheckChanged(object sender, EventArgs e)
        {
            Settings.Default.IncludeDevBuilds = ((MenuBarCheckBox)sender).Checked;
            Settings.Default.Save();
        }

        private void _changeLanguageMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var locale = LocalizationResources.GetLocaleByName(((MenuBarRadio)sender).SelectedItem.Text);

            Settings.Default.Locale = locale;
            Settings.Default.Save();

            LocalizationResources.Instance.ChangeLocale(locale);
        }

        private void _changeThemeMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var theme = _themes[((MenuBarRadio)sender).SelectedItem];

            Settings.Default.Theme = theme.ToString();
            Settings.Default.Save();

            Style.ChangeTheme(theme);

            // Update colors manually
            _progressBar.ProgressColor = ColorResources.Progress;
        }

        #endregion

        #region File manager

        private async void KoreFileManagerOnManualSelection(object sender, KoreFileManager.ManualSelectionEventArgs e)
        {
            var selectedPlugin = await ChoosePlugin(e.FilePlugins.ToArray(), e.FilteredFilePlugins.ToArray(), e.SelectionStatus);
            if (selectedPlugin != null)
                e.Result = selectedPlugin;
        }

        #endregion

        #region TabControl

        private void _tabControl_PageRemoved(object sender, RemoveEventArgs e)
        {
            UpdateFormTitle();
        }

        private async Task _tabControl_PageRemoving(object sender, RemovingEventArgs e)
        {
            var tabEntry = _tabDictionary[e.Page];
            var parentStateInfo = tabEntry.FileState.ParentFileState;

            // Select parent tab
            TabPage parentTab = null;
            if (parentStateInfo != null && _stateDictionary.ContainsKey(parentStateInfo))
                parentTab = _stateDictionary[parentStateInfo].TabPage;

            // Close file
            if (!await CloseFile(tabEntry.FileState))
            {
                e.Cancel = true;
                return;
            }

            // Switch to parent tab
            if (parentTab != null)
                _tabControl.SelectedPage = parentTab;
        }

        #endregion

        private async void _aboutButton_Clicked(object sender, EventArgs e)
        {
            await ShowAboutDialog();
        }

        #endregion

        #region Load methods

        private Manifest LoadLocalManifest()
        {
            return JsonConvert.DeserializeObject<Manifest>(BinaryResources.VersionManifest);
        }

        private ILogger LoadLogger()
        {
            var logPath = Path.Combine(GetBaseDirectory(), "Kuriimu2.log");
            return new LoggerConfiguration().WriteTo.File(logPath).CreateLogger();
        }

        private ProgressContext LoadProgressContext(ProgressBar progressBar)
        {
            return new ProgressContext(new ProgressBarOutput(progressBar, 20));
        }

        private KoreFileManager LoadFileManager(ILogger logger, ProgressContext progress)
        {
            var pluginPath = Path.Combine(GetBaseDirectory(), "plugins");

            return new KoreFileManager(pluginPath)
            {
                AllowManualSelection = true,

                Progress = progress,
                DialogManager = new DialogManager(),
                Logger = logger
            };
        }

        #endregion

        #region File methods

        #region Open

        private async Task OpenPhysicalFile(bool manualIdentification)
        {
            var fileToOpen = await SelectFile();

            if (fileToOpen == null)
            {
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileSelectNone());
                return;
            }

            await OpenPhysicalFiles(new[] { fileToOpen }, manualIdentification);
        }

        private async Task OpenPhysicalFiles(IList<string> filesToOpen, bool manualIdentification)
        {
            foreach (var fileToOpen in filesToOpen)
            {
                var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                    _koreFileManager.LoadFile(fileToOpen, plugin?.PluginId ?? Guid.Empty));
                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));

                await OpenFile(fileToOpen, manualIdentification, loadAction, tabColor);
            }
        }

        private async Task<bool> OpenFile(UPath filePath, bool manualIdentification, Func<IFilePlugin, Task<LoadResult>> loadFileFunc, Color tabColor)
        {
            ReportStatus(StatusKind.Info, LocalizationResources.StatusFileLoadStart(filePath));

            // Check if path is invalid
            if (filePath.IsNull || filePath.IsEmpty)
            {
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileSelectInvalid());
                return false;
            }

            // Check if file is already loading
            if (_koreFileManager.IsLoading(filePath))
            {
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadOpening(filePath));
                return true;
            }

            // Check if file is already opened
            if (_koreFileManager.IsLoaded(filePath))
            {
                var selectedTabPage = _stateDictionary[_koreFileManager.GetLoadedFile(filePath)].TabPage;
                _tabControl.SelectedPage = selectedTabPage;

                return true;
            }

            // Choose plugin
            IFilePlugin chosenPlugin = null;
            if (manualIdentification)
            {
                var allPlugins = _koreFileManager.GetFilePlugins().ToArray();

                chosenPlugin = await ChoosePlugin(allPlugins, allPlugins, KoreFileManager.SelectionStatus.All);
                if (chosenPlugin == null)
                {
                    ReportStatus(StatusKind.Failure, LocalizationResources.StatusPluginSelectNone());
                    return false;
                }
            }

            // Load file
            var loadResult = (KoreLoadResult)await loadFileFunc(chosenPlugin);
            if (loadResult.IsCancelled)
            {
                // Load was canceled
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadCancel());
                return false;
            }

            if (!loadResult.IsSuccessful)
            {
                ReportStatus(StatusKind.Failure, GetReasonString(filePath, loadResult.Reason));
                return false;
            }

            // Open tab page
            var wasAdded = await AddTabPage(loadResult.LoadedFileState, tabColor);
            if (!wasAdded)
            {
                _koreFileManager.Close(loadResult.LoadedFileState);
                return false;
            }

            UpdateFormTitle();

            ReportStatus(StatusKind.Success, LocalizationResources.StatusFileLoadSuccess());

            return true;
        }

        private LocalizedString GetReasonString(UPath path, LoadErrorReason reason)
        {
            switch (reason)
            {
                case LoadErrorReason.Loading:
                    return LocalizationResources.StatusFileLoadOpening(path);

                case LoadErrorReason.NoPlugin:
                    return LocalizationResources.StatusPluginLoadNone();

                case LoadErrorReason.NoArchive:
                    return LocalizationResources.StatusPluginLoadNoArchive();

                case LoadErrorReason.StateCreateError:
                    return LocalizationResources.StatusPluginStateInitError();

                case LoadErrorReason.StateNoLoad:
                    return LocalizationResources.StatusPluginStateLoadNone();

                case LoadErrorReason.StateLoadError:
                    return LocalizationResources.StatusPluginStateLoadError();

                case LoadErrorReason.None:
                    return string.Empty;

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        #region Save

        private async Task SaveAll(bool invokeUpdateForm)
        {
            foreach (var entry in _tabDictionary.Values)
                await SaveFile(entry.FileState, true, invokeUpdateForm);
        }

        private async Task<bool> SaveFile(IFileState fileState, bool saveAs, bool invokeUpdateForm)
        {
            ReportStatus(StatusKind.Info, LocalizationResources.StatusFileSaveStart(fileState.FilePath.ToRelative()));

            // Check if file is already attempted to be saved
            if (_koreFileManager.IsSaving(fileState))
            {
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileLoadSaving(fileState.FilePath.ToRelative()));
                return false;
            }

            // Select save path if necessary
            var savePath = UPath.Empty;
            if (saveAs)
            {
                savePath = await SelectNewFile(fileState.FilePath.GetName());
                if (savePath.IsNull || savePath.IsEmpty)
                {
                    ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileSelectInvalid());
                    return false;
                }
            }

            var saveResult = savePath.IsEmpty ?
                (KoreSaveResult)await _koreFileManager.SaveFile(fileState) :
                (KoreSaveResult)await _koreFileManager.SaveFile(fileState, savePath.FullName);

            if (!saveResult.IsSuccessful)
            {
                ReportStatus(StatusKind.Failure, GetReasonString(fileState.FilePath.ToRelative(), saveResult.Reason));
                return false;
            }

            // Update current state form if enabled
            UpdateTab(fileState, invokeUpdateForm, false);

            // Update children
            UpdateChildrenTabs(fileState);

            // Update parents
            UpdateTab(fileState.ParentFileState, true);

            ReportStatus(StatusKind.Success, LocalizationResources.StatusFileSaveSuccess());

            return true;
        }

        private LocalizedString GetReasonString(UPath path, SaveErrorReason reason)
        {
            switch (reason)
            {
                case SaveErrorReason.Closed:
                    return LocalizationResources.StatusFileSaveClosed();

                case SaveErrorReason.Saving:
                    return LocalizationResources.StatusFileSaveSaving(path);

                case SaveErrorReason.Closing:
                    return LocalizationResources.StatusFileSaveClosing(path);

                case SaveErrorReason.NotLoaded:
                    return LocalizationResources.StatusFileSaveNotLoaded();

                case SaveErrorReason.NoChanges:
                    return LocalizationResources.StatusFileSaveNoChanges();

                case SaveErrorReason.StateSaveError:
                    return LocalizationResources.StatusFileSaveStateError();

                case SaveErrorReason.DestinationNotExist:
                    return LocalizationResources.StatusFileSaveDestinationNotExist();

                case SaveErrorReason.FileReplaceError:
                    return LocalizationResources.StatusFileSaveReplaceError();

                case SaveErrorReason.FileCopyError:
                    return LocalizationResources.StatusFileSaveCopyError();

                case SaveErrorReason.StateReloadError:
                    return LocalizationResources.StatusFileSaveStateReloadError();

                case SaveErrorReason.None:
                    return string.Empty;

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        #region Close

        private async Task<bool> CloseFile(IFileState fileState, bool ignoreChildWarning = false, bool ignoreChangesWarning = false, bool ignoreRunningOperations = false)
        {
            ReportStatus(StatusKind.Info, LocalizationResources.StatusFileCloseStart(fileState.FilePath.ToRelative()));

            // Check if operations are running
            if (!ignoreRunningOperations && _stateDictionary[fileState].Form.HasRunningOperations())
            {
                ReportStatus(StatusKind.Failure, LocalizationResources.StatusOperationRunning());
                return false;
            }

            // Security question, so the user knows that every sub file will be closed
            if (fileState.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogDependantFilesCaption(), LocalizationResources.DialogDependantFilesText());

                switch (result)
                {
                    case DialogResult.Yes:
                        break;

                    default:
                        ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileCloseCancel());
                        return false;
                }
            }

            // Save unchanged files, if wanted
            if (fileState.StateChanged && !ignoreChangesWarning)
            {
                var result = await ConfirmSavingChanges(fileState);
                switch (result)
                {
                    case DialogResult.Yes:
                        var saveResult = (KoreSaveResult)await _koreFileManager.SaveFile(fileState);
                        if (!saveResult.IsSuccessful)
                        {
                            ReportStatus(StatusKind.Failure, GetReasonString(fileState.FilePath.ToRelative(), saveResult.Reason));
                            return false;
                        }

                        break;

                    case DialogResult.No:
                        // Close state and tabs without doing anything
                        break;

                    default:
                        ReportStatus(StatusKind.Failure, LocalizationResources.StatusFileCloseCancel());
                        return false;
                }
            }

            // Collect all opened children states
            var childrenStates = CollectChildrenStates(fileState).ToArray();

            // Close all related states
            var parentState = fileState.ParentFileState;
            var closeResult = (KoreCloseResult)_koreFileManager.Close(fileState);

            // If closing of the state was not successful
            if (!closeResult.IsSuccessful)
            {
                ReportStatus(StatusKind.Failure, GetReasonString(fileState.FilePath.ToRelative(), closeResult.Reason));
                return false;
            }

            // Remove all tabs related to the state, if closing was successful
            foreach (var childState in childrenStates)
                RemoveOpenedFile(childState);
            RemoveOpenedFile(fileState);

            // Update parents before state is disposed
            UpdateTab(parentState, true);

            ReportStatus(StatusKind.Success, LocalizationResources.StatusFileCloseSuccess());

            return true;
        }

        private Task<DialogResult> ConfirmSavingChanges(IFileState fileState = null)
        {
            var text = fileState == null ? LocalizationResources.DialogUnsavedChangesTextGeneric() : LocalizationResources.DialogUnsavedChangesTextSpecific(fileState.FilePath);
            return MessageBox.ShowYesNoCancelAsync(LocalizationResources.DialogUnsavedChangesCaption(), text);
        }

        private void RemoveOpenedFile(IFileState fileState)
        {
            // We only close the tab related to the state itself, not its archive children
            // Closing archive children is done by CloseFile, to enable proper rollback if closing the state itself was unsuccessful
            if (!_stateDictionary.ContainsKey(fileState))
                return;

            var stateEntry = _stateDictionary[fileState];

            _tabControl.RemovePage(stateEntry.TabPage);
            _tabDictionary.Remove(stateEntry.TabPage);
            _stateDictionary.Remove(fileState);
        }

        private LocalizedString GetReasonString(UPath path, CloseErrorReason reason)
        {
            switch (reason)
            {
                case CloseErrorReason.Saving:
                    return LocalizationResources.StatusFileCloseSaving(path);

                case CloseErrorReason.Closing:
                    return LocalizationResources.StatusFileCloseClosing(path);

                case CloseErrorReason.NotLoaded:
                    return LocalizationResources.StatusFileCloseNotLoaded();

                case CloseErrorReason.None:
                    return string.Empty;

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        private async Task<DialogResult> ShowAboutDialog()
        {
            var AboutDialog = new AboutDialog();
            return await AboutDialog.ShowAsync();
        }

        #endregion

        #region Update methods

        private void UpdateFormTitle()
        {
            if (_tabControl.SelectedPage == null)
            {
                Title = string.Format(FormTitle_, _localManifest.Version, _localManifest.BuildNumber);
                return;
            }

            var stateEntry = _tabDictionary[_tabControl.SelectedPage];

            var pluginAssemblyName = ((UPath)stateEntry.FileState.PluginState.GetType().Assembly.Location).GetName();
            var pluginName = stateEntry.FileState.FilePlugin.Metadata.Name;
            var pluginId = stateEntry.FileState.FilePlugin.PluginId;

            Title = string.Format(FormTitlePlugin_, _localManifest.Version, _localManifest.BuildNumber, pluginAssemblyName, pluginName, pluginId.ToString("D"));
        }

        private void UpdateTab(IFileState fileState, bool invokeUpdateForm = false, bool iterateParents = true)
        {
            if (fileState == null || !_stateDictionary.ContainsKey(fileState))
                return;

            // Update this tab pages information
            var stateEntry = _stateDictionary[fileState];

            stateEntry.TabPage.HasChanges = fileState.StateChanged;
            stateEntry.TabPage.Title = fileState.FilePath.GetName();

            // If the call was not made by the requesting state, propagate an update action to it
            if (invokeUpdateForm)
                stateEntry.Form.UpdateForm();

            // Update the information of the states parents
            if (iterateParents)
                UpdateTab(fileState.ParentFileState, true);
        }

        private void UpdateChildrenTabs(IFileState fileState)
        {
            // Iterate through children
            foreach (var child in fileState.ArchiveChildren)
            {
                UpdateTab(child, true, false);
                UpdateChildrenTabs(child);
            }
        }

        #endregion

        #region FormCommunicator methods

        private IArchiveFormCommunicator CreateFormCommunicator(IFileState fileState)
        {
            var communicator = new FormCommunicator(fileState, this);
            return communicator;
        }

        #endregion

        #region Support methods

        private async Task DisplayPluginErrors(IReadOnlyList<PluginLoadError> errors)
        {
            if (!errors.Any())
                return;

            var sb = new StringBuilder();

            sb.AppendLine(LocalizationResources.DialogPluginsNotAvailableText());

            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            await MessageBox.ShowErrorAsync(LocalizationResources.DialogPluginsNotAvailableCaption(), sb.ToString());
        }

        private async Task CheckForUpdate()
        {
            if (_localManifest == null)
                return;

            var platform = GetCurrentPlatform();

            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl_, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest, Settings.Default.IncludeDevBuilds))
                return;

            var result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogUpdateAvailableCaption(),
                LocalizationResources.DialogUpdateAvailableText(_localManifest.Version, _localManifest.BuildNumber, remoteManifest.Version, remoteManifest.BuildNumber));
            if (result == DialogResult.No)
                return;

            var executablePath = UpdateUtilities.DownloadUpdateExecutable();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{ApplicationType_}{platform} {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)}")
            };
            process.Start();

            Close();
        }

        private string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Mac";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";

            throw new InvalidOperationException(LocalizationResources.ErrorUnsupportedOperatingSystem(RuntimeInformation.OSDescription));
        }

        private string GetBaseDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ".";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                if (string.IsNullOrEmpty(path))
                    path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                return path;
            }

            throw new InvalidOperationException(LocalizationResources.ErrorUnsupportedOperatingSystem(RuntimeInformation.OSDescription));
        }

        private IEnumerable<IFileState> CollectChildrenStates(IFileState fileState)
        {
            foreach (var child in fileState.ArchiveChildren)
            {
                yield return child;

                foreach (var collectedChild in CollectChildrenStates(child))
                    yield return collectedChild;
            }
        }

        private async Task<string> SelectNewFile(string fileName)
        {
            var sfd = new SaveFileDialog(fileName);
            return await sfd.ShowAsync() == DialogResult.Ok ? sfd.SelectedPath : null;
        }

        private async Task<string> SelectFile()
        {
            var ofd = new OpenFileDialog { InitialDirectory = Settings.Default.LastDirectory };

            // Set file filters
            foreach (var filter in GetFileFilters(_koreFileManager.GetFilePluginLoaders()).OrderBy(x => x.Name))
                ofd.FileFilters.Add(filter);

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            Settings.Default.LastDirectory = Path.GetDirectoryName(ofd.SelectedPath);
            Settings.Default.Save();

            return ofd.SelectedPath;
        }

        private async Task<IFilePlugin> ChoosePlugin(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, KoreFileManager.SelectionStatus status)
        {
            var pluginDialog = new ChoosePluginDialog(allFilePlugins, filteredFilePlugins, status);
            return await pluginDialog.ShowAsync() == DialogResult.Ok ? pluginDialog.SelectedPlugin : null;
        }

        private IList<FileFilter> GetFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<FileFilter>
            {
                new FileFilter(LocalizationResources.FilterAll(), ".*")
            };

            foreach (var plugin in pluginLoaders.SelectMany(x => x.Plugins).Where(x => x.FileExtensions != null))
            {
                var pluginName = plugin.Metadata?.Name ?? plugin.GetType().Name;
                filters.Add(new FileFilter(pluginName, plugin.FileExtensions.Select(x => x.Replace("*", "")).ToArray()));
            }

            return filters;
        }

        private async Task<bool> AddTabPage(IFileState fileState, Color tabColor)
        {
            var communicator = CreateFormCommunicator(fileState);

            IKuriimuForm kuriimuForm;
            try
            {
                switch (fileState.PluginState)
                {
                    // TODO: Implement state forms
                    //case ITextState _:
                    //    kuriimuForm = new TextForm(new FormInfo<ITextState>(fileState, communicator, _progress, _logger));
                    //    break;

                    case IImageState _:
                        kuriimuForm = new ImageForm(new FormInfo<IImageState>(fileState, communicator, _progress, _logger));
                        break;

                    case IArchiveState _:
                        kuriimuForm = new ArchiveForm(new ArchiveFormInfo(fileState, communicator, _progress, _logger), _koreFileManager);
                        break;

                    case IRawState _:
                        kuriimuForm = new RawForm(new FormInfo<IRawState>(fileState, communicator, _progress, _logger));
                        break;

                    default:
                        var status = LocalizationResources.StatusPluginSelectUnknown(fileState.PluginState);
                        ReportStatus(StatusKind.Failure, status);

                        return false;
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Error creating state form.");
                await MessageBox.ShowErrorAsync(LocalizationResources.DialogExceptionCatchedCaption(), e.Message);

                return false;
            }

            // Create new tab page
            var tabPage = new TabPage((Component)kuriimuForm)
            {
                Title = fileState.FilePath.ToRelative().GetName()
            };

            // Add tab page to tab control
            _tabControl.AddPage(tabPage);

            var openedFile = new OpenedFile(fileState, kuriimuForm, tabPage, tabColor);
            _stateDictionary[fileState] = openedFile;
            _tabDictionary[tabPage] = openedFile;

            // Select tab page in tab control
            _tabControl.SelectedPage = tabPage;

            UpdateTab(fileState);

            return true;
        }

        #endregion

        #region IMainForm implementation

        public Task<bool> OpenFile(IFileState fileState, IArchiveFileInfo file, Guid pluginId)
        {
            var absoluteFilePath = fileState.AbsoluteDirectory / fileState.FilePath.ToRelative() / file.FilePath.ToRelative();
            var loadAction = new Func<IFilePlugin, Task<LoadResult>>(_ =>
                _koreFileManager.LoadFile(fileState, file, pluginId));
            var tabColor = _stateDictionary[fileState].TabColor;

            return OpenFile(absoluteFilePath, false, loadAction, tabColor);
        }

        public Task<bool> SaveFile(IFileState fileState, bool saveAs)
        {
            return SaveFile(fileState, saveAs, false);
        }

        public Task<bool> CloseFile(IFileState fileState, IArchiveFileInfo file)
        {
            var absolutePath = fileState.AbsoluteDirectory / fileState.FilePath.ToRelative() / file.FilePath.ToRelative();
            if (!_koreFileManager.IsLoaded(absolutePath))
                return Task.FromResult(true);

            var loadedFile = _koreFileManager.GetLoadedFile(absolutePath);
            return CloseFile(loadedFile);
        }

        public void RenameFile(IFileState fileState, IArchiveFileInfo file, UPath newPath)
        {
            var absolutePath = fileState.AbsoluteDirectory / fileState.FilePath.ToRelative() / file.FilePath.ToRelative();
            if (!_koreFileManager.IsLoaded(absolutePath))
                return;

            var loadedFile = _koreFileManager.GetLoadedFile(absolutePath);
            loadedFile.RenameFilePath(newPath);

            UpdateTab(loadedFile, true, false);
        }

        public void Update(IFileState fileState, bool updateParents, bool updateChildren)
        {
            UpdateTab(fileState, false, updateParents);
            if (updateChildren)
                UpdateChildrenTabs(fileState);
        }

        public void ReportStatus(StatusKind kind, LocalizedString message) =>
            _statusText.Report(kind, message);

        public void ClearStatus() => _statusText.Clear();

        #endregion
    }
}
