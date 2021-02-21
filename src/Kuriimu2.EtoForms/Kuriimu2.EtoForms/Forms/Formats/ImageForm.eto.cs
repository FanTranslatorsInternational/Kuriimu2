using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Controls.ImageView;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ImageForm:Panel
    {
        private Command saveCommand;
        private Command saveAsCommand;
        private Command exportCommand;
        private Command importCommand;

        private ButtonToolStripItem saveButton;
        private ButtonToolStripItem saveAsButton;
        private ButtonToolStripItem exportButton;
        private ButtonToolStripItem importButton;

        private ImageViewEx imageView;

        private Label width;
        private Label height;
        private ComboBox formats;
        private ComboBox palettes;
        private ListBox images;
        private Drawable imagePalette;

        private void InitializeComponent()
        {
            #region Commands

            saveCommand = new Command();
            saveAsCommand = new Command();
            exportCommand = new Command();
            importCommand = new Command();

            #endregion

            #region Controls

            #region Buttons

            saveButton = new ButtonToolStripItem
            {
                Command = saveCommand,
                Image = MenuSaveResource
            };

            saveAsButton = new ButtonToolStripItem
            {
                Command = saveAsCommand,
                Image = MenuSaveAsResource
            };

            exportButton = new ButtonToolStripItem
            {
                Command = exportCommand,
                Image = MenuExportResource
            };

            importButton = new ButtonToolStripItem
            {
                Command = importCommand,
                Image = MenuImportResource
            };

            #endregion

            #region Default

            imageView = new ImageViewEx();
            var widthLabel = new Label { Text = "Width:" };
            var heightLabel = new Label { Text = "Height:" };
            var formatLabel = new Label { Text = "Format:" };
            var paletteLabel = new Label { Text = "Palette:" };
            width = new Label();
            height = new Label();
            formats = new ComboBox();
            palettes = new ComboBox();
            images = new ListBox();
            imagePalette = new Drawable();

            #endregion

            #region Toolstrip

            var mainToolStrip = new ToolStrip
            {
                Items =
                {
                    saveButton,
                    saveAsButton,
                    new SplitterToolStripItem(),
                    exportButton,
                    importButton
                }
            };

            #endregion

            #region Layouts

            var imageLayout = new StackLayout
            {
                Spacing = 3,
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    new StackLayoutItem(imageView) { Expand = true, HorizontalAlignment = HorizontalAlignment.Stretch },
                    new TableLayout
                    {
                        Spacing = new Size(3, 3),
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    widthLabel,
                                    width,
                                    heightLabel,
                                    height,
                                    new TableCell { ScaleWidth = true }
                                }
                            },
                            new TableRow
                            {
                                Cells =
                                {
                                    formatLabel,
                                    formats,
                                    paletteLabel,
                                    palettes,
                                    new TableCell { ScaleWidth = true }
                                }
                            }
                        }
                    }
                }
            };

            var listLayout = new StackLayout
            {
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Vertical,
                Items =
                {
                    new StackLayoutItem(images, true),
                    new StackLayoutItem(imagePalette, true)
                }
            };

            var mainLayout = new TableLayout
            {
                Spacing = new Size(3, 3),
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(imageLayout) { ScaleWidth = true },
                            listLayout
                        }
                    }
                }
            };

            #endregion

            #endregion

            Content = new TableLayout
            {
                Rows =
                {
                    new TableRow(new Panel { Content = mainToolStrip, Size = new Size(-1, (int)ToolStripItem.Height + 6) }),
                    mainLayout
                }
            };
        }
    }
}
