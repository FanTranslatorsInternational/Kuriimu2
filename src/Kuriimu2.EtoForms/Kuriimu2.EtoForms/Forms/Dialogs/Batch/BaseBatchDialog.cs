using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Eto.Forms;
using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;
using Kore.Batch;
using Kore.Extensions;
using Kore.Factories;
using Kore.Logging;
using Kore.Managers;
using Kore.Managers.Plugins;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Logging;
using Kuriimu2.EtoForms.Resources;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    abstract partial class BaseBatchDialog : Dialog
    {
        private readonly BaseBatchProcessor _batchProcessor;
        private readonly System.Timers.Timer _avgTimer;

        protected abstract string SourceEmptyText { get; }

        protected abstract string DestinationEmptyText { get; }

        protected IConcurrentLogger Logger { get; }

        public BaseBatchDialog(IInternalPluginManager pluginManager)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));

            InitializeComponent();

            Logger = new ConcurrentLogger(ApplicationLevel.Ui, new RichTextAreaLogOutput(log));
            _batchProcessor = InitializeBatchProcessor(pluginManager, Logger);

            _avgTimer = new System.Timers.Timer(300);
            _avgTimer.Elapsed += avgTimer_Elapsed;

            var loadedPlugins = LoadPlugins(pluginManager);
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

        protected abstract BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, IConcurrentLogger logger);

        private IList<PluginElement> LoadPlugins(IInternalPluginManager pluginManager)
        {
            return pluginManager.GetFilePlugins().Select(x => new PluginElement(x)).ToArray();
        }

        #endregion

        #region Events

        private void avgTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var avgTime = _batchProcessor.AverageFileTime;
            Application.Instance.Invoke(() => timerLabel.Text = "Avg time per file: " + avgTime.Milliseconds + "ms");
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
            Logger.StartLogging();

            if (!VerifyInput())
            {
                Logger.StopLogging();
                return;
            }

            ToggleUi(false);

            var selectedPlugin = (PluginElement)plugins.SelectedValue;

            _batchProcessor.PluginId = selectedPlugin.IsEmpty ? Guid.Empty : selectedPlugin.Plugin.PluginId;
            _batchProcessor.ScanSubDirectories = subDirectoryBox.Checked ?? false;

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(selectedInputPath.Text, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(selectedOutputPath.Text, new StreamManager());

            await _batchProcessor.Process(sourceFileSystem, destinationFileSystem);

            ToggleUi(true);
        }

        private bool VerifyInput()
        {
            if (plugins.SelectedIndex < 0)
            {
                Logger.QueueMessage(LogLevel.Error, "Select a plugin entry.");
                return false;
            }

            if (string.IsNullOrEmpty(selectedInputPath.Text))
            {
                Logger.QueueMessage(LogLevel.Error, SourceEmptyText);
                return false;
            }

            if (string.IsNullOrEmpty(selectedOutputPath.Text))
            {
                Logger.QueueMessage(LogLevel.Error, DestinationEmptyText);
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

        private string SelectFolder(string selectedPath)
        {
            var sfd = new SelectFolderDialog
            {
                Directory = selectedPath
            };

            return sfd.ShowDialog(this) != DialogResult.Ok ? string.Empty : sfd.Directory;
        }

        #endregion
    }
}
