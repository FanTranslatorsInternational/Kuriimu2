using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using Kuriimu2.EtoForms.Exceptions;
using Kuriimu2.EtoForms.Forms.Dialogs;
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
        private readonly IDialogManager _dialogs;
        private readonly PluginManager _pluginManager;

        private readonly IList<string> _openingFiles = new List<string>();
        private readonly IList<string> _savingFiles = new List<string>();

        private readonly IDictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)> _stateDictionary =
            new Dictionary<IStateInfo, (IKuriimuForm KuriimuForm, TabPage TabPage, Color TabColor)>();
        private readonly IDictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)> _tabDictionary =
            new Dictionary<TabPage, (IKuriimuForm KuriimuForm, IStateInfo StateInfo, Color TabColor)>();

        private readonly Manifest _localManifest;

        #region Constants

        private const string LoadError = "Load Error";
        private const string InvalidFile = "The selected file is invalid.";
        private const string NoPluginSelected = "No plugin was selected.";

        private const string ExceptionCatched = "Exception catched";
        private const string PluginsNotAvailable = "Plugins not available";

        private const string FormTitle = "Kuriimu2 {0}";
        private const string FormTitlePlugin = "Kuriimu2 {0} - {1} - {2} - {3}";

        private const string MenuDeleteResource = "Kuriimu2.EtoForms.Images.menu-delete.png";
        private const string ManifestResource = "Kuriimu2.EtoForms.Resources.version.json";

        #endregion

        // ReSharper disable once UseObjectOrCollectionInitializer
        public MainForm()
        {
            InitializeComponent();

            _localManifest = LoadLocalManifest();
            UpdateFormText();

            // TODO: Implement dialog manager
            // TODO: Introduce settings like in WinForms
            _progress = new ProgressContext(new Kuriimu2ProgressBarOutput(progressBar, 300));
            _dialogs = new DefaultDialogManager();
            _pluginManager = new PluginManager(_progress, _dialogs, "plugins");
            _pluginManager.AllowManualSelection = true;

            if (_pluginManager.LoadErrors.Any())
                DisplayPluginErrors(_pluginManager.LoadErrors);

            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;

            #region Set Command delegates

            openFileCommand.Executed += OpenFileCommand_Executed;
            openFileWithCommand.Executed += OpenFileWithCommand_Executed;

            #endregion
        }

        #region Open File

        private void OpenFileCommand_Executed(object sender, EventArgs e)
        {
            OpenPhysicalFile(false);
        }

        private void OpenFileWithCommand_Executed(object sender, EventArgs e)
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
                    MessageBox.Show(NoPluginSelected, LoadError, MessageBoxButtons.OK, MessageBoxType.Error);
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

            Application.Instance.Invoke(() => tabPage.Image = new Icon(new IconFrame(1, Bitmap.FromResource(MenuDeleteResource))));

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

        #endregion

        #region Drag & Drop

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            OpenPhysicalFiles(e.Data.Uris.Select(x => x.AbsolutePath).ToArray(), false);
        }

        #endregion

        #region Support

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
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ManifestResource);
            if (resourceStream == null)
                return null;

            return JsonConvert.DeserializeObject<Manifest>(new StreamReader(resourceStream).ReadToEnd());
        }

        private IFilePlugin ChoosePlugin(IReadOnlyList<IFilePlugin> filePlugins)
        {
            var pluginDialog = new ChoosePluginDialog(filePlugins);
            pluginDialog.ShowModal(this);

            return pluginDialog.SelectedFilePlugin;
        }

        #endregion

        #region Form Communication

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
            throw new NotImplementedException();
        }

        public Task<bool> CloseFile(IStateInfo stateInfo, IArchiveFileInfo file)
        {
            throw new NotImplementedException();
        }

        public void RenameFile(IStateInfo stateInfo, IArchiveFileInfo file, UPath newPath)
        {
            throw new NotImplementedException();
        }

        public void Update(IStateInfo stateInfo, bool updateParents, bool updateChildren)
        {
            throw new NotImplementedException();
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            if (message == null)
                return;

            var textColor = isSuccessful ? KnownColors.Black : KnownColors.DarkRed;

            statusMessage.Text = message;
            statusMessage.TextColor = textColor;
        }

        private IArchiveFormCommunicator CreateFormCommunicator(IStateInfo stateInfo)
        {
            var communicator = new FormCommunicator(stateInfo, this);
            return communicator;
        }

        #endregion
    }
}
