using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Models.Forms.Dialogs;
using Kuriimu2.ImGui.Resources;
using System.Text;
using ImGui.Forms.Models.IO;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class TextSequenceSearchDialog : Modal
    {
        private StackLayout _mainLayout;
        private StackLayout _settingsLayout;

        private TextBox _searchTextBox;
        private ComboBox<Encoding> _encodingBox;

        private TextBox _inputTextBox;
        private Button _fileBtn;
        private Button _folderBtn;
        private CheckBox _subDirCheckBox;

        private Button _executeBtn;
        private Button _cancelBtn;

        private DataTable<SequenceSearcherResult> _resultTable;

        private ProgressBar _progress;

        [MemberNotNull(nameof(_mainLayout), nameof(_settingsLayout))]
        [MemberNotNull(nameof(_searchTextBox), nameof(_encodingBox))]
        [MemberNotNull(nameof(_inputTextBox), nameof(_fileBtn), nameof(_folderBtn), nameof(_subDirCheckBox))]
        [MemberNotNull(nameof(_executeBtn), nameof(_cancelBtn))]
        [MemberNotNull(nameof(_resultTable), nameof(_progress))]
        private void InitializeComponent()
        {
            _searchTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsTextSequenceSearcherSearchPlaceholder };
            _encodingBox = new ComboBox<Encoding> { Width = SizeValue.Parent };

            _inputTextBox = new TextBox { IsReadOnly = true };
            _fileBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsTextSequenceSearcherInputFile };
            _folderBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsTextSequenceSearcherInputFolder };
            _subDirCheckBox = new CheckBox { Checked = SettingsResources.SequenceSearchSubDirectories, Text = LocalizationResources.DialogToolsTextSequenceSearcherInputSubDirectories };

            _executeBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsTextSequenceSearcherExecute, KeyAction = new KeyCommand(ImGuiKey.Enter) };
            _cancelBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsTextSequenceSearcherCancel, Enabled = false };

            _resultTable = new DataTable<SequenceSearcherResult>
            {
                Size = new Size(SizeValue.Relative(.7f), SizeValue.Parent),
                ShowHeaders = true,
                IsResizable = true,
                CanSelectMultiple = true,
                Columns =
                {
                    new DataTableColumn<SequenceSearcherResult>(value => value.FilePath, LocalizationResources.DialogToolsTextSequenceSearcherPath),
                    new DataTableColumn<SequenceSearcherResult>(value => $"{value.Offset}", LocalizationResources.DialogToolsTextSequenceSearcherOffset)
                }
            };

            _progress = new ProgressBar
            {
                Size = new Size(SizeValue.Parent, 24),
                ProgressColor = ColorResources.Progress,
                Text = LocalizationResources.DialogToolsTextSequenceSearcherProgress
            };

            _settingsLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                Size = new Size(SizeValue.Relative(.3f), SizeValue.Parent),
                ItemSpacing = 4,
                Items =
                {
                    _searchTextBox,
                    _encodingBox,
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
                    _fileBtn,
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
                    _resultTable
                }
            };

            Caption = LocalizationResources.DialogToolsTextSequenceSearcherCaption;

            Content = _mainLayout;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            AllowDragDrop = true;

            InitializeEncodings();
        }

        private void InitializeEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _encodingBox.Items.Clear();

            Encoding encoding = Encoding.ASCII;
            _encodingBox.Items.Add(new DropDownItem<Encoding>(encoding, encoding.EncodingName));

            encoding = Encoding.Latin1;
            _encodingBox.Items.Add(new DropDownItem<Encoding>(encoding, encoding.EncodingName));

            encoding = Encoding.Unicode;
            _encodingBox.Items.Add(new DropDownItem<Encoding>(encoding, encoding.EncodingName));

            encoding = Encoding.UTF8;
            _encodingBox.Items.Add(new DropDownItem<Encoding>(encoding, encoding.EncodingName));

            encoding = Encoding.GetEncoding("Shift-JIS");
            _encodingBox.Items.Add(new DropDownItem<Encoding>(encoding, encoding.EncodingName));

            var selectedEncoding = SettingsResources.SequenceSearchEncoding;
            foreach (var item in _encodingBox.Items)
            {
                if (item.Name == selectedEncoding)
                {
                    _encodingBox.SelectedItem = item;
                    return;
                }
            }

            _encodingBox.SelectedItem = _encodingBox.Items[0];
        }
    }
}
