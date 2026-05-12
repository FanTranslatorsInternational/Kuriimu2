using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO.Windows;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Management.Batch;
using Konnect.Management.Files;
using Konnect.Progress;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Progress;
using Kuriimu2.ImGui.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.DataClasses.Management.Batch;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class BatchDialog
    {
        private readonly IPluginManager _pluginManager;
        private readonly BatchExtractor _extractor;
        private readonly BatchInjector _injector;

        private CancellationTokenSource? _source;
        private IFilePlugin? _selectedPlugin;
        private IGamePlugin? _selectedGamePlugin;

        public BatchDialog(IPluginManager pluginManager)
        {
            _pluginManager = pluginManager;

            InitializeComponent();

            FileManager fileManager = LoadFileManager(pluginManager);
            var progress = new ProgressContext(new ProgressBarOutput(_progress, 20, LocalizationResources.DialogToolsBatchProgressValue));

            _extractor = new BatchExtractor(fileManager, progress);
            _injector = new BatchInjector(fileManager, progress);

            _selectFilePluginBtn.Clicked += SelectFilePluginBtn_Clicked;

            _folderBtn.Clicked += FolderBtn_Clicked;

            _gamePluginComboBox.SelectedItemChanged += GamePluginComboBox_SelectedItemChanged;

            _executeBtn.Clicked += ExecuteBtn_Clicked;
            _cancelBtn.Clicked += CancelBtn_Clicked;

            _extractor.FileProcessed += Extractor_FileProcessed;
            _injector.FileProcessed += Injector_FileProcessed;

            DragDrop += BatchDialog_DragDrop;

            UpdateFormInternal();
        }

        private async Task Extractor_FileProcessed(BatchFileResult arg)
        {
            if (_extractor.ReuseDialogOptions || arg.DialogOptions.Count <= 0)
                return;

            var result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogBatchReuseOptionsCaption, LocalizationResources.DialogBatchReuseOptionsText);

            if (result == DialogResult.Yes)
                _extractor.ReuseDialogOptions = true;
        }

        private async Task Injector_FileProcessed(BatchFileResult arg)
        {
            if (_injector.ReuseDialogOptions || arg.DialogOptions.Count <= 0)
                return;

            var result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogBatchReuseOptionsCaption, LocalizationResources.DialogBatchReuseOptionsText);

            if (result == DialogResult.Yes)
                _injector.ReuseDialogOptions = true;
        }

        private void GamePluginComboBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            _selectedGamePlugin = _gamePluginComboBox.SelectedItem?.Content;
        }

        private async void SelectFilePluginBtn_Clicked(object? sender, EventArgs e)
        {
            var selectPluginDialog = new PluginSelectionDialog(_pluginManager, SelectablePlugins.All & ~SelectablePlugins.Font);
            DialogResult result = await selectPluginDialog.ShowAsync();

            if (result is not DialogResult.Ok)
                return;

            var selectedPlugin = selectPluginDialog.SelectedPlugin;

            if (selectedPlugin is null)
                return;

            _settingsLayout.Items[4] = selectedPlugin.PluginType switch
            {
                PluginType.Text => _textParametersLayout,
                _ => _emptyPanel
            };

            _filePluginTextBox.Text = $"{selectedPlugin.Metadata.Name} ({selectedPlugin.PluginId})";
            _selectedPlugin = selectedPlugin;

            UpdateFormInternal();
        }

        private void BatchDialog_DragDrop(object? sender, string[] e)
        {
            if (!Directory.Exists(e[0]))
                return;

            _inputTextBox.Text = e[0];

            UpdateFormInternal();
        }

        private async void ExecuteBtn_Clicked(object? sender, EventArgs e)
        {
            _source = new CancellationTokenSource();

            _operations.Enabled = false;
            _selectFilePluginBtn.Enabled = false;

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

        private void UpdateFormInternal()
        {
            _executeBtn.Enabled = _operations.SelectedItem is not null && _selectedPlugin is not null && !string.IsNullOrEmpty(_inputTextBox.Text);
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

        private async Task Process()
        {
            var options = new BatchOptions { SubDirectories = _subDirCheckBox.Checked };

            if (_selectedPlugin!.PluginType is PluginType.Text)
            {
                options.TextOptions = new BatchTextOptions
                {
                    Format = _textFormats.SelectedItem == _textFormats.Items[0] ? TextFormat.Kup : TextFormat.Po,
                    Preview = _selectedGamePlugin
                };
            }

            if (_operations.SelectedItem == _operations.Items[0])
            {
                _extractor.ReuseDialogOptions = false;
                await _extractor.Extract(_inputTextBox.Text!, _selectedPlugin!, options);
            }
            else
            {
                _injector.ReuseDialogOptions = false;
                await _injector.Inject(_inputTextBox.Text!, _selectedPlugin!, options);
            }

            _operations.Enabled = true;
            _selectFilePluginBtn.Enabled = true;

            _executeBtn.Enabled = true;
            _cancelBtn.Enabled = false;
        }

        private static FileManager LoadFileManager(IPluginManager pluginManager)
        {
            return new FileManager(pluginManager)
            {
                DialogManager = new ImGuiDialogManager()
            };
        }
    }
}
