using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog
    {
        #region Commands

        private Command okButtonCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            okButtonCommand = new Command();

            #endregion

            Title = "Choose plugin";
            ClientSize = new Size(400, 700);
            Padding = new Padding(3);

            #region Content

            Content = new Splitter
            {
                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel2,

                Panel1 = new StackLayout(),
                Panel2 = new TableLayout
                {
                    Padding = new Padding(3),
                    Spacing=new Size(3,3),
                    Rows =
                    {
                        new TableRow
                        {
                            Cells =
                            {
                                new TableCell { ScaleWidth = true },
                                new Button { Text = "Cancel" },
                                new Button { Text = "Ok", Command = okButtonCommand }
                            }
                        }
                    }
                }
            };

            #endregion
        }
    }
}
