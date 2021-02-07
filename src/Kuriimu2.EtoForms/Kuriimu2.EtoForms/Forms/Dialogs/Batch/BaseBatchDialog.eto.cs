using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Batch
{
    abstract partial class BaseBatchDialog:Dialog
    {
        private ComboBox plugins;
        private TextBox selectedInputPath;
        private CheckBox subDirectoryBox;
        private TextBox selectedOutputPath;
        private Label timerLabel;
        private RichTextArea log;

        #region Commands

        private Command selectInputCommand;
        private Command selectOutputCommand;
        private Command executeCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            plugins = new ComboBox();
            selectedInputPath = new TextBox { ReadOnly = true };
            subDirectoryBox = new CheckBox { Text = "Sub Directories" };
            selectedOutputPath = new TextBox { ReadOnly = true };
            timerLabel = new Label { Text = "Avg time per file:" };
            log = new RichTextArea { ReadOnly = true, BackgroundColor = KnownColors.Black, TextColor = KnownColors.NeonGreen };

            #endregion

            #region Commands

            selectInputCommand = new Command();
            selectOutputCommand = new Command();
            executeCommand = new Command();

            #endregion

            Title = "BatchDialog";
            Padding = new Padding(6);
            Size = new Size(700, 300);

            #region Content

            var pluginLabel = new Label { Text = "Plugins" };
            var selectInputButton = new Button { Text = "Select Input...", Command = selectInputCommand, Size = new Size(130, -1) };
            var selectOutputButton = new Button { Text = "Select Output...", Command = selectOutputCommand, Size = new Size(130, -1) };
            var executeButton = new Button { Text = "Execute", Command = executeCommand, Size = new Size(130, -1) };

            var inputLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,

                Spacing = 6,

                Items =
                {
                    pluginLabel,
                    plugins,
                    selectedInputPath,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Stretch,

                        Spacing = 6,

                        Items =
                        {
                            selectInputButton,
                            subDirectoryBox
                        }
                    },
                    selectedOutputPath,
                    selectOutputButton
                }
            };

            Content = new TableLayout
            {
                AllowDrop=true,
                Spacing = new Size(6, 6),

                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableLayout
                            {
                                Rows =
                                {
                                    inputLayout,
                                    new TableLayout
                                    {
                                        Rows =
                                        {
                                            new TableRow { ScaleHeight = true },
                                            executeButton
                                        }
                                    }
                                }
                            },
                            new TableLayout
                            {
                                Spacing = new Size(6, 6),
                                Rows =
                                {
                                    timerLabel,
                                    log
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
