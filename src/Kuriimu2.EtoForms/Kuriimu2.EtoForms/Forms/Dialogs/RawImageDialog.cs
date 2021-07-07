using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Eto.Forms;
using Kanvas;
using Kanvas.Configuration;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.IO;
using Kuriimu2.EtoForms.Extensions;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Resources;
using Kuriimu2.EtoForms.Support;
using Bitmap = Eto.Drawing.Bitmap;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class RawImageDialog : Dialog
    {
        private string _openedPath;
        private Stream _openedFile;

        private readonly ParameterBuilder _encodingsBuilder;
        private readonly ParameterBuilder _palEncodingBuilder;
        private readonly ParameterBuilder _swizzlesBuilder;

        private ExtensionType SelectedColorEncodingExtension => (encodings.SelectedValue as ExtensionTypeElement)?.Value;

        private ExtensionType SelectedPaletteEncodingExtension => (palEncodings.SelectedValue as ExtensionTypeElement)?.Value;

        private ExtensionType SelectedSwizzleExtension => (swizzles.SelectedValue as ExtensionTypeElement)?.Value;

        #region Localization Keys

        private const string NoSwizzleKey_ = "NoSwizzle";
        private const string CustomSwizzleKey_ = "CustomSwizzle";

        private const string ComponentOrderKey_ = "ComponentOrder";
        private const string ByteOrderKey_ = "ByteOrder";
        private const string BitOrderKey_ = "BitOrder";
        private const string ZOrderKey_ = "ZOrder";
        private const string AlphaLuminanceKey_ = "AlphaLuminance";
        private const string AlphaModeKey_ = "AlphaMode";
        private const string SwizzleModeKey_ = "SwizzleMode";
        private const string BitMappingKey_ = "BitMapping";
        private const string InitPointKey_ = "InitPoint";
        private const string YTransformKey_ = "YTransform";

        private const string InvalidInputValuesStatusKey_ = "InvalidInputValuesStatus";
        private const string InvalidCustomSwizzleStatusKey_ = "InvalidCustomSwizzleStatus";

        private const string NoFileSelectedStatusKey_ = "NoFileSelectedStatus";
        private const string SelectedFileNotExistStatusKey_ = "SelectedFileNotExistStatus";

        #endregion

        public RawImageDialog()
        {
            InitializeComponent();

            _encodingsBuilder = new ParameterBuilder(encodingParameters);
            _palEncodingBuilder = new ParameterBuilder(palEncodingParameters);
            _swizzlesBuilder = new ParameterBuilder(swizzleParameters);

            _encodingsBuilder.ValueChanged += _encodingsBuilder_ValueChanged;
            _swizzlesBuilder.ValueChanged += _swizzlesBuilder_ValueChanged;

            encodings.DataStore = GetEncodings().Select(x => new ExtensionTypeElement(x)).ToList();
            palEncodings.DataStore = GetPaletteEncodings().Select(x => new ExtensionTypeElement(x)).ToList();
            swizzles.DataStore = GetSwizzles().Select(x => new ExtensionTypeElement(x)).ToList();

            encodings.SelectedValueChanged += Encodings_SelectedValueChanged;
            palEncodings.SelectedValueChanged += PalEncodings_SelectedValueChanged;
            swizzles.SelectedValueChanged += Swizzles_SelectedValueChanged;

            encodings.SelectedValue = encodings.DataStore.First();
            palEncodings.SelectedValue = palEncodings.DataStore.First();
            swizzles.SelectedValue = swizzles.DataStore.First();

            widthText.TextChanged += WidthText_TextChanged;
            heightText.TextChanged += HeightText_TextChanged;
            offsetText.TextChanged += OffsetText_TextChanged;
            palOffsetText.TextChanged += PalOffsetText_TextChanged; ;

            #region Commands

            openFileCommand.Executed += OpenFileCommand_Executed;
            closeFileCommand.Executed += CloseFileCommand_Executed;
            extractImageCommand.Executed += ExtractImageCommand_Executed;
            processCommand.Executed += ProcessCommand_Executed;

            #endregion
        }

        #region Update

        private void UpdateImage()
        {
            SetStatus(string.Empty);

            if (_openedFile == null)
                return;

            if (!int.TryParse(offsetText.Text, out var offset) &&
                !int.TryParse(offsetText.Text.Replace("0x", string.Empty), NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out offset) ||
                !int.TryParse(palOffsetText.Text, out var palOffset) &&
                !int.TryParse(palOffsetText.Text.Replace("0x", string.Empty), NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out palOffset) ||
                !int.TryParse(widthText.Text, out var width) ||
                !int.TryParse(heightText.Text, out var height))
            {
                SetStatus(Localize(InvalidInputValuesStatusKey_));
                return;
            }

            if (offset < 0 || offset >= _openedFile.Length || width <= 0 || height <= 0)
            {
                SetStatus(Localize(InvalidInputValuesStatusKey_));
                return;
            }

            if (SelectedSwizzleExtension.Name == Localize(CustomSwizzleKey_) && CheckCustomSwizzle())
            {
                SetStatus(Localize(InvalidCustomSwizzleStatusKey_));
                return;
            }

            UpdateParameters(false);
            UpdateForm(false);

            try
            {
                var encoding = CreateEncoding(SelectedColorEncodingExtension);
                var indexEncoding = CreateIndexEncoding(SelectedColorEncodingExtension);
                var paletteEncoding = CreateEncoding(SelectedPaletteEncodingExtension);
                var colorEncodingInfo = (IEncodingInfo)encoding ?? indexEncoding;

                var swizzle = CreateSwizzle(new SwizzlePreparationContext(colorEncodingInfo, new System.Drawing.Size(width, height)));

                var colorCount = swizzle == null ? width * height : swizzle.Width * swizzle.Height;
                var dataSize = colorCount / colorEncodingInfo.ColorsPerValue * colorEncodingInfo.BitsPerValue / 8;
                var imgData = new byte[Math.Max(0, Math.Min(_openedFile.Length - offset, dataSize))];

                _openedFile.Position = offset;
                _openedFile.Read(imgData, 0, imgData.Length);

                var imageConfiguration = new ImageConfiguration();
                if (SelectedSwizzleExtension.Name != Localize(NoSwizzleKey_))
                    imageConfiguration.RemapPixels.With(CreateSwizzle);

                System.Drawing.Bitmap image;
                if (encoding != null)
                {
                    var transcoder = imageConfiguration
                        .Transcode.With(encoding)
                        .Build();
                    image = transcoder.Decode(imgData, new System.Drawing.Size(width, height));
                }
                else
                {
                    colorCount = 1 << colorEncodingInfo.BitsPerValue;
                    dataSize = colorCount / paletteEncoding.ColorsPerValue * paletteEncoding.BitsPerValue / 8;
                    var palData = new byte[Math.Max(0, Math.Min(_openedFile.Length - palOffset, dataSize))];

                    _openedFile.Position = palOffset;
                    _openedFile.Read(palData, 0, palData.Length);

                    var transcoder = imageConfiguration
                        .Transcode.With(indexEncoding)
                        .TranscodePalette.With(paletteEncoding)
                        .Build();
                    image = transcoder.Decode(imgData, palData, new System.Drawing.Size(width, height));
                }

                imageView.Image = image.ToEto();
                imageView.Invalidate();

                SetStatus();
            }
            catch (Exception e)
            {
                SetStatus(e.Message);
            }

            UpdateParameters(true);
            UpdateForm(true);
        }

        private void UpdateEncodingProperties()
        {
            _encodingsBuilder.SetParameters(SelectedColorEncodingExtension?.Parameters.Values.ToArray());
        }

        private void UpdatePaletteEncodingProperties()
        {
            _palEncodingBuilder.SetParameters(SelectedPaletteEncodingExtension?.Parameters.Values.ToArray());
        }

        private void UpdateSwizzleProperties()
        {
            _swizzlesBuilder.SetParameters(SelectedSwizzleExtension?.Parameters.Values.ToArray());
        }

        private void UpdateParameters(bool toggle)
        {
            encodingParameters.Enabled = toggle;
            palEncodingParameters.Enabled = toggle;
            swizzleParameters.Enabled = toggle;
        }

        private void UpdateForm(bool toggle)
        {
            encodings.Enabled = toggle;
            palEncodings.Enabled = toggle;
            swizzles.Enabled = toggle;
            openFileCommand.Enabled = toggle;
            processCommand.Enabled = toggle;
        }

        #endregion

        #region Setup

        #region Encodings

        private IList<ExtensionType> GetEncodings()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("RGBA8888", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB888", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB565", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB555", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA5551", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA4444", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RG88", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RG"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("R8", true),
                new ExtensionType("G8", true),
                new ExtensionType("B8", true),

                new ExtensionType("LA88", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "LA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("LA44", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "LA")),
                new ExtensionType("L8", true),
                new ExtensionType("A8", true),
                new ExtensionType("L4", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst)),
                new ExtensionType("A4", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst)),

                new ExtensionType("I2", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst)),
                new ExtensionType("I4", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst)),
                new ExtensionType("I8", true),
                new ExtensionType("IA35", true),
                new ExtensionType("IA53", true),

                new ExtensionType("ETC1", true,
                    new ExtensionTypeParameter(Localize(ZOrderKey_), typeof(bool), false)),
                new ExtensionType("ETC1A4", true,
                    new ExtensionTypeParameter(Localize(ZOrderKey_), typeof(bool), false)),

                new ExtensionType("ETC2 RGB", true),
                new ExtensionType("ETC2 RGBA", true),
                new ExtensionType("ETC2 RGBA1", true),
                new ExtensionType("EAC R11", true),
                new ExtensionType("EAC RG11", true),

                new ExtensionType("DXT1", true),
                new ExtensionType("DXT3", true),
                new ExtensionType("DXT5", true),
                new ExtensionType("ATI1", true),
                new ExtensionType("ATI1L", true),
                new ExtensionType("ATI1A", true),
                new ExtensionType("ATI2", true,
                    new ExtensionTypeParameter(Localize(AlphaLuminanceKey_), typeof(bool), false)),

                new ExtensionType("ATC", true),
                new ExtensionType("ATCA", true,
                    new ExtensionTypeParameter(Localize(AlphaModeKey_), typeof(AtcAlpha), AtcAlpha.Explicit)),

                new ExtensionType("BC7", true),

                new ExtensionType("PVRTC 2Bpp", true),
                new ExtensionType("PVRTC 4Bpp", true),
                new ExtensionType("PVRTCA 2Bpp", true),
                new ExtensionType("PVRTCA 4Bpp", true),
                new ExtensionType("PVRTC2 2Bpp", true),
                new ExtensionType("PVRTC2 4Bpp", true),

                new ExtensionType("ASTC 4x4", true)
            };
        }

        private IList<ExtensionType> GetPaletteEncodings()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("RGBA8888", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB888", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB565", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB555", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGB"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA5551", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA4444", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RGBA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RG88", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "RG"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("R8", true),
                new ExtensionType("G8", true),
                new ExtensionType("B8", true),

                new ExtensionType("LA88", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "LA"),
                    new ExtensionTypeParameter(Localize(ByteOrderKey_), typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("LA44", true,
                    new ExtensionTypeParameter(Localize(ComponentOrderKey_), typeof(string), "LA")),
                new ExtensionType("L8", true),
                new ExtensionType("A8", true),
                new ExtensionType("L4", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst)),
                new ExtensionType("A4", true,
                    new ExtensionTypeParameter(Localize(BitOrderKey_), typeof(BitOrder), BitOrder.MostSignificantBitFirst))
            };
        }

        private IColorEncoding CreateEncoding(ExtensionType extType)
        {
            switch (extType.Name)
            {
                case "RGBA8888":
                    var componentOrder1 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder1 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(8, 8, 8, 8, componentOrder1, byteOrder1);

                case "RGB888":
                    var componentOrder2 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder2 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(8, 8, 8, componentOrder2, byteOrder2);

                case "RGB565":
                    var componentOrder3 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder3 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(5, 6, 5, componentOrder3, byteOrder3);

                case "RGB555":
                    var componentOrder4 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder4 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(5, 5, 5, componentOrder4, byteOrder4);

                case "RGBA5551":
                    var componentOrder5 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder5 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(5, 5, 5, 1, componentOrder5, byteOrder5);

                case "RGBA4444":
                    var componentOrder6 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder6 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(4, 4, 4, 4, componentOrder6, byteOrder6);

                case "RG88":
                    var componentOrder7 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder7 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new Rgba(8, 8, 0, componentOrder7, byteOrder7);

                case "R8":
                    return new Rgba(8, 0, 0);

                case "G8":
                    return new Rgba(0, 8, 0);

                case "B8":
                    return new Rgba(0, 0, 8);

                case "LA88":
                    var componentOrder8 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    var byteOrder8 = extType.GetParameterValue<ByteOrder>(Localize(ByteOrderKey_));
                    return new La(8, 8, componentOrder8, byteOrder8);

                case "LA44":
                    var componentOrder9 = extType.GetParameterValue<string>(Localize(ComponentOrderKey_));
                    return new La(4, 4, componentOrder9);

                case "L8":
                    return ImageFormats.L8();

                case "A8":
                    return ImageFormats.A8();

                case "L4":
                    return ImageFormats.L4(extType.GetParameterValue<BitOrder>(Localize(BitOrderKey_)));

                case "A4":
                    return ImageFormats.A4(extType.GetParameterValue<BitOrder>(Localize(BitOrderKey_)));

                case "ETC1":
                    return ImageFormats.Etc1(extType.GetParameterValue<bool>(Localize(ZOrderKey_)));

                case "ETC1A4":
                    return ImageFormats.Etc1A4(extType.GetParameterValue<bool>(Localize(ZOrderKey_)));

                case "ETC2 RGB":
                    return ImageFormats.Etc2();

                case "ETC2 RGBA":
                    return ImageFormats.Etc2A();

                case "ETC2 RGBA1":
                    return ImageFormats.Etc2A1();

                case "EAC R11":
                    return ImageFormats.EacR11();

                case "EAC RG11":
                    return ImageFormats.EacRG11();

                case "DXT1":
                    return ImageFormats.Dxt1();

                case "DXT3":
                    return ImageFormats.Dxt3();

                case "DXT5":
                    return ImageFormats.Dxt5();

                case "ATI1":
                    return ImageFormats.Ati1();

                case "ATI1L":
                    return ImageFormats.Ati1L();

                case "ATI1A":
                    return ImageFormats.Ati1A();

                case "ATI2":
                    var wiiU = extType.GetParameterValue<bool>(Localize(AlphaLuminanceKey_));
                    return wiiU ? ImageFormats.Ati2() : ImageFormats.Ati2AL();

                case "ATC":
                    return ImageFormats.Atc();

                case "ATCA":
                    var atcAlpha = extType.GetParameterValue<AtcAlpha>(Localize(AlphaModeKey_));
                    return atcAlpha == AtcAlpha.Explicit ?
                        ImageFormats.AtcExplicit() :
                        ImageFormats.AtcInterpolated();

                case "BC7":
                    return ImageFormats.Bc7();

                case "PVRTC 2Bpp":
                    return ImageFormats.Pvrtc_2bpp();

                case "PVRTC 4Bpp":
                    return ImageFormats.Pvrtc_4bpp();

                case "PVRTCA 2Bpp":
                    return ImageFormats.PvrtcA_2bpp();

                case "PVRTCA 4Bpp":
                    return ImageFormats.PvrtcA_4bpp();

                case "PVRTC2 2Bpp":
                    return ImageFormats.Pvrtc2_2bpp();

                case "PVRTC2 4Bpp":
                    return ImageFormats.Pvrtc2_4bpp();

                case "ASTC 4x4":
                    return ImageFormats.Astc4x4();

                default:
                    return null;
            }
        }

        private IIndexEncoding CreateIndexEncoding(ExtensionType extType)
        {
            switch (extType.Name)
            {
                case "I2":
                    var bitOrder1 = extType.GetParameterValue<BitOrder>(Localize(BitOrderKey_));
                    return ImageFormats.I2(bitOrder1);

                case "I4":
                    var bitOrder2 = extType.GetParameterValue<BitOrder>(Localize(BitOrderKey_));
                    return ImageFormats.I4(bitOrder2);

                case "I8":
                    return ImageFormats.I8();

                case "IA53":
                    return ImageFormats.Ia53();

                case "IA35":
                    return ImageFormats.Ia35();

                default:
                    return null;
            }
        }

        #endregion

        #region Swizzles

        private IList<ExtensionType> GetSwizzles()
        {
            return new List<ExtensionType>
            {
                new ExtensionType(Localize(NoSwizzleKey_), true),
                new ExtensionType("BC", true),
                new ExtensionType("NDS", true),
                new ExtensionType("3DS", true),
                new ExtensionType("WiiU", true,
                    new ExtensionTypeParameter(Localize(SwizzleModeKey_), typeof(int))),
                new ExtensionType("Switch", true,
                    new ExtensionTypeParameter(Localize(SwizzleModeKey_), typeof(int))),
                new ExtensionType("PS2", true),
                new ExtensionType("Vita", true),

                new ExtensionType(Localize(CustomSwizzleKey_), true,
                    new ExtensionTypeParameter(Localize(BitMappingKey_), typeof(string), "{1,0},{0,1}"),
                    new ExtensionTypeParameter(Localize(InitPointKey_), typeof(string), "{0,0}"),
                    new ExtensionTypeParameter(Localize(YTransformKey_), typeof(string), ""))
            };
        }

        private IImageSwizzle CreateSwizzle(SwizzlePreparationContext context)
        {
            switch (SelectedSwizzleExtension.Name)
            {
                case "BC":
                    return new BcSwizzle(context);

                case "NDS":
                    return new NitroSwizzle(context);

                case "3DS":
                    return new CtrSwizzle(context);

                case "WiiU":
                    var swizzleTileMode = SelectedSwizzleExtension.GetParameterValue<byte>(Localize(SwizzleModeKey_));
                    return new CafeSwizzle(context, swizzleTileMode);

                case "Switch":
                    var swizzleMode = SelectedSwizzleExtension.GetParameterValue<int>(Localize(SwizzleModeKey_));
                    return new NxSwizzle(context, swizzleMode);

                case "PS2":
                    return new Ps2Swizzle(context);

                case "Vita":
                    return new VitaSwizzle(context);

                case "Custom":
                    var pointSequenceRegex = new Regex(@"\{([\d]+),([\d]+)\}[,]?");

                    // Bit mapping
                    var mappingText = SelectedSwizzleExtension.GetParameterValue<string>(Localize(BitMappingKey_)).Replace(" ", "");
                    var mappingPoints = pointSequenceRegex.Matches(mappingText).Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));

                    // Init point
                    var initPointText = SelectedSwizzleExtension.GetParameterValue<string>(Localize(InitPointKey_)).Replace(" ", "");
                    var initPointMatch = pointSequenceRegex.Match(initPointText);
                    var finalInitPoint = new System.Drawing.Point(int.Parse(initPointMatch.Groups[1].Value), int.Parse(initPointMatch.Groups[2].Value));

                    // Y Transform
                    var transformText = SelectedSwizzleExtension.GetParameterValue<string>(Localize(YTransformKey_)).Replace(" ", "");
                    var transformPoints = pointSequenceRegex.Matches(transformText).Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));

                    var masterSwizzle = new MasterSwizzle(context.Size.Width, finalInitPoint, mappingPoints.ToArray(), transformPoints.ToArray());
                    return new CustomSwizzle(context, masterSwizzle);

                default:
                    return null;
            }
        }

        private bool CheckCustomSwizzle()
        {
            var swizzleText = SelectedSwizzleExtension.GetParameterValue<string>(Localize(BitMappingKey_));
            var escapedSwizzleText = swizzleText.Replace(" ", "");

            var regex = new Regex(@"^({\d+,\d+}[,]?)+$");
            return regex.Match(escapedSwizzleText).Value != escapedSwizzleText;
        }

        #endregion

        #endregion

        #region Events

        private void OpenFileCommand_Executed(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void ExtractImageCommand_Executed(object sender, EventArgs e)
        {
            ExtractImage();
        }

        private void CloseFileCommand_Executed(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void WidthText_TextChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void HeightText_TextChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void OffsetText_TextChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void PalOffsetText_TextChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void ProcessCommand_Executed(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void Encodings_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateEncodingProperties();
            UpdateImage();
        }

        private void PalEncodings_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdatePaletteEncodingProperties();
            UpdateImage();
        }

        private void Swizzles_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateSwizzleProperties();
            UpdateImage();
        }

        private void _swizzlesBuilder_ValueChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void _encodingsBuilder_ValueChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        #endregion

        #region Support

        private string Localize(string name, params object[] args)
        {
            return string.Format(Application.Instance.Localize(this, name), args);
        }

        private void OpenFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog(this) != DialogResult.Ok)
            {
                SetStatus(Localize(NoFileSelectedStatusKey_));
                return;
            }

            if (!File.Exists(ofd.FileName))
            {
                SetStatus(Localize(SelectedFileNotExistStatusKey_));
                return;
            }

            _openedPath = ofd.FileName;
            _openedFile = File.OpenRead(ofd.FileName);
            closeFileCommand.Enabled = true;
            extractImageCommand.Enabled = true;

            UpdateImage();
        }

        private void ExtractImage()
        {
            var sfd = new SaveFileDialog
            {
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                FileName = $"{Path.GetFileName(_openedPath)}.png"
            };

            if (sfd.ShowDialog(this) != DialogResult.Ok)
            {
                SetStatus(Localize(NoFileSelectedStatusKey_));
                return;
            }

            (imageView.Image as Bitmap)?.Save(sfd.FileName, ImageFormat.Png);
        }

        private void CloseFile()
        {
            _openedPath = null;

            _openedFile?.Close();
            _openedFile = null;

            closeFileCommand.Enabled = false;
            extractImageCommand.Enabled = false;

            imageView.Image = null;
        }

        private void SetStatus(string message = "")
        {
            statusLabel.Text = message;
        }

        #endregion
    }

    enum AtcAlpha
    {
        Explicit,
        Interpolated
    }

    class CustomSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public CustomSwizzle(SwizzlePreparationContext context, MasterSwizzle swizzle)
        {
            _swizzle = swizzle;

            Width = (context.Size.Width + _swizzle.MacroTileWidth - 1) & -_swizzle.MacroTileWidth;
            Height = (context.Size.Height + _swizzle.MacroTileHeight - 1) & -_swizzle.MacroTileHeight;
        }

        public System.Drawing.Point Transform(System.Drawing.Point point) => _swizzle.Get(point.Y * Width + point.X);
    }

    class ExtensionTypeElement
    {
        public ExtensionType Value { get; }

        public ExtensionTypeElement(ExtensionType type)
        {
            Value = type;
        }

        public override string ToString()
        {
            return Value.Name;
        }
    }
}
