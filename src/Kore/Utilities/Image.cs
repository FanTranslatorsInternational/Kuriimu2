using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kore.Utilities
{
    /// <summary>
    /// Container class for Image related utility functions.
    /// </summary>
    public static class Image
    {
        /// <summary>
        /// Converts a list of colors into a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap ComposeImage(IList<Color> colors, int width, int height)
        {
            var image = new Bitmap(width, height);
            var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    if (i >= colors.Count)
                        break;
                    ptr[i] = colors[i].ToArgb();
                }
            }
            image.UnlockBits(data);

            return image;
        }
    }
}
