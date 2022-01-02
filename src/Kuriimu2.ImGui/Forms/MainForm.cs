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
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Models;
using Kontract.Extensions;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Extensions;
using Kore.Generators;
using Kore.Managers.Plugins;
using Kore.Models.Update;
using Kore.Progress;
using Kore.Update;
using Kuriimu2.ImGui.Forms.Dialogs;
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
        private readonly FileManager _fileManager;

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
            _fileManager = LoadFileManager(_logger, _progress);

            #endregion

            #region Events

            Load += MainForm_Load;

            _fileManager.OnManualSelection += _fileManager_OnManualSelection;

            _includeDevBuildsButton.CheckChanged += _includeDevBuildsButton_CheckChanged;
            _changeLanguageMenu.SelectedItemChanged += _changeLanguageMenu_SelectedItemChanged;
            _changeThemeMenu.SelectedItemChanged += _changeThemeMenu_SelectedItemChanged;

            _openButton.Clicked += _openButton_Clicked;
            _openWithButton.Clicked += _openWithButton_Clicked;

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

            // Show plugin load errors
            // TODO: Change to small label at bottom of window "Some plugins couldn't be loaded."
            // TODO: Add menu bar button to show which plugins were loaded successfully and which did not and why
            await DisplayPluginErrors(_fileManager.LoadErrors);
        }

        protected override async Task OnClosingAsync(ClosingEventArgs e)
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
                        SaveAll(false).Wait();
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

        #endregion

        #region Change application settings

        private void _includeDevBuildsButton_CheckChanged(object sender, EventArgs e)
        {
            Settings.Default.IncludeDevBuilds = ((MenuBarCheckBox)sender).Checked;
            Settings.Default.Save();
        }

        private async void _changeLanguageMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var locale = LocalizationResources.GetLocaleByName(((MenuBarRadio)sender).SelectedItem.Caption);

            Settings.Default.Locale = locale;
            Settings.Default.Save();

            await MessageBox.ShowInformationAsync(LocalizationResources.ChangeLanguageTitleResource(),
                LocalizationResources.ChangeLanguageCaptionResource());

            // TODO: Change locale at runtime, when reloading all other strings as well
            //LocalizationResources.Instance.ChangeLocale(locale);
        }

        private void _changeThemeMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var theme = ((MenuBarRadio)sender).SelectedItem.Caption;

            if (!Enum.TryParse<Theme>(theme, out var parsedTheme))
                return;

            Settings.Default.Theme = theme;
            Settings.Default.Save();

            Theme = parsedTheme;
        }

        #endregion

        #region File manager

        private async void _fileManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            var selectedPlugin = await ChoosePlugin(e.FilePlugins.ToArray(), e.FilteredFilePlugins.ToArray(), e.SelectionStatus);
            if (selectedPlugin != null)
                e.Result = selectedPlugin;
        }

        #endregion

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

        private FileManager LoadFileManager(ILogger logger, ProgressContext progress)
        {
            var pluginPath = Path.Combine(GetBaseDirectory(), "plugins");

            return new FileManager(pluginPath)
            {
                AllowManualSelection = true,

                Progress = progress,
                // TODO: DialogManagerDialog
                //DialogManager = new DialogManagerDialog(this),
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
                ReportStatus(false, LocalizationResources.NoFileSelectedStatusResource());
                return;
            }

            await OpenPhysicalFiles(new[] { fileToOpen }, manualIdentification);
        }

        private async Task OpenPhysicalFiles(IList<string> filesToOpen, bool manualIdentification)
        {
            foreach (var fileToOpen in filesToOpen)
            {
                var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                    plugin == null ?
                        _fileManager.LoadFile(fileToOpen) :
                        _fileManager.LoadFile(fileToOpen, plugin.PluginId));
                var tabColor = Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256));

                await OpenFile(fileToOpen, manualIdentification, loadAction, tabColor);
            }
        }

        private async Task<bool> OpenFile(UPath filePath, bool manualIdentification, Func<IFilePlugin, Task<LoadResult>> loadFileFunc, Color tabColor)
        {
            ReportStatus(true, string.Empty);

            // Check if path is invalid
            if (filePath.IsNull || filePath.IsEmpty)
            {
                await MessageBox.ShowErrorAsync(LocalizationResources.LoadErrorCaptionResource(), LocalizationResources.SelectedFileInvalidResource());
                return false;
            }

            // Check if file is already loading
            if (_fileManager.IsLoading(filePath))
            {
                ReportStatus(false, LocalizationResources.FileAlreadyOpeningStatusResource(filePath));
                return true;
            }

            // Check if file is already opened
            if (_fileManager.IsLoaded(filePath))
            {
                var selectedTabPage = _stateDictionary[_fileManager.GetLoadedFile(filePath)].TabPage;
                _tabControl.SelectedPage = selectedTabPage;

                return true;
            }

            // Choose plugin
            IFilePlugin chosenPlugin = null;
            if (manualIdentification)
            {
                var allPlugins = _fileManager.GetFilePlugins().ToArray();

                chosenPlugin = await ChoosePlugin(allPlugins, allPlugins, SelectionStatus.All);
                if (chosenPlugin == null)
                {
                    ReportStatus(false, LocalizationResources.NoPluginSelectedStatusResource());
                    return false;
                }
            }

            // Load file
            var loadResult = await loadFileFunc(chosenPlugin);
            if (loadResult.IsCancelled)
            {
                // Load was canceled
                ReportStatus(false, LocalizationResources.LoadCancelledStatusResource());
                return false;
            }

            if (!loadResult.IsSuccessful)
            {
                await MessageBox.ShowErrorAsync(LocalizationResources.LoadErrorCaptionResource(), loadResult.Message);

                return false;
            }

            // Open tab page
            var wasAdded = await AddTabPage(loadResult.LoadedFileState, tabColor);
            if (!wasAdded)
            {
                _fileManager.Close(loadResult.LoadedFileState);
                return false;
            }

            UpdateFormTitle();

            return true;
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
            ReportStatus(true, string.Empty);

            // Check if file is already attempted to be saved
            if (_fileManager.IsSaving(fileState))
            {
                ReportStatus(false, LocalizationResources.FileAlreadySavingStatusResource(fileState.FilePath.ToRelative()));
                return false;
            }

            // Select save path if necessary
            var savePath = UPath.Empty;
            if (saveAs)
            {
                savePath = await SelectNewFile(fileState.FilePath.GetName());
                if (savePath.IsNull || savePath.IsEmpty)
                {
                    ReportStatus(false, LocalizationResources.SelectedFileInvalidResource());

                    return false;
                }
            }

            var saveResult = savePath.IsEmpty ?
                await _fileManager.SaveFile(fileState) :
                await _fileManager.SaveFile(fileState, savePath.FullName);

            if (!saveResult.IsSuccessful)
            {
                ReportStatus(false, LocalizationResources.FileNotSavedSuccessfullyStatusResource());

                await MessageBox.ShowErrorAsync(LocalizationResources.SaveErrorCaptionResource(), saveResult.Message);

                return false;
            }

            // Update current state form if enabled
            UpdateTab(fileState, invokeUpdateForm, false);

            // Update children
            UpdateChildrenTabs(fileState);

            // Update parents
            UpdateTab(fileState.ParentFileState, true);

            ReportStatus(true, LocalizationResources.FileSavedSuccessfullyStatusResource());

            return true;
        }

        #endregion

        #region Close

        private async Task<bool> CloseFile(IFileState fileState, bool ignoreChildWarning = false, bool ignoreChangesWarning = false, bool ignoreRunningOperations = false)
        {
            ReportStatus(true, string.Empty);

            // Check if operations are running
            if (!ignoreRunningOperations && _stateDictionary[fileState].Form.HasRunningOperations())
            {
                ReportStatus(false, LocalizationResources.OperationsStillRunningStatusResource());
                return false;
            }

            // Security question, so the user knows that every sub file will be closed
            if (fileState.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = await MessageBox.ShowYesNoAsync(LocalizationResources.DependantFilesCaptionResource(), LocalizationResources.DependantFilesResource());

                switch (result)
                {
                    case DialogResult.Yes:
                        break;

                    default:
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
                        await _fileManager.SaveFile(fileState);

                        // TODO: Somehow propagate save error to user?

                        break;

                    case DialogResult.No:
                        // Close state and tabs without doing anything
                        break;

                    default:
                        return false;
                }
            }

            // Collect all opened children states
            var childrenStates = CollectChildrenStates(fileState).ToArray();

            // Close all related states
            var parentState = fileState.ParentFileState;
            var closeResult = _fileManager.Close(fileState);

            // If closing of the state was not successful
            if (!closeResult.IsSuccessful)
            {
                ReportStatus(false, closeResult.Message);
                return false;
            }

            // Remove all tabs related to the state, if closing was successful
            foreach (var childState in childrenStates)
                CloseTab(childState);
            CloseTab(fileState);

            // Update parents before state is disposed
            UpdateTab(parentState, true);

            UpdateFormTitle();

            return true;
        }

        private Task<DialogResult> ConfirmSavingChanges(IFileState fileState = null)
        {
            var text = fileState == null ? LocalizationResources.UnsavedChangesGenericResource() : LocalizationResources.UnsavedChangesToFileResource(fileState.FilePath);
            return MessageBox.ShowYesNoCancelAsync(LocalizationResources.UnsavedChangesCaptionResource(), text);
        }

        private void CloseTab(IFileState fileState)
        {
            // We only close the tab related to the state itself, not its archive children
            // Closing archive children is done by CloseFile, to enable proper rollback if closing the state itself was unsuccessful
            if (!_stateDictionary.ContainsKey(fileState))
                return;

            var stateEntry = _stateDictionary[fileState];
            _tabDictionary.Remove(stateEntry.TabPage);

            _tabControl.RemovePage(stateEntry.TabPage);
            _stateDictionary.Remove(fileState);
        }

        #endregion

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
            stateEntry.TabPage.Title = (fileState.StateChanged ? "* " : "") + fileState.FilePath.GetName();

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

            sb.AppendLine(LocalizationResources.FollowingPluginsNotLoadedResource());

            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            await MessageBox.ShowErrorAsync(LocalizationResources.PluginsNotAvailableCaptionResource(), sb.ToString());
        }

        private async Task CheckForUpdate()
        {
            if (_localManifest == null)
                return;

            var platform = GetCurrentPlatform();

            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl_, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest, Settings.Default.IncludeDevBuilds))
                return;

            var result = await MessageBox.ShowYesNoAsync(LocalizationResources.UpdateAvailableCaptionResource(),
                LocalizationResources.UpdateAvailableResource(_localManifest.Version, _localManifest.BuildNumber, remoteManifest.Version, remoteManifest.BuildNumber));
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

            throw new InvalidOperationException(LocalizationResources.UnsupportedOperatingSystemExceptionResource(RuntimeInformation.OSDescription));
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

            throw new InvalidOperationException(LocalizationResources.UnsupportedOperatingSystemExceptionResource(RuntimeInformation.OSDescription));
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
            // TODO: Add FileName to SaveFileDialog
            var sfd = new SaveFileDialog();
            return await sfd.ShowAsync() == DialogResult.Ok ? sfd.SelectedPath : null;
        }

        private async Task<string> SelectFile()
        {
            var ofd = new OpenFileDialog { InitialDirectory = Settings.Default.LastDirectory };

            // Set file filters
            foreach (var filter in GetFileFilters(_fileManager.GetFilePluginLoaders()).OrderBy(x => x.Name))
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

        private async Task<IFilePlugin> ChoosePlugin(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            var pluginDialog = new ChoosePluginDialog(allFilePlugins, filteredFilePlugins, status);
            return await pluginDialog.ShowAsync() == DialogResult.Ok ? pluginDialog.SelectedPlugin : null;
        }

        private IList<FileFilter> GetFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<FileFilter>
            {
                new FileFilter(LocalizationResources.AllFilesFilterResource(), ".*")
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

                    //case IImageState _:
                    //    kuriimuForm = new ImageForm(new FormInfo<IImageState>(fileState, communicator, _progress, _logger));
                    //    break;

                    //case IArchiveState _:
                    //    kuriimuForm = new ArchiveForm(new ArchiveFormInfo(fileState, communicator, _progress, _logger), _fileManager);
                    //    break;

                    //case IHexState _:
                    //    kuriimuForm = new HexForm(new FormInfo<IHexState>(fileState, communicator, _progress, _logger));
                    //    break;

                    // TODO: Remove hex state dummy
                    case IHexState _:
                        kuriimuForm = null;
                        break;

                    default:
                        throw new InvalidOperationException(LocalizationResources.UnknownPluginStateResource(fileState.PluginState));
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message, e);
                await MessageBox.ShowInformationAsync(LocalizationResources.ExceptionCatchedCaptionResource(), e.Message);

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
            var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                pluginId == Guid.Empty ?
                    _fileManager.LoadFile(fileState, file) :
                    _fileManager.LoadFile(fileState, file, pluginId));
            var tabColor = _stateDictionary[fileState].TabColor;

            return OpenFile(absoluteFilePath, false, loadAction, tabColor);
        }

        public Task<bool> SaveFile(IFileState fileState, bool saveAs)
        {
            return SaveFile(fileState, saveAs, false);
        }

        public Task<bool> CloseFile(IFileState fileState, IArchiveFileInfo file)
        {
            var absolutePath = fileState.AbsoluteDirectory / fileState.FilePath / file.FilePath.ToRelative();
            if (!_fileManager.IsLoaded(absolutePath))
                return Task.FromResult(true);

            var loadedFile = _fileManager.GetLoadedFile(absolutePath);
            return CloseFile(loadedFile);
        }

        public void RenameFile(IFileState fileState, IArchiveFileInfo file, UPath newPath)
        {
            var absolutePath = fileState.AbsoluteDirectory / fileState.FilePath / file.FilePath.ToRelative();
            if (!_fileManager.IsLoaded(absolutePath))
                return;

            var loadedFile = _fileManager.GetLoadedFile(absolutePath);
            loadedFile.RenameFilePath(newPath);

            UpdateTab(loadedFile, true, false);
        }

        public void Update(IFileState fileState, bool updateParents, bool updateChildren)
        {
            UpdateTab(fileState, false, updateParents);
            if (updateChildren)
                UpdateChildrenTabs(fileState);
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            if (message == null)
                return;

            // TODO: Get color from style somehow
            var textColor = isSuccessful ? Color.ForestGreen : Color.Red;//isSuccessful ? Themer.Instance.GetTheme().AltColor : Themer.Instance.GetTheme().LogFatalColor;

            _statusText.Caption = message;
            _statusText.TextColor = textColor;
        }

        #endregion
    }
}
