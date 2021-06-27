﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;
using Kore.Batch;
using Kore.Extensions;
using Kore.Factories;
using Kore.Logging;
using Kore.Managers;
using Kore.Managers.Plugins;
using Kuriimu2.WinForms.BatchForms.Model;
using Kuriimu2.WinForms.ExtensionForms.Support;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.BatchForms
{
    public abstract partial class BaseBatchForm : Form
    {
        private readonly BaseBatchProcessor _batchProcessor;
        private readonly System.Timers.Timer _avgTimer;

        protected abstract string SourceEmptyText { get; }

        protected abstract string DestinationEmptyText { get; }

        protected IConcurrentLogger Logger { get; }

        public BaseBatchForm(IInternalPluginManager pluginManager)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));

            InitializeComponent();

            Logger = InitializeLogger();
            _batchProcessor = InitializeBatchProcessor(pluginManager, Logger);

            _avgTimer = new System.Timers.Timer(300);
            _avgTimer.Elapsed += avgTimer_Elapsed;

            var loadedPlugins = LoadPlugins(pluginManager);
            cmbPlugins.Items.Add(PluginElement.Empty);
            foreach (var loadedPlugin in loadedPlugins)
                cmbPlugins.Items.Add(loadedPlugin);

            cmbPlugins.SelectedIndex = 0;
        }

        #region Initialization

        protected abstract BaseBatchProcessor InitializeBatchProcessor(IInternalPluginManager pluginManager, IConcurrentLogger logger);

        private IConcurrentLogger InitializeLogger()
        {
            return new ConcurrentLogger(ApplicationLevel.Ui, new RichTextboxLogOutput(txtLog));
        }

        private IList<PluginElement> LoadPlugins(IInternalPluginManager pluginManager)
        {
            return pluginManager.GetFilePlugins().Select(x => new PluginElement(x)).ToArray();
        }

        #endregion

        #region Events

        private void avgTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var avgTime = _batchProcessor.AverageFileTime;
            if (lblTime.InvokeRequired)
                lblTime.Invoke(new Action(() => lblTime.Text = "Avg time per file: " + avgTime.Milliseconds + "ms"));
        }

        private void btnSourceFolder_Click(object sender, EventArgs e)
        {
            var selectedFolder = SelectFolder(Settings.Default.BatchInputDirectory);
            if (string.IsNullOrEmpty(selectedFolder))
                return;

            Settings.Default.BatchInputDirectory = selectedFolder;
            Settings.Default.Save();

            txtSourcePath.Text = selectedFolder;
        }

        private void btnDestinationFolder_Click(object sender, EventArgs e)
        {
            var selectedFolder = SelectFolder(Settings.Default.BatchOutputDirectory);
            if (string.IsNullOrEmpty(selectedFolder))
                return;

            Settings.Default.BatchOutputDirectory = selectedFolder;
            Settings.Default.Save();

            txtDestinationPath.Text = selectedFolder;
        }

        private async void btnExecute_Click(object sender, EventArgs e)
        {
            _avgTimer.Start();
            await Execute();
            _avgTimer.Stop();
        }

        private void BatchForm_DragDrop(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            var path = paths[0];
            txtSourcePath.Text = path;

        }

        private void BatchForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void BaseBatchForm_Closed(object sender, EventArgs e)
        {
            _batchProcessor.Cancel();
            _avgTimer.Stop();
        }

        #endregion

        #region Support

        private string SelectFolder(string selectedPath)
        {
            var sfd = new FolderBrowserDialog
            {
                SelectedPath = selectedPath
            };

            return sfd.ShowDialog() != DialogResult.OK ? string.Empty : sfd.SelectedPath;
        }

        private async Task Execute()
        {
            txtLog.Clear();
            Logger.StartLogging();

            ToggleUi(false);

            if (!VerifyInput())
            {
                Logger.StopLogging();
                ToggleUi(true);
                return;
            }

            var selectedPlugin = (PluginElement)cmbPlugins.SelectedItem;

            _batchProcessor.PluginId = selectedPlugin.IsEmpty ? Guid.Empty : selectedPlugin.Plugin.PluginId;
            _batchProcessor.ScanSubDirectories = chkSubDirectories.Checked;

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(txtSourcePath.Text, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(txtDestinationPath.Text, new StreamManager());

            await _batchProcessor.Process(sourceFileSystem, destinationFileSystem);

            ToggleUi(true);
        }

        private bool VerifyInput()
        {
            if (cmbPlugins.SelectedIndex < 0)
            {
                Logger.QueueMessage(LogLevel.Error, "Select a plugin entry.");
                return false;
            }

            if (string.IsNullOrEmpty(txtSourcePath.Text))
            {
                Logger.QueueMessage(LogLevel.Error, SourceEmptyText);
                return false;
            }

            if (string.IsNullOrEmpty(txtDestinationPath.Text))
            {
                Logger.QueueMessage(LogLevel.Error, DestinationEmptyText);
                return false;
            }

            return true;
        }

        private void ToggleUi(bool toggle)
        {
            cmbPlugins.Enabled = toggle;

            btnExecute.Enabled = btnSourceFolder.Enabled = btnDestinationFolder.Enabled = toggle;

            chkSubDirectories.Enabled = toggle;
        }

        #endregion
    }
}
