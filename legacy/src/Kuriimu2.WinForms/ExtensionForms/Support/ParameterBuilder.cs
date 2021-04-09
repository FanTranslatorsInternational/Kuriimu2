using System;
using System.Drawing;
using System.Windows.Forms;
using Kuriimu2.WinForms.ExtensionForms.Models;

namespace Kuriimu2.WinForms.ExtensionForms.Support
{
    class ParameterBuilder
    {
        private const int LineHeight_ = 40;
        private const int ControlDelta_ = 10;

        private readonly GroupBox _groupBox;
        private readonly Point _topLeftCorner = new Point(5, 15);

        private int _paddingLeft;
        private int _paddingTop;

        public ParameterBuilder(GroupBox groupBox)
        {
            _groupBox = groupBox;
        }

        public void Reset()
        {
            _groupBox.Controls.Clear();

            _paddingLeft = 0;
            _paddingTop = 0;
        }

        public void AddParameters(ExtensionTypeParameter[] parameters)
        {
            foreach (var parameter in parameters)
                AddParameter(parameter);
        }

        public void AddParameter(ExtensionTypeParameter parameter)
        {
            if (parameter.IsFile)
            {
                CreateFileInput(parameter);
                return;
            }

            if (parameter.ParameterType.IsEnum)
            {
                CreateComboBox(parameter);
                return;
            }

            switch (Type.GetTypeCode(parameter.ParameterType))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Char:
                case TypeCode.String:
                    CreateTextBox(parameter);
                    break;

                case TypeCode.Boolean:
                    CreateCheckBox(parameter);
                    break;
            }
        }

        private void CreateTextBox(ExtensionTypeParameter parameter)
        {
            var labelHeight = 15;
            var width = 100;

            UpdateLine(width + ControlDelta_);

            var label = new Label
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop),
                Size = new Size(width, labelHeight),
                Text = parameter.Name + ":",
                Name = "lbl" + parameter.Name
            };
            var textBox = new TextBox
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop + labelHeight),
                Size = new Size(width, 20),
                Name = parameter.Name,
                Tag = parameter
            };

            if (parameter.HasDefaultValue)
                textBox.Text = parameter.Value.ToString();

            _groupBox.Controls.Add(label);
            _groupBox.Controls.Add(textBox);

            UpdateWidth(width + ControlDelta_);
        }

        private void CreateCheckBox(ExtensionTypeParameter parameter)
        {
            var labelHeight = 15;
            var width = 100;

            UpdateLine(width + ControlDelta_);

            var chk = new CheckBox
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop + labelHeight),
                Size = new Size(width, 20),
                Text = parameter.Name,
                Name = parameter.Name,
                Tag = parameter,
            };

            if (parameter.HasDefaultValue)
                chk.Checked = (bool)parameter.Value;

            _groupBox.Controls.Add(chk);

            UpdateWidth(width + ControlDelta_);
        }

        private void CreateComboBox(ExtensionTypeParameter parameter)
        {
            var labelHeight = 15;
            var width = 100;

            UpdateLine(width + ControlDelta_);

            var label = new Label
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop),
                Size = new Size(width, labelHeight),
                Text = parameter.Name + ":",
                Name = "lbl" + parameter.Name
            };
            var comboBox = new ComboBox
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop + labelHeight),
                Size = new Size(width, 20),
                Name = parameter.Name,
                Tag = parameter
            };
            comboBox.Items.AddRange(Enum.GetNames(parameter.ParameterType));

            comboBox.Text = parameter.HasDefaultValue ?
                parameter.Value.ToString() :
                Enum.GetNames(parameter.ParameterType)[0];

            _groupBox.Controls.Add(label);
            _groupBox.Controls.Add(comboBox);

            UpdateWidth(width + ControlDelta_);
        }

        private void CreateFileInput(ExtensionTypeParameter parameter)
        {
            var labelHeight = 15;
            var width = 100;

            UpdateLine(width + ControlDelta_);

            var label = new Label
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop),
                Size = new Size(width, labelHeight),
                Text = parameter.Name + ":",
                Name = "lbl" + parameter.Name
            };
            var textBox = new TextBox
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft, _topLeftCorner.Y + _paddingTop + labelHeight),
                Size = new Size(width - 30, 20),
                Name = parameter.Name,
                ReadOnly = true
            };
            var button = new Button
            {
                Location = new Point(_topLeftCorner.X + _paddingLeft + textBox.Size.Width + 5, _topLeftCorner.Y + _paddingTop + labelHeight),
                Size = new Size(25, 20),
                Name = "btn" + parameter.Name,
                Text = "..."
            };

            button.Click += Button_Click;

            _groupBox.Controls.Add(label);
            _groupBox.Controls.Add(textBox);
            _groupBox.Controls.Add(button);

            UpdateWidth(width + ControlDelta_);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var textBox = _groupBox.Controls.Find(button.Name.Substring(3), false)[0];

            var ofd = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            textBox.Text = ofd.FileName;
        }

        private void UpdateLine(int controlWidth)
        {
            if (_paddingLeft + controlWidth >= _groupBox.Width)
            {
                _paddingLeft = 0;
                _paddingTop += LineHeight_;
            }
        }

        private void UpdateWidth(int controlWidth)
        {
            _paddingLeft += controlWidth;
        }
    }
}
