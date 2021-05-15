using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Support
{
    class ParameterBuilder
    {
        private const int RowNumber = 3;

        private readonly GroupBox _tableLayout;

        public event EventHandler ValueChanged;

        public ParameterBuilder(GroupBox tableLayout)
        {
            _tableLayout = tableLayout;
        }

        public void SetParameters(IList<ExtensionTypeParameter> parameters)
        {
            if (parameters == null)
            {
                _tableLayout.Content = new TableLayout();
                return;
            }

            var rows = new List<TableRow>();
            for (var i = 0; i < parameters.Count; i++)
            {
                if (i % RowNumber == 0)
                    rows.Add(new TableRow());

                var parameter = parameters[i];
                rows.Last().Cells.Add(CreateControl(parameter));
            }

            _tableLayout.Content = new TableLayout(rows);
        }

        private Control CreateControl(ExtensionTypeParameter parameter)
        {
            if (parameter.IsFile)
                return CreateFileInput(parameter);

            if (parameter.ParameterType.IsEnum)
                return CreateComboBox(parameter);

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
                    return CreateTextBox(parameter);

                case TypeCode.Boolean:
                    return CreateCheckBox(parameter);
            }

            throw new InvalidOperationException("The parameter cannot be parsed to a control.");
        }

        private Control CreateFileInput(ExtensionTypeParameter parameter)
        {
            var buttonCommand = new Command { Tag = parameter };
            buttonCommand.Executed += buttonCommand_Click;

            return new TableLayout
            {
                Rows =
                {
                    new Label {Text = parameter.Name + ":"},
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            new TextBox {ReadOnly = true, ID = parameter.Name},
                            new Button {Text = "...", Command = buttonCommand}
                        }
                    }
                }
            };
        }

        private Control CreateComboBox(ExtensionTypeParameter parameter)
        {
            var enumNames = Enum.GetNames(parameter.ParameterType).ToList();

            var comboBox = new ComboBox { Tag = parameter, DataStore = enumNames };
            comboBox.SelectedValueChanged += comboBox_SelectedValueChanged;

            if (parameter.HasDefaultValue)
                comboBox.SelectedIndex = enumNames.ToList().IndexOf(parameter.Value.ToString());

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,

                Items =
                {
                    new Label {Text = parameter.Name + ":"},
                    comboBox
                }
            };
        }

        private Control CreateTextBox(ExtensionTypeParameter parameter)
        {
            var textBox = new TextBox
            {
                Text = parameter.HasDefaultValue ? parameter.Value.ToString() : string.Empty,
                Tag = parameter
            };
            textBox.TextChanged += textBox_TextChanged;

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,

                Items =
                {
                    new Label {Text = parameter.Name + ":"},
                    textBox
                }
            };
        }

        private Control CreateCheckBox(ExtensionTypeParameter parameter)
        {
            var checkBox = new CheckBox
            {
                Text = parameter.Name,
                Checked = parameter.HasDefaultValue && (bool)parameter.Value,
                Tag = parameter
            };
            checkBox.CheckedChanged += checkBox_CheckedChanged;

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,

                Items = { checkBox }
            };
        }

        #region Events

        private void buttonCommand_Click(object sender, EventArgs e)
        {
            var parameter = (ExtensionTypeParameter)((Command)sender).Tag;

            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog(_tableLayout) != DialogResult.Ok)
                return;

            // Update value
            _tableLayout.FindChild<TextBox>(parameter.Name).Text = ofd.FileName;
            parameter.Value = ofd.FileName;

            OnValueChanged(_tableLayout.FindChild<TextBox>(parameter.Name));
        }

        private void comboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            var parameter = (ExtensionTypeParameter)((ComboBox)sender).Tag;

            // Update value
            parameter.Value = ((ComboBox)sender).SelectedValue;

            OnValueChanged(sender);
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            var parameter = (ExtensionTypeParameter)((CheckBox)sender).Tag;

            // Update value
            parameter.Value = ((CheckBox)sender).Checked ?? false;

            OnValueChanged(sender);
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            var parameter = (ExtensionTypeParameter)textBox.Tag;

            // If value is char
            if (parameter.ParameterType == typeof(char))
                if (char.TryParse(textBox.Text, out var charValue))
                {
                    parameter.Value = charValue;
                    OnValueChanged(sender);
                    return;
                }

            // If value is a string
            if (parameter.ParameterType == typeof(string))
            {
                parameter.Value = textBox.Text;
                OnValueChanged(sender);
                return;
            }

            // If value is number
            if (TryParseNumber(parameter.ParameterType, ((TextBox)sender).Text, out var parsedValue))
                parameter.Value = parsedValue;
            OnValueChanged(sender);
        }

        #endregion

        #region Support

        private static bool TryParseNumber(Type numberType, string stringValue, out object parsedValue)
        {
            parsedValue = null;

            var method = numberType.GetMethod("TryParse", new[] { typeof(string), numberType.MakeByRefType() });
            var inputObjects = new[] { stringValue, Activator.CreateInstance(numberType) };

            if (method == null)
                return false;

            var result = method.Invoke(null, inputObjects);
            if (result == null || !(bool)result)
                return false;

            parsedValue = inputObjects[1];
            return true;
        }

        #endregion

        private void OnValueChanged(object sender)
        {
            ValueChanged?.Invoke(sender, new EventArgs());
        }
    }
}
