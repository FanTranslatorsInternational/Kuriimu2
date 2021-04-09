using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Kontract.Interfaces.Managers;
using Kontract.Models.Dialog;

namespace Kuriimu2.WinForms.ExtensionForms
{
    partial class DialogManagerForm : Form, IDialogManager
    {
        private const int ControlPadding_ = 5;

        public void ShowDialog(DialogField[] fields)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DialogField[]>(ShowDialog));
                return;
            }

            BuildForm(fields);
            if (ShowDialog() != DialogResult.OK)
                throw new InvalidOperationException("Dialog was cancelled.");
        }

        private void BuildForm(IList<DialogField> fields)
        {
            Controls.Clear();

            var position = new Point(ControlPadding_, ControlPadding_);
            foreach (var field in fields)
            {
                // Create label if text is given
                if (!string.IsNullOrEmpty(field.Text))
                    position = AddLabel(field.Text, position);

                // Create input control
                position = AddInput(field, position);
            }

            // Add Ok button
            position = AddOkButton(position);

            // Adjust height of form
            var topBarSize = 50;
            Height = position.Y + topBarSize;
        }

        private Point AddLabel(string text, Point position)
        {
            var label = new Label
            {
                AutoSize = true,
                Text = text,
                Location = position
            };
            Controls.Add(label);

            return new Point(position.X, position.Y + label.Height + ControlPadding_);
        }

        private Point AddInput(DialogField field, Point position)
        {
            Control inputControl;
            switch (field.Type)
            {
                case DialogFieldType.DropDown:
                    var cmb = new ComboBox
                    {
                        AutoSize = true,
                        Text = field.DefaultValue,
                        Location = position,
                        Tag = field
                    };
                    cmb.Items.AddRange(field.Options);
                    cmb.SelectedIndexChanged += Cmb_SelectedIndexChanged;

                    inputControl = cmb;
                    break;

                case DialogFieldType.TextBox:
                    var txt = new TextBox
                    {
                        AutoSize = true,
                        Text = field.DefaultValue,
                        Location = position,
                        Tag = field
                    };
                    txt.TextChanged += Txt_TextChanged;

                    inputControl = txt;
                    break;

                default:
                    return position;
            }

            field.Result = field.DefaultValue;

            Controls.Add(inputControl);
            return new Point(position.X, position.Y + inputControl.Height + ControlPadding_);
        }

        private Point AddOkButton(Point position)
        {
            var btn = new Button
            {
                AutoSize = true,
                Text = "OK",
                Location = position
            };
            btn.Click += Btn_Click;

            Controls.Add(btn);

            return new Point(position.X, position.Y + btn.Height + ControlPadding_);
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            var field = comboBox?.Tag as DialogField;
            if (comboBox == null || field == null)
                return;

            field.Result = (string)comboBox.SelectedItem;
        }

        private void Txt_TextChanged(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            var field = textBox?.Tag as DialogField;
            if (textBox == null || field == null)
                return;

            field.Result = textBox.Text;
        }
    }
}
