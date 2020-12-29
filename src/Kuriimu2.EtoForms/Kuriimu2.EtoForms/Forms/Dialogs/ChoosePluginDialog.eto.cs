using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;
using Kuriimu2.EtoForms.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog<IFilePlugin>
    {
        private StackLayout pluginListPanel;
        private Button okButton;

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
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            okButton = new Button { Text = "Ok", Command = okButtonCommand };

            Content = new FixedSplitter(620)
            {
                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel2,

                Panel1 = new Scrollable
                {
                    Content = pluginListPanel
                },
                Panel2 = new TableLayout
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
                                new Button { Text = "Cancel", Command = cancelButtonCommand },
                                okButton
                            }
                        }
                    }
                }
            };

            #endregion
        }
    }
}
