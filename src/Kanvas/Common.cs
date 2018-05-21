using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Kanvas.Interface;

namespace Kanvas
{
    /// <summary>
    /// Defines the settings with which an image will be loaded/saved
    /// </summary>
    public class ImageSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public IImageFormat Format { get; set; }

        public int padWidth { get; set; } = 0;
        public int padHeight { get; set; } = 0;

        public IImageSwizzle Swizzle { get; set; }
        public Func<Color, Color> PixelShader { get; set; }
    }

    /// <summary>
    /// Basic wrapper for all supported Image Formats in Kuriimu
    /// </summary>
    public class Common
    {
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max - 1);
        public static int Clamp(double n) => (int)Math.Max(0, Math.Min(n, 255));

        /// <summary>
        /// Gives back a sequence of points, modified by Swizzles if applied
        /// </summary>
        static IEnumerable<Point> GetPointSequence(ImageSettings settings)
        {
            int strideWidth = (settings.Swizzle != null) ? settings.Swizzle.Width : settings.Width;
            int strideHeight = (settings.Swizzle != null) ? settings.Swizzle.Height : settings.Height;

            for (int i = 0; i < strideWidth * strideHeight; i++)
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

            var points = GetPointSequence(settings);

            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (int*)data.Scan0;
                foreach (var pair in points.Zip(settings.Format.Load(bytes), Tuple.Create))
                {
                    int x = pair.Item1.X, y = pair.Item1.Y;
                    if (0 <= x && x < width && 0 <= y && y < height)
                    {
                        var color = pair.Item2;
                        if (settings.PixelShader != null) color = settings.PixelShader(color);
                        ptr[data.Stride * y / 4 + x] = color.ToArgb();
                    }
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
            var points = GetPointSequence(settings);
            var colors = new List<Color>();

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
    }
}