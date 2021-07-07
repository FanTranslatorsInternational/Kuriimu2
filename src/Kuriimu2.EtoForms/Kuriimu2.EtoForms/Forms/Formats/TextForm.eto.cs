using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel
    {
        private Label firstEntryText;
        private Button toggleButton;
        private Button saveButton;

        private Command toggleCommand;
        private Command saveCommand;

        private void InitializeComponent()
        {
            toggleCommand = new Command();
            saveCommand = new Command();

            firstEntryText = new Label{ Size=new Eto.Drawing.Size(-1,200)};
            toggleButton = new Button { Text = "Toggle codes", Command = toggleCommand };
            saveButton= new Button { Text = "Save", Command = saveCommand };

            Content = new TableLayout
            {
                Rows =
                {
                    firstEntryText,
                    toggleButton,
                    saveButton
                }
            };
        }
    }
}
