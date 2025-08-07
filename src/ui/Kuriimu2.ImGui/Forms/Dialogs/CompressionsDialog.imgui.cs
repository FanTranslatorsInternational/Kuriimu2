using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kompression;
using Kompression.Contract;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class CompressionsDialog : Modal
    {
        private StackLayout _mainLayout;
        private StackLayout _settingsLayout;

        private RadioButtonGroup _operations;

        private ComboBox<ICompression> _compressions;
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
                    new RadioButtonItem(LocalizationResources.MenuToolsCompressionsCompress),
                    new RadioButtonItem(LocalizationResources.MenuToolsCompressionsDecompress)
                }
            };
            _operations.SelectedItem = _operations.Items[0];

            _compressions = new ComboBox<ICompression> { Alignment = ComboBoxAlignment.Bottom, MaxShowItems = 10 };
            _inputTextBox = new TextBox { IsReadOnly = true };
            _fileBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCompressionsInputFile };
            _folderBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCompressionsInputFolder };
            _subDirCheckBox = new CheckBox { Text = LocalizationResources.MenuToolsCompressionsInputSubDirectories };
            _executeBtn = new Button { Width = SizeValue.Parent, Text = LocalizationResources.MenuToolsCompressionsExecute, KeyAction = new(Key.Enter) };
            _logEditor = new TextEditor { IsReadOnly = true };

            _progress = new ProgressBar
            {
                Size = new Size(SizeValue.Parent, 24),
                ProgressColor = ColorResources.Progress,
                Text = LocalizationResources.MenuToolsCompressionsProgress
            };

            #endregion

            #region Layouts

            _settingsLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                Size = new Size(.5f, SizeValue.Parent),
                ItemSpacing = 4,
                Items =
                {
                    _operations,
                    _compressions,
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
                    new StackItem(_executeBtn){Size = Size.Parent,VerticalAlignment = VerticalAlignment.Bottom},
                    new StackItem(_progress){Size = Size.WidthAlign}
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

            Caption = LocalizationResources.MenuToolsCompressionsCaption;
            Size = new Size(SizeValue.Relative(.5f), SizeValue.Relative(.6f));

            Content = _mainLayout;

            AllowDragDrop = true;

            InitializeCompressions();
        }

        private void InitializeCompressions()
        {
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Lz10.Build(), "Nintendo Lz10"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Lz11.Build(), "Nintendo Lz11"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Lz40.Build(), "Nintendo Lz40"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Lz60.Build(), "Nintendo Lz60"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Rle.Build(), "Nintendo Rle"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Huffman4Bit.Build(), "Nintendo Huffman 4Bit"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Huffman8Bit.Build(), "Nintendo Huffman 8Bit"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.BackwardLz77.Build(), "Backwards Lz77"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Mio0Be.Build(), "Mio0 BE"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Mio0Le.Build(), "Mio0 LE"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Yay0Be.Build(), "Yay0 BE"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Yay0Le.Build(), "Yay0 LE"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Yaz0Be.Build(), "Yaz0 BE"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Nintendo.Yaz0Le.Build(), "Yaz0 LE"));

            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Level5.Lz10.Build(), "Level5 Lz10"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Level5.Rle.Build(), "Level5 Rle"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Level5.Huffman4Bit.Build(), "Level5 Huffman 4Bit"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Level5.Huffman8Bit.Build(), "Level5 Huffman 8Bit"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Level5.Inazuma3Lzss.Build(), "Inazuma3 Lzss"));

            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Crilayla.Build(), "Crilayla"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Danganronpa3.Build(), "Danganronpa3"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Deflate.Build(), "Deflate"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Iecp.Build(), "Iecp"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.IrLz.Build(), "IrLz"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Lz4Headerless.Build(), "Lz4 Headerless"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Lz77.Build(), "Lz77"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.LzEcd.Build(), "LzEcd"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.LzEnc.Build(), "LzEnc"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Lze.Build(), "Lze"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Lzss.Build(), "Lzss"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.LzssVlc.Build(), "LzssVlc"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.PsLz.Build(), "PsLz"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.ShadeLz.Build(), "ShadeLz"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.ShadeLzHeaderless.Build(), "ShadeLz Headerless"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.SosLz3.Build(), "SosLz3"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.StingLz.Build(), "StingLz"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.TaikoLz80.Build(), "Taiko Lz80"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.TaikoLz81.Build(), "Taiko Lz81"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.TalesOf01.Build(), "TalesOf01"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.TalesOf03.Build(), "TalesOf03"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.Wp16.Build(), "Wp16"));
            _compressions.Items.Add(new DropDownItem<ICompression>(Compressions.ZLib.Build(), "ZLib"));

            _compressions.SelectedItem = _compressions.Items[0];
        }
    }
}
