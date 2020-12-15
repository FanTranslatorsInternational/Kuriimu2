using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ArchiveForm : Panel
    {
        #region Commands

        private Command saveCommand;
        private Command saveAsCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            saveCommand = new Command { MenuText = "Save" };
            saveAsCommand = new Command { MenuText = "Save As" };

            #endregion

            Content = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,

                Items =
                {
                    new Button { Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save.png"), Size = new Size(32, -1), Command = saveCommand },
                    new Button { Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save-as.png"), Size = new Size(32, -1), Command = saveAsCommand },
                    new Label { BackgroundColor = KnownColors.Black, Size = new Size(2, -1) }
                }
            };
        }
    }
}
