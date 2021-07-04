using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel
    {
        private Label _firstEntryText;
        private Button _toggleButton;

        private Command _toggleCommand;

        private void InitializeComponent()
        {
            _toggleCommand = new Command();

            _firstEntryText = new Label();
            _toggleButton = new Button { Text = "Toggle codes", Command = _toggleCommand };

            Content = new TableLayout
            {
                Rows =
                {
                    _firstEntryText,
                    _toggleButton
                }
            };
        }
    }
}
