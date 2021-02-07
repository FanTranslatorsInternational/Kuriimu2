using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Kanvas.Configuration;
using Kanvas.Encoding;
using Kanvas.Encoding.BlockCompressions.ATC.Models;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Kontract.Kanvas;
using Kontract.Models.IO;
using Kuriimu2.WinForms.ExtensionForms.Models;
using Kuriimu2.WinForms.ExtensionForms.Support;
using Kuriimu2.WinForms.Extensions;
using Kuriimu2.WinForms.MainForms.Models;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class RawImageViewer : Form
    {
        //private ByteOrder _byteOrder = ByteOrder.LittleEndian;

        private bool _fileLoaded;
        private Stream _openedFile;

        private int _selectedEncodingIndex;
        private int _selectedSwizzleIndex;

        private readonly ParameterBuilder _encodingParameterBuilder;
        private readonly ParameterBuilder _swizzleParameterBuilder;

        private ExtensionType SelectedColorEncodingExtension
        {
            get
            {
                if (_selectedEncodingIndex < cbEncoding.Items.Count)
                    return (cbEncoding.Items[_selectedEncodingIndex] as ComboBoxElement).Value as ExtensionType;

                return null;
            }
        }

        private ExtensionType SelectedSwizzleExtension
        {
            get
            {
                if (_selectedSwizzleIndex < cbSwizzle.Items.Count)
                    return (cbSwizzle.Items[_selectedSwizzleIndex] as ComboBoxElement).Value as ExtensionType;

                return null;
            }
        }

        public RawImageViewer()
        {
            InitializeComponent();

            _encodingParameterBuilder = new ParameterBuilder(gbEncParameters);
            _swizzleParameterBuilder = new ParameterBuilder(gbSwizzleParameters);

            cbEncoding.SelectedIndexChanged -= CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged -= CbSwizzle_SelectedIndexChanged;
            LoadEncodings(cbEncoding);
            LoadSwizzles(cbSwizzle);
            cbEncoding.SelectedIndexChanged += CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged += CbSwizzle_SelectedIndexChanged;

            UpdateForm();
            UpdateExtendedProperties();
        }

        #region Load

        private void LoadEncodings(ComboBox cb)
        {
            var selectedIndex = cb.SelectedIndex;
            cb.Items.Clear();

            // Populate encoding dropdown
            foreach (var encodingExtension in GetEncodings())
                cb.Items.Add(new ComboBoxElement(encodingExtension, encodingExtension.Name));

            if (selectedIndex < 0)
                selectedIndex = 0;

            cb.SelectedIndex = selectedIndex;
            _selectedEncodingIndex = selectedIndex;
        }

        private void LoadSwizzles(ComboBox cb)
        {
            var selectedIndex = cb.SelectedIndex;
            cb.Items.Clear();

            // Populate swizzle dropdown
            foreach (var swizzleExtension in GetSwizzles())
                cb.Items.Add(new ComboBoxElement(swizzleExtension, swizzleExtension.Name));

            if (selectedIndex < 0)
                selectedIndex = 0;

            cb.SelectedIndex = selectedIndex;
            _selectedSwizzleIndex = selectedIndex;
        }

        private void LoadImage(bool throwOnError = true)
        {
            if (!_fileLoaded || _openedFile == null)
                return;

            if (!int.TryParse(tbOffset.Text, out var offset) &&
                !int.TryParse(tbOffset.Text.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out offset) ||
                !int.TryParse(tbWidth.Text, out var width) ||
                !int.TryParse(tbHeight.Text, out var height))
                return;

            if (offset < 0 || offset >= _openedFile.Length || width <= 0 || height <= 0)
                return;

            if (!TryParseParameters(gbEncParameters, SelectedColorEncodingExtension.Parameters.Values.ToArray()) ||
                !TryParseParameters(gbSwizzleParameters, SelectedSwizzleExtension.Parameters.Values.ToArray()))
                return;

            if (SelectedSwizzleExtension.Name == "Custom" && CheckCustomSwizzle())
                return;

            var activeControl = this.GetActiveControl();

            ToggleParameters(false);
            ToggleForm(false);

            //var progress = new Progress<ProgressReport>();
            try
            {
                var encoding = CreateEncoding();

                var totalColors = width * height;
                if (totalColors % encoding.ColorsPerValue > 0)
                    return;

                var bitsPerColor = encoding.BitsPerValue / encoding.ColorsPerValue;
                var dataLength = totalColors * bitsPerColor / 8;

                var minDataLength = Math.Min(dataLength, _openedFile.Length - offset);
                var imgData = new byte[minDataLength];

                _openedFile.Position = offset;
                _openedFile.Read(imgData, 0, imgData.Length);

                var imageConfiguration = new ImageConfiguration();
                if (SelectedSwizzleExtension.Name != "None")
                    imageConfiguration.RemapPixelsWith(size => CreateSwizzle(size, encoding));

                var transcoder = imageConfiguration.TranscodeWith(size => encoding).Build();

                pbMain.Image = transcoder.Decode(imgData, new Size(width, height));
            }
            catch (Exception e)
            {
                if (throwOnError)
                    MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            tslWidth.Text = pbMain.Width.ToString();
            tslHeight.Text = pbMain.Height.ToString();

            ToggleParameters(true);
            ToggleForm(true);

            if (activeControl != null && Contains(activeControl))
                activeControl.Focus();
        }

        #endregion

        #region Events

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            LoadImage(false);
        }

        private void CbEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedEncodingIndex = cbEncoding.SelectedIndex;

            UpdateEncodingProperties();
            LoadImage(false);
        }

        private void CbSwizzle_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedSwizzleIndex = cbSwizzle.SelectedIndex;

            UpdateSwizzleProperties();
            LoadImage(false);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Open raw image...",
                Filter = "All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            OpenFile(ofd.FileName);
            LoadImage();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void PbMain_ZoomChanged(object sender, EventArgs e)
        {
            // ReSharper disable once LocalizableElement
            tslZoom.Text = $"Zoom: {pbMain.Zoom}%";
        }

        private void tbWidth_TextChanged(object sender, EventArgs e)
        {
            LoadImage(false);
        }

        private void tbHeight_TextChanged(object sender, EventArgs e)
        {
            LoadImage(false);
        }

        private void tbOffset_TextChanged(object sender, EventArgs e)
        {
            LoadImage(false);
        }

        #endregion

        #region Update

        private void UpdateForm()
        {
            cbEncoding.Enabled = cbEncoding.Items.Count > 0;
            cbSwizzle.Enabled = cbSwizzle.Items.Count > 0;

            tbOffset.Enabled = true;

            btnProcess.Enabled = _fileLoaded;
        }

        private void UpdateExtendedProperties()
        {
            UpdateEncodingProperties();
            UpdateSwizzleProperties();
        }

        private void UpdateEncodingProperties()
        {
            UpdateProperties(_encodingParameterBuilder, SelectedColorEncodingExtension.Parameters.Values.ToArray(), gbEncParameters);
        }

        private void UpdateSwizzleProperties()
        {
            UpdateProperties(_swizzleParameterBuilder, SelectedSwizzleExtension.Parameters.Values.ToArray(), gbSwizzleParameters);
        }

        private void UpdateProperties(ParameterBuilder parameterBuilder, ExtensionTypeParameter[] parameters, GroupBox groupBox)
        {
            parameterBuilder.Reset();
            if (!(parameters?.Any() ?? false))
                return;

            parameterBuilder.AddParameters(parameters);

            foreach (var parameter in parameters)
            {
                var parameterControl = groupBox.Controls.Find(parameter.Name, false)[0];

                switch (parameterControl)
                {
                    case TextBox textBox:
                        textBox.TextChanged += (sender, args) => LoadImage(false);
                        break;

                    case CheckBox checkBox:
                        checkBox.CheckedChanged += (sender, args) => LoadImage(false);
                        break;

                    case ComboBox comboBox:
                        comboBox.SelectedIndexChanged += (sender, args) => LoadImage(false);
                        break;
                }
            }
        }

        #endregion

        #region Toggles

        private void ToggleParameters(bool toggle)
        {
            gbEncParameters.Enabled = toggle;
            gbSwizzleParameters.Enabled = toggle;
        }

        private void ToggleForm(bool toggle)
        {
            cbEncoding.Enabled = toggle;
            cbSwizzle.Enabled = toggle;
            openToolStripMenuItem.Enabled = toggle;
            btnProcess.Enabled = toggle;
        }

        #endregion

        #region List setup and values

        private enum AtcAlpha
        {
            Explicit,
            Interpolated
        }

        private class CustomSwizzle : IImageSwizzle
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

            public Point Transform(Point point) =>
                _swizzle.Get(point.Y * Width + point.X);
        }

        private IList<ExtensionType> GetEncodings()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("RGBA8888", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA")),
                new ExtensionType("RGB888", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB")),
                new ExtensionType("RGB565", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB")),
                new ExtensionType("RGB555", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGB")),
                new ExtensionType("RGBA5551", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA")),
                new ExtensionType("RGBA4444", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RGBA")),
                new ExtensionType("RG88", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "RG")),
                new ExtensionType("R8", true),
                new ExtensionType("G8", true),
                new ExtensionType("B8", true),

                new ExtensionType("LA88", true,
                    new ExtensionTypeParameter("ComponentOrder", typeof(string), "LA")),
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

        private IColorEncoding CreateEncoding()
        {
            switch (SelectedColorEncodingExtension.Name)
            {
                case "RGBA8888":
                    var componentOrder1 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(8, 8, 8, 8, componentOrder1);

                case "RGB888":
                    var componentOrder2 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(8, 8, 8, componentOrder2);

                case "RGB565":
                    var componentOrder3 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(5, 6, 5, componentOrder3);

                case "RGB555":
                    var componentOrder4 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(5, 5, 5, componentOrder4);

                case "RGBA5551":
                    var componentOrder5 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(5, 5, 5, 1, componentOrder5);

                case "RGBA4444":
                    var componentOrder6 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(4, 4, 4, 4, componentOrder6);

                case "RG88":
                    var componentOrder7 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new Rgba(8, 8, 0, componentOrder7);

                case "R8":
                    return new Rgba(8, 0, 0);

                case "G8":
                    return new Rgba(0, 8, 0);

                case "LA88":
                    var componentOrder8 = SelectedColorEncodingExtension.GetParameterValue<string>("ComponentOrder");
                    return new La(8, 8, componentOrder8);

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

                case "DXT1":
                    return new Bc(BcFormat.DXT1);

                case "DXT3":
                    return new Bc(BcFormat.DXT3);

                case "DXT5":
                    return new Bc(BcFormat.DXT5);

                case "ATI1":
                    return new Bc(BcFormat.ATI1);

                case "ATI1L":
                    return new Bc(BcFormat.ATI1L_WiiU);

                case "ATI1A":
                    return new Bc(BcFormat.ATI1A_WiiU);

                case "ATI2":
                    var wiiU = SelectedColorEncodingExtension.GetParameterValue<bool>("WiiU Variant");
                    return wiiU ? new Bc(BcFormat.ATI2) : new Bc(BcFormat.ATI2_WiiU);

                case "ATC":
                    return new Atc(AtcFormat.ATC, ByteOrder.LittleEndian);

                case "ATCA":
                    var atcAlpha = SelectedColorEncodingExtension.GetParameterValue<AtcAlpha>("AlphaMode");
                    return atcAlpha == AtcAlpha.Explicit ?
                        new Atc(AtcFormat.ATCA_Exp, ByteOrder.LittleEndian) :
                        new Atc(AtcFormat.ATCA_Int, ByteOrder.LittleEndian);

                default:
                    return null;
            }
        }

        private IImageSwizzle CreateSwizzle(Size size, IEncodingInfo encoding)
        {
            switch (SelectedSwizzleExtension.Name)
            {
                case "NDS":
                    return new NitroSwizzle(size.Width, size.Height);

                case "3DS":
                    return new CTRSwizzle(size.Width, size.Height, CtrTransformation.None, true);

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

                    var masterSwizzle = new MasterSwizzle(size.Width, Point.Empty, finalPoints.ToArray());
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

        private void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
                return;

            _openedFile = File.OpenRead(fileName);
            _fileLoaded = closeToolStripMenuItem.Enabled = true;
        }

        private void CloseFile()
        {
            _openedFile?.Close();
            _fileLoaded = closeToolStripMenuItem.Enabled = false;

            pbMain.Image = null;
        }

        private bool TryParseParameters(GroupBox gbControl, ExtensionTypeParameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                var control = gbControl.Controls.Find(parameter.Name, false)[0];

                if (!parameter.TryParse(control, out var error))
                {
                    //Logger.QueueMessage(LogLevel.Error, error);
                    return false;
                }
            }

            return true;
        }
    }
}
