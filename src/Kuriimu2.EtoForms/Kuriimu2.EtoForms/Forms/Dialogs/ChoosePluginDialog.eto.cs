using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private StackLayout pluginListPanel;
        private Button okButton;
        private Button cancelButton;

        #region Commands

        private Command okButtonCommand;
        private Command cancelButtonCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            okButtonCommand = new Command();
            cancelButtonCommand = new Command();

            #endregion

            Title = "Choose plugin";
            Size = new Size(450, 700);
            Padding = new Padding(3);

            #region Content

            pluginListPanel = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            cancelButton = new Button { Text = "Cancel", Command = cancelButtonCommand };
            okButton = new Button { Text = "Ok", Command = okButtonCommand };

            Content = new TableLayout
            {
                Rows =
                {
                    new TableRow { ScaleHeight = true, Cells = { new Scrollable { Content = pluginListPanel } } },
                    new TableLayout
                    {
                        Padding=new Padding(0, 3),
                        Spacing = new Size(3, 3),
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    new TableCell { ScaleWidth = true },
                                    cancelButton,
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
