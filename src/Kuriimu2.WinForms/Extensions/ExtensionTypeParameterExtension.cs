using System;
using System.Windows.Forms;
using Kuriimu2.WinForms.ExtensionForms.Models;

namespace Kuriimu2.WinForms.Extensions
{
    static class ExtensionTypeParameterExtension
    {
        public static bool TryParse(this ExtensionTypeParameter parameter, Control control, out string error)
        {
            error = "";

            // If type is a file
            if (parameter.IsFile)
            {
                var fileTextBox = control as TextBox;
                if (string.IsNullOrEmpty(fileTextBox.Text))
                {
                    error = $"Parameter '{parameter.Name}' is empty.";
                    return false;
                }

                parameter.Value = fileTextBox.Text;
                return true;
            }

            // If type is an enum
            if (parameter.ParameterType.IsEnum)
            {
                var enumName = ((ComboBox)control).SelectedText;
                if (!Enum.IsDefined(parameter.ParameterType, enumName))
                {
                    error = $"'{enumName}' is no valid member of  parameter '{parameter.Name}'.";
                    return false;
                }

                parameter.Value = Enum.Parse(parameter.ParameterType, enumName);
                return true;
            }

            // If type is a boolean
            if (parameter.ParameterType == typeof(bool))
            {
                parameter.Value = ((CheckBox)control).Checked;
                return true;
            }

            // If type is text based
            var textBox = (TextBox)control;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                error = $"Parameter '{parameter.Name}' is empty.";
                return false;
            }

            if (parameter.ParameterType == typeof(char))
            {
                if (!char.TryParse(textBox.Text, out var result))
                {
                    error = $"'{textBox.Text}' in parameter '{parameter.Name}' is no valid character.";
                    return false;
                }

                parameter.Value = result;
                return true;
            }

            if (parameter.ParameterType == typeof(string))
            {
                parameter.Value = textBox.Text;
                return true;
            }

            if (!TryParseNumber(parameter.ParameterType, textBox.Text, out var value))
            {
                error = $"'{textBox.Text}' in parameter '{parameter.Name}' is no valid '{parameter.ParameterType.Name}'.";
                return false;
            }

            parameter.Value = value;
            return true;
        }

        private static bool TryParseNumber(Type numberType, string stringValue, out object parsedValue)
        {
            parsedValue = null;

            var method = numberType.GetMethod("TryParse", new[] { typeof(string), numberType.MakeByRefType() });
            var inputObjects = new[] { stringValue, Activator.CreateInstance(numberType) };

            if (!(bool)method.Invoke(null, inputObjects))
                return false;

            parsedValue = inputObjects[1];
            return true;
        }
    }
}
