using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;
using Kore.Batch;
using Kore.Logging;
using Kuriimu2.WinForms.ExtensionForms.Models;
using Kuriimu2.WinForms.ExtensionForms.Support;
using Kuriimu2.WinForms.Extensions;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.ExtensionForms
{
    public abstract partial class TypeExtensionForm<TExtension, TResult> : Form
    {
        private bool _isDirectory;

        private BatchExtensionProcessor<TExtension, TResult> _batchProcessor;
        private ParameterBuilder _parameterBuilder;

        protected IConcurrentLogger Logger { get; }

        protected abstract string TypeExtensionName { get; }

        public TypeExtensionForm()
        {
            InitializeComponent();

            txtLog.ForeColor = Color.FromArgb(0x20, 0xC2, 0x0E);
            Logger = new ConcurrentLogger(ApplicationLevel.Ui, new RichTextboxLogOutput(txtLog));

            _batchProcessor = new BatchExtensionProcessor<TExtension, TResult>(ProcessFile, Logger);
            _parameterBuilder = new ParameterBuilder(gbTypeExtensionParameters);

            var loadedExtensions = LoadExtensionTypes();
            foreach (var loadedExtension in loadedExtensions)
                cmbExtensions.Items.Add(loadedExtension);

            cmbExtensions.SelectedIndex = 0;

            Text = TypeExtensionName + " Extensions";
            label1.Text = Text + ":";
        }

        protected abstract IList<ExtensionType> LoadExtensionTypes();

        protected abstract TExtension CreateExtensionType(ExtensionType selectedExtension);

        protected abstract TResult ProcessFile(TExtension extensionType, string filePath);

        protected abstract void FinalizeProcess(IList<(string, TResult)> results, string rootDir);

        private void btnFolder_Click(object sender, EventArgs e)
        {
            var sfd = new FolderBrowserDialog
            {
                SelectedPath = Settings.Default.TypeExtensionLastDirectory
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            Settings.Default.TypeExtensionLastDirectory = sfd.SelectedPath;

            txtPath.Text = sfd.SelectedPath;
            _isDirectory = true;
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.TypeExtensionLastDirectory,
                Filter = "All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            Settings.Default.TypeExtensionLastDirectory = Path.GetDirectoryName(ofd.FileName);

            txtPath.Text = ofd.FileName;
            _isDirectory = false;
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            Execute();
        }

        private void Execute()
        {
            txtLog.Clear();
            Logger.StartLogging();

            if (!VerifyInput())
            {
                Logger.StopLogging();
                return;
            }

            var selectedType = (ExtensionType)cmbExtensions.SelectedItem;

            if (!TryParseParameters(selectedType.Parameters.Values.ToArray()))
            {
                Logger.StopLogging();
                return;
            }

            // Create type
            var createdType = CreateExtensionType(selectedType);

            // Execute processing
            ExecuteInternal(txtPath.Text, _isDirectory, chkSubDirectories.Checked, createdType);
        }

        private bool VerifyInput()
        {
            if (cmbExtensions.SelectedIndex < 0)
            {
                Logger.QueueMessage(LogLevel.Error, $"Select a {TypeExtensionName}.");
                return false;
            }

            if (string.IsNullOrEmpty(txtPath.Text))
            {
                Logger.QueueMessage(LogLevel.Error, "Select a file or directory to process.");
                return false;
            }

            return true;
        }

        private bool TryParseParameters(ExtensionTypeParameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                var control = gbTypeExtensionParameters.Controls.Find(parameter.Name, false)[0];

                if (!parameter.TryParse(control, out var error))
                {
                    Logger.QueueMessage(LogLevel.Error, error);
                    return false;
                }
            }

            return true;
        }

        private void ToggleUi(bool toggle)
        {
            cmbExtensions.Enabled = toggle;
            gbTypeExtensionParameters.Enabled = toggle;

            btnExecute.Enabled = btnFile.Enabled = btnFolder.Enabled = toggle;

            chkSubDirectories.Enabled = toggle;
            chkAutoExecute.Enabled = toggle;
        }

        private async void ExecuteInternal(string path, bool isDirectory, bool searchSubDirectories, TExtension extensionType)
        {
            ToggleUi(false);

            // Process all files
            var results = await _batchProcessor.Process(path, isDirectory, searchSubDirectories, extensionType);

            // Finalize the processing/create a report
            FinalizeProcess(results, isDirectory ? path : Path.GetDirectoryName(path));

            ToggleUi(true);

            Logger.StopLogging();
        }

        private void cmbExtensions_SelectedIndexChanged(object sender, EventArgs e)
        {
            _parameterBuilder.Reset();

            var extensionType = cmbExtensions.SelectedItem as ExtensionType;

            _parameterBuilder.AddParameters(extensionType.Parameters.Values.ToArray());
        }

        private void TypeExtensionForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            var path = paths[0];
            txtPath.Text = path;

            _isDirectory = Directory.Exists(path);

            if (chkAutoExecute.Checked)
                Execute();

        }

        private void TypeExtensionForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }
}
