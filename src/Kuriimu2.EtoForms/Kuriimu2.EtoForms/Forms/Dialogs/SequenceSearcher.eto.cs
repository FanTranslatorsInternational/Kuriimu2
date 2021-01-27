﻿using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class SequenceSearcher:Dialog
    {
        private TextBox inputText;
        private TextBox searchText;

        private ComboBox encodings;
        private CheckBox searchSubfoldersBox;

        private ListBox resultList;
        private Label warningLabel;

        #region Commands

        private Command browseCommand;
        private Command executeCommand;
        private Command cancelCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            inputText = new TextBox { ReadOnly = true, Size = new Size(300, -1) };
            searchText = new TextBox();
            encodings = new ComboBox { Size=new Size(200,-1)};
            searchSubfoldersBox = new CheckBox { Text="Search Subfolders" };
            resultList = new ListBox { Size=new Size(-1, 250) };
            warningLabel = new Label();

            #endregion

            #region Command

            browseCommand = new Command();
            executeCommand = new Command();
            cancelCommand = new Command { Enabled = false };

            #endregion

            Title = "Text Sequence Searcher";
            Size = new Size(500, 400);
            Padding = new Padding(6);

            #region Content

            var inputLayout = new TableLayout
            {
                Spacing=new Size(6,6),

                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new Label{Text="Find in:",VerticalAlignment=VerticalAlignment.Center},
                            inputText,
                            new Button{Text="Browse...", Command=browseCommand}
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new Label{Text="Find what:",VerticalAlignment=VerticalAlignment.Center},
                            searchText,
                            new Button{Text="Find", Command=executeCommand}
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new Label{Text="Encoding:",VerticalAlignment=VerticalAlignment.Center},
                            new StackLayout
                            {
                                Orientation=Orientation.Horizontal,
                                VerticalContentAlignment=VerticalAlignment.Stretch,

                                Spacing=6,

                                Items =
                                {
                                    encodings,
                                    searchSubfoldersBox
                                }
                            },
                            new Button{Text="Cancel", Command=cancelCommand}
                        }
                    }
                }
            };

            Content = new StackLayout
            {
                Orientation=Orientation.Vertical,
                HorizontalContentAlignment=HorizontalAlignment.Stretch,

                Spacing=6,

                Items =
                {
                    inputLayout,
                    resultList,
                    warningLabel
                }
            };

            #endregion
        }
    }
}
