using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Stream _openedFile;

        private readonly ParameterBuilder _encodingsBuilder;
        private readonly ParameterBuilder _swizzlesBuilder;

        private ExtensionType SelectedColorEncodingExtension => (encodings.SelectedValue as ExtensionTypeElement)?.Value;

        private ExtensionType SelectedSwizzleExtension => (swizzles.SelectedValue as ExtensionTypeElement)?.Value;

        public RawImageDialog()
        {
            InitializeComponent();

            _encodingsBuilder = new ParameterBuilder(encodingParameters);
            _swizzlesBuilder = new ParameterBuilder(swizzleParameters);

            _encodingsBuilder.ValueChanged += _encodingsBuilder_ValueChanged;
            _swizzlesBuilder.ValueChanged += _swizzlesBuilder_ValueChanged;

            encodings.DataStore = GetEncodings().Select(x => new ExtensionTypeElement(x)).ToList();
            swizzles.DataStore = GetSwizzles().Select(x => new ExtensionTypeElement(x)).ToList();

            encodings.SelectedValueChanged += Encodings_SelectedValueChanged;
            swizzles.SelectedValueChanged += Swizzles_SelectedValueChanged;

            encodings.SelectedValue = encodings.DataStore.First();
            swizzles.SelectedValue = swizzles.DataStore.First();

            widthText.TextChanged += WidthText_TextChanged;
            heightText.TextChanged += HeightText_TextChanged;
            offsetText.TextChanged += OffsetText_TextChanged;

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
                !int.TryParse(offsetText.Text.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.CurrentCulture,
                    out offset) ||
                !int.TryParse(widthText.Text, out var width) ||
                !int.TryParse(heightText.Text, out var height))
            {
                SetStatus("Input values are invalid.");
                return;
            }

            if (offset < 0 || offset >= _openedFile.Length || width <= 0 || height <= 0)
            {
                SetStatus("Input values are invalid.");
                return;
            }

            if (SelectedSwizzleExtension.Name == "Custom" && CheckCustomSwizzle())
            {
                SetStatus("Custom swizzle is invalid.");
                return;
            }

            UpdateParameters(false);
            UpdateForm(false);

            try
            {
                var encoding = CreateEncoding();
                var swizzle = CreateSwizzle(new SwizzlePreparationContext(encoding, new System.Drawing.Size(width, height)));

                var colorCount = swizzle == null ? width * height : swizzle.Width * swizzle.Height;
                var dataSize = colorCount / encoding.ColorsPerValue * encoding.BitsPerValue / 8;
                var imgData = new byte[Math.Min(_openedFile.Length - offset, dataSize)];

                _openedFile.Position = offset;
                _openedFile.Read(imgData, 0, imgData.Length);

                var imageConfiguration = new ImageConfiguration();
                if (SelectedSwizzleExtension.Name != "None")
                    imageConfiguration.RemapPixels.With(context => CreateSwizzle(context));

                var transcoder = imageConfiguration.Transcode.With(encoding).Build();
                imageView.Image = transcoder.Decode(imgData, new System.Drawing.Size(width, height)).ToEto();
                imageView.Invalidate();
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

        private void UpdateSwizzleProperties()
        {
            _swizzlesBuilder.SetParameters(SelectedSwizzleExtension?.Parameters.Values.ToArray());
        }

        private void UpdateParameters(bool toggle)
        {
            encodingParameters.Enabled = toggle;
            swizzleParameters.Enabled = toggle;
        }

        private void UpdateForm(bool toggle)
        {
            encodings.Enabled = toggle;
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
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB888", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB565", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGB555", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA5551", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RGBA4444", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("RG88", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RG"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("R8", true),
                new ExtensionType("G8", true),
                new ExtensionType("B8", true),

                new ExtensionType("LA88", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "LA"),
                    new ExtensionTypeParameter("ByteOrder", typeof(ByteOrder), ByteOrder.LittleEndian)),
                new ExtensionType("LA44", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "LA")),
                new ExtensionType("L8", true),
                new ExtensionType("A8", true),
                new ExtensionType("L4", true,
                    new ExtensionTypeParameter("BitOrder", typeof(BitOrder), BitOrder.MostSignificantBitFirst)),
                new ExtensionType("A4", true,
                    new ExtensionTypeParameter("BitOrder", typeof(BitOrder), BitOrder.MostSignificantBitFirst)),

                new ExtensionType("ETC1", true,
                    new ExtensionTypeParameter("Z-Order", typeof(bool), false)),
                new ExtensionType("ETC1A4", true,
                    new ExtensionTypeParameter("Z-Order", typeof(bool), false)),

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
                    new ExtensionTypeParameter("Alpha/Luminance", typeof(bool), false)),

                new ExtensionType("ATC", true),
                new ExtensionType("ATCA", true,
                    new ExtensionTypeParameter("AlphaMode", typeof(AtcAlpha), AtcAlpha.Explicit)),

                new ExtensionType("BC7", true),

                new ExtensionType("ASTC 4x4", true)
            };
        }

        private IColorEncoding CreateEncoding()
        {
            switch (SelectedColorEncodingExtension.Name)
            {
                case "RGBA8888":
                    var componentOrder1 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder1 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(8, 8, 8, 8, componentOrder1, byteOrder1);

                case "RGB888":
                    var componentOrder2 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder2 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(8, 8, 8, componentOrder2, byteOrder2);

                case "RGB565":
                    var componentOrder3 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder3 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(5, 6, 5, componentOrder3, byteOrder3);

                case "RGB555":
                    var componentOrder4 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder4 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(5, 5, 5, componentOrder4, byteOrder4);

                case "RGBA5551":
                    var componentOrder5 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder5 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(5, 5, 5, 1, componentOrder5, byteOrder5);

                case "RGBA4444":
                    var componentOrder6 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder6 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(4, 4, 4, 4, componentOrder6, byteOrder6);

                case "RG88":
                    var componentOrder7 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder7 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new Rgba(8, 8, 0, componentOrder7, byteOrder7);

                case "R8":
                    return new Rgba(8, 0, 0);

                case "G8":
                    return new Rgba(0, 8, 0);

                case "B8":
                    return new Rgba(0, 0, 8);

                case "LA88":
                    var componentOrder8 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder8 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new La(8, 8, componentOrder8, byteOrder8);

                case "LA44":
                    var componentOrder9 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new La(4, 4, componentOrder9);

                case "L8":
                    return ImageFormats.L8();

                case "A8":
                    return ImageFormats.A8();

                case "L4":
                    return ImageFormats.L4(SelectedColorEncodingExtension.GetParameterValue<BitOrder>("BitOrder"));

                case "A4":
                    return ImageFormats.A4(SelectedColorEncodingExtension.GetParameterValue<BitOrder>("BitOrder"));

                case "ETC1":
                    return ImageFormats.Etc1(SelectedColorEncodingExtension.GetParameterValue<bool>("Z-Order"));

                case "ETC1A4":
                    return ImageFormats.Etc1A4(SelectedColorEncodingExtension.GetParameterValue<bool>("Z-Order"));

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
                    var wiiU = SelectedColorEncodingExtension.GetParameterValue<bool>("Alpha/Luminance");
                    return wiiU ? ImageFormats.Ati2() : ImageFormats.Ati2AL();

                case "ATC":
                    return ImageFormats.Atc();

                case "ATCA":
                    var atcAlpha = SelectedColorEncodingExtension.GetParameterValue<AtcAlpha>("AlphaMode");
                    return atcAlpha == AtcAlpha.Explicit ?
                        ImageFormats.AtcExplicit() :
                        ImageFormats.AtcInterpolated();

                case "BC7":
                    return ImageFormats.Bc7();

                case "ASTC 4x4":
                    return ImageFormats.Astc4x4();

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
                new ExtensionType("None", true),
                new ExtensionType("NDS", true),
                new ExtensionType("3DS", true),
                new ExtensionType("WiiU", true,
                    new ExtensionTypeParameter("SwizzleTileMode", typeof(int))),
                new ExtensionType("Switch", true,
                    new ExtensionTypeParameter("SwizzleMode", typeof(int))),

                new ExtensionType("Custom", true,
                    new ExtensionTypeParameter("BitMapping", typeof(string), "{1,0},{0,1}"),
                    new ExtensionTypeParameter("InitPoint", typeof(string), "{0,0}"),
                    new ExtensionTypeParameter("YTransform", typeof(string), ""))
            };
        }

        private IImageSwizzle CreateSwizzle(SwizzlePreparationContext context)
        {
            switch (SelectedSwizzleExtension.Name)
            {
                case "NDS":
                    return new NitroSwizzle(context);

                case "3DS":
                    return new CtrSwizzle(context);

                case "WiiU":
                    var swizzleTileMode = SelectedSwizzleExtension.GetParameterValue<byte>("SwizzleTileMode");
                    return new CafeSwizzle(context, swizzleTileMode);

                case "Switch":
                    var swizzleMode = SelectedSwizzleExtension.GetParameterValue<int>("SwizzleMode");
                    return new NxSwizzle(context, swizzleMode);

                case "Custom":
                    var pointSequenceRegex = new Regex(@"\{([\d]+),([\d]+)\}[,]?");

                    // Bit mapping
                    var mappingText = SelectedSwizzleExtension.GetParameterValue<string>("BitMapping").Replace(" ", "");
                    var mappingPoints = pointSequenceRegex.Matches(mappingText).Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));

                    // Init point
                    var initPointText = SelectedSwizzleExtension.GetParameterValue<string>("InitPoint").Replace(" ", "");
                    var initPointMatch = pointSequenceRegex.Match(initPointText);
                    var finalInitPoint = new System.Drawing.Point(int.Parse(initPointMatch.Groups[1].Value), int.Parse(initPointMatch.Groups[2].Value));

                    // Y Transform
                    var transformText = SelectedSwizzleExtension.GetParameterValue<string>("YTransform").Replace(" ", "");
                    var transformPoints = pointSequenceRegex.Matches(transformText).Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));

                    var masterSwizzle = new MasterSwizzle(context.Size.Width, finalInitPoint, mappingPoints.ToArray(), transformPoints.ToArray());
                    return new CustomSwizzle(context, masterSwizzle);

                default:
                    return null;
            }
        }

        private bool CheckCustomSwizzle()
        {
            var swizzleText = SelectedSwizzleExtension.GetParameterValue<string>("BitMapping");
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

        private void ProcessCommand_Executed(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void Encodings_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateEncodingProperties();
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

        private void OpenFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog(this) != DialogResult.Ok)
            {
                SetStatus("No file selected.");
                return;
            }

            if (!File.Exists(ofd.FileName))
            {
                SetStatus("Selected file does not exist.");
                return;
            }

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
                FileName = "extract.png"
            };

            if (sfd.ShowDialog(this) != DialogResult.Ok)
            {
                SetStatus("No file selected.");
                return;
            }

            (imageView.Image as Bitmap)?.Save(sfd.FileName, ImageFormat.Png);
        }

        private void CloseFile()
        {
            _openedFile?.Close();
            _openedFile = null;

            closeFileCommand.Enabled = false;
            extractImageCommand.Enabled = false;

            imageView.Image = null;
        }

        private void SetStatus(string message)
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
