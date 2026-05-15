using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Kryptography.Checksum;
using Kryptography.Checksum.Crc;
using Kryptography.Checksum.Fnv;
using Kryptography.Contract.Checksum;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class ChecksumsDialog : Modal
    {
        private StackLayout _mainLayout;
        private StackLayout _settingsLayout;

        private StackLayout _fullParameterLayout;
        private TableLayout _parameterLayout;

        private ComboBox<ChecksumData> _checksums;

        private TextBox _inputTextBox;
        private Button _fileBtn;
        private Button _folderBtn;
        private CheckBox _subDirCheckBox;

        private Button _executeBtn;
        private Button _cancelBtn;
        private TextEditor _logEditor;

        private ProgressBar _progress;

        [MemberNotNull(nameof(_mainLayout), nameof(_settingsLayout))]
        [MemberNotNull(nameof(_fullParameterLayout), nameof(_parameterLayout))]
        [MemberNotNull(nameof(_checksums))]
        [MemberNotNull(nameof(_inputTextBox), nameof(_fileBtn), nameof(_folderBtn), nameof(_subDirCheckBox))]
        [MemberNotNull(nameof(_executeBtn), nameof(_cancelBtn), nameof(_logEditor))]
        [MemberNotNull(nameof(_progress))]
        private void InitializeComponent()
        {
            #region Components

            _checksums = new ComboBox<ChecksumData> { Alignment = ComboBoxAlignment.Bottom, MaxShowItems = 10 };
            _inputTextBox = new TextBox { IsReadOnly = true };
            _fileBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsChecksumsInputFile };
            _folderBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsChecksumsInputFolder };
            _subDirCheckBox = new CheckBox { Text = LocalizationResources.DialogToolsChecksumsInputSubDirectories };
            _executeBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsChecksumsExecute, KeyAction = new KeyCommand(ImGuiKey.Enter) };
            _cancelBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.DialogToolsChecksumsCancel, Enabled = false };
            _logEditor = new TextEditor { IsReadOnly = true };

            _progress = new ProgressBar
            {
                Size = new Size(SizeValue.Parent, 24),
                ProgressColor = ColorResources.Progress,
                Text = LocalizationResources.DialogToolsChecksumsProgress
            };

            #endregion

            #region Layouts

            _parameterLayout = new TableLayout
            {
                Size = Size.Parent,
                Spacing = new Vector2(4, 4)
            };

            _fullParameterLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    new Label(LocalizationResources.DialogToolsChecksumsInputParameters),
                    _parameterLayout
                }
            };

            _settingsLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                Size = new Size(.5f, SizeValue.Parent),
                ItemSpacing = 4,
                Items =
                {
                    _checksums,
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
                    new Panel(),
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

            #endregion

            Caption = LocalizationResources.DialogToolsChecksumsCaption;
            Size = new Size(SizeValue.Relative(.5f), SizeValue.Relative(.6f));

            Content = _mainLayout;

            AllowDragDrop = true;

            InitializeChecksums();
            UpdateParameters();
        }

        private void InitializeChecksums()
        {
            var xbbChecksum = new ChecksumData(_ => new Xbb());
            var simpleChecksum = new ChecksumData(
                parameters => new Simple((uint)parameters[0].Value),
                new ChecksumParameter("Magic", 0u));
            var sha256Checksum = new ChecksumData(_ => new Sha256());
            var fnv1Checksum = new ChecksumData(_ => Fnv1.Create());
            var fnv1AChecksum = new ChecksumData(_ => Fnv1a.Create());
            var crc16X25Checksum = new ChecksumData(_ => Crc16.X25);
            var crc16ModBusChecksum = new ChecksumData(_ => Crc16.ModBus);
            var crc32BChecksum = new ChecksumData(_ => Crc32.Crc32B);
            var crc32CChecksum = new ChecksumData(_ => Crc32.Crc32C);
            var crc32JamChecksum = new ChecksumData(_ => Crc32.JamCrc);
            var crc32NamcoChecksum = new ChecksumData(_ => Crc32Namco.Create());

            _checksums.Items.Add(new DropDownItem<ChecksumData>(xbbChecksum, "Xbb"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(simpleChecksum, "Simple"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(sha256Checksum, "SHA256"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(fnv1Checksum, "FNV-1"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(fnv1AChecksum, "FNV-1a"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc16X25Checksum, "CRC16 X25"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc16ModBusChecksum, "CRC16 ModBus"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc32BChecksum, "CRC32-B"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc32CChecksum, "CRC32-C"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc32JamChecksum, "CRC32 Jam"));
            _checksums.Items.Add(new DropDownItem<ChecksumData>(crc32NamcoChecksum, "CRC32 Namco Bandai"));

            _checksums.SelectedItem = _checksums.Items[0];
        }

        private void UpdateParameters()
        {
            if (_checksums.SelectedItem is null || _checksums.SelectedItem.Content.Parameters.Length <= 0)
            {
                _settingsLayout.Items[^3] = new Panel();
                return;
            }

            _parameterLayout.Rows.Clear();

            for (var i = 0; i < _checksums.SelectedItem.Content.Parameters.Length; i++)
            {
                _parameterLayout.Rows.Add(new TableRow
                {
                    Cells =
                    {
                        new Label(_checksums.SelectedItem.Content.Parameters[i].Name),
                        CreateParameterComponent(i)
                    }
                });
            }

            _settingsLayout.Items[^3] = new Panel(_fullParameterLayout);
        }

        private Component CreateParameterComponent(int index)
        {
            ChecksumParameter? parameter = _checksums.SelectedItem?.Content.Parameters[index];

            switch (parameter?.Value)
            {
                case uint uintValue:
                    var textBox = new TextBox { Text = $"{uintValue}" };
                    textBox.TextChanged += (_, _) => parameter.Value = uint.TryParse(textBox.Text, out uint parsedValue) ? parsedValue : parameter.Value;

                    return textBox;

                default:
                    throw new InvalidOperationException("Unsupported parameter type.");
            }
        }
    }

    internal class ChecksumData(Func<ChecksumParameter[], IChecksum> checksumWrapper, params ChecksumParameter[] parameters)
    {
        public ChecksumParameter[] Parameters { get; } = parameters;

        public byte[] Compute(Stream input)
        {
            return checksumWrapper(Parameters).Compute(input);
        }
    }

    internal class ChecksumParameter(string name, object value)
    {
        public string Name { get; } = name;

        public object Value { get; set; } = value;
    }
}
