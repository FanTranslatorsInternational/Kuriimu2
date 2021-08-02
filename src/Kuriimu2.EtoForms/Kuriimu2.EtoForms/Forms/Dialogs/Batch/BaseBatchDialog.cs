using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using Eto.Forms;
using Kontract;
using Kore.Batch;
using Kore.Extensions;
using Kore.Factories;
using Kore.Managers;
using Kore.Managers.Plugins;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Logging;
using Kuriimu2.EtoForms.Resources;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    abstract partial class BaseBatchDialog : Dialog
    {
        private readonly BaseBatchProcessor _batchProcessor;
        private readonly System.Timers.Timer _avgTimer;

        protected abstract string SourceEmptyText { get; }

        protected abstract string DestinationEmptyText { get; }

        protected ILogger Logger { get; }

        #region Localization Keys

        private const string AvgTimePerFileTimedKey_ = "AvgTimePerFileTimed";

        private const string BatchSelectPluginStatusKey_ = "BatchSelectPluginStatus";

        private const string UnsupportedOperatingSystemExceptionKey_ = "UnsupportedOperatingSystemException";

        #endregion

        public BaseBatchDialog(IInternalFileManager fileManager)
        {
            ContractAssertions.IsNotNull(fileManager, nameof(fileManager));

            InitializeComponent();

            Logger = new LoggerConfiguration()
                .WriteTo.Sink(new RichTextAreaSink(log))
                .WriteTo.File($"{GetBaseDirectory()}/Kuriimu2_Extensions.log")
                .CreateLogger();
            _batchProcessor = InitializeBatchProcessor(fileManager, Logger);

            _avgTimer = new System.Timers.Timer(300);
            _avgTimer.Elapsed += avgTimer_Elapsed;

            var loadedPlugins = LoadPlugins(fileManager);
            plugins.DataStore = loadedPlugins;

            plugins.SelectedIndex = 0;

            Content.DragEnter += Content_DragEnter;
            Content.DragDrop += Content_DragDrop;
            Closing += BaseBatchDialog_Closing;

            #region Commands

            selectInputCommand.Executed += SelectInputCommand_Executed;
            selectOutputCommand.Executed += SelectOutputCommand_Executed;
            executeCommand.Executed += ExecuteCommand_Executed;

            #endregion
        }

        #region Initialization

        protected abstract BaseBatchProcessor InitializeBatchProcessor(IInternalFileManager fileManager, ILogger logger);

        private IList<PluginElement> LoadPlugins(IInternalFileManager fileManager)
        {
            return fileManager.GetFilePlugins().Select(x => new PluginElement(x)).OrderBy(x => x.ToString()).ToArray();
        }

        #endregion

        #region Events

        private void avgTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var avgTime = _batchProcessor.AverageFileTime;
            Application.Instance.Invoke(() => timerLabel.Text = Localize(AvgTimePerFileTimedKey_, avgTime.Milliseconds));
        }

        private void SelectInputCommand_Executed(object sender, EventArgs e)
        {
            var selectedFolder = SelectFolder(Settings.Default.BatchInputDirectory);
            if (string.IsNullOrEmpty(selectedFolder))
                return;

            Settings.Default.BatchInputDirectory = selectedFolder;
            Settings.Default.Save();

            selectedInputPath.Text = selectedFolder;
        }

        private void SelectOutputCommand_Executed(object sender, EventArgs e)
        {
            var selectedFolder = SelectFolder(Settings.Default.BatchOutputDirectory);
            if (string.IsNullOrEmpty(selectedFolder))
                return;

            Settings.Default.BatchOutputDirectory = selectedFolder;
            Settings.Default.Save();

            selectedOutputPath.Text = selectedFolder;
        }

        private async void ExecuteCommand_Executed(object sender, EventArgs e)
        {
            _avgTimer.Start();
            await Execute();
            _avgTimer.Stop();
        }

        private void Content_DragDrop(object sender, DragEventArgs e)
        {
            var paths = e.Data.Uris.Select(x => HttpUtility.UrlDecode(x.AbsolutePath)).ToArray();

            var path = paths[0];
            selectedInputPath.Text = path;
        }

        private void Content_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragEffects.Copy;
        }

        private void BaseBatchDialog_Closing(object sender, CancelEventArgs e)
        {
            _batchProcessor.Cancel();
            _avgTimer.Stop();
        }

        #endregion

        #region Execution

        private async Task Execute()
        {
            log.Text = string.Empty;

            if (!VerifyInput())
                return;

            ToggleUi(false);

            var selectedPlugin = (PluginElement)plugins.SelectedValue;

            _batchProcessor.Plugin = selectedPlugin.IsEmpty ? null : selectedPlugin.Plugin;
            _batchProcessor.ScanSubDirectories = subDirectoryBox.Checked ?? false;

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(selectedInputPath.Text, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(selectedOutputPath.Text, new StreamManager());

            await _batchProcessor.Process(sourceFileSystem, destinationFileSystem);

            ToggleUi(true);
        }

        private bool VerifyInput()
        {
            // HINT: Those log messages get localized, since they are directly output to the user.
            if (plugins.SelectedIndex < 0)
            {
                Logger.Error(Localize(BatchSelectPluginStatusKey_));
                return false;
            }

            if (string.IsNullOrEmpty(selectedInputPath.Text))
            {
                Logger.Error(SourceEmptyText);
                return false;
            }

            if (string.IsNullOrEmpty(selectedOutputPath.Text))
            {
                Logger.Error(DestinationEmptyText);
                return false;
            }

            return true;
        }

        private void ToggleUi(bool toggle)
        {
            plugins.Enabled = toggle;
            subDirectoryBox.Enabled = toggle;
            executeCommand.Enabled = selectInputCommand.Enabled = selectOutputCommand.Enabled = toggle;
        }

        #endregion

        #region Support

        protected string Localize(string name, params object[] args)
        {
            return string.Format(Application.Instance.Localize(this, name), args);
        }

        private string SelectFolder(string selectedPath)
        {
            var sfd = new SelectFolderDialog
            {
                Directory = selectedPath
            };

            return sfd.ShowDialog(this) != DialogResult.Ok ? string.Empty : sfd.Directory;
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

        #endregion
    }
}
