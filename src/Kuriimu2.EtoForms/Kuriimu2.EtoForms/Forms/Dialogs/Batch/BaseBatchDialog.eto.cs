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

        #region Localization Keys

        private const string SearchSubFoldersKey_="SearchSubfolders";
        private const string AvgTimePerFileKey_ = "AvgTimePerFile";

        private const string BatchAvailablePluginsKey_ = "BatchAvailablePlugins";
        private const string BatchSelectInputKey_ = "BatchSelectInput";
        private const string BatchSelectOutputKey_ = "BatchSelectOutput";
        private const string BatchExecuteKey_ = "Execute";

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            plugins = new ComboBox();
            selectedInputPath = new TextBox { ReadOnly = true };
            subDirectoryBox = new CheckBox { Text = Localize(SearchSubFoldersKey_) };
            selectedOutputPath = new TextBox { ReadOnly = true };
            timerLabel = new Label { Text = Localize(AvgTimePerFileKey_) };
            log = new RichTextArea { ReadOnly = true, BackgroundColor = KnownColors.Black, TextColor = KnownColors.NeonGreen };

            #endregion

            #region Commands

            selectInputCommand = new Command();
            selectOutputCommand = new Command();
            executeCommand = new Command();

            #endregion

            Padding = new Padding(6);
            Size = new Size(700, 300);

            #region Content

            var pluginLabel = new Label { Text = Localize(BatchAvailablePluginsKey_) };
            var selectInputButton = new Button { Text = Localize(BatchSelectInputKey_), Command = selectInputCommand, Size = new Size(130, -1) };
            var selectOutputButton = new Button { Text = Localize(BatchSelectOutputKey_), Command = selectOutputCommand, Size = new Size(130, -1) };
            var executeButton = new Button { Text = Localize(BatchExecuteKey_), Command = executeCommand, Size = new Size(130, -1) };

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
