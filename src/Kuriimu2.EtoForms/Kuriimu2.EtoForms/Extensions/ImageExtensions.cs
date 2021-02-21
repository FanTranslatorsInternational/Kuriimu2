using System.Linq;
using Eto.Drawing;
using Kanvas;

namespace Kuriimu2.EtoForms.Extensions
{
    public static class ImageExtensions
    {
        public static Bitmap ToEto(this System.Drawing.Bitmap image)
        {
            return new Bitmap(image.Width, image.Height, PixelFormat.Format32bppRgba,
                image.ToColors().Select(x => Color.FromArgb(x.R, x.G, x.B, x.A)).ToArray());
        }
    }
}
