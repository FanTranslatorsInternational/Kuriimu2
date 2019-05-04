using System;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private IColorEncodingAdapter SelectedColorEncodingAdapter =>
            (cbEncoding.Items[_selectedEncodingIndex] as EncodingWrapper)?.EncodingAdapter;
        private IImageSwizzleAdapter SelectedSwizzleAdapter =>
            (cbSwizzle.Items[_selectedSwizzleIndex] as SwizzleWrapper)?.SwizzleAdapter;

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
            cbEncoding.SelectedIndex = _selectedEncodingIndex;
        }

        private void LoadSwizzles()
        {
            // Populate swizzle dropdown
            cbSwizzle.Items.Add(new SwizzleWrapper(null));
            foreach (var adapter in _loader.GetAdapters<IImageSwizzleAdapter>())
                cbSwizzle.Items.Add(new SwizzleWrapper(adapter));
            cbSwizzle.SelectedIndex = _selectedSwizzleIndex;
        }

        private void CbEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedEncodingIndex = cbEncoding.SelectedIndex;
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
                    50,
                    EncodingPropertyTextBox_TextChanged,
                    EncodingPropertyComboBox_SelectedIndexChanged,
                    SelectedColorEncodingAdapter.
                        GetType().
                        GetCustomAttributes(typeof(PropertyAttribute), false).
                        Cast<PropertyAttribute>().
                        ToArray());
        }

        private void UpdateSwizzleProperty()
        {
            _pnlSwizzleProperties.Controls.Clear();

            if (SelectedSwizzleAdapter != null)
                UpdateExtendedPropertiesWith(
                    _pnlSwizzleProperties,
                    50,
                    SwizzlePropertyTextBox_TextChanged,
                    SwizzlePropertyComboBox_SelectedIndexChanged,
                    SelectedSwizzleAdapter.
                        GetType().
                        GetCustomAttributes(typeof(PropertyAttribute), false).
                        Cast<PropertyAttribute>().
                        ToArray());
        }

        private void UpdateExtendedPropertiesWith(SplitterPanel panel, int width, EventHandler textChangedEvent, EventHandler indexChangedEvent, params PropertyAttribute[] propAttributes)
        {
            int x = 3;
            foreach (var attr in propAttributes)
            {
                if (attr.PropertyType.IsPrimitive)
                {
                    AddPrimitiveProperty(attr, _pnlEncodingProperties, textChangedEvent, width, x, 0);
                }
                else if (attr.PropertyType.IsEnum)
                {
                    AddEnumProperty(attr, _pnlEncodingProperties, indexChangedEvent, width, x, 0);
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
                Location = new Point(x, 0),
                Size = new Size(50, 15)
            };
            var textBox = new TextBox
            {
                Location = new Point(x, label.Height),
                Size = new Size(50, 20),
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

            LoadImage();
        }

        private void SwizzlePropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            if (!int.TryParse(tb.Text, out var value))
                return;

            var propAttr = (PropertyAttribute)tb.Tag;

            var adapterProperty = SelectedSwizzleAdapter.GetType()
                .GetProperty(propAttr.PropertyName, propAttr.PropertyType);
            adapterProperty?.SetValue(SelectedSwizzleAdapter, value);

            LoadImage();
        }

        private void AddEnumProperty(PropertyAttribute propAttr, SplitterPanel panel, EventHandler indexChangedEvent, int width, int x, int y)
        {
            if (!propAttr.PropertyType.IsEnum)
                return;

            var formatWrappers = Enum.GetNames(propAttr.PropertyType).
                Zip(Enum.GetValues(propAttr.PropertyType).Cast<object>(), Tuple.Create).
                Select(enumValue => (object)new FormatWrapper(Convert.ToInt32(enumValue.Item2), enumValue.Item1)).
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
            comboBox.SelectedIndex = 0;

            panel.Controls.Add(label);
            panel.Controls.Add(comboBox);
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

            LoadImage();
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

            LoadImage();
        }

        #endregion

        private void TbOffset_TextChanged(object sender, EventArgs e)
        {
            LoadImage();
        }

        private async void LoadImage()
        {
            if (!_fileLoaded || _openedFile == null)
                return;

            if (!int.TryParse(tbOffset.Text, out var offset) || !int.TryParse(tbWidth.Text, out var width) || !int.TryParse(tbHeight.Text, out var height))
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
            pbMain.Image = await SelectedColorEncodingAdapter.Decode(imgData, width, height, progress);

            ToggleProperties(true);
            ToggleForm(true);
        }

        private void ToggleProperties(bool toggle)
        {
            var textBoxes = splExtendedProperties.Panel1.Controls.OfType<TextBox>();
            var comboBoxes = splExtendedProperties.Panel1.Controls.OfType<ComboBox>();

            foreach (var textBox in textBoxes)
                textBox.Enabled = toggle;
            foreach (var comboBox in comboBoxes)
                comboBox.Enabled = toggle;
        }

        private void ToggleForm(bool toggle)
        {
            cbEncoding.Enabled = toggle;
            cbSwizzle.Enabled = toggle;
            openToolStripMenuItem.Enabled = toggle;
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

        private void TbWidth_TextChanged(object sender, EventArgs e)
        {
            LoadImage();
        }

        private void TbHeight_TextChanged(object sender, EventArgs e)
        {
            LoadImage();
        }
    }
}
