using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using Komponent.Text;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class TextSequenceSearchDialog
    {
        private CancellationTokenSource? _source;

        public TextSequenceSearchDialog()
        {
            InitializeComponent();

            _searchTextBox.TextChanged += _searchTextBox_TextChanged;
            _encodingBox.SelectedItemChanged += _encodingBox_SelectedItemChanged;

            _folderBtn.Clicked += _folderBtn_Clicked;
            _fileBtn.Clicked += _fileBtn_Clicked;
            _subDirCheckBox.CheckChanged += _subDirCheckBox_CheckChanged;

            _executeBtn.Clicked += _executeBtn_Clicked;
            _cancelBtn.Clicked += _cancelBtn_Clicked;

            DragDrop += TextSequenceSearcherDialog_DragDrop;

            UpdateFormInternal();
        }

        private void _searchTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateFormInternal();
        }

        private void _encodingBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            SettingsResources.SequenceSearchEncoding = _encodingBox.SelectedItem.Name;

            UpdateFormInternal();
        }

        private async void _folderBtn_Clicked(object? sender, EventArgs e)
        {
            var folderPath = await SelectFolder();
            if (folderPath is null)
                return;

            _inputTextBox.Text = folderPath;

            UpdateFormInternal();
        }

        private async void _fileBtn_Clicked(object? sender, EventArgs e)
        {
            var filePath = await SelectFile();
            if (filePath is null)
                return;

            _inputTextBox.Text = filePath;

            UpdateFormInternal();
        }

        private void _subDirCheckBox_CheckChanged(object? sender, EventArgs e)
        {
            SettingsResources.SequenceSearchSubDirectories = _subDirCheckBox.Checked;
        }

        private async void _executeBtn_Clicked(object? sender, EventArgs e)
        {
            _source = new CancellationTokenSource();

            _searchTextBox.IsReadOnly = true;
            _encodingBox.Enabled = false;

            _executeBtn.Enabled = false;
            _cancelBtn.Enabled = true;

            await Task.Run(Process, _source.Token);
        }

        private void _cancelBtn_Clicked(object? sender, EventArgs e)
        {
            _source?.Cancel();
        }

        private void TextSequenceSearcherDialog_DragDrop(object? sender, string[] e)
        {
            _inputTextBox.Text = e[0];

            if (File.Exists(e[0]))
                SettingsResources.SequenceSearchTarget = Path.GetDirectoryName(e[0]);
            else if (Directory.Exists(e[0]))
                SettingsResources.SequenceSearchTarget = e[0];

            UpdateFormInternal();
        }

        private void UpdateFormInternal()
        {
            _executeBtn.Enabled = !string.IsNullOrEmpty(_inputTextBox.Text) && !string.IsNullOrEmpty(_searchTextBox.Text) && _encodingBox.SelectedItem is not null;
        }

        private async Task<string?> SelectFile()
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = SettingsResources.SequenceSearchTarget,
                Filters = [new FileFilter(LocalizationResources.FilterAll, "*")]
            };

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.SequenceSearchTarget = Path.GetDirectoryName(ofd.Files[0]);

            return ofd.Files[0];
        }

        private async Task<string?> SelectFolder()
        {
            var sfd = new WindowsSelectFolderDialog
            {
                InitialDirectory = SettingsResources.SequenceSearchTarget
            };

            // Show dialog and wait for result
            var result = await sfd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.SequenceSearchTarget = sfd.Directory;

            return sfd.Directory;
        }

        private void Process()
        {
            _progress.Value = 0;

            if (File.Exists(_inputTextBox.Text))
            {
                _progress.Maximum = 1;

                ProcessFile(_inputTextBox.Text);
            }
            else
            {
                ProcessDirectory(_inputTextBox.Text);
            }

            _searchTextBox.IsReadOnly = false;
            _encodingBox.Enabled = true;

            _executeBtn.Enabled = true;
            _cancelBtn.Enabled = false;
        }

        private void ProcessDirectory(string directoryPath)
        {
            var searchOptions = _subDirCheckBox.Checked
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(directoryPath, "*", searchOptions);
            _progress.Maximum = files.Length;

            foreach (string filePath in files)
            {
                if (_source?.IsCancellationRequested ?? false)
                    break;

                ProcessFile(filePath);
            }
        }

        private void ProcessFile(string filePath)
        {
            byte[] content = File.ReadAllBytes(filePath);
            var searcher = KmpSearcher.Create(_searchTextBox.Text, _encodingBox.SelectedItem.Content);

            int offset = searcher.Find(content);

            if (offset > -1)
                _resultTable.Rows.Add(new DataTableRow<SequenceSearcherResult>(new SequenceSearcherResult(filePath, offset)));

            _progress.Value++;
        }
    }
}
