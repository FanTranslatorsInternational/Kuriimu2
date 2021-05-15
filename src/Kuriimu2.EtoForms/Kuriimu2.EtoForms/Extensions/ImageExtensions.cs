using System.IO;
using System.Linq;
using Eto.Drawing;
using Kanvas;

namespace Kuriimu2.EtoForms.Extensions
{
    public static class ImageExtensions
    {
        public static Bitmap ToEto(this System.Drawing.Bitmap image)
        {
            // HINT: Substitute solution; Convert to PNG and load it with Eto
            var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            ms.Position = 0;
            return new Bitmap(ms);

            // TODO: Get direct conversion of System.Drawing to Eto.Drawing working
            //return new Bitmap(image.Width, image.Height,PixelFormat.Format32bppRgba,
            //    image.ToColors().Select(x => Color.FromArgb(x.R, x.G, x.B, x.A)).ToArray());
        }
    }
}
