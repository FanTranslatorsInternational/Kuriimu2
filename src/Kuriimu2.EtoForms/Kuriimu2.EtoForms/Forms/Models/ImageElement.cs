using Eto.Drawing;

namespace Kuriimu2.EtoForms.Forms.Models
{
    class ImageElement
    {
        public Image Thumbnail { get; private set; }

        public string Text { get; }

        public ImageElement(Image thumbnail, string text)
        {
            Thumbnail = thumbnail;
            Text = text;
        }

        public void UpdateThumbnail(Image thumbnail)
        {
            Thumbnail = thumbnail;
        }
    }
}
