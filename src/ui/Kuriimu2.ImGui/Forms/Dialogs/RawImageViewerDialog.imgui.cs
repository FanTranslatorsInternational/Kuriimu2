using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;
using Kanvas;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class RawImageViewerDialog : Modal
    {
        private static readonly KeyCommand CustomSwizzleCopyCommand = new(ImGuiKey.ModAlt, ImGuiKey.C);

        private MenuBarButton _openBtn;

        private StackLayout _mainLayout;
        private TableLayout _settingsLayout;

        private CheckBox _renderSwizzleBox;
        private ImageButton _exportBtn;

        private TextBox _widthTextBox;
        private TextBox _heightTextBox;
        private TextBox _offsetTextBox;
        private TextBox _paletteOffsetTextBox;
        private ComboBox<int> _formats;
        private ComboBox<int> _paletteFormats;
        private TextBox _componentsTextBox;
        private TextBox _paletteComponentsTextBox;

        private ComboBox<CreatePixelRemapperDelegate?> _swizzles;
        private TextBox _swizzleTextBox;

        private ZoomableSwizzlePictureBox _imageBox;
        private ZoomableSwizzleEditorPictureBox _imageEditorBox;

        private EncodingDefinition _encodingDefinition;

        private DropDownItem<CreatePixelRemapperDelegate?> _customSwizzleItem;

        private readonly Dictionary<int, string> _components = [];
        private readonly Dictionary<int, string> _paletteComponents = [];
        private readonly HashSet<DropDownItem<CreatePixelRemapperDelegate?>> _swizzleParameterItems = [];

        [MemberNotNull(nameof(_mainLayout), nameof(_settingsLayout))]
        [MemberNotNull(nameof(_renderSwizzleBox), nameof(_openBtn), nameof(_exportBtn))]
        [MemberNotNull(nameof(_widthTextBox), nameof(_heightTextBox), nameof(_offsetTextBox), nameof(_paletteOffsetTextBox))]
        [MemberNotNull(nameof(_formats), nameof(_paletteFormats), nameof(_componentsTextBox), nameof(_paletteComponentsTextBox))]
        [MemberNotNull(nameof(_swizzles), nameof(_swizzleTextBox))]
        [MemberNotNull(nameof(_imageBox), nameof(_imageEditorBox))]
        [MemberNotNull(nameof(_encodingDefinition), nameof(_customSwizzleItem))]
        private void InitializeComponent()
        {
            #region Components

            _openBtn = new MenuBarButton
            {
                Text = LocalizationResources.DialogToolsRawImageViewerFileOpen,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl, ImGuiKey.O, LocalizationResources.DialogToolsRawImageViewerFileOpenShortcut)
            };

            _renderSwizzleBox = new CheckBox
            {
                Text = LocalizationResources.DialogToolsRawImageViewerRenderSwizzle,
                Checked = true
            };
            _exportBtn = new ImageButton(ImageResources.ImageExport)
            {
                Tooltip = LocalizationResources.ImageMenuExport,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };

            _imageBox = new ZoomableSwizzlePictureBox
            {
                RenderSwizzle = true,
                ShowBorder = true
            };
            _imageEditorBox = new ZoomableSwizzleEditorPictureBox
            {
                RenderSwizzle = true,
                ShowBorder = true
            };

            _widthTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsRawImageViewerPlaceholder };
            _heightTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsRawImageViewerPlaceholder };
            _offsetTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsRawImageViewerPlaceholder };
            _paletteOffsetTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsRawImageViewerPlaceholder };
            _formats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _paletteFormats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _componentsTextBox = new TextBox();
            _paletteComponentsTextBox = new TextBox();

            _swizzles = new ComboBox<CreatePixelRemapperDelegate?> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _swizzleTextBox = new TextBox { Placeholder = LocalizationResources.DialogToolsRawImageViewerPlaceholder };

            #endregion

            #region Layouts

            _settingsLayout = new TableLayout
            {
                Size = Size.WidthAlign,
                Spacing = new Vector2(4, 4),
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerWidth)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerOffset)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerEncoding)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerEncodingComponentOrder)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerSwizzle))
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(_widthTextBox),
                            new TableCell(_offsetTextBox),
                            new TableCell(_formats),
                            new TableCell(_componentsTextBox),
                            new TableCell(_swizzles),
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerHeight)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerPaletteOffset)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerPaletteEncoding)),
                            new TableCell(new Label(LocalizationResources.DialogToolsRawImageViewerPaletteEncodingComponentOrder))
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(_heightTextBox),
                            new TableCell(_paletteOffsetTextBox),
                            new TableCell(_paletteFormats),
                            new TableCell(_paletteComponentsTextBox),
                            new TableCell(_swizzleTextBox)
                        }
                    }
                }
            };

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.WidthAlign,
                        ItemSpacing = 4,
                        Items =
                        {
                            _renderSwizzleBox,
                            new StackItem(_exportBtn){HorizontalAlignment = HorizontalAlignment.Right, Size = Size.WidthAlign}
                        }
                    },
                    _imageBox,
                    _settingsLayout
                }
            };

            var mainMenu = new ModalMenuBar
            {
                Items =
                {
                    new MenuBarMenu(LocalizationResources.DialogToolsRawImageViewerFile)
                    {
                        Items =
                        {
                            _openBtn
                        }
                    }
                }
            };

            #endregion

            InitializeFormats();
            InitializeSwizzles();

            Caption = LocalizationResources.DialogToolsRawImageViewerCaption;

            MenuBar = mainMenu;
            Content = _mainLayout;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            AllowDragDrop = true;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (CustomSwizzleCopyCommand.IsPressed() && IsCustomSwizzle())
            {
                (int, int)[] coords = GetCustomSwizzleCoordinates();
                Hexa.NET.ImGui.ImGui.SetClipboardText(string.Join(',', coords.Select(c => $"({c.Item1},{c.Item2})")));
            }

            base.UpdateInternal(contentRect);
        }

        [MemberNotNull(nameof(_encodingDefinition))]
        private void InitializeFormats()
        {
            _encodingDefinition = new EncodingDefinition();

            InitializePaletteEncodings(_encodingDefinition);
            InitializeEncodings(_encodingDefinition);

            _paletteFormats.SelectedItem = _paletteFormats.Items.FirstOrDefault()!;
            _formats.SelectedItem = _formats.Items.FirstOrDefault()!;
        }

        [MemberNotNull(nameof(_customSwizzleItem))]
        private void InitializeSwizzles()
        {
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(null, LocalizationResources.DialogToolsRawImageViewerNoSwizzle));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new BcSwizzle(context), "Bc"));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new NitroSwizzle(context), "NDS"));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new CtrSwizzle(context), "3DS"));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new DolphinSwizzle(context), "Gamecube"));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new RevolutionSwizzle(context), "Wii"));

            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new CafeSwizzle(context, (byte)GetValue(_swizzleTextBox)), "WiiU"));
            _swizzleParameterItems.Add(_swizzles.Items[^1]);

            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new NxSwizzle(context, GetValue(_swizzleTextBox)), "Switch"));
            _swizzleParameterItems.Add(_swizzles.Items[^1]);

            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new Ps2Swizzle(context), "PS2"));
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(context => new VitaSwizzle(context), "Vita"));

            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(null, LocalizationResources.DialogToolsRawImageViewerCustomSwizzle));
            _customSwizzleItem = _swizzles.Items[^1];

            _swizzles.SelectedItem = _swizzles.Items.FirstOrDefault()!;
        }

        private void UpdateFormats()
        {
            if (_paletteFormats.SelectedItem is null || _formats.SelectedItem is null)
                return;

            _encodingDefinition = new EncodingDefinition();

            int selectedPalette = _paletteFormats.Items.IndexOf(_paletteFormats.SelectedItem);
            int selectedFormat = _formats.Items.IndexOf(_formats.SelectedItem);

            _paletteFormats.Items.Clear();
            _formats.Items.Clear();

            InitializePaletteEncodings(_encodingDefinition, true);
            InitializeEncodings(_encodingDefinition, true);

            _paletteFormats.SelectedItem = _paletteFormats.Items[selectedPalette];
            _formats.SelectedItem = _formats.Items[selectedFormat];
        }

        private void InitializePaletteEncodings(EncodingDefinition encodingDefinition, bool isUpdate = false)
        {
            var index = 0;

            string components = isUpdate ? _paletteComponents[index] : "RGBA";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(8, 8, 8, 8, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGBA";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(10, 10, 10, 2, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGB";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(8, 8, 8, 0, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGBA";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(5, 5, 5, 1, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGBA";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(4, 4, 4, 4, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGB";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(5, 6, 5, 0, components), components);

            components = isUpdate ? _paletteComponents[index] : "RGB";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(5, 5, 5, 0, components), components);

            components = isUpdate ? _paletteComponents[index] : "RG";
            AddPaletteEncoding(encodingDefinition, index++, new Rgba(8, 8, 0, 0, components), components);

            components = isUpdate ? _paletteComponents[index] : "LA";
            AddPaletteEncoding(encodingDefinition, index++, new La(8, 8, components), components);

            components = isUpdate ? _paletteComponents[index] : "LA";
            AddPaletteEncoding(encodingDefinition, index++, new La(4, 4, components), components);

            AddPaletteEncoding(encodingDefinition, index++, ImageFormats.L8());
            AddPaletteEncoding(encodingDefinition, index++, ImageFormats.A8());
            AddPaletteEncoding(encodingDefinition, index++, ImageFormats.L4());
            AddPaletteEncoding(encodingDefinition, index++, ImageFormats.A4());
            AddPaletteEncoding(encodingDefinition, index++, new La(2, 0));
            AddPaletteEncoding(encodingDefinition, index++, new La(0, 2));
            AddPaletteEncoding(encodingDefinition, index++, new La(1, 0));
            AddPaletteEncoding(encodingDefinition, index, new La(0, 1));
        }

        private void InitializeEncodings(EncodingDefinition encodingDefinition, bool isUpdate = false)
        {
            var index = 0;

            string components = isUpdate ? _components[index] : "RGBA";
            AddEncoding(encodingDefinition, index++, new Rgba(8, 8, 8, 8, components), components);

            components = isUpdate ? _components[index] : "RGBA";
            AddEncoding(encodingDefinition, index++, new Rgba(10, 10, 10, 2, components), components);

            components = isUpdate ? _components[index] : "RGB";
            AddEncoding(encodingDefinition, index++, new Rgba(8, 8, 8, 0, components), components);

            components = isUpdate ? _components[index] : "RGBA";
            AddEncoding(encodingDefinition, index++, new Rgba(5, 5, 5, 1, components), components);

            components = isUpdate ? _components[index] : "RGBA";
            AddEncoding(encodingDefinition, index++, new Rgba(4, 4, 4, 4, components), components);

            components = isUpdate ? _components[index] : "RGB";
            AddEncoding(encodingDefinition, index++, new Rgba(5, 6, 5, 0, components), components);

            components = isUpdate ? _components[index] : "RGB";
            AddEncoding(encodingDefinition, index++, new Rgba(5, 5, 5, 0, components), components);

            components = isUpdate ? _components[index] : "RG";
            AddEncoding(encodingDefinition, index++, new Rgba(8, 8, 0, 0, components), components);

            components = isUpdate ? _components[index] : "LA";
            AddEncoding(encodingDefinition, index++, new La(8, 8, components), components);

            components = isUpdate ? _components[index] : "LA";
            AddEncoding(encodingDefinition, index++, new La(4, 4, components), components);

            AddEncoding(encodingDefinition, index++, ImageFormats.L8());
            AddEncoding(encodingDefinition, index++, ImageFormats.A8());
            AddEncoding(encodingDefinition, index++, ImageFormats.L4());
            AddEncoding(encodingDefinition, index++, ImageFormats.A4());
            AddEncoding(encodingDefinition, index++, new La(2, 0));
            AddEncoding(encodingDefinition, index++, new La(0, 2));
            AddEncoding(encodingDefinition, index++, new La(1, 0));
            AddEncoding(encodingDefinition, index++, new La(0, 1));
            AddIndexEncoding(encodingDefinition, index++, ImageFormats.I8());
            AddIndexEncoding(encodingDefinition, index++, ImageFormats.I4());
            AddIndexEncoding(encodingDefinition, index++, ImageFormats.I2());

            components = isUpdate ? _components[index] : "IA";
            AddIndexEncoding(encodingDefinition, index++, new Index(5, 3, components), components);

            components = isUpdate ? _components[index] : "IA";
            AddIndexEncoding(encodingDefinition, index++, new Index(3, 5, components), components);

            AddEncoding(encodingDefinition, index++, ImageFormats.Dxt1());
            AddEncoding(encodingDefinition, index++, ImageFormats.Dxt3());
            AddEncoding(encodingDefinition, index++, ImageFormats.Dxt5());
            AddEncoding(encodingDefinition, index++, ImageFormats.Ati1());
            AddEncoding(encodingDefinition, index++, ImageFormats.Ati2());
            AddEncoding(encodingDefinition, index++, ImageFormats.Ati1A());
            AddEncoding(encodingDefinition, index++, ImageFormats.Ati1L());
            AddEncoding(encodingDefinition, index++, ImageFormats.Ati2AL());
            AddEncoding(encodingDefinition, index++, ImageFormats.Bc6H());
            AddEncoding(encodingDefinition, index++, ImageFormats.Bc7());
            AddEncoding(encodingDefinition, index++, ImageFormats.Atc());
            AddEncoding(encodingDefinition, index++, ImageFormats.AtcExplicit());
            AddEncoding(encodingDefinition, index++, ImageFormats.AtcInterpolated());
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc1(false));
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc1A4(false));
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc1(true));
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc1A4(true));
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc2());
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc2A());
            AddEncoding(encodingDefinition, index++, ImageFormats.Etc2A1());
            AddEncoding(encodingDefinition, index++, ImageFormats.EacR11());
            AddEncoding(encodingDefinition, index++, ImageFormats.EacRG11());
            AddEncoding(encodingDefinition, index++, ImageFormats.Pvrtc_4bpp());
            AddEncoding(encodingDefinition, index++, ImageFormats.Pvrtc_2bpp());
            AddEncoding(encodingDefinition, index++, ImageFormats.PvrtcA_4bpp());
            AddEncoding(encodingDefinition, index++, ImageFormats.PvrtcA_2bpp());
            AddEncoding(encodingDefinition, index++, ImageFormats.Pvrtc2_4bpp());
            AddEncoding(encodingDefinition, index, ImageFormats.Pvrtc2_2bpp());
        }

        private void AddPaletteEncoding(EncodingDefinition encodingDefinition, int format, IColorEncoding encoding, string? components = null)
        {
            if (components is not null)
                _paletteComponents[format] = components;

            encodingDefinition.AddPaletteEncoding(format, encoding);
            _paletteFormats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }

        private void AddEncoding(EncodingDefinition encodingDefinition, int format, IColorEncoding encoding, string? components = null)
        {
            if (components is not null)
                _components[format] = components;

            encodingDefinition.AddColorEncoding(format, encoding);
            _formats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }

        private void AddIndexEncoding(EncodingDefinition encodingDefinition, int format, IIndexEncoding encoding, string? components = null)
        {
            if (components is not null)
                _components[format] = components;

            encodingDefinition.AddIndexEncoding(format, encoding, [.. _paletteFormats.Items.Select(f => f.Content)]);
            _formats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }
    }
}
