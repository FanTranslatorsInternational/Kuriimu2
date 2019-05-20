using System;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.MainForms
{
    public partial class InputBox : Form
    {
        public InputBox(string labelText, string formText = "", string defaultValue = "")
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(labelText))
                label.Text = labelText + ":";

            if (!string.IsNullOrEmpty(formText))
                Text = formText;

            if (!string.IsNullOrEmpty(defaultValue))
                txtInput.Text = defaultValue;
        }

        public string InputText { get => txtInput.Text; }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
