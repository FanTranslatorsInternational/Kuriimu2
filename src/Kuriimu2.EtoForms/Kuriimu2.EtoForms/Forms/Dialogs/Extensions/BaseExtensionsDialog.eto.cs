using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    abstract partial class BaseExtensionsDialog<TExtension, TResult> : Dialog
    {
        private GroupBox parameterBox;
        private RichTextArea log;

        private Label typeLabel;
        private ComboBox extensions;
        private TextBox selectedPath;
        private CheckBox subDirectoryBox;

        private CheckBox autoExecuteBox;

        private Button selectFolderButton;
        private Button selectFileButton;
        private Button executeButton;

        #region Commands

        private Command selectFolderCommand;
        private Command selectFileCommand;
        private Command executeCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            parameterBox = new GroupBox { Text = "Parameters" };
            log = new RichTextArea { BackgroundColor = KnownColors.Black, TextColor = KnownColors.NeonGreen };

            typeLabel = new Label { Text = "TypeExtension:" };
            extensions = new ComboBox();
            selectedPath = new TextBox { ReadOnly = true };
            subDirectoryBox = new CheckBox { Text = "Sub Directories" };

            autoExecuteBox = new CheckBox { Text = "Auto-Execute on Drop" };

            #endregion

            #region Commands

            selectFolderCommand = new Command();
            selectFileCommand = new Command();
            executeCommand = new Command();

            #endregion

            Title = "TypeExtensionDialog";
            Padding = new Padding(6);
            Size = new Size(700, 300);

            AllowDrop = true;

            #region Content

            selectFolderButton = new Button { Text = "Select Folder...", Command = selectFolderCommand, Size = new Size(130, -1) };
            selectFileButton = new Button { Text = "Select File...", Command = selectFileCommand };
            executeButton = new Button { Text = "Execute", Command = executeCommand };

            var inputLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment=HorizontalAlignment.Stretch,

                Size=new Size(250,-1),
                Spacing=6,

                Items =
                {
                    typeLabel,
                    extensions,
                    selectedPath,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment=VerticalAlignment.Stretch,

                        Spacing=6,

                        Items =
                        {
                            selectFolderButton,
                            subDirectoryBox
                        }
                    },
                    selectFileButton
                }
            };

            var executeLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment=VerticalAlignment.Stretch,

                Spacing=6,

                Items =
                {
                    executeButton,
                    autoExecuteBox
                }
            };

            Content = new TableLayout
            {
                Spacing = new Size(6, 6),

                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            inputLayout,
                            parameterBox
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableLayout
                            {
                                Rows =
                                {
                                    new TableRow { ScaleHeight = true },
                                    new TableRow
                                    {
                                        Cells = { executeLayout }
                                    }
                                }
                            },
                            log
                        }
                    }
                }
            };

            #endregion
        }
    }
}
