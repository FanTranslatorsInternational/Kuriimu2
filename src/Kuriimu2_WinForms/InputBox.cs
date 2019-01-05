using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms
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
                textBox.Text = defaultValue;
        }

        public string InputText { get => textBox.Text; }

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
