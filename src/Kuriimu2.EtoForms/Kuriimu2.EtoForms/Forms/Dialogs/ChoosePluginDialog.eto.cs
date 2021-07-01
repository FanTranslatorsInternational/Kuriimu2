using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private StackLayout pluginListPanel;
        private Button continueButton;
        private Button viewRawButton;
        private Button cancelButton;
        private CheckBox showAllCheckbox;

        #region Commands

        private Command continueButtonCommand;
        private Command viewRawButtonCommand;
        private Command cancelButtonCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            continueButtonCommand = new Command();
            viewRawButtonCommand = new Command();
            cancelButtonCommand = new Command();

            #endregion

            Title = "Choose plugin";
            Size = new Size(550, 500);
            Padding = new Padding(3);

            #region Content

            pluginListPanel = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            
            continueButton = new Button { Text = "Continue", Command = continueButtonCommand };
            viewRawButton = new Button {Text = "View raw bytes", Command = viewRawButtonCommand };
            viewRawButton.Width = viewRawButton.MinimumSize.Width + 16; // add some ooga-booga padding
            cancelButton = new Button { Text = "Cancel", Command = cancelButtonCommand };
            
            showAllCheckbox = new CheckBox()
            {
                Text = "Show all plugins",
                ToolTip = _filterNote,
                Checked = _filteredPlugins == null,
                Enabled = _filteredPlugins != null
            };

            Content = new TableLayout
            {
                Padding = new Padding(10, 6),
                Spacing = new Size(0, 10),
                
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new Label
                            {
                                Text = _message + ":",
                                TextAlignment = TextAlignment.Center,
                            }
                        } 
                    },
                    
                    new TableRow { ScaleHeight = true, Cells = { new Scrollable { Content = pluginListPanel } } },
                    
                    new TableRow { Cells = { showAllCheckbox } },
                    
                    new TableLayout
                    {
                        Padding = new Padding(0, 3),
                        Spacing = new Size(3, 3),
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    new TableCell { ScaleWidth = true },
                                    continueButton,
                                    viewRawButton,
                                    cancelButton
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
