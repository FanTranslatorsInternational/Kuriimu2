using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Kanvas.Encoding;
using Kanvas.Encoding.BlockCompressions.ATC.Models;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Kanvas.Swizzle;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces.Plugins.State.Intermediate;
using Kontract.Kanvas;
using Kontract.Models.IO;
using Kore.Managers.Plugins;
using Kuriimu2.WinForms.MainForms.Models;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class RawImageViewer : Form
    {
        private ByteOrder _byteOrder = ByteOrder.LittleEndian;

        private bool _fileLoaded;
        private Stream _openedFile;

        private int _selectedEncodingIndex;
        private int _selectedSwizzleIndex;

        private readonly SplitterPanel _pnlEncodingProperties;
        private readonly SplitterPanel _pnlSwizzleProperties;

        private IColorEncodingAdapter SelectedColorEncodingAdapter
        {
            get
            {
                if (_selectedEncodingIndex < cbEncoding.Items.Count)
                    return (cbEncoding.Items[_selectedEncodingIndex] as EncodingWrapper)?.EncodingAdapter;

                return null;
            }
        }

        private IImageSwizzleAdapter SelectedSwizzleAdapter
        {
            get
            {
                if (_selectedSwizzleIndex < cbSwizzle.Items.Count)
                    return (cbSwizzle.Items[_selectedSwizzleIndex] as SwizzleWrapper)?.SwizzleAdapter;

                return null;
            }
        }

        public RawImageViewer()
        {
            InitializeComponent();

            _pnlEncodingProperties = splExtendedProperties.Panel1;
            _pnlSwizzleProperties = splExtendedProperties.Panel2;

            cbEncoding.SelectedIndexChanged -= CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged -= CbSwizzle_SelectedIndexChanged;
            LoadEncodings(cbEncoding);
            LoadSwizzles(cbSwizzle);
            cbEncoding.SelectedIndexChanged += CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged += CbSwizzle_SelectedIndexChanged;

            UpdateForm();
            UpdateExtendedProperties();
        }

        private void LoadEncodings(ComboBox cb)
        {
            var selectedIndex = cb.SelectedIndex;
            cb.Items.Clear();

            // Populate encoding dropdown
            foreach (var (encodingAction, name) in GetEncodings())
                cb.Items.Add(new ComboBoxElement(encodingAction, name));

            if (selectedIndex < cb.Items.Count)
                cb.SelectedIndex = selectedIndex;

            _selectedEncodingIndex = selectedIndex;
        }

        private void LoadSwizzles(ComboBox cb)
        {
            var selectedIndex = cb.SelectedIndex;
            cb.Items.Clear();

            // Populate swizzle dropdown
            foreach (var (swizzleAction, name) in GetSwizzles())
                cb.Items.Add(new ComboBoxElement(swizzleAction, name));

            if (selectedIndex < cb.Items.Count)
                cb.SelectedIndex = selectedIndex;

            _selectedSwizzleIndex = selectedIndex;
        }

        private IEnumerable<(Func<ByteOrder, IEncodingInfo>, string)> GetEncodings()
        {
            var encodings = new List<(Func<ByteOrder, IEncodingInfo>, string)>
            {
                (bo => new Rgba(8, 8, 8, 8, bo), "RGBA8888"),
                (bo => new Rgba(8, 8, 8, 0, bo), "RGB888"),
                (bo => new Rgba(5, 6, 5, 0, bo), "RGB565"),
                (bo => new Rgba(5, 5, 5, 1, bo), "RGBA5551"),
                (bo => new Rgba(5, 5, 5, 0, bo), "RGB555"),
                (bo => new Rgba(4, 4, 4, 4, bo), "RGBA4444"),
                (bo => new Rgba(8, 8, 0, 0, bo), "RG88"),
                (bo => new Rgba(8, 0, 0, 0, bo), "R8"),
                (bo => new Rgba(0, 8, 0, 0, bo), "G8"),
                (bo => new Rgba(0, 0, 8, 0, bo), "B8"),

                (bo => new Rgba(8, 8, 8, 8, "ABGR", bo), "ABGR8888"),
                (bo => new Rgba(8, 8, 8, 0, "BGR", bo), "BGR888"),
                (bo => new Rgba(5, 6, 5, 0, "BGR", bo), "BGR565"),
                (bo => new Rgba(5, 5, 5, 1, "ABGR", bo), "ABGR1555"),
                (bo => new Rgba(5, 5, 5, 0, "BGR", bo), "BGR555"),
                (bo => new Rgba(4, 4, 4, 4, "ABGR", bo), "ABGR4444"),
                (bo => new Rgba(8, 8, 0, 0, "GR", bo), "GR88"),
                (bo => new Rgba(8, 0, 0, 0, "R", bo), "R8"),
                (bo => new Rgba(0, 8, 0, 0, "G", bo), "G8"),
                (bo => new Rgba(0, 0, 8, 0, "B", bo), "B8"),

                (bo => new La(8, 8, bo), "LA88"),
                (bo => new La(8, 0, bo), "L8"),
                (bo => new La(0, 8, bo), "A8"),
                (bo => new La(4, 4, bo), "LA44"),
                (bo => new La(4, 0, bo), "L4"),
                (bo => new La(0, 4, bo), "A4"),

                (bo => new Etc1(false, false), "ETC1"),
                (bo => new Etc1(true, false), "ETC1A4"),

                (bo => new Bc(BcFormat.DXT1), "DXT1"),
                (bo => new Bc(BcFormat.DXT3), "DXT3"),
                (bo => new Bc(BcFormat.DXT5), "DXT5"),
                (bo => new Bc(BcFormat.ATI1), "ATI1"),
                (bo => new Bc(BcFormat.ATI1L_WiiU), "ATI1L (WiiU)"),
                (bo => new Bc(BcFormat.ATI1A_WiiU), "ATI1A (WiiU)"),
                (bo => new Bc(BcFormat.ATI2), "ATI2"),
                (bo => new Bc(BcFormat.ATI2_WiiU), "ATI2 (WiiU)"),

                (bo => new Atc(AtcFormat.ATC, bo), "ATC"),
                (bo => new Atc(AtcFormat.ATCA_Exp, bo), "ATC (Explicit Alpha)"),
                (bo => new Atc(AtcFormat.ATCA_Int, bo), "ATC (Interpolated Alpha)")
            };

            foreach (var encoding in encodings)
                yield return encoding;
        }

        private IEnumerable<(Func<Size, IImageSwizzle>, string)> GetSwizzles()
        {
            var swizzles = new List<(Func<Size, IImageSwizzle>, string)>
            {
                (size=>new NitroSwizzle(size.Width,size.Height),"Nitro")
            };

            foreach (var swizzle in swizzles)
                yield return swizzle;
        }

        private void CbEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedEncodingIndex = cbEncoding.SelectedIndex;
            SelectedColorEncodingAdapter.Swizzle = SelectedSwizzleAdapter;

            UpdateEncodingProperties();
        }

        private void CbSwizzle_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedSwizzleIndex = cbSwizzle.SelectedIndex;
            SelectedColorEncodingAdapter.Swizzle = SelectedSwizzleAdapter;

            UpdateSwizzleProperty();
        }

        #region Update

        private void UpdateForm()
        {
            cbEncoding.Enabled = cbEncoding.Items.Count > 0;
            cbSwizzle.Enabled = cbSwizzle.Items.Count > 0;
            tbOffset.Enabled = true;
            btnDecode.Enabled = _fileLoaded;
        }

        private void UpdateExtendedProperties()
        {
            UpdateEncodingProperties();
            UpdateSwizzleProperty();
        }

        private void UpdateEncodingProperties()
        {
            _pnlEncodingProperties.Controls.Clear();

            if (SelectedColorEncodingAdapter != null)
                UpdateExtendedPropertiesWith(
                    _pnlEncodingProperties,
                    80,
                    EncodingPropertyTextBox_TextChanged,
                    EncodingPropertyComboBox_SelectedIndexChanged,
                    EncodingPropertyCheckBox_CheckedChanged,
                    SelectedColorEncodingAdapter.
                        GetType().
                        GetCustomAttributes(typeof(PropertyAttribute), false).
                        Cast<PropertyAttribute>().
                        ToArray());
        }

        private void UpdateSwizzleProperty()
        {
            _pnlSwizzleProperties.Controls.Clear();
            tbWidth.TextChanged -= SwizzlePropertyTextBox_TextChanged;
            tbWidth.Tag = null;
            tbHeight.TextChanged -= SwizzlePropertyTextBox_TextChanged;
            tbHeight.Tag = null;

            if (SelectedSwizzleAdapter != null && SelectedSwizzleAdapter.Name == "Custom")
            {
                var label = new Label
                {
                    // ReSharper disable once LocalizableElement
                    Text = "Bit Field:",
                    Location = new Point(3, 0),
                    Size = new Size(200, 15)
                };
                var textBox = new TextBox
                {
                    Location = new Point(3, 0 + label.Height),
                    Size = new Size(200, 20),
                    Tag = SelectedSwizzleAdapter.GetType().GetProperty("BitField")
                };

                _pnlSwizzleProperties.Controls.Add(label);
                _pnlSwizzleProperties.Controls.Add(textBox);
                textBox.TextChanged += CustomSwizzleBitField_TextChanged;
            }

            if (SelectedSwizzleAdapter != null)
                UpdateExtendedPropertiesWith(
                    _pnlSwizzleProperties,
                    80,
                    SwizzlePropertyTextBox_TextChanged,
                    SwizzlePropertyComboBox_SelectedIndexChanged,
                    SwizzlePropertyCheckBox_CheckedChanged,
                    SelectedSwizzleAdapter.
                        GetType().
                        GetCustomAttributes(typeof(PropertyAttribute), false).
                        Cast<PropertyAttribute>().
                        ToArray());
        }

        private void CustomSwizzleBitField_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            var prop = (PropertyInfo)tb.Tag;

            var splitted = Regex.Split(tb.Text, "\\)[ ]*,[ ]*\\(").Select(x => Regex.Match(x, "\\d+[ ]*,[ ]*\\d+").Value).ToArray();
            if (splitted.Any(x => string.IsNullOrEmpty(x)))
                return;

            prop.SetValue(SelectedSwizzleAdapter, splitted.Select(x =>
            {
                var internalSplit = Regex.Split(x, "[ ]*,[ ]*").ToArray();
                return (int.Parse(internalSplit[0]), int.Parse(internalSplit[1]));
            }).ToList());
        }

        private void UpdateExtendedPropertiesWith(SplitterPanel panel, int width, EventHandler textChangedEvent,
            EventHandler indexChangedEvent, EventHandler checkedChangedEvent, params PropertyAttribute[] propAttributes)
        {
            int x = 3;
            foreach (var attr in propAttributes)
            {
                if (attr.PropertyType == typeof(bool))
                {
                    AddBooleanProperty(attr, panel, checkedChangedEvent, width, x, 0);
                }
                else if (attr.PropertyType.IsPrimitive)
                {
                    if (attr.PropertyName == "Width")
                    {
                        tbWidth.TextChanged += textChangedEvent;
                        tbWidth.Tag = attr;
                        tbWidth.Text = tbWidth.Text;
                        continue;
                    }

                    if (attr.PropertyName == "Height")
                    {
                        tbHeight.TextChanged += textChangedEvent;
                        tbHeight.Tag = attr;
                        tbHeight.Text = tbHeight.Text;
                        continue;
                    }

                    AddPrimitiveProperty(attr, panel, textChangedEvent, width, x, 0);
                }
                else if (attr.PropertyType.IsEnum)
                {
                    AddEnumProperty(attr, panel, indexChangedEvent, width, x, 0);
                }

                x += width + 3;
            }
        }

        private void AddPrimitiveProperty(PropertyAttribute propAttr, SplitterPanel panel, EventHandler textChangedEvent, int width, int x, int y)
        {
            if (!propAttr.PropertyType.IsPrimitive)
                return;

            var label = new Label
            {
                // ReSharper disable once LocalizableElement
                Text = $"{propAttr.PropertyName}:",
                Location = new Point(x, y),
                Size = new Size(width, 15)
            };
            var textBox = new TextBox
            {
                Location = new Point(x, y + label.Height),
                Size = new Size(width, 20),
                Tag = propAttr
            };

            textBox.TextChanged += textChangedEvent;
            textBox.Text = propAttr.DefaultValue.ToString();

            panel.Controls.Add(label);
            panel.Controls.Add(textBox);
        }

        private void EncodingPropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            var propAttr = (PropertyAttribute)tb.Tag;

            object value;
            switch (Type.GetTypeCode(propAttr.PropertyType))
            {
                case TypeCode.Byte:
                    byte val1;
                    if (!byte.TryParse(tb.Text, out val1))
                        return;
                    value = val1;
                    break;
                case TypeCode.SByte:
                    sbyte val2;
                    if (!sbyte.TryParse(tb.Text, out val2))
                        return;
                    value = val2;
                    break;
                case TypeCode.Int16:
                    short val3;
                    if (!short.TryParse(tb.Text, out val3))
                        return;
                    value = val3;
                    break;
                case TypeCode.UInt16:
                    ushort val4;
                    if (!ushort.TryParse(tb.Text, out val4))
                        return;
                    value = val4;
                    break;
                case TypeCode.Int32:
                    int val5;
                    if (!int.TryParse(tb.Text, out val5))
                        return;
                    value = val5;
                    break;
                case TypeCode.UInt32:
                    uint val6;
                    if (!uint.TryParse(tb.Text, out val6))
                        return;
                    value = val6;
                    break;
                case TypeCode.Int64:
                    long val7;
                    if (!long.TryParse(tb.Text, out val7))
                        return;
                    value = val7;
                    break;
                case TypeCode.UInt64:
                    ulong val8;
                    if (!ulong.TryParse(tb.Text, out val8))
                        return;
                    value = val8;
                    break;
                default:
                    return;
            }

            var adapterProperty = SelectedColorEncodingAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedColorEncodingAdapter, value);
        }

        private void SwizzlePropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            var propAttr = (PropertyAttribute)tb.Tag;

            object value;
            switch (Type.GetTypeCode(propAttr.PropertyType))
            {
                case TypeCode.Byte:
                    byte val1;
                    if (!byte.TryParse(tb.Text, out val1))
                        return;
                    value = val1;
                    break;
                case TypeCode.SByte:
                    sbyte val2;
                    if (!sbyte.TryParse(tb.Text, out val2))
                        return;
                    value = val2;
                    break;
                case TypeCode.Int16:
                    short val3;
                    if (!short.TryParse(tb.Text, out val3))
                        return;
                    value = val3;
                    break;
                case TypeCode.UInt16:
                    ushort val4;
                    if (!ushort.TryParse(tb.Text, out val4))
                        return;
                    value = val4;
                    break;
                case TypeCode.Int32:
                    int val5;
                    if (!int.TryParse(tb.Text, out val5))
                        return;
                    value = val5;
                    break;
                case TypeCode.UInt32:
                    uint val6;
                    if (!uint.TryParse(tb.Text, out val6))
                        return;
                    value = val6;
                    break;
                case TypeCode.Int64:
                    long val7;
                    if (!long.TryParse(tb.Text, out val7))
                        return;
                    value = val7;
                    break;
                case TypeCode.UInt64:
                    ulong val8;
                    if (!ulong.TryParse(tb.Text, out val8))
                        return;
                    value = val8;
                    break;
                default:
                    return;
            }

            var adapterProperty = SelectedSwizzleAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedSwizzleAdapter, value);
        }

        private void EncodingPropertyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var cb = (CheckBox)sender;
            var propAttr = (PropertyAttribute)cb.Tag;

            var adapterProperty = SelectedColorEncodingAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedColorEncodingAdapter, cb.Checked);
        }

        private void SwizzlePropertyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var cb = (CheckBox)sender;
            var propAttr = (PropertyAttribute)cb.Tag;

            var adapterProperty = SelectedSwizzleAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedSwizzleAdapter, cb.Checked);
        }

        private void AddEnumProperty(PropertyAttribute propAttr, SplitterPanel panel, EventHandler indexChangedEvent, int width, int x, int y)
        {
            if (!propAttr.PropertyType.IsEnum)
                return;

            var formatWrappers = Enum.GetNames(propAttr.PropertyType).
                Zip(Enum.GetValues(propAttr.PropertyType).Cast<object>(), Tuple.Create).
                Select(enumValue => (object)new FormatWrapper(enumValue.Item2, enumValue.Item1)).
                ToArray();

            var label = new Label
            {
                // ReSharper disable once LocalizableElement
                Text = $"{propAttr.PropertyName}:",
                Location = new Point(x, y),
                Size = new Size(width, 15)
            };
            var comboBox = new ComboBox
            {
                Location = new Point(x, y + label.Height),
                Size = new Size(width, 20),
                Tag = propAttr
            };
            comboBox.Items.AddRange(formatWrappers);

            comboBox.SelectedIndexChanged += indexChangedEvent;
            var defaultIndex = formatWrappers.ToList().IndexOf(
                formatWrappers.ToList().FirstOrDefault(fw =>
                    (fw as FormatWrapper)?.Name == Enum.GetName(propAttr.PropertyType, propAttr.DefaultValue)));
            comboBox.SelectedIndex = defaultIndex < 0 ? 0 : defaultIndex;

            panel.Controls.Add(label);
            panel.Controls.Add(comboBox);
        }

        private void AddBooleanProperty(PropertyAttribute propAttr, SplitterPanel panel, EventHandler checkedChanged,
            int width, int x, int y)
        {
            if (propAttr.PropertyType != typeof(bool))
                return;

            var label = new Label
            {
                // ReSharper disable once LocalizableElement
                Text = $"{propAttr.PropertyName}:",
                Location = new Point(x, y),
                Size = new Size(width, 15)
            };
            var checkBox = new CheckBox
            {
                Location = new Point(x, y + label.Height),
                //Size = new Size(width, 20),
                Tag = propAttr
            };

            checkBox.CheckedChanged += checkedChanged;
            checkBox.Checked = Convert.ToBoolean(propAttr.DefaultValue);

            panel.Controls.Add(label);
            panel.Controls.Add(checkBox);
        }

        private void EncodingPropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = (ComboBox)sender;
            var index = cb.SelectedIndex;
            var format = (FormatWrapper)cb.Items[index];

            var propAttr = (PropertyAttribute)cb.Tag;

            var adapterProperty = SelectedColorEncodingAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedColorEncodingAdapter, format.Value);
        }

        private void SwizzlePropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = (ComboBox)sender;
            var index = cb.SelectedIndex;
            var format = (FormatWrapper)cb.Items[index];

            var propAttr = (PropertyAttribute)cb.Tag;

            var adapterProperty = SelectedSwizzleAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedSwizzleAdapter, format.Value);
        }

        #endregion

        private async void LoadImage()
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

            ToggleProperties(false);
            ToggleForm(false);

            var neededData = SelectedColorEncodingAdapter.CalculateLength(width, height);

            _openedFile.Position = offset;
            var imgData = new byte[Math.Min(neededData, _openedFile.Length - offset)];
            _openedFile.Read(imgData, 0, imgData.Length);

            var progress = new Progress<ProgressReport>();
            try
            {
                pbMain.Image = await SelectedColorEncodingAdapter.Decode(imgData, width, height, progress);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            tslPbWidth.Text = pbMain.Width.ToString();
            tslPbHeight.Text = pbMain.Height.ToString();

            ToggleProperties(true);
            ToggleForm(true);
        }

        private void ToggleProperties(bool toggle)
        {
            var textBoxes = splExtendedProperties.Panel1.Controls.OfType<TextBox>();
            var comboBoxes = splExtendedProperties.Panel1.Controls.OfType<ComboBox>();
            var checkBoxes = splExtendedProperties.Panel1.Controls.OfType<CheckBox>();

            foreach (var textBox in textBoxes)
                textBox.Enabled = toggle;
            foreach (var comboBox in comboBoxes)
                comboBox.Enabled = toggle;
            foreach (var checkBox in checkBoxes)
                checkBox.Enabled = toggle;

            textBoxes = splExtendedProperties.Panel2.Controls.OfType<TextBox>();
            comboBoxes = splExtendedProperties.Panel2.Controls.OfType<ComboBox>();
            checkBoxes = splExtendedProperties.Panel2.Controls.OfType<CheckBox>();

            foreach (var textBox in textBoxes)
                textBox.Enabled = toggle;
            foreach (var comboBox in comboBoxes)
                comboBox.Enabled = toggle;
            foreach (var checkBox in checkBoxes)
                checkBox.Enabled = toggle;
        }

        private void ToggleForm(bool toggle)
        {
            cbEncoding.Enabled = toggle;
            cbSwizzle.Enabled = toggle;
            openToolStripMenuItem.Enabled = toggle;
            btnDecode.Enabled = toggle;
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open raw image...",
                Filter = "All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            OpenFile(ofd.FileName);
            LoadImage();
        }

        private void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
                return;

            _openedFile = File.OpenRead(fileName);
            _fileLoaded = true;
        }

        private void BtnDecode_Click(object sender, EventArgs e)
        {
            LoadImage();
        }

        private void PbMain_ZoomChanged(object sender, EventArgs e)
        {
            // ReSharper disable once LocalizableElement
            tslZoom.Text = $"Zoom: {pbMain.Zoom}%";
        }
    }
}
