using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kanvas;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.Encoding;
using Kanvas.Enums.Quantization.ColorCache.LocalSensitivityHash;
using Kanvas.Quantization.ColorCache;
using Kanvas.Quantization.ColorDitherer.ErrorDiffusion;
using Kanvas.Quantization.ColorDitherer.Ordered;
using Kanvas.Quantization.ColorQuantizer;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class ImageTranscoderDialog : Modal
    {
        private StackLayout _mainLayout;

        private MenuBarButton _openBtn;

        private StackLayout _imageCompareLayout;
        private TableLayout _transcodingSettingsLayout;

        private ImageButton _exportBtn;

        private PictureBox _compareImageBox;
        private ZoomablePictureBox _origImageBox;
        private ZoomablePictureBox _transcodedImageBox;

        private ComboBox<int> _formats;
        private ComboBox<int> _paletteFormats;
        private ComboBox<CreateColorQuantizerDelegate> _quantizers;
        private ComboBox<CreateColorCacheDelegate> _caches;
        private ComboBox<CreateColorDithererDelegate?> _ditherers;
        private TextBox _countText;

        private EncodingDefinition _encodingDefinition;

        private void InitializeComponent()
        {
            #region Components

            _openBtn = new MenuBarButton
            {
                Text = LocalizationResources.MenuToolsImageTranscoderFileOpen,
                KeyAction = new(ModifierKeys.Control, Key.O, LocalizationResources.MenuToolsImageTranscoderFileOpenShortcut)
            };

            _exportBtn = new ImageButton(ImageResources.ImageExport)
            {
                Tooltip = LocalizationResources.ImageMenuExport,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };

            _compareImageBox = new PictureBox(ImageResources.ArrowRight);
            _origImageBox = new ZoomablePictureBox { ShowBorder = true };
            _transcodedImageBox = new ZoomablePictureBox { ShowBorder = true };

            _formats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, MaxShowItems = 10 };
            _paletteFormats = new ComboBox<int> { Alignment = ComboBoxAlignment.Top, MaxShowItems = 10 };

            _quantizers = new ComboBox<CreateColorQuantizerDelegate> { Alignment = ComboBoxAlignment.Top, MaxShowItems = 10 };
            _caches = new ComboBox<CreateColorCacheDelegate> { Alignment = ComboBoxAlignment.Top, MaxShowItems = 10 };
            _ditherers = new ComboBox<CreateColorDithererDelegate?> { Alignment = ComboBoxAlignment.Top, MaxShowItems = 10 };
            _countText = new TextBox { Text = "256", AllowedCharacters = CharacterRestriction.Decimal };

            #endregion

            #region Layout

            _imageCompareLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    _origImageBox,
                    new StackItem(_compareImageBox){Size = Size.Content,VerticalAlignment = VerticalAlignment.Center},
                    _transcodedImageBox
                }
            };

            _transcodingSettingsLayout = new TableLayout
            {
                Size = Size.Content,
                Spacing = new(4, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new Label(LocalizationResources.MenuToolsImageTranscoderEncoding),
                            new Label(LocalizationResources.MenuToolsImageTranscoderPaletteEncoding),
                            new Label(LocalizationResources.MenuToolsImageTranscoderQuantizer),
                            new Label(LocalizationResources.MenuToolsImageTranscoderColorCache),
                            new Label(LocalizationResources.MenuToolsImageTranscoderDitherer),
                            new Label(LocalizationResources.MenuToolsImageTranscoderColorCount)
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            _formats,
                            _paletteFormats,
                            _quantizers,
                            _caches,
                            _ditherers,
                            _countText
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
                    new StackItem(_exportBtn){HorizontalAlignment = HorizontalAlignment.Right},
                    _imageCompareLayout,
                    _transcodingSettingsLayout
                }
            };

            var mainMenu = new ModalMenuBar
            {
                Items =
                {
                    new MenuBarMenu(LocalizationResources.MenuToolsImageTranscoderFile)
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
            InitializeQuantizers();
            InitializeColorCaches();
            InitializeColorDitherers();

            Caption = LocalizationResources.MenuToolsImageTranscoderCaption;

            MenuBar = mainMenu;
            Content = _mainLayout;
            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            AllowDragDrop = true;
        }

        private void InitializeFormats()
        {
            _encodingDefinition = new EncodingDefinition();

            InitializePaletteEncodings(_encodingDefinition);
            InitializeEncodings(_encodingDefinition);

            _paletteFormats.SelectedItem = _paletteFormats.Items.FirstOrDefault()!;
            _formats.SelectedItem = _formats.Items.FirstOrDefault()!;
        }

        private void InitializeQuantizers()
        {
            _quantizers.Items.Add(new DropDownItem<CreateColorQuantizerDelegate>(
                (count, taskCount) => new DistinctSelectionColorQuantizer(count, taskCount), "Distinct Selection"));
            _quantizers.Items.Add(new DropDownItem<CreateColorQuantizerDelegate>(
                (count, _) => new WuColorQuantizer(6, 2, count), "Wu"));

            _quantizers.SelectedItem = _quantizers.Items.FirstOrDefault()!;
        }

        private void InitializeColorCaches()
        {
            _caches.Items.Add(new DropDownItem<CreateColorCacheDelegate>(
                palette => new EuclideanDistanceColorCache(palette), "Euclidean Distance"));
            _caches.Items.Add(new DropDownItem<CreateColorCacheDelegate>(
                palette => new LocalitySensitiveHashColorCache(palette, ColorModel.Rgba), "Locality Sensitivity"));
            _caches.Items.Add(new DropDownItem<CreateColorCacheDelegate>(
                palette => new OctreeColorCache(palette), "Octree"));

            _caches.SelectedItem = _caches.Items.FirstOrDefault()!;
        }

        private void InitializeColorDitherers()
        {
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>(null, LocalizationResources.MenuToolsImageTranscoderNoDitherer));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new Bayer8Ditherer(size, count), "Bayer 8"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new Bayer4Ditherer(size, count), "Bayer 4"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new Bayer2Ditherer(size, count), "Bayer 2"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new ClusteredDotDitherer(size, count), "Clustered Dot"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new DotHalfToneDitherer(size, count), "Dot Half Tone"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new AtkinsonDitherer(size, count), "Atkinson"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new BurkesDitherer(size, count), "Burkes"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new FloydSteinbergDitherer(size, count), "Floyd Steinberg"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new JarvisJudiceNinkeDitherer(size, count), "Jarvis Judice Ninke"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new ShiauFanDitherer(size, count), "Shiau Fan"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new ShiauFan2Ditherer(size, count), "Shiau Fan 2"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new SierraLiteDitherer(size, count), "Sierra Lite"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new Sierra2RowDitherer(size, count), "Sierra 2"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new Sierra3RowDitherer(size, count), "Sierra 3"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new StuckiDitherer(size, count), "Stucki"));
            _ditherers.Items.Add(new DropDownItem<CreateColorDithererDelegate?>((size, count) => new ZhigangFanDitherer(size, count), "Zhigang"));

            _ditherers.SelectedItem = _ditherers.Items.FirstOrDefault()!;
        }

        private void InitializePaletteEncodings(EncodingDefinition encodingDefinition)
        {
            AddPaletteEncoding(encodingDefinition, 00, ImageFormats.Rgba8888());
            AddPaletteEncoding(encodingDefinition, 01, ImageFormats.Rgba1010102());
            AddPaletteEncoding(encodingDefinition, 02, ImageFormats.Rgb888());
            AddPaletteEncoding(encodingDefinition, 03, ImageFormats.Rgba5551());
            AddPaletteEncoding(encodingDefinition, 04, ImageFormats.Rgba4444());
            AddPaletteEncoding(encodingDefinition, 05, ImageFormats.Rgb565());
            AddPaletteEncoding(encodingDefinition, 06, ImageFormats.Rgb555());
            AddPaletteEncoding(encodingDefinition, 07, ImageFormats.Rg88());
            AddPaletteEncoding(encodingDefinition, 08, ImageFormats.La88());
            AddPaletteEncoding(encodingDefinition, 09, ImageFormats.La44());
            AddPaletteEncoding(encodingDefinition, 10, ImageFormats.L8());
            AddPaletteEncoding(encodingDefinition, 11, ImageFormats.A8());
            AddPaletteEncoding(encodingDefinition, 12, ImageFormats.L4());
        }

        private void InitializeEncodings(EncodingDefinition encodingDefinition)
        {
            AddEncoding(encodingDefinition, 00, ImageFormats.Rgba8888());
            AddEncoding(encodingDefinition, 01, ImageFormats.Rgba1010102());
            AddEncoding(encodingDefinition, 02, ImageFormats.Rgb888());
            AddEncoding(encodingDefinition, 03, ImageFormats.Rgba5551());
            AddEncoding(encodingDefinition, 04, ImageFormats.Rgba4444());
            AddEncoding(encodingDefinition, 05, ImageFormats.Rgb565());
            AddEncoding(encodingDefinition, 06, ImageFormats.Rgb555());
            AddEncoding(encodingDefinition, 07, ImageFormats.Rg88());
            AddEncoding(encodingDefinition, 08, ImageFormats.La88());
            AddEncoding(encodingDefinition, 09, ImageFormats.La44());
            AddEncoding(encodingDefinition, 10, ImageFormats.L8());
            AddEncoding(encodingDefinition, 11, ImageFormats.A8());
            AddEncoding(encodingDefinition, 12, ImageFormats.L4());
            AddIndexEncoding(encodingDefinition, 13, ImageFormats.I8());
            AddIndexEncoding(encodingDefinition, 14, ImageFormats.I4());
            AddIndexEncoding(encodingDefinition, 15, ImageFormats.I2());
            AddIndexEncoding(encodingDefinition, 16, ImageFormats.Ia53());
            AddIndexEncoding(encodingDefinition, 17, ImageFormats.Ia35());
            AddEncoding(encodingDefinition, 18, ImageFormats.Dxt1());
            AddEncoding(encodingDefinition, 19, ImageFormats.Dxt3());
            AddEncoding(encodingDefinition, 20, ImageFormats.Dxt5());
            AddEncoding(encodingDefinition, 21, ImageFormats.Ati1());
            AddEncoding(encodingDefinition, 22, ImageFormats.Ati2());
            AddEncoding(encodingDefinition, 23, ImageFormats.Ati1A());
            AddEncoding(encodingDefinition, 24, ImageFormats.Ati1L());
            AddEncoding(encodingDefinition, 25, ImageFormats.Ati2AL());
            AddEncoding(encodingDefinition, 26, ImageFormats.Bc6H());
            AddEncoding(encodingDefinition, 27, ImageFormats.Bc7());
            AddEncoding(encodingDefinition, 28, ImageFormats.Atc());
            AddEncoding(encodingDefinition, 29, ImageFormats.AtcExplicit());
            AddEncoding(encodingDefinition, 30, ImageFormats.AtcInterpolated());
            AddEncoding(encodingDefinition, 31, ImageFormats.Etc1(false));
            AddEncoding(encodingDefinition, 32, ImageFormats.Etc1A4(false));
            AddEncoding(encodingDefinition, 33, ImageFormats.Etc2());
            AddEncoding(encodingDefinition, 34, ImageFormats.Etc2A());
            AddEncoding(encodingDefinition, 35, ImageFormats.Etc2A1());
            AddEncoding(encodingDefinition, 36, ImageFormats.EacR11());
            AddEncoding(encodingDefinition, 37, ImageFormats.EacRG11());
            AddEncoding(encodingDefinition, 38, ImageFormats.Pvrtc_4bpp());
            AddEncoding(encodingDefinition, 39, ImageFormats.Pvrtc_2bpp());
            AddEncoding(encodingDefinition, 40, ImageFormats.PvrtcA_4bpp());
            AddEncoding(encodingDefinition, 41, ImageFormats.PvrtcA_2bpp());
            AddEncoding(encodingDefinition, 42, ImageFormats.Pvrtc2_4bpp());
            AddEncoding(encodingDefinition, 43, ImageFormats.Pvrtc2_2bpp());
        }

        private void AddPaletteEncoding(EncodingDefinition encodingDefinition, int format, IColorEncoding encoding)
        {
            encodingDefinition.AddPaletteEncoding(format, encoding);
            _paletteFormats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }

        private void AddEncoding(EncodingDefinition encodingDefinition, int format, IColorEncoding encoding)
        {
            encodingDefinition.AddColorEncoding(format, encoding);
            _formats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }

        private void AddIndexEncoding(EncodingDefinition encodingDefinition, int format, IIndexEncoding encoding)
        {
            encodingDefinition.AddIndexEncoding(format, encoding, _paletteFormats.Items.Select(f => f.Content).ToArray());
            _formats.Items.Add(new DropDownItem<int>(format, encoding.FormatName));
        }
    }
}
