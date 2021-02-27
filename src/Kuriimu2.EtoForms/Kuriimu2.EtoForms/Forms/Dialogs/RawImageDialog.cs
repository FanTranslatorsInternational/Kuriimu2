using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Eto.Forms;
using Kanvas.Configuration;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Kontract.Kanvas;
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

                var totalColors = width * height;
                if (totalColors % encoding.ColorsPerValue > 0)
                    throw new InvalidOperationException($"Total pixels does not match with the encoding specification.");

                var bitsPerColor = encoding.BitsPerValue / encoding.ColorsPerValue;
                var dataLength = totalColors * bitsPerColor / 8;

                var minDataLength = Math.Min(dataLength, _openedFile.Length - offset);
                var imgData = new byte[minDataLength];

                _openedFile.Position = offset;
                _openedFile.Read(imgData, 0, imgData.Length);

                var imageConfiguration = new ImageConfiguration();
                if (SelectedSwizzleExtension.Name != "None")
                    imageConfiguration.RemapPixels.With(() => CreateSwizzle(new System.Drawing.Size(width, height), encoding));

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

                new ExtensionType("DXT1", true),
                new ExtensionType("DXT3", true),
                new ExtensionType("DXT5", true),
                new ExtensionType("ATI1", true),
                new ExtensionType("ATI1L", true),
                new ExtensionType("ATI1A", true),
                new ExtensionType("ATI2", true,
                    new ExtensionTypeParameter("WiiU Variant", typeof(bool), false)),

                new ExtensionType("ATC", true),
                new ExtensionType("ATCA", true,
                    new ExtensionTypeParameter("AlphaMode", typeof(AtcAlpha), AtcAlpha.Explicit)),
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

                case "LA88":
                    var componentOrder8 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    var byteOrder8 = SelectedColorEncodingExtension.GetParameterValue<ByteOrder>("ByteOrder");
                    return new La(8, 8, componentOrder8, byteOrder8);

                case "LA44":
                    var componentOrder9 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new La(4, 4, componentOrder9);

                case "L8":
                    return new La(8, 0);

                case "A8":
                    return new La(0, 8);

                case "L4":
                    return new La(4, 0, ByteOrder.LittleEndian, SelectedColorEncodingExtension.GetParameterValue<BitOrder>("BitOrder"));

                case "A4":
                    return new La(0, 4, ByteOrder.LittleEndian, SelectedColorEncodingExtension.GetParameterValue<BitOrder>("BitOrder"));

                case "B8":
                    return new Rgba(0, 0, 8);

                case "ETC1":
                    var zOrder1 = SelectedColorEncodingExtension.GetParameterValue<bool>("Z-Order");
                    return new Etc1(false, zOrder1);

                case "ETC1A4":
                    var zOrder2 = SelectedColorEncodingExtension.GetParameterValue<bool>("Z-Order");
                    return new Etc1(true, zOrder2);

                //case "DXT1":
                //    return new Bc(BcFormat.DXT1);

                //case "DXT3":
                //    return new Bc(BcFormat.DXT3);

                //case "DXT5":
                //    return new Bc(BcFormat.DXT5);

                //case "ATI1":
                //    return new Bc(BcFormat.ATI1);

                //case "ATI1L":
                //    return new Bc(BcFormat.ATI1L_WiiU);

                //case "ATI1A":
                //    return new Bc(BcFormat.ATI1A_WiiU);

                //case "ATI2":
                //    var wiiU = SelectedColorEncodingExtension.GetParameterValue<bool>("WiiU Variant");
                //    return wiiU ? new Bc(BcFormat.ATI2) : new Bc(BcFormat.ATI2_WiiU);

                //case "ATC":
                //    return new Atc(AtcFormat.ATC, ByteOrder.LittleEndian);

                //case "ATCA":
                //    var atcAlpha = SelectedColorEncodingExtension.GetParameterValue<AtcAlpha>("AlphaMode");
                //    return atcAlpha == AtcAlpha.Explicit ?
                //        new Atc(AtcFormat.ATCA_Exp, ByteOrder.LittleEndian) :
                //        new Atc(AtcFormat.ATCA_Int, ByteOrder.LittleEndian);

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
                new ExtensionType("None",true),
                new ExtensionType("NDS",true),
                new ExtensionType("3DS",true),
                new ExtensionType("WiiU",true,
                    new ExtensionTypeParameter("SwizzleTileMode",typeof(int))),

                new ExtensionType("Custom",true,
                    new ExtensionTypeParameter("BitMapping",typeof(string),"{1,0},{0,1}"))
            };
        }

        private IImageSwizzle CreateSwizzle(System.Drawing.Size size, IEncodingInfo encoding)
        {
            switch (SelectedSwizzleExtension.Name)
            {
                case "NDS":
                    return new NitroSwizzle(size.Width, size.Height);

                case "3DS":
                    return new CTRSwizzle(size.Width, size.Height);

                case "WiiU":
                    var swizzleTileMode = SelectedSwizzleExtension.GetParameterValue<byte>("SwizzleTileMode");
                    return new CafeSwizzle(swizzleTileMode, encoding.BitsPerValue > 32, encoding.BitDepth, size.Width, size.Height);

                case "Custom":
                    var swizzleText = SelectedSwizzleExtension.GetParameterValue<string>("BitMapping");
                    var escapedSwizzleText = swizzleText.Replace(" ", "");

                    var pointStrings = escapedSwizzleText.Substring(1, escapedSwizzleText.Length - 2)
                        .Split(new[] { "},{" }, StringSplitOptions.None);
                    var finalPoints = pointStrings.Select(x => x.Split(','))
                        .Select(x => (int.Parse(x[0]), int.Parse(x[1])));

                    var masterSwizzle = new MasterSwizzle(size.Width, System.Drawing.Point.Empty, finalPoints.ToArray());
                    return new CustomSwizzle(size.Width, size.Height, masterSwizzle);

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

        public CustomSwizzle(int width, int height, MasterSwizzle swizzle)
        {
            Width = width;
            Height = height;

            _swizzle = swizzle;
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
