using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Managers;
using Kontract.Models.Dialog;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    // HINT: This class can't derive and act as the dialog directly, since in Eto.Forms, once closed a dialog seemingly can't be opened after being closed.
    class DialogManagerDialog : IDialogManager
    {
        private readonly Control _owner;

        private Dialog<DialogResult> _dialog;

        public DialogManagerDialog(Control owner)
        {
            _owner = owner;
        }

        public void ShowDialog(DialogField[] fields)
        {
            Application.Instance.Invoke(() =>
            {
                _dialog = new Dialog<DialogResult> { Padding = new Padding(5), Content = CreateContent(fields) };
                if (_dialog.ShowModal(_owner) != DialogResult.Ok)
                    throw new InvalidOperationException("Dialog was cancelled.");
            });
        }

        #region Create controls

        private Control CreateContent(IList<DialogField> fields)
        {
            var layout = new StackLayout { Orientation = Orientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Stretch, Spacing = 5 };
            foreach (var field in fields)
            {
                // Create label if text is given
                if (!string.IsNullOrEmpty(field.Text))
                    layout.Items.Add(CreateLabel(field.Text));

                // Create input control
                layout.Items.Add(CreateInput(field));
            }

            // Add Ok button
            layout.Items.Add(CreateOkButton());

            return layout;
        }

        private Control CreateLabel(string text)
        {
            return new Label { Text = text };
        }

        private Control CreateInput(DialogField field)
        {
            field.Result = field.DefaultValue;

            switch (field.Type)
            {
                case DialogFieldType.DropDown:
                    var cmb = new ComboBox
                    {
                        Text = field.DefaultValue,
                        Tag = field
                    };
                    cmb.Items.AddRange(field.Options.Select(v => new ListItem { Text = v }));
                    cmb.SelectedIndexChanged += cmb_SelectedIndexChanged;

                    return cmb;

                case DialogFieldType.TextBox:
                    var txt = new TextBox
                    {
                        Text = field.DefaultValue,
                        Tag = field
                    };
                    txt.TextChanged += txt_TextChanged;

                    return txt;

                default:
                    throw new InvalidOperationException($"Unsupported dialog field type {field.Text}.");
            }
        }

        // TODO: Create right-aligned button control structure
        private Control CreateOkButton()
        {
            var okButton = new Button { Text = "OK" };
            okButton.Click += okButton_Click;

            return okButton;
        }

        #endregion

        #region Events

        private void cmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            var field = comboBox?.Tag as DialogField;
            if (comboBox == null || field == null)
                return;

            field.Result = comboBox.SelectedValue.ToString();
        }

        private void txt_TextChanged(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            var field = textBox?.Tag as DialogField;
            if (textBox == null || field == null)
                return;

            field.Result = textBox.Text;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            _dialog.Close(DialogResult.Ok);
        }

        #endregion
    }
}
