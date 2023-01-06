using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kontract.Interfaces.Managers.Dialogs;
using Kontract.Models.Managers.Dialogs;
using Kuriimu2.ImGui.Resources;
using Serilog;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    class DialogManager : IDialogManager
    {
        public async Task<bool> ShowDialog(DialogField[] fields)
        {
            var modal = CreateDialog(fields);
            var result = await modal.ShowAsync();
            if (result == DialogResult.Ok)
                return true;

            return false;
        }

        private Modal CreateDialog(DialogField[] fields)
        {
            var modal = new Modal();

            // Setup layout
            var layout = new TableLayout
            {
                Size = Size.Content,
                Spacing = new Vector2(4, 4)
            };

            AddFields(fields, layout);
            AddOkButton(layout, modal);

            // Setup modal
            var layoutWidth = layout.GetWidth(Application.Instance.MainForm.Width);
            var layoutHeight = layout.GetWidth(Application.Instance.MainForm.Height);

            modal.Size = new Vector2(layoutWidth, layoutHeight);
            modal.Content = layout;

            return modal;
        }

        private void AddOkButton(TableLayout layout, Modal modal)
        {
            layout.Rows.Add(new TableRow
            {
                Cells =
                {
                    null,
                    new TableCell(CreateOkButton(modal)) {HorizontalAlignment = HorizontalAlignment.Right}
                }
            });
        }

        private void AddFields(DialogField[] fields, TableLayout layout)
        {
            foreach (var field in fields)
                AddField(field, layout);
        }

        private void AddField(DialogField field, TableLayout layout)
        {
            var row = new TableRow();

            // Add descriptive label
            var textLabel = string.IsNullOrEmpty(field.Text) ? null : new Label { Text = field.Text };
            row.Cells.Add(new TableCell(textLabel));

            // Add field input
            Component input = null;
            switch (field.Type)
            {
                case DialogFieldType.TextBox:
                    input = CreateTextBox(field);
                    break;

                case DialogFieldType.DropDown:
                    input = CreateComboBox(field);
                    break;
            }

            row.Cells.Add(input);

            // Add field row to layout
            layout.Rows.Add(row);
        }

        private Button CreateOkButton(Modal modal)
        {
            var button = new Button { Text = LocalizationResources.DialogManagerButtonOk(), Width = 75 };
            button.Clicked += (s, e) => modal.Close(DialogResult.Ok);

            return button;
        }

        private TextBox CreateTextBox(DialogField field)
        {
            var input = new TextBox { Text = field.DefaultValue };
            input.TextChanged += (s, e) => field.Result = input.Text;

            return input;
        }

        private ComboBox<string> CreateComboBox(DialogField field)
        {
            var comboBox = new ComboBox<string>();
            comboBox.SelectedItemChanged += (s, e) => field.Result = comboBox.SelectedItem.Content;

            foreach (var option in field.Options)
                comboBox.Items.Add(option);

            field.Result = field.DefaultValue;
            comboBox.SelectedItem = new ComboBoxItem<string>(field.DefaultValue);

            return comboBox;
        }
    }
}
