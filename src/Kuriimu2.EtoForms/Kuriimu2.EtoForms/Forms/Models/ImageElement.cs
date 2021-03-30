using Eto.Drawing;

namespace Kuriimu2.EtoForms.Forms.Models
{
    class ImageElement
    {
        public Image Image { get; }

        public string Text { get; }

        public ImageElement(Image image, string text)
        {
            Image = image;
            Text = text;
        }
    }
}
