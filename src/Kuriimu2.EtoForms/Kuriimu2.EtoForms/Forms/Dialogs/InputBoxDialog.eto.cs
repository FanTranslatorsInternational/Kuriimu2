using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class InputBoxDialog:Dialog<string>
    {
        private Label label;
        private TextBox input;
        private Button okButton;

        #region Commands

        private Command okCommand;

        #endregion

        #region Initialization Keys

        private const string InputBoxOkButtonKey_ = "InputBoxOkButton";

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            okCommand = new Command();

            #endregion

            Size = new Size(300,110);
            Padding = new Padding(5);

            #region Content

            label = new Label();
            input = new TextBox();
            okButton = new Button { Text=Localize(InputBoxOkButtonKey_), Command = okCommand };

            Content = new StackLayout
            {
                Spacing=3,

                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Stretch,

                Items =
                {
                    label,
                    input,
                    new TableLayout
                    {
                        Padding = new Padding(3),
                        Spacing = new Size(3, 3),
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    new TableCell { ScaleWidth = true },
                                    okButton
                                }
                            }
                        }
                    }
                }
            };

            #endregion
        }
    }
}
