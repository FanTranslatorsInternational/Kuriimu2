using System;
using System.IO;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kryptography.Encryption;
using Kryptography.Encryption.AES;
using Kryptography.Encryption.Blowfish;
using Kryptography.Encryption.IntiCreates;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class CiphersDialog : Modal
    {
        private StackLayout _mainLayout;
        private StackLayout _settingsLayout;

        private StackLayout _fullParameterLayout;
        private TableLayout _parameterLayout;

        private RadioButtonGroup _operations;

        private ComboBox<CipherData> _ciphers;
        private TextBox _inputTextBox;
        private Button _fileBtn;
        private Button _folderBtn;
        private CheckBox _subDirCheckBox;
        private Button _executeBtn;
        private TextEditor _logEditor;

        private ProgressBar _progress;

        private void InitializeComponent()
        {
            #region Components

            _operations = new RadioButtonGroup
            {
                Items =
                {
                    new RadioButtonItem(LocalizationResources.MenuToolsCiphersEncrypt),
                    new RadioButtonItem(LocalizationResources.MenuToolsCiphersDecrypt)
                }
            };
            _operations.SelectedItem = _operations.Items[0];

            _ciphers = new ComboBox<CipherData> { Alignment = ComboBoxAlignment.Bottom, MaxShowItems = 10 };
            _inputTextBox = new TextBox { IsReadOnly = true };
            _fileBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCiphersInputFile };
            _folderBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCiphersInputFolder };
            _subDirCheckBox = new CheckBox { Text = LocalizationResources.MenuToolsCiphersInputSubDirectories };
            _executeBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCiphersExecute, KeyAction = new(Key.Enter) };
            _logEditor = new TextEditor { IsReadOnly = true };

            _progress = new ProgressBar
            {
                Size = new Size(SizeValue.Parent, 24),
                ProgressColor = ColorResources.Progress,
                Text = LocalizationResources.MenuToolsCiphersProgress
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
                    new Label(LocalizationResources.MenuToolsCiphersInputParameters),
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
                    _operations,
                    _ciphers,
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
                    new StackItem(_fileBtn){Size = new Size(SizeValue.Relative(.5f), SizeValue.Content)},
                    new Panel(),
                    _executeBtn,
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

            Caption = LocalizationResources.MenuToolsCiphersCaption;
            Size = new Size(SizeValue.Relative(.5f), SizeValue.Relative(.6f));

            Content = _mainLayout;

            AllowDragDrop = true;

            InitializeCiphers();
            UpdateParameters();
        }

        private void InitializeCiphers()
        {
            var xorCipher = new CipherData(
                (stream, parameters) => new XorStream(stream, (byte[])parameters[0].Value),
                new CipherParameter("Key", Array.Empty<byte>()));
            var posXorCipher = new CipherData(
                (stream, parameters) => new PositionalXorStream(stream, (byte[])parameters[0].Value),
                new CipherParameter("Key", Array.Empty<byte>()));
            var seqXorCipher = new CipherData(
                (stream, parameters) => new SequentialXorStream(stream, (byte)parameters[0].Value, (byte)parameters[1].Value),
                new CipherParameter("Key", (byte)0),
                new CipherParameter("Step", (byte)0));
            var rotCipher = new CipherData(
                (stream, parameters) => new RotStream(stream, (byte)parameters[0].Value),
                new CipherParameter("Rotation", (byte)0));
            var aesEcbCipher = new CipherData(
                (stream, parameters) => new EcbStream(stream, (byte[])parameters[0].Value),
                new CipherParameter("Key", Array.Empty<byte>()));
            var aesCbcCipher = new CipherData(
                (stream, parameters) => new CbcStream(stream, (byte[])parameters[0].Value, (byte[])parameters[1].Value),
                new CipherParameter("Key", Array.Empty<byte>()),
                new CipherParameter("IV", Array.Empty<byte>()));
            var aesCtrCipher = new CipherData(
                (stream, parameters) => new CtrStream(stream, (byte[])parameters[0].Value, (byte[])parameters[1].Value, (bool)parameters[2].Value),
                new CipherParameter("Key", Array.Empty<byte>()),
                new CipherParameter("Ctr", Array.Empty<byte>()),
                new CipherParameter("Ctr LE?", false));
            var aesXtsCipher = new CipherData(
                (stream, parameters) => new XtsStream(stream, (byte[])parameters[0].Value, (byte[])parameters[1].Value, (bool)parameters[2].Value,
                    (bool)parameters[3].Value, (int)parameters[4].Value),
                new CipherParameter("Key", Array.Empty<byte>()),
                new CipherParameter("SectorId", Array.Empty<byte>()),
                new CipherParameter("Advance Sector?", false),
                new CipherParameter("SectorId LE?", false),
                new CipherParameter("SectorSize", 0));
            var blowfishCipher = new CipherData(
                (stream, parameters) => new BlowfishStream(stream, (byte[])parameters[0].Value),
                new CipherParameter("Key", Array.Empty<byte>()));
            var intiCipher = new CipherData(
                (stream, parameters) => new IntiCreatesStream(stream, (string)parameters[0].Value),
                new CipherParameter("Password", string.Empty));

            _ciphers.Items.Add(new DropDownItem<CipherData>(xorCipher, "Xor"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(posXorCipher, "Positional Xor"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(seqXorCipher, "Sequential Xor"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(rotCipher, "Rot"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(aesEcbCipher, "AES ECB"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(aesCbcCipher, "AES CBC"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(aesCtrCipher, "AES CTR"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(aesXtsCipher, "AES XTS"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(blowfishCipher, "Blowfish"));
            _ciphers.Items.Add(new DropDownItem<CipherData>(intiCipher, "IntiCreates"));

            _ciphers.SelectedItem = _ciphers.Items[0];
        }

        private void UpdateParameters()
        {
            if (_ciphers.SelectedItem is null || _ciphers.SelectedItem.Content.Parameters.Length <= 0)
                _settingsLayout.Items[^3] = new Panel();

            _parameterLayout.Rows.Clear();

            for (var i = 0; i < _ciphers.SelectedItem.Content.Parameters.Length; i++)
            {
                _parameterLayout.Rows.Add(new TableRow
                {
                    Cells =
                    {
                        new Label(_ciphers.SelectedItem.Content.Parameters[i].Name),
                        CreateParameterComponent(i)
                    }
                });
            }

            _settingsLayout.Items[^3] = new Panel(_fullParameterLayout);
        }

        private Component CreateParameterComponent(int index)
        {
            CipherParameter parameter = _ciphers.SelectedItem.Content.Parameters[index];

            switch (parameter.Value)
            {
                case bool boolValue:
                    var checkBox = new CheckBox { Checked = boolValue };
                    checkBox.CheckChanged += (_, _) => parameter.Value = checkBox.Checked;

                    return checkBox;

                case string stringValue:
                    var textBox = new TextBox { Text = stringValue };
                    textBox.TextChanged += (_, _) => parameter.Value = textBox.Text;

                    return textBox;

                case byte byteValue:
                    var textBox3 = new TextBox { Text = $"{byteValue}" };
                    textBox3.TextChanged += (_, _) => parameter.Value = byte.TryParse(textBox3.Text, out byte parsedValue) ? parsedValue : parameter.Value;

                    return textBox3;

                case int intValue:
                    var textBox1 = new TextBox { Text = $"{intValue}" };
                    textBox1.TextChanged += (_, _) => parameter.Value = int.TryParse(textBox1.Text, out int parsedValue) ? parsedValue : parameter.Value;

                    return textBox1;

                case byte[] dataValue:
                    var textBox2 = new TextBox { Text = $"0x{Convert.ToHexString(dataValue)}" };
                    textBox2.TextChanged += (_, _) => parameter.Value = GetData(textBox2);

                    return textBox2;

                default:
                    throw new InvalidOperationException("Unsupported parameter type.");
            }
        }

        private static byte[] GetData(TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text))
                return [];

            if (!textBox.Text.StartsWith("0x"))
                return [];

            if (textBox.Text.Length <= 2)
                return [];

            try
            {
                return Convert.FromHexString(textBox.Text[2..]);
            }
            catch (Exception e)
            {
                return [];
            }
        }
    }

    class CipherData
    {
        private readonly Func<Stream, CipherParameter[], Stream> _cipherWrapper;

        public CipherParameter[] Parameters { get; }

        public CipherData(Func<Stream, CipherParameter[], Stream> cipherWrapper, params CipherParameter[] parameters)
        {
            _cipherWrapper = cipherWrapper;

            Parameters = parameters;
        }

        public void Decrypt(Stream input, Stream output)
        {
            input = _cipherWrapper(input, Parameters);
            input.CopyTo(output);

            output.Flush();
        }

        public void Encrypt(Stream input, Stream output)
        {
            output = _cipherWrapper(output, Parameters);
            input.CopyTo(output);

            output.Flush();
        }
    }

    class CipherParameter(string name, object value)
    {
        public string Name { get; } = name;

        public object Value { get; set; } = value;
    }
}
