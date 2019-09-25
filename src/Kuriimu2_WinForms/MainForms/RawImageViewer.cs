using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Kontract;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;
using Kuriimu2_WinForms.MainForms.Models;

namespace Kuriimu2_WinForms.MainForms
{
    public partial class RawImageViewer : Form
    {
        private readonly PluginLoader _loader;

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

        public RawImageViewer(PluginLoader loader)
        {
            InitializeComponent();

            _loader = loader;
            _pnlEncodingProperties = splExtendedProperties.Panel1;
            _pnlSwizzleProperties = splExtendedProperties.Panel2;

            cbEncoding.SelectedIndexChanged -= CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged -= CbSwizzle_SelectedIndexChanged;
            LoadComboBoxes();
            cbEncoding.SelectedIndexChanged += CbEncoding_SelectedIndexChanged;
            cbSwizzle.SelectedIndexChanged += CbSwizzle_SelectedIndexChanged;

            UpdateForm();
            UpdateExtendedProperties();
        }

        private void LoadComboBoxes()
        {
            LoadEncodings();
            LoadSwizzles();
        }

        private void LoadEncodings()
        {
            // Populate encoding dropdown
            foreach (var adapter in _loader.GetAdapters<IColorEncodingAdapter>())
                cbEncoding.Items.Add(new EncodingWrapper(adapter));
            if (_selectedEncodingIndex < cbEncoding.Items.Count)
                cbEncoding.SelectedIndex = _selectedEncodingIndex;
        }

        private void LoadSwizzles()
        {
            // Populate swizzle dropdown
            cbSwizzle.Items.Add(new SwizzleWrapper(null));

            var customSwizzle = _loader.GetAdapters<IImageSwizzleAdapter>().FirstOrDefault(x => x.Name == "Custom");
            if (customSwizzle != null)
                cbSwizzle.Items.Add(new SwizzleWrapper(customSwizzle));

            foreach (var adapter in _loader.GetAdapters<IImageSwizzleAdapter>().Where(x => x.Name != "Custom"))
                cbSwizzle.Items.Add(new SwizzleWrapper(adapter));
            if (_selectedSwizzleIndex < cbSwizzle.Items.Count)
                cbSwizzle.SelectedIndex = _selectedSwizzleIndex;
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
