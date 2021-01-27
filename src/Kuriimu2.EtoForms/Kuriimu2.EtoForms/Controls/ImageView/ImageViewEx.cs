using Eto.Drawing;
using Eto.Forms;

// Credit: https://github.com/rafntor/Eto.ImageViewEx
namespace Kuriimu2.EtoForms.Controls.ImageView
{
    public class ImageViewEx : DragScrollable
    {
        public ImageViewEx()
        {
            DragButton = MouseButtons.Primary;
            Content.PanButton = MouseButtons.Alternate;
            base.Content = Content;
        }

        public new ImageViewZoomable Content { get; } = new ImageViewZoomable();

        public Image Image
        {
            get => Content.Image;
            set => Content.Image = value;
        }
    }
}
