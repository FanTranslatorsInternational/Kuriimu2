using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;
using Kuriimu2_WinForms.MainForms.Models;

namespace Kuriimu2_WinForms.MainForms
{
    public partial class ImageTranscoder : Form
    {
        private readonly PluginLoader _loader;

        private bool _imgLoaded;
        private Stream _imgStream;

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

        public ImageTranscoder(PluginLoader loader)
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
            foreach (var adapter in _loader.GetAdapters<IImageSwizzleAdapter>())
                cbSwizzle.Items.Add(new SwizzleWrapper(adapter));
            if (_selectedSwizzleIndex < cbSwizzle.Items.Count)
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

        private void UpdateForm()
        {
            cbEncoding.Enabled = cbEncoding.Items.Count > 0;
            cbSwizzle.Enabled = cbSwizzle.Items.Count > 0;
            exportToolStripMenuItem.Enabled = _imgLoaded;
            btnTranscode.Enabled = _imgLoaded;
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

            if (SelectedSwizzleAdapter != null)
                UpdateExtendedPropertiesWith(
                    _pnlSwizzleProperties,
                    50,
                    SwizzlePropertyTextBox_TextChanged,
                    SwizzlePropertyComboBox_SelectedIndexChanged,
                    SwizzlePropertyCheckBox_CheckedChanged,
                    SelectedSwizzleAdapter.
                        GetType().
                        GetCustomAttributes(typeof(PropertyAttribute), false).
                        Cast<PropertyAttribute>().
                        ToArray());
        }

        private void UpdateExtendedPropertiesWith(SplitterPanel panel, int width, EventHandler textChangedEvent,
            EventHandler indexChangedEvent, EventHandler checkedChangedEvent, params PropertyAttribute[] propAttributes)
        {
            int x = 3;
            foreach (var attr in propAttributes)
            {
                if (attr.PropertyType == typeof(bool))
                {
                    AddBooleanProperty(attr, _pnlEncodingProperties, checkedChangedEvent, width, x, 0);
                }
                else if (attr.PropertyType.IsPrimitive)
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

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open image...",
                Filter = "Portable Network Graphics (*.png)|*.png|JPEG (*.jpg)|*.jpg"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            OpenImage(ofd.FileName);
            TranscodeImage();
        }

        private void OpenImage(string imgFile)
        {
            _imgStream = File.OpenRead(imgFile);
            pbSource.Image = (Bitmap)Image.FromStream(_imgStream);
            _imgLoaded = true;

            UpdateForm();
        }

        private async void TranscodeImage()
        {
            if (!_imgLoaded || pbSource.Image == null)
                return;

            ToggleProperties(false);
            ToggleForm(false);

            var sourceImage = (Bitmap)Image.FromStream(_imgStream);
            var progress = new Progress<ProgressReport>();
            progress.ProgressChanged += Progress_ProgressChanged;

            try
            {
                var imgData = await SelectedColorEncodingAdapter.Encode(sourceImage, progress);
                pbTarget.Image = await SelectedColorEncodingAdapter.Decode(imgData, sourceImage.Width, sourceImage.Height, progress);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            tslPbHeightSource.Text = pbSource.Image.Height.ToString();
            tslPbWidthSource.Text = pbSource.Image.Width.ToString();
            tslPbHeightTarget.Text = pbTarget.Image.Height.ToString();
            tslPbWidthTarget.Text = pbTarget.Image.Width.ToString();

            ToggleProperties(true);
            ToggleForm(true);
        }

        private void Progress_ProgressChanged(object sender, ProgressReport e)
        {
            throw new NotImplementedException();
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
            exportToolStripMenuItem.Enabled = toggle;
            btnTranscode.Enabled = toggle;
        }

        private void ExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportImage();
        }

        private void ExportImage()
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export image...",
                Filter = "Portable Network Graphics (*.png)|*.png|JPEG (*.jpg)|*.jpg"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            pbTarget.Image.Save(sfd.FileName);
        }

        private void BtnTranscode_Click(object sender, EventArgs e)
        {
            TranscodeImage();
        }

        private void PbSource_ZoomChanged(object sender, EventArgs e)
        {
            // ReSharper disable once LocalizableElement
            tslZoomSource.Text = $"Zoom: {pbSource.Zoom}%";
        }

        private void PbTarget_ZoomChanged(object sender, EventArgs e)
        {
            // ReSharper disable once LocalizableElement
            tslZoomTarget.Text = $"Zoom: {pbTarget.Zoom}%";
        }
    }
}
