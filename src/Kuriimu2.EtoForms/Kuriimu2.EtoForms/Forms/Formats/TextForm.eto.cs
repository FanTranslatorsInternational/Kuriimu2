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

            _firstEntryText = new Label{ Size=new Eto.Drawing.Size(-1,300)};
            _toggleButton = new Button { Text = "Toggle codes", Command = _toggleCommand, Size=new Eto.Drawing.Size(-1,30) };

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
