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

            encodingParameters = new GroupBox { Text = "Encoding Parameters" };
            palEncodingParameters= new GroupBox { Text = "Encoding Palette Parameters" };
            swizzleParameters = new GroupBox { Text = "Swizzle Parameters" };

            statusLabel = new Label { TextColor = KnownColors.DarkRed };

            #endregion

            #region Commands

            openFileCommand = new Command { MenuText = "Open" };
            closeFileCommand = new Command { MenuText = "Close", Enabled = false };
            extractImageCommand = new Command { MenuText = "Extract", Enabled = false };
            processCommand = new Command { Enabled = false };

            #endregion

            Title = "Raw Image Viewer";
            Size = new Size(500, 600);
            Padding = new Padding(4);

            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem { Text = "File", Items = { openFileCommand, closeFileCommand, new SeparatorMenuItem(), extractImageCommand } },
                }
            };

            #region Content

            var baseParameterLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment=HorizontalAlignment.Stretch,

                Spacing=3,

                Items =
                {
                    new TableLayout
                    {
                        Spacing=new Size(3,3),
                        Rows =
                        {
                            new TableRow { Cells = { new Label { Text="Width:"}, widthText } },
                            new TableRow { Cells = { new Label { Text="Height:"}, heightText } },
                            new TableRow { Cells = { new Label { Text="Offset:"}, offsetText } },
                            new TableRow { Cells = { new Label { Text="Encoding:"}, encodings } },
                            new TableRow { Cells = { new Label { Text="Palette Offset:"}, palOffsetText } },
                            new TableRow { Cells = { new Label { Text="Palette Encoding:"}, palEncodings } },
                            new TableRow { Cells = { new Label { Text="Swizzles:"}, swizzles } },
                        }
                    },
                    new Button { Text = "Process", Command = processCommand }
                }
            };

            var encodingParameterLayout = new StackLayout
            {
                Orientation=Orientation.Vertical,

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
