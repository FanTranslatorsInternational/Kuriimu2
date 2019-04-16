using Kanvas.Interface;
using Kanvas.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Kanvas
{
    /// <summary>
    /// Main wrapper for all supported Image Formats in Kanvas.
    /// </summary>
    public class Common
    {
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max - 1);
        public static int Clamp(double n) => (int)Math.Max(0, Math.Min(n, 255));

        /// <summary>
        /// Gives back a sequence of points, modified by Swizzles if applied
        /// </summary>
        private static IEnumerable<Point> GetPointSequence(ImageSettings settings)
        {
            int strideWidth = settings.Swizzle?.Width ?? settings.Width;
            int strideHeight = settings.Swizzle?.Height ?? settings.Height;

            for (var i = 0; i < strideWidth * strideHeight; i++)
            {
                var point = new Point(i % strideWidth, i / strideWidth);
                if (settings.Swizzle != null)
                    point = settings.Swizzle.Get(point);

                yield return point;
            }
        }

        /// <summary>
        /// Loads the binary data with given settings as an image
        /// </summary>
        /// <param name="bytes">Bytearray containing the binary image data</param>
        /// <param name="settings">The settings determining the final image output</param>
        /// <returns>Bitmap</returns>
        public static Bitmap Load(byte[] bytes, ImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;

            // Used mainly for the block compressions PVRTC and ASTC
            if (settings.Format is IImageFormatKnownDimensions ifkd)
            {
                ifkd.Width = width;
                ifkd.Height = height;
            }

            var points = GetPointSequence(settings);

            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var imagePtr = (int*)data.Scan0;
                if (imagePtr == null) throw new ArgumentNullException(nameof(imagePtr));
                foreach (var (point, color) in points.Zip(settings.Format.Load(bytes), Tuple.Create))
                {
                    int x = point.X, y = point.Y;
                    if (0 > x || x >= width || 0 > y || y >= height) continue;

                    imagePtr[data.Stride * y / 4 + x] = settings.PixelShader?.Invoke(color).ToArgb() ?? color.ToArgb();
                }
            }
            bmp.UnlockBits(data);

            return bmp;
        }

        /// <summary>
        /// Converts a given Bitmap, modified by given settings, in binary data
        /// </summary>
        /// <param name="bmp">The bitmap, which will be converted</param>
        /// <param name="settings">Settings like Format, Dimensions and Swizzles</param>
        /// <returns>byte[]</returns>
        public static byte[] Save(Bitmap bmp, ImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;

            if (settings.Format is IImageFormatKnownDimensions ifkd)
            {
                ifkd.Width = width;
                ifkd.Height = height;
            }

            var colors = new List<Color>();
            var points = GetPointSequence(settings);

            foreach (var point in points)
            {
                int x = Clamp(point.X, 0, bmp.Width);
                int y = Clamp(point.Y, 0, bmp.Height);

                var color = bmp.GetPixel(x, y);

                if (settings.PixelShader != null) color = settings.PixelShader(color);

                colors.Add(color);
            }

            return settings.Format.Save(colors);
        }

        /// <summary>
        /// Loads the binary data with given settings as an image.
        /// </summary>
        /// <param name="bytes">Bytearray containing the binary image data.</param>
        /// <param name="paletteBytes">Bytearray containing the binary palette data.</param>
        /// <param name="settings">The settings determining the final image output.</param>
        /// <returns>Bitmap</returns>
        public static (Bitmap image, IList<Color> palette) Load(byte[] bytes, byte[] paletteBytes, PaletteImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;
            var paletteFormat = settings.PaletteFormat;

            var points = GetPointSequence(settings);
            var indeces = paletteFormat.LoadIndeces(bytes);
            var palette = settings.Format.Load(paletteBytes).ToList();

            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var imagePtr = (int*)data.Scan0;
                if (imagePtr == null) throw new ArgumentNullException(nameof(imagePtr));
                foreach (var (point, indexData) in points.Zip(indeces, Tuple.Create))
                {
                    int x = point.X, y = point.Y;
                    if (0 > x || x >= width || 0 > y || y >= height) continue;

                    imagePtr[data.Stride * y / 4 + x] = paletteFormat.RetrieveColor(indexData, palette).ToArgb();
                }
            }
            bmp.UnlockBits(data);

            return (bmp, palette);
        }

        /// <summary>
        /// Converts a given Bitmap, modified by given settings, in binary data
        /// </summary>
        /// <param name="bmp">The bitmap, which will be converted.</param>
        /// <param name="palette">The list containing all colors of the palette to use.</param>
        /// <param name="settings">The settings determining the final binary data output.</param>
        /// <returns><see cref="Tuple"/> containing 2 byte arrays</returns>
        public static (byte[] indexData, byte[] paletteData) Save(Bitmap bmp, IList<Color> palette, PaletteImageSettings settings)
        {
            var indeces = new List<IndexData>();
            var points = GetPointSequence(settings);  // Swizzle

            foreach (var point in points)
            {
                var x = Clamp(point.X, 0, bmp.Width);
                var y = Clamp(point.Y, 0, bmp.Height);

                var color = bmp.GetPixel(x, y);
                var index = settings.PaletteFormat.RetrieveIndex(color, palette);

                indeces.Add(index);
            }

            return (settings.PaletteFormat.SaveIndices(indeces), settings.Format.Save(palette));
        }
    }
}