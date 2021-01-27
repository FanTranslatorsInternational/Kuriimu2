using System;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class InputBox : Dialog<string>
    {
        public InputBox(string labelText, string formText = "", string defaultValue = "")
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(labelText))
                label.Text = labelText + ":";

            if (!string.IsNullOrEmpty(formText))
                Title = formText;

            if (!string.IsNullOrEmpty(defaultValue))
                input.Text = defaultValue;

            okCommand.Executed += OkCommand_Executed;
            KeyUp += InputBox_KeyUp;
        }

        private void InputBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
                Close(input.Text);
        }

        private void OkCommand_Executed(object sender, EventArgs e)
        {
            Close(input.Text);
        }
    }
}
