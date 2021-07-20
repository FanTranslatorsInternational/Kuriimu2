using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base
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

        #region Localization Keys

        private const string ExtensionParametersKey_ = "ExtensionParameters";
        private const string SearchSubfoldersKey_ = "SearchSubfolders";
        private const string AutoExecuteKey_ = "AutoExecute";

        private const string SelectFolderKey_ = "SelectFolder";
        private const string SelectFileKey_ = "SelectFile";

        private const string ExecuteKey_ = "Execute";

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            parameterBox = new GroupBox { Text = Localize(ExtensionParametersKey_) };
            log = new RichTextArea { ReadOnly = true, BackgroundColor = Themer.GetTheme().LoggerBackColor };

            typeLabel = new Label();
            extensions = new ComboBox();
            selectedPath = new TextBox { ReadOnly = true };
            subDirectoryBox = new CheckBox { Text = Localize(SearchSubfoldersKey_) };

            autoExecuteBox = new CheckBox { Text = Localize(AutoExecuteKey_) };

            #endregion

            #region Commands

            selectFolderCommand = new Command();
            selectFileCommand = new Command();
            executeCommand = new Command();

            #endregion

            Padding = new Padding(6);
            Size = new Size(700, 300);

            #region Content

            selectFolderButton = new Button { Text = Localize(SelectFolderKey_), Command = selectFolderCommand, Size = new Size(130, -1) };
            selectFileButton = new Button { Text = Localize(SelectFileKey_), Command = selectFileCommand };
            executeButton = new Button { Text = Localize(ExecuteKey_), Command = executeCommand };

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
                AllowDrop = true,

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
