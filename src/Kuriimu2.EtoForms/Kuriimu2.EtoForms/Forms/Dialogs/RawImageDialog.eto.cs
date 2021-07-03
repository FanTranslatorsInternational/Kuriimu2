using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Controls.ImageView;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class RawImageDialog:Dialog
    {
        private ImageViewEx imageView;

        private TextBox widthText;
        private TextBox heightText;
        private TextBox offsetText;
        private TextBox palOffsetText;
        private ComboBox encodings;
        private ComboBox palEncodings;
        private ComboBox swizzles;

        private GroupBox encodingParameters;
        private GroupBox palEncodingParameters;
        private GroupBox swizzleParameters;

        private Label statusLabel;

        #region Commands

        private Command openFileCommand;
        private Command closeFileCommand;
        private Command extractImageCommand;
        private Command processCommand;

        #endregion

        #region Localization Keys

        private const string RawImageViewerKey_ = "RawImageViewer";

        private const string EncodingParametersKey_ = "RawImageViewerEncodingParameters";
        private const string EncodingPaletteParametersKey_ = "RawImageViewerEncodingPaletteParameters";
        private const string SwizzleParametersKey_ = "RawImageViewerSwizzleParameters";

        private const string OpenKey_ = "Open";
        private const string CloseKey_ = "Close";
        private const string ExtractKey_ = "Extract";
        private const string FileKey_ = "File";

        private const string WidthKey_ = "Width";
        private const string HeightKey_ = "Height";
        private const string FormatKey_ = "Format";
        private const string PaletteKey_ = "Palette";

        private const string OffsetKey_ = "Offset";
        private const string PaletteOffsetKey_ = "PaletteOffset";
        private const string SwizzleKey_ = "Swizzle";

        private const string ProcessKey_ = "Process";

        #endregion

        private void InitializeComponent()
        {
            #region Controls

            imageView = new ImageViewEx { BackgroundColor = KnownColors.DarkGreen };

            widthText = new TextBox { Text = "1" };
            heightText = new TextBox { Text = "1" };
            offsetText = new TextBox { Text = "0" };
            palOffsetText = new TextBox { Text = "0" };
            encodings = new ComboBox();
            palEncodings = new ComboBox();
            swizzles = new ComboBox();

            encodingParameters = new GroupBox { Text = Localize(EncodingParametersKey_) };
            palEncodingParameters= new GroupBox { Text = Localize(EncodingPaletteParametersKey_) };
            swizzleParameters = new GroupBox { Text = Localize(SwizzleParametersKey_) };

            statusLabel = new Label { TextColor = KnownColors.DarkRed };

            #endregion

            #region Commands

            openFileCommand = new Command { MenuText = Localize(OpenKey_) };
            closeFileCommand = new Command { MenuText = Localize(CloseKey_), Enabled = false };
            extractImageCommand = new Command { MenuText = Localize(ExtractKey_), Enabled = false };
            processCommand = new Command { Enabled = false };

            #endregion

            Title = Localize(RawImageViewerKey_);
            Size = new Size(500, 600);
            Padding = new Padding(4);

            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem { Text = Localize(FileKey_), Items = { openFileCommand, closeFileCommand, new SeparatorMenuItem(), extractImageCommand } },
                }
            };

            #region Content

            var baseParameterLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,

                Spacing = 3,

                Items =
                {
                    new TableLayout
                    {
                        Spacing = new Size(3,3),
                        Rows =
                        {
                            new TableRow { Cells = { new Label { Text = Localize(WidthKey_) }, widthText } },
                            new TableRow { Cells = { new Label { Text = Localize(HeightKey_) }, heightText } },
                            new TableRow { Cells = { new Label { Text = Localize(OffsetKey_) }, offsetText } },
                            new TableRow { Cells = { new Label { Text = Localize(FormatKey_) }, encodings } },
                            new TableRow { Cells = { new Label { Text = Localize(PaletteOffsetKey_) }, palOffsetText } },
                            new TableRow { Cells = { new Label { Text = Localize(PaletteKey_) }, palEncodings } },
                            new TableRow { Cells = { new Label { Text = Localize(SwizzleKey_) }, swizzles } },
                        }
                    },
                    new Button { Text = Localize(ProcessKey_), Command = processCommand }
                }
            };

            var encodingParameterLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,

                Items =
                {
                    encodingParameters,
                    palEncodingParameters,
                    swizzleParameters
                }
            };

            Content = new TableLayout
            {
                Spacing = new Size(3, 3),

                Rows =
                {
                    new TableRow { ScaleHeight = true, Cells = { imageView } },
                    new TableLayout
                    {
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    baseParameterLayout,
                                    encodingParameterLayout
                                }
                            }
                        }
                    },
                    statusLabel
                }
            };

            #endregion
        }
    }
}
