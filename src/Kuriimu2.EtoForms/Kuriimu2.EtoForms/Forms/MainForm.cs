using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Progress;
using Kuriimu2.EtoForms.Resources;
using Kuriimu2.EtoForms.Support;
using Newtonsoft.Json;
using Serilog;

namespace Kuriimu2.EtoForms.Forms
{
    public partial class MainForm : Form, IMainForm
    {
        private readonly Random _rand = new Random();

        private readonly IProgressContext _progress;
        private readonly string _logFile;
        private readonly ILogger _logger;
        private readonly FileManager _fileManager;

        private readonly IDictionary<IFileState, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)> _stateDictionary =
            new Dictionary<IFileState, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)>();
        private readonly IDictionary<TabPage, (IKuriimuForm KuriimuForm, IFileState StateInfo, Color TabColor)> _tabDictionary =
            new Dictionary<TabPage, (IKuriimuForm KuriimuForm, IFileState StateInfo, Color TabColor)>();

        private readonly Manifest _localManifest;

        private Localizer _localizer;

        #region Hot Keys

        private const Keys OpenHotKey = Keys.Control | Keys.O;
        private const Keys OpenWithHotKey = Keys.Control | Keys.Shift | Keys.O;
        private const Keys SaveAllHotKey = Keys.Control | Keys.Shift | Keys.S;

        #endregion

        #region Constants

        private const string ManifestUrl = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-EtoForms-Update/main/{0}/manifest.json";
        private const string ApplicationType = "EtoForms.{0}";

        private const string ManifestResourceName = "Kuriimu2.EtoForms.Resources.version.json";

        private const string FormTitle = "Kuriimu2 {0}-{1}";
        private const string FormTitlePlugin = "Kuriimu2 {0}-{1} - {2} - {3} - {4}";

        #endregion

        #region Localization Keys

        private const string ChangeLanguageCaptionKey_ = "ChangeLanguageCaption";
        private const string ChangeLanguageTitleKey_ = "ChangeLanguageTitle";

        private const string LoadErrorCaptionKey_ = "LoadErrorCaption";
        private const string SaveErrorCaptionKey_ = "SaveErrorCaption";
        private const string UnsavedChangesCaptionKey_ = "UnsavedChangesCaption";
        private const string DependantFilesCaptionKey_ = "DependantFilesCaption";
        private const string ExceptionCatchedCaptionKey_ = "ExceptionCatchedCaption";
        private const string UnhandledExceptionCaptionKey_ = "UnhandledExceptionCaption";

        private const string LoadCancelledStatusKey_ = "LoadCancelledStatus";
        private const string NoPluginSelectedStatusKey_ = "NoPluginSelectedStatus";
        private const string NoFileSelectedStatusKey_ = "NoFileSelectedStatus";
        private const string FileAlreadyOpeningStatusKey_ = "FileAlreadyOpeningStatus";
        private const string FileAlreadySavingStatusKey_ = "FileAlreadySavingStatus";
        private const string FileSuccessfullyLoadedKey_ = "FileSuccessfullyLoaded";
        private const string FileNotSuccessfullyLoadedKey_ = "FileNotSuccessfullyLoaded";
        private const string FileSavedSuccessfullyStatusKey_ = "FileSavedSuccessfullyStatus";
        private const string FileNotSavedSuccessfullyStatusKey_ = "FileNotSavedSuccessfullyStatus";
        private const string OperationsStillRunningStatusKey_ = "OperationsStillRunningStatus";
        private const string UpdateAvailableCaptionKey_ = "UpdateAvailableCaption";

        private const string PluginsNotAvailableCaptionKey_ = "PluginsNotAvailableCaption";
        private const string SelectedFileInvalidKey_ = "SelectedFileInvalid";
        private const string UnsavedChangesToFileKey_ = "UnsavedChangesToFile";
        private const string UnsavedChangesGenericKey_ = "UnsavedChangesGeneric";
        private const string DependantFilesKey_ = "DependantFiles";
        private const string UnhandledExceptionCloseAppKey_ = "UnhandledExceptionCloseApp";
        private const string UnhandledExceptionNotCloseAppKey_ = "UnhandledExceptionNotCloseApp";
        private const string UpdateAvailableKey_ = "UpdateAvailable";
        private const string FollowingPluginsNotLoadedKey_ = "FollowingPluginsNotLoaded";

        private const string AllFilesFilterKey_ = "AllFilesFilter";

        private const string UnsupportedOperatingSystemExceptionKey_ = "UnsupportedOperatingSystemException";
        private const string UnsupportedPlatformExceptionKey_ = "UnsupportedPlatformException";

        #endregion

        // ReSharper disable once UseObjectOrCollectionInitializer
        public MainForm()
        {
            Themer.Instance.LoadThemes();
            this.BackgroundColor = Themer.Instance.GetTheme().WindowBackColor;

            _localizer = InitializeLocalizer();
            Application.Instance.LocalizeString += Instance_LocalizeString;

            InitializeComponent();

            Application.Instance.UnhandledException += MainForm_UnhandledException;

            _localManifest = LoadLocalManifest();
            UpdateFormText();

            _logFile = $"{GetBaseDirectory()}/Kuriimu2.log";
            _logger = new LoggerConfiguration().WriteTo.File(_logFile).CreateLogger();
            _progress = new ProgressContext(new ProgressBarExOutput(_progressBarEx, 20));
            _fileManager = new FileManager($"{GetBaseDirectory()}/plugins")
            {
                AllowManualSelection = true,

                Progress = _progress,
                DialogManager = new DialogManagerDialog(this),
                Logger = _logger
            };
            _fileManager.OnManualSelection += fileManager_OnManualSelection;

            if (_fileManager.LoadErrors.Any())
                DisplayPluginErrors(_fileManager.LoadErrors);

            // HINT: The form cannot directly handle DragDrop for some reason and needs a catalyst (on every platform beside WinForms)
            // HINT: Some kind of form spanning control, which handles the drop action instead
            // HINT: https://github.com/picoe/Eto/issues/1852
            Content.DragEnter += mainForm_DragEnter;
            Content.DragDrop += mainForm_DragDrop;

            Content.Load += mainForm_Load;
            Closing += mainForm_Closing;

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

            openRawImageViewerCommand.Executed += openRawImageViewerCommand_Executed;

            includeDevBuildCommand.Executed += IncludeDevBuildCommand_Executed;

            englishCommand.Executed += (sender, args) => ChangeLocale("en");
            germanCommand.Executed += (sender, args) => ChangeLocale("de");
            danishCommand.Executed += (sender, args) => ChangeLocale("da");
            russianCommand.Executed += (sender, args) => ChangeLocale("ru");
            simpleChineseCommand.Executed += (sender, args) => ChangeLocale("zh");

            LightThemeCommand.Executed += (sender, args) => Themer.Instance.ChangeTheme("light");
            DarkThemeCommand.Executed += (sender, args) => Themer.Instance.ChangeTheme("dark");

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
                ReportStatus(false, Localize(NoFileSelectedStatusKey_));
                return;
            }

            OpenPhysicalFiles(new[] { fileToOpen }, manualIdentification);
        }

        private async void OpenPhysicalFiles(IList<string> filesToOpen, bool manualIdentification)
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
                MessageBox.Show(Localize(SelectedFileInvalidKey_), Localize(LoadErrorCaptionKey_), MessageBoxButtons.OK, MessageBoxType.Error);
                return false;
            }

            // Check if file is already loading
            if (_fileManager.IsLoading(filePath))
            {
                ReportStatus(false, Localize(FileAlreadyOpeningStatusKey_, filePath));
                return true;
            }

            // Check if file is already opened
            if (_fileManager.IsLoaded(filePath))
            {
                var selectedTabPage = _stateDictionary[_fileManager.GetLoadedFile(filePath)].TabPage;
                Application.Instance.Invoke(() => tabControl.SelectedPage = selectedTabPage);

                return true;
            }

            // Choose plugin
            IFilePlugin chosenPlugin = null;
            if (manualIdentification)
            {
                var allPlugins = _fileManager.GetFilePlugins().ToArray();

                chosenPlugin = ChoosePlugin(allPlugins, allPlugins, SelectionStatus.All);
                if (chosenPlugin == null)
                {
                    ReportStatus(false, Localize(NoPluginSelectedStatusKey_));
                    return false;
                }
            }

            // Load file
            var loadResult = await loadFileFunc(chosenPlugin);
            if (loadResult.IsCancelled)
            {
                // Load was canceled
                ReportStatus(false, Localize(LoadCancelledStatusKey_));
                return false;
            }

            if (!loadResult.IsSuccessful)
            {
#if DEBUG
                var message = loadResult.Exception?.ToString() ?? loadResult.Message;
#else
                var message = loadResult.Message;
#endif

                MessageBox.Show(message, Localize(LoadErrorCaptionKey_), MessageBoxButtons.OK, MessageBoxType.Error);
                return false;
            }

            // Open tab page
            var wasAdded = Application.Instance.Invoke(() => AddTabPage(loadResult.LoadedFileState, tabColor));
            if (!wasAdded)
            {
                _fileManager.Close(loadResult.LoadedFileState);
                return false;
            }

            Application.Instance.Invoke(UpdateFormText);

            return true;
        }

        private bool AddTabPage(IFileState fileState, Color tabColor)
        {
            var communicator = CreateFormCommunicator(fileState);

            IKuriimuForm kuriimuForm;
            try
            {
                switch (fileState.PluginState)
                {
#if DEBUG

                    case ITextState _:
                        kuriimuForm = new TextForm(new FormInfo<ITextState>(fileState, communicator, _progress, _logger));
                        break;

#endif

                    case IImageState _:
                        kuriimuForm = new ImageForm(new FormInfo<IImageState>(fileState, communicator, _progress, _logger));
                        break;

                    case IArchiveState _:
                        kuriimuForm = new ArchiveForm(new ArchiveFormInfo(fileState, communicator, _progress, _logger), _fileManager);
                        break;

                    case IHexState _:
                        kuriimuForm = new HexForm(new FormInfo<IHexState>(fileState, communicator, _progress, _logger));
                        break;

                    default:
                        throw new UnknownPluginStateException(fileState.PluginState);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(e.ToString(), Localize(ExceptionCatchedCaptionKey_));
#else
                MessageBox.Show(e.Message, Localize(ExceptionCatchedCaptionKey_));
#endif

                return false;
            }

            // Create new tab page
            var tabPage = new TabPage((Panel)kuriimuForm)
            {
                Padding = new Padding(0, 2, 2, 1),
                Text = fileState.FilePath.ToRelative().GetName()
            };

            // Add tab page to tab control
            Application.Instance.Invoke(() => tabControl.Pages.Add(tabPage));

            _stateDictionary[fileState] = (kuriimuForm, tabPage, tabColor);
            _tabDictionary[tabPage] = (kuriimuForm, fileState, tabColor);

            Application.Instance.Invoke(() =>
            {
                tabPage.Image = new Icon(new IconFrame(1, ImageResources.Actions.Close));
                tabPage.MouseUp += tabPage_MouseUp;
            });

            // Select tab page in tab control
            Application.Instance.Invoke(() => tabControl.SelectedPage = tabPage);

            UpdateTab(fileState);

            return true;
        }

        private string SelectFile()
        {
            var ofd = new OpenFileDialog();

            foreach (var filter in GetFileFilters(_fileManager.GetFilePluginLoaders()))
                ofd.Filters.Add(filter);

            return ofd.ShowDialog(this) == DialogResult.Ok ? ofd.FileName : null;
        }

        private IList<FileFilter> GetFileFilters(IPluginLoader<IFilePlugin>[] pluginLoaders)
        {
            var filters = new List<FileFilter>
            {
                new FileFilter(Localize(AllFilesFilterKey_), ".*")
            };

            foreach (var plugin in pluginLoaders.SelectMany(x => x.Plugins).Where(x => x.FileExtensions != null))
            {
                var pluginName = plugin.Metadata?.Name ?? plugin.GetType().Name;
                filters.Add(new FileFilter(pluginName, plugin.FileExtensions.Select(x => x.Replace("*", "")).ToArray()));
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

        private async Task<bool> SaveFile(IFileState fileState, bool saveAs, bool invokeUpdateForm)
        {
            ReportStatus(true, string.Empty);

            // Check if file is already attempted to be saved
            if (_fileManager.IsSaving(fileState))
            {
                ReportStatus(false, Localize(FileAlreadySavingStatusKey_, fileState.FilePath.ToRelative()));
                return false;
            }

            // Select save path if necessary
            var savePath = UPath.Empty;
            if (saveAs)
            {
                savePath = SelectNewFile(fileState.FilePath.GetName());
                if (savePath.IsNull || savePath.IsEmpty)
                {
                    ReportStatus(false, Localize(SelectedFileInvalidKey_));

                    return false;
                }
            }

            var saveResult = savePath.IsEmpty ?
                await _fileManager.SaveFile(fileState) :
                await _fileManager.SaveFile(fileState, savePath.FullName);

            if (!saveResult.IsSuccessful)
            {
#if DEBUG
                var message = saveResult.Exception?.ToString() ?? saveResult.Message;
#else
                var message = saveResult.Message;
#endif

                ReportStatus(false, Localize(FileNotSavedSuccessfullyStatusKey_));
                MessageBox.Show(message, Localize(SaveErrorCaptionKey_), MessageBoxButtons.OK, MessageBoxType.Error);

                return false;
            }

            // Update current state form if enabled
            UpdateTab(fileState, invokeUpdateForm, false);

            // Update children
            UpdateChildrenTabs(fileState);

            // Update parents
            UpdateTab(fileState.ParentFileState, true);

            ReportStatus(true, Localize(FileSavedSuccessfullyStatusKey_));

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

        private async Task<bool> CloseFile(IFileState fileState, bool ignoreChildWarning = false, bool ignoreChangesWarning = false, bool ignoreRunningOperations = false)
        {
            ReportStatus(true, string.Empty);

            // Check if operations are running
            if (!ignoreRunningOperations && _stateDictionary[fileState].KuriimuForm.HasRunningOperations())
            {
                ReportStatus(false, Localize(OperationsStillRunningStatusKey_));
                return false;
            }

            // Security question, so the user knows that every sub file will be closed
            if (fileState.ArchiveChildren.Any() && !ignoreChildWarning)
            {
                var result = MessageBox.Show(Localize(DependantFilesKey_), Localize(DependantFilesCaptionKey_), MessageBoxButtons.YesNo);

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
                var result = ConfirmSavingChanges(fileState);
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

            Application.Instance.Invoke(UpdateFormText);

            return true;
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

        private void CloseTab(IFileState fileState)
        {
            // We only close the tab related to the state itself, not its archive children
            // Closing archive children is done by CloseFile, to enable proper rollback if closing the state itself was unsuccessful
            if (!_stateDictionary.ContainsKey(fileState))
                return;

            var stateEntry = _stateDictionary[fileState];
            _tabDictionary.Remove(stateEntry.TabPage);

            tabControl.Pages.Remove(stateEntry.TabPage);
            _stateDictionary.Remove(fileState);
            stateEntry.TabPage.Dispose();
        }

        private DialogResult ConfirmSavingChanges(IFileState fileState = null)
        {
            var text = fileState == null ? Localize(UnsavedChangesGenericKey_) : Localize(UnsavedChangesToFileKey_, fileState.FilePath);
            return MessageBox.Show(text, Localize(UnsavedChangesCaptionKey_), MessageBoxButtons.YesNoCancel);
        }

        #endregion

        #region Update

        private void UpdateFormText()
        {
            if (tabControl.Pages.Count <= 0 || tabControl.SelectedIndex < 0)
            {
                Title = string.Format(FormTitle, _localManifest.Version, _localManifest.BuildNumber);
                return;
            }

            var stateEntry = _tabDictionary[tabControl.SelectedPage];

            var pluginAssemblyName = ((UPath)stateEntry.StateInfo.PluginState.GetType().Assembly.Location).GetName();
            var pluginName = stateEntry.StateInfo.FilePlugin.Metadata.Name;
            var pluginId = stateEntry.StateInfo.FilePlugin.PluginId;

            Title = string.Format(FormTitlePlugin, _localManifest.Version, _localManifest.BuildNumber, pluginAssemblyName, pluginName, pluginId.ToString("D"));
        }

        private void UpdateTab(IFileState fileState, bool invokeUpdateForm = false, bool iterateParents = true)
        {
            if (fileState == null || !_stateDictionary.ContainsKey(fileState))
                return;

            // Update this tab pages information
            var stateEntry = _stateDictionary[fileState];
            Application.Instance.Invoke(() => stateEntry.TabPage.Text = (fileState.StateChanged ? "* " : "") + fileState.FilePath.GetName());

            // If the call was not made by the requesting state, propagate an update action to it
            if (invokeUpdateForm)
                stateEntry.KuriimuForm.UpdateForm();

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

        #region Events

        #region Main Form

        private void mainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragEffects.Copy;
        }

        private void mainForm_DragDrop(object sender, DragEventArgs e)
        {
            // Filter out directory paths and only keep file paths
            var paths = e.Data.Uris.Select(x => HttpUtility.UrlDecode(x.AbsolutePath)).Where(p => !Directory.Exists(p)).ToArray();
            OpenPhysicalFiles(paths, false);
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
#if !DEBUG
            CheckForUpdate();
#endif
        }

        private void mainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_stateDictionary.Keys.Any(x => x.StateChanged))
            {
                var result = ConfirmSavingChanges();
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
                if (CloseFile(stateInfo, true, true, true).Result)
                    continue;

                e.Cancel = true;
                break;
            }
        }

        private void MainForm_UnhandledException(object sender, Eto.UnhandledExceptionEventArgs e)
        {
            // HINT: Logging messages do not get localized!
            _logger.Fatal((Exception)e.ExceptionObject, "An unhandled exception occurred.");

            if (e.IsTerminating)
                MessageBox.Show(Localize(UnhandledExceptionCloseAppKey_), Localize(UnhandledExceptionCaptionKey_), MessageBoxType.Error);
            else
                ReportStatus(false, Localize(UnhandledExceptionNotCloseAppKey_));
        }

        private void Instance_LocalizeString(object sender, LocalizeEventArgs e)
        {
            e.LocalizedText = _localizer.GetLocalization(e.Text.Replace("&", ""));
        }

        #endregion

        #region Tools

        private void openBatchExtractorCommand_Executed(object sender, EventArgs e)
        {
            new BatchExtractDialog(_fileManager).ShowModal(this);
        }

        private void openBatchInjectorCommand_Executed(object sender, EventArgs e)
        {
            new BatchInjectDialog(_fileManager).ShowModal(this);
        }

        private void openTextSequenceSearcherCommand_Execute(object sender, EventArgs e)
        {
            new SequenceSearcherDialog().ShowModal(this);
        }

        #endregion

        #region Extensions

        private void openHashCommand_Executed(object sender, EventArgs e)
        {
            new HashExtensionDialog().ShowModal(this);
        }

        private void openCompressionCommand_Executed(object sender, EventArgs e)
        {
            new CompressExtensionDialog().ShowModal(this);
        }

        private void openDecompressionCommand_Executed(object sender, EventArgs e)
        {
            new DecompressExtensionDialog().ShowModal(this);
        }

        private void openEncryptionCommand_Executed(object sender, EventArgs e)
        {
            new EncryptExtensionsDialog().ShowModal(this);
        }

        private void openDecryptionCommand_Executed(object sender, EventArgs e)
        {
            new DecryptExtensionDialog().ShowModal(this);
        }

        #endregion

        private void openRawImageViewerCommand_Executed(object sender, EventArgs e)
        {
            new RawImageDialog().ShowModal(this);
        }

        private void IncludeDevBuildCommand_Executed(object sender, EventArgs e)
        {
            Settings.Default.IncludeDevBuilds = !Settings.Default.IncludeDevBuilds;
            Settings.Default.Save();
        }

        private void ChangeLocale(string locale)
        {
            Settings.Default.Locale = locale;
            Settings.Default.Save();

            MessageBox.Show(Localize(ChangeLanguageCaptionKey_), Localize(ChangeLanguageTitleKey_));
        }

        private void fileManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            var selectedPlugin = ChoosePlugin(e.FilePlugins.ToArray(), e.FilteredFilePlugins.ToArray(), e.SelectionStatus);
            if (selectedPlugin != null)
                e.Result = selectedPlugin;
        }

        private async void tabPage_MouseUp(object sender, MouseEventArgs e)
        {
            if (!e.Buttons.HasFlag(MouseButtons.Primary) && !e.Buttons.HasFlag(MouseButtons.Middle))
                return;

            var page = (TabPage)sender;
            if (page == null)
                return;

            // ISSUE: For multiple rows of tabs, primary clicking a tab not in the bottom row, will move the row the clicked tab is in to the bottom
            // However, the mouse or click event will be invoked on the tab overlapping the mouse position AFTER the row was moved to the bottom, leading to the event being invoked on the wrong sender
            // The event is invoked AFTER the SelectedPage of the TabControl was updated, so we circumvent the issue by retrieving the SelectedPage from the TabControl here.
            // We also need to adjust the mouse location, since it is relative to the wrong sender as well.
            if (Platform.IsWpf && e.Buttons.HasFlag(MouseButtons.Primary))
            {
                e = new MouseEventArgs(e.Buttons, e.Modifiers, new PointF(tabControl.SelectedPage.PointFromScreen(page.PointToScreen(e.Location)).X, e.Location.Y), e.Delta, e.Pressure);
                page = tabControl.SelectedPage;
            }

            var tabEntry = _tabDictionary[page];
            var parentStateInfo = tabEntry.StateInfo.ParentFileState;

            if (e.Buttons.HasFlag(MouseButtons.Middle))
            {
                if (!new Rectangle(page.Bounds.Size).Contains((Point)e.Location))
                    return;
            }

            if (e.Buttons.HasFlag(MouseButtons.Primary))
            {
                UpdateFormText();

                // There's 5 pixels of spacing on the right side of the close icon
                var textPosition = (page.Width - SystemFonts.Default().MeasureString(page.Text).Width) / 2;
                var closeButtonRect = new RectangleF(textPosition - page.Image.Width + 5, 4, page.Image.Width - 3, page.Image.Height);
                if (!closeButtonRect.Contains(e.Location))
                    return;
            }

            // Select parent tab
            TabPage parentTab = null;
            if (parentStateInfo != null && _stateDictionary.ContainsKey(parentStateInfo))
                parentTab = _stateDictionary[parentStateInfo].TabPage;

            // Close file
            if (!await CloseFile(tabEntry.StateInfo))
                return;

            // Switch to parent tab
            if (parentTab != null)
                tabControl.SelectedPage = parentTab;
        }

        #endregion

        #region Support

        private void CheckForUpdate()
        {
            if (_localManifest == null)
                return;

            var platform = GetCurrentPlatform();

            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest, Settings.Default.IncludeDevBuilds))
                return;

            var result = MessageBox.Show(
                    Localize(UpdateAvailableKey_, _localManifest.Version, _localManifest.BuildNumber, remoteManifest.Version, remoteManifest.BuildNumber),
                    Localize(UpdateAvailableCaptionKey_), MessageBoxButtons.YesNo);
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

        private string GetBaseDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ".";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "~/Applications/Kuriimu2";

            throw new InvalidOperationException(Localize(UnsupportedOperatingSystemExceptionKey_, RuntimeInformation.OSDescription));
        }

        private string GetCurrentPlatform()
        {
            if (Application.Instance.Platform.IsGtk)
                return "Gtk";

            if (Application.Instance.Platform.IsWpf)
                return "Wpf";

            if (Application.Instance.Platform.IsMac)
                return "Mac";

            throw new InvalidOperationException(Localize(UnsupportedPlatformExceptionKey_, Application.Instance.Platform.ID));
        }

        private Localizer InitializeLocalizer()
        {
            if (string.IsNullOrEmpty(Settings.Default.Locale))
            {
                Settings.Default.Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                Settings.Default.Save();
            }

            return new Localizer(Settings.Default.Locale);
        }

        private string Localize(string name, params object[] args)
        {
            return string.Format(Application.Instance.Localize(this, name), args);
        }

        private void DisplayPluginErrors(IReadOnlyList<PluginLoadError> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Localize(FollowingPluginsNotLoadedKey_) + ":");
            foreach (var error in errors)
                sb.AppendLine(error.AssemblyPath);

            MessageBox.Show(sb.ToString(), Localize(PluginsNotAvailableCaptionKey_), MessageBoxButtons.OK, MessageBoxType.Error);
        }

        private Manifest LoadLocalManifest()
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ManifestResourceName);
            if (resourceStream == null)
                return null;

            return JsonConvert.DeserializeObject<Manifest>(new StreamReader(resourceStream).ReadToEnd());
        }

        private IFilePlugin ChoosePlugin(IList<IFilePlugin> allFilePlugins, IList<IFilePlugin> filteredFilePlugins, SelectionStatus status)
        {
            return Application.Instance.Invoke(() =>
            {
                var pluginDialog = new ChoosePluginDialog(allFilePlugins, filteredFilePlugins, status);
                return pluginDialog.ShowModal(this);
            });
        }

        #endregion

        #region Form Communication

        private IArchiveFormCommunicator CreateFormCommunicator(IFileState fileState)
        {
            var communicator = new FormCommunicator(fileState, this);
            return communicator;
        }

        public Task<bool> OpenFile(IFileState fileState, IArchiveFileInfo file, Guid pluginId)
        {
            var absoluteFilePath = fileState.AbsoluteDirectory / fileState.FilePath.ToRelative() / file.FilePath.ToRelative();
            var loadAction = new Func<IFilePlugin, Task<LoadResult>>(plugin =>
                pluginId == Guid.Empty ?
                    _fileManager.LoadFile(fileState, file) :
                    _fileManager.LoadFile(fileState, file, pluginId));
            var tabColor = _stateDictionary[fileState].TabColor;

            return Task.Run(() => OpenFile(absoluteFilePath, false, loadAction, tabColor));
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

            var textColor = isSuccessful ? Themer.Instance.GetTheme().AltColor : Themer.Instance.GetTheme().LogFatalColor;

            Application.Instance.Invoke(() =>
            {
                statusMessage.Text = message;
                statusMessage.TextColor = textColor;
            });
        }

        #endregion
    }
}
