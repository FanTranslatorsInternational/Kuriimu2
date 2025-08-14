using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Kanvas;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class RawImageViewerDialog : Modal
    {
        private static readonly KeyCommand CustomSwizzleCopyCommand = new(ModifierKeys.Alt, Key.C);

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

        private readonly Dictionary<int, string> _components = new();
        private readonly Dictionary<int, string> _paletteComponents = new();
        private readonly HashSet<DropDownItem<CreatePixelRemapperDelegate?>> _swizzleParameterItems = new();

        private void InitializeComponent()
        {
            #region Components

            _openBtn = new MenuBarButton
            {
                Text = LocalizationResources.MenuToolsRawImageViewerFileOpen,
                KeyAction = new(ModifierKeys.Control, Key.O, LocalizationResources.MenuToolsRawImageViewerFileOpenShortcut)
            };

            _renderSwizzleBox = new CheckBox
            {
                Text = LocalizationResources.MenuToolsRawImageViewerRenderSwizzle,
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

            _widthTextBox = new TextBox { Placeholder = LocalizationResources.MenuToolsRawImageViewerPlaceholder };
            _heightTextBox = new TextBox { Placeholder = LocalizationResources.MenuToolsRawImageViewerPlaceholder };
            _offsetTextBox = new TextBox { Placeholder = LocalizationResources.MenuToolsRawImageViewerPlaceholder };
            _paletteOffsetTextBox = new TextBox { Placeholder = LocalizationResources.MenuToolsRawImageViewerPlaceholder };
            _formats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _paletteFormats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _componentsTextBox = new TextBox();
            _paletteComponentsTextBox = new TextBox();

            _swizzles = new ComboBox<CreatePixelRemapperDelegate?> { Alignment = ComboBoxAlignment.Top, Width = SizeValue.Parent };
            _swizzleTextBox = new TextBox { Placeholder = LocalizationResources.MenuToolsRawImageViewerPlaceholder };

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
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerWidth)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerOffset)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerEncoding)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerEncodingComponentOrder)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerSwizzle))
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
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerHeight)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerPaletteOffset)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerPaletteEncoding)),
                            new TableCell(new Label(LocalizationResources.MenuToolsRawImageViewerPaletteEncodingComponentOrder))
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
                    new MenuBarMenu(LocalizationResources.MenuToolsRawImageViewerFile)
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

            Caption = LocalizationResources.MenuToolsRawImageViewerCaption;

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
                ImGuiNET.ImGui.SetClipboardText(string.Join(',', coords.Select(c => $"({c.Item1},{c.Item2})")));
            }

            base.UpdateInternal(contentRect);
        }

        private void InitializeFormats()
        {
            _encodingDefinition = new EncodingDefinition();

            InitializePaletteEncodings(_encodingDefinition);
            InitializeEncodings(_encodingDefinition);

            _paletteFormats.SelectedItem = _paletteFormats.Items.FirstOrDefault()!;
            _formats.SelectedItem = _formats.Items.FirstOrDefault()!;
        }

        private void InitializeSwizzles()
        {
            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(null, LocalizationResources.MenuToolsRawImageViewerNoSwizzle));
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

            _swizzles.Items.Add(new DropDownItem<CreatePixelRemapperDelegate?>(null, LocalizationResources.MenuToolsRawImageViewerCustomSwizzle));
            _customSwizzleItem = _swizzles.Items[^1];

            _swizzles.SelectedItem = _swizzles.Items.FirstOrDefault()!;
        }

        private void UpdateFormats()
        {
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
            string components = isUpdate ? _paletteComponents[00] : "RGBA";
            AddPaletteEncoding(encodingDefinition, 00, new Rgba(8, 8, 8, 8, components), components);

            components = isUpdate ? _paletteComponents[01] : "RGBA";
            AddPaletteEncoding(encodingDefinition, 01, new Rgba(10, 10, 10, 2, components), components);

            components = isUpdate ? _paletteComponents[02] : "RGB";
            AddPaletteEncoding(encodingDefinition, 02, new Rgba(8, 8, 8, 0, components), components);

            components = isUpdate ? _paletteComponents[03] : "RGBA";
            AddPaletteEncoding(encodingDefinition, 03, new Rgba(5, 5, 5, 1, components), components);

            components = isUpdate ? _paletteComponents[04] : "RGBA";
            AddPaletteEncoding(encodingDefinition, 04, new Rgba(4, 4, 4, 4, components), components);

            components = isUpdate ? _paletteComponents[05] : "RGB";
            AddPaletteEncoding(encodingDefinition, 05, new Rgba(5, 6, 5, 0, components), components);

            components = isUpdate ? _paletteComponents[06] : "RGB";
            AddPaletteEncoding(encodingDefinition, 06, new Rgba(5, 5, 5, 0, components), components);

            components = isUpdate ? _paletteComponents[07] : "RG";
            AddPaletteEncoding(encodingDefinition, 07, new Rgba(8, 8, 0, 0, components), components);

            components = isUpdate ? _paletteComponents[08] : "LA";
            AddPaletteEncoding(encodingDefinition, 08, new La(8, 8, components), components);

            components = isUpdate ? _paletteComponents[09] : "LA";
            AddPaletteEncoding(encodingDefinition, 09, new La(4, 4, components), components);

            AddPaletteEncoding(encodingDefinition, 10, ImageFormats.L8());
            AddPaletteEncoding(encodingDefinition, 11, ImageFormats.A8());
            AddPaletteEncoding(encodingDefinition, 12, ImageFormats.L4());
            AddPaletteEncoding(encodingDefinition, 13, ImageFormats.A4());
        }

        private void InitializeEncodings(EncodingDefinition encodingDefinition, bool isUpdate = false)
        {
            string components = isUpdate ? _components[00] : "RGBA";
            AddEncoding(encodingDefinition, 00, new Rgba(8, 8, 8, 8, components), components);

            components = isUpdate ? _components[01] : "RGBA";
            AddEncoding(encodingDefinition, 01, new Rgba(10, 10, 10, 2, components), components);

            components = isUpdate ? _components[02] : "RGB";
            AddEncoding(encodingDefinition, 02, new Rgba(8, 8, 8, 0, components), components);

            components = isUpdate ? _components[03] : "RGBA";
            AddEncoding(encodingDefinition, 03, new Rgba(5, 5, 5, 1, components), components);

            components = isUpdate ? _components[04] : "RGBA";
            AddEncoding(encodingDefinition, 04, new Rgba(4, 4, 4, 4, components), components);

            components = isUpdate ? _components[05] : "RGB";
            AddEncoding(encodingDefinition, 05, new Rgba(5, 6, 5, 0, components), components);

            components = isUpdate ? _components[06] : "RGB";
            AddEncoding(encodingDefinition, 06, new Rgba(5, 5, 5, 0, components), components);

            components = isUpdate ? _components[07] : "RG";
            AddEncoding(encodingDefinition, 07, new Rgba(8, 8, 0, 0, components), components);

            components = isUpdate ? _components[08] : "LA";
            AddEncoding(encodingDefinition, 08, new La(8, 8, components), components);

            components = isUpdate ? _components[09] : "LA";
            AddEncoding(encodingDefinition, 09, new La(4, 4, components), components);

            AddEncoding(encodingDefinition, 10, ImageFormats.L8());
            AddEncoding(encodingDefinition, 11, ImageFormats.A8());
            AddEncoding(encodingDefinition, 12, ImageFormats.L4());
            AddEncoding(encodingDefinition, 13, ImageFormats.A4());
            AddIndexEncoding(encodingDefinition, 14, ImageFormats.I8());
            AddIndexEncoding(encodingDefinition, 15, ImageFormats.I4());
            AddIndexEncoding(encodingDefinition, 16, ImageFormats.I2());

            components = isUpdate ? _components[17] : "IA";
            AddIndexEncoding(encodingDefinition, 17, new Index(5, 3, components), components);

            components = isUpdate ? _components[18] : "IA";
            AddIndexEncoding(encodingDefinition, 18, new Index(3, 5, components), components);

            AddEncoding(encodingDefinition, 19, ImageFormats.Dxt1());
            AddEncoding(encodingDefinition, 20, ImageFormats.Dxt3());
            AddEncoding(encodingDefinition, 21, ImageFormats.Dxt5());
            AddEncoding(encodingDefinition, 22, ImageFormats.Ati1());
            AddEncoding(encodingDefinition, 23, ImageFormats.Ati2());
            AddEncoding(encodingDefinition, 24, ImageFormats.Ati1A());
            AddEncoding(encodingDefinition, 25, ImageFormats.Ati1L());
            AddEncoding(encodingDefinition, 26, ImageFormats.Ati2AL());
            AddEncoding(encodingDefinition, 27, ImageFormats.Bc6H());
            AddEncoding(encodingDefinition, 28, ImageFormats.Bc7());
            AddEncoding(encodingDefinition, 29, ImageFormats.Atc());
            AddEncoding(encodingDefinition, 30, ImageFormats.AtcExplicit());
            AddEncoding(encodingDefinition, 31, ImageFormats.AtcInterpolated());
            AddEncoding(encodingDefinition, 32, ImageFormats.Etc1(false));
            AddEncoding(encodingDefinition, 33, ImageFormats.Etc1A4(false));
            AddEncoding(encodingDefinition, 34, ImageFormats.Etc1(true));
            AddEncoding(encodingDefinition, 35, ImageFormats.Etc1A4(true));
            AddEncoding(encodingDefinition, 36, ImageFormats.Etc2());
            AddEncoding(encodingDefinition, 37, ImageFormats.Etc2A());
            AddEncoding(encodingDefinition, 38, ImageFormats.Etc2A1());
            AddEncoding(encodingDefinition, 39, ImageFormats.EacR11());
            AddEncoding(encodingDefinition, 40, ImageFormats.EacRG11());
            AddEncoding(encodingDefinition, 41, ImageFormats.Pvrtc_4bpp());
            AddEncoding(encodingDefinition, 42, ImageFormats.Pvrtc_2bpp());
            AddEncoding(encodingDefinition, 43, ImageFormats.PvrtcA_4bpp());
            AddEncoding(encodingDefinition, 44, ImageFormats.PvrtcA_2bpp());
            AddEncoding(encodingDefinition, 45, ImageFormats.Pvrtc2_4bpp());
            AddEncoding(encodingDefinition, 46, ImageFormats.Pvrtc2_2bpp());
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

            encodingDefinition.AddIndexEncoding(format, encoding, _paletteFormats.Items.Select(f => f.Content).ToArray());
            _formats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }
    }
}
