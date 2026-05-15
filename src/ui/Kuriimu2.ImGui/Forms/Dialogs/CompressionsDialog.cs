using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using Konnect.Progress;
using Kuriimu2.ImGui.Progress;
using Kuriimu2.ImGui.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class CompressionsDialog
    {
        private readonly ProgressContext _progressContext;

        private CancellationTokenSource? _source;

        public CompressionsDialog()
        {
            InitializeComponent();

            _progressContext = new ProgressContext(new ProgressBarOutput(_progress, 20, LocalizationResources.DialogToolsCompressionsProgressValue));

            _folderBtn.Clicked += FolderBtn_Clicked;
            _fileBtn.Clicked += FileBtn_Clicked;

            _executeBtn.Clicked += ExecuteBtn_Clicked;
            _cancelBtn.Clicked += CancelBtn_Clicked;

            DragDrop += CompressionsDialog_DragDrop;

            UpdateFormInternal();
        }

        private async void ExecuteBtn_Clicked(object? sender, EventArgs e)
        {
            _source = new CancellationTokenSource();

            _operations.Enabled = false;
            _compressions.Enabled = false;

            _executeBtn.Enabled = false;
            _cancelBtn.Enabled = true;

            await Task.Run(Process);
        }

        private void CancelBtn_Clicked(object? sender, EventArgs e)
        {
            _source?.Cancel();
        }

        private async void FolderBtn_Clicked(object? sender, EventArgs e)
        {
            var folderPath = await SelectFolder();
            if (folderPath is null)
                return;

            _inputTextBox.Text = folderPath;

            UpdateFormInternal();
        }

        private async void FileBtn_Clicked(object? sender, EventArgs e)
        {
            var filePath = await SelectFile();
            if (filePath is null)
                return;

            _inputTextBox.Text = filePath;

            UpdateFormInternal();
        }

        private void CompressionsDialog_DragDrop(object? sender, string[] e)
        {
            _inputTextBox.Text = e[0];

            UpdateFormInternal();
        }

        private void UpdateFormInternal()
        {
            _executeBtn.Enabled = _operations.SelectedItem is not null && _compressions.SelectedItem is not null && !string.IsNullOrEmpty(_inputTextBox.Text);
        }

        private static async Task<string?> SelectFile()
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = SettingsResources.LastDirectory,
                Filters = [new FileFilter(LocalizationResources.FilterAll, "*")]
            };

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.LastDirectory = Path.GetDirectoryName(ofd.Files[0]) ?? string.Empty;

            return ofd.Files[0];
        }

        private static async Task<string?> SelectFolder()
        {
            var sfd = new WindowsSelectFolderDialog
            {
                InitialDirectory = SettingsResources.LastDirectory
            };

            // Show dialog and wait for result
            var result = await sfd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.LastDirectory = sfd.Directory ?? string.Empty;

            return sfd.Directory;
        }

        private void Process()
        {
            _logEditor.SetText(string.Empty);

            _progressContext.StartProgress();

            if (File.Exists(_inputTextBox.Text))
            {
                _progressContext.ReportProgress(0, 1);

                ProcessFile(_inputTextBox.Text);

                _progressContext.ReportProgress(1, 1);
            }
            else if (Directory.Exists(_inputTextBox.Text))
            {
                ProcessDirectory(_inputTextBox.Text!);
            }

            _progressContext.FinishProgress();

            _operations.Enabled = true;
            _compressions.Enabled = true;

            _executeBtn.Enabled = true;
            _cancelBtn.Enabled = false;
        }

        private void ProcessDirectory(string directoryPath)
        {
            var searchOptions = _subDirCheckBox.Checked
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(directoryPath, "*", searchOptions);

            var index = 0;
            foreach (string filePath in files)
            {
                _progressContext.ReportProgress(index++, files.Length);

                if (_source?.IsCancellationRequested ?? false)
                    break;

                ProcessFile(filePath);
            }

            _progressContext.ReportProgress(files.Length, files.Length);
        }

        private void ProcessFile(string filePath)
        {
            string logText = _logEditor.GetText();
            _logEditor.SetText(logText + LocalizationResources.DialogToolsCompressionsLogProcess(filePath) + Environment.NewLine);

            string outPath = filePath + ".out";

            using var input = File.OpenRead(filePath);
            using var output = File.Create(outPath);

            try
            {
                if (_operations.SelectedItem == _operations.Items[0])
                    _compressions.SelectedItem?.Content.Compress(input, output);
                else
                    _compressions.SelectedItem?.Content.Decompress(input, output);
            }
            catch (Exception)
            {
                _logEditor.SetText(logText + LocalizationResources.DialogToolsCompressionsLogError(filePath) + Environment.NewLine);

                output.Close();
                File.Delete(outPath);
            }
        }
    }
}
