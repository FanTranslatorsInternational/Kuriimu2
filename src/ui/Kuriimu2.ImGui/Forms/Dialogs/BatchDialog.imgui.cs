using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Konnect.Contract.Plugin.Game;
using Kuriimu2.ImGui.Resources;
using System.Diagnostics.CodeAnalysis;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class BatchDialog : Modal
    {
        private StackLayout _mainLayout;
        private StackLayout _settingsLayout;
        private StackLayout _textParametersLayout;
        private Panel _emptyPanel;

        private RadioButtonGroup _operations;

        private TextBox _filePluginTextBox;
        private Button _selectFilePluginBtn;

        private TextBox _inputTextBox;
        private Button _folderBtn;
        private CheckBox _subDirCheckBox;

        private RadioButtonGroup _textFormats;
        private ComboBox<IGamePlugin?> _gamePluginComboBox;

        private Button _executeBtn;
        private Button _cancelBtn;
        private TextEditor _logEditor;

        private ProgressBar _progress;

        [MemberNotNull(nameof(_mainLayout), nameof(_settingsLayout), nameof(_textParametersLayout), nameof(_emptyPanel))]
        [MemberNotNull(nameof(_operations))]
        [MemberNotNull(nameof(_filePluginTextBox), nameof(_selectFilePluginBtn))]
        [MemberNotNull(nameof(_inputTextBox), nameof(_folderBtn), nameof(_subDirCheckBox))]
        [MemberNotNull(nameof(_textFormats), nameof(_gamePluginComboBox))]
        [MemberNotNull(nameof(_executeBtn), nameof(_cancelBtn), nameof(_logEditor))]
        [MemberNotNull(nameof(_progress))]
        private void InitializeComponent()
        {
            _operations = new RadioButtonGroup
            {
                Items =
                {
                    new RadioButtonItem(LocalizationResources.DialogToolsBatchExport),
                    new RadioButtonItem(LocalizationResources.DialogToolsBatchImport)
                }
            };
            _operations.SelectedItem = _operations.Items[0];

            _filePluginTextBox = new TextBox { Width = SizeValue.Relative(.7f), IsReadOnly = true };
            _selectFilePluginBtn = new Button { Width = SizeValue.Relative(.3f), Text = LocalizationResources.DialogToolsBatchSelectPlugin };

            _inputTextBox = new TextBox { IsReadOnly = true };
            _folderBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsBatchInputFolder };
            _subDirCheckBox = new CheckBox { Text = LocalizationResources.DialogToolsBatchInputSubDirectories };

            _textFormats = new RadioButtonGroup
            {
                Items =
                {
                    new RadioButtonItem(LocalizationResources.DialogToolsBatchTextKup),
                    new RadioButtonItem(LocalizationResources.DialogToolsBatchTextPo)
                }
            };
            _textFormats.SelectedItem = _textFormats.Items[0];

            _gamePluginComboBox = new ComboBox<IGamePlugin?> { Width = SizeValue.Parent };

            _executeBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsBatchExecute, KeyAction = new KeyCommand(ImGuiKey.Enter) };
            _cancelBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsBatchCancel, Enabled = false };
            _logEditor = new TextEditor { IsReadOnly = true };

            _progress = new ProgressBar
            {
                Size = new Size(SizeValue.Parent, 24),
                ProgressColor = ColorResources.Progress,
                Text = LocalizationResources.DialogToolsBatchProgress
            };

            _emptyPanel = new Panel();

            _textParametersLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                Size = new Size(.5f, SizeValue.Parent),
                ItemSpacing = 4,
                Items =
                {
                    new Label(LocalizationResources.DialogToolsBatchText),
                    _gamePluginComboBox,
                    _textFormats
                }
            };

            _settingsLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                Size = new Size(.5f, SizeValue.Parent),
                ItemSpacing = 4,
                Items =
                {
                    _operations,
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.WidthAlign,
                        ItemSpacing = 4,
                        Items =
                        {
                            _filePluginTextBox,
                            _selectFilePluginBtn
                        }
                    },
                    _inputTextBox,
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.WidthAlign,
                        ItemSpacing = 4,
                        Items =
                        {
                            _folderBtn,
                            _subDirCheckBox
                        }
                    },
                    _emptyPanel,
                    new StackItem(new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.Content,
                        ItemSpacing = 4,
                        Items =
                        {
                            new StackItem(_executeBtn){Size = new Size(SizeValue.Relative(.8f), SizeValue.Content)},
                            new StackItem(_cancelBtn){Size = new Size(SizeValue.Relative(.2f), SizeValue.Content)}
                        }
                    }){Size = Size.Parent, VerticalAlignment = VerticalAlignment.Bottom},
                    _progress
                }
            };

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                Size = Size.Parent,
                ItemSpacing = 4,
                Items =
                {
                    _settingsLayout,
                    new Splitter(Alignment.Vertical),
                    new StackItem(_logEditor) { Size = new Size(.5f, SizeValue.Parent) }
                }
            };

            Caption = LocalizationResources.DialogToolsBatchCaption;
            Size = new Size(SizeValue.Relative(.5f), SizeValue.Relative(.6f));

            Content = _mainLayout;

            AllowDragDrop = true;

            InitializeGamePlugins();
        }

        private void InitializeGamePlugins()
        {
            IGamePlugin[] gamePlugins = [.. _pluginManager.GetPlugins<IGamePlugin>()];

            _gamePluginComboBox.Items.Add(new DropDownItem<IGamePlugin?>(null, LocalizationResources.TextPreviewDefault));

            foreach (IGamePlugin gamePlugin in gamePlugins)
                _gamePluginComboBox.Items.Add(new DropDownItem<IGamePlugin?>(gamePlugin, gamePlugin.Metadata.Name));

            if (_gamePluginComboBox.Items.Count > 0)
            {
                _gamePluginComboBox.SelectedItem = _gamePluginComboBox.Items[0];
                _selectedGamePlugin = _gamePluginComboBox.SelectedItem.Content;
            }
        }
    }
}
