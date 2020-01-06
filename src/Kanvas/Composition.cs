using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Kontract.Kanvas;

namespace Kanvas
{
    public static class Composition
    {
        /// <summary>
        /// Compose an image from a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to compose in the image.</param>
        /// <param name="imageSize">The true dimensions of the composed image.</param>
        /// <param name="paddedSize">The padded dimensions of the composed image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <returns>The composed image.</returns>
        public static Image ComposeImage(IEnumerable<Color> colors, Size imageSize, Size paddedSize, IImageSwizzle swizzle = null)
        {
            var image = new Bitmap(imageSize.Width, imageSize.Height);

            var bitmapData = image.LockBits(new Rectangle(Point.Empty, imageSize), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            var colorPoints = colors.Zip(GetPointSequence(image.Size, paddedSize, swizzle));

            foreach (var (color, point) in colorPoints)
            {
                if (point.X >= imageSize.Width || point.Y >= imageSize.Height)
                    continue;

                var index = point.Y * imageSize.Width + point.X;
                SetColor(bitmapData, index, color);
            }

            image.UnlockBits(bitmapData);

            return image;
        }

        /// <summary>
        /// Decomposes an image to a collection of colors.
        /// </summary>
        /// <param name="image">The image to decompose.</param>
        /// <param name="paddedSize">The padded dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <returns>The collection of colors.</returns>
        public static IEnumerable<Color> DecomposeImage(Bitmap image, Size paddedSize, IImageSwizzle swizzle = null)
        {
            var bitmapData = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var points = GetPointSequence(image.Size, paddedSize, swizzle)
                .Clamp(Point.Empty, new Point(image.Width - 1, image.Height));

            foreach (var point in points)
            {
                var index = point.Y * image.Width + point.X;
                yield return GetColor(bitmapData, index);
            }

            image.UnlockBits(bitmapData);
        }

        /// <summary>
        /// Create a sequence of <see cref="Point"/>s.
        /// </summary>
        /// <param name="imageSize">The true dimensions of the image.</param>
        /// <param name="paddedSize">The padded dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the points.</param>
        /// <returns>The sequence of <see cref="Point"/>s.</returns>
        internal static IEnumerable<Point> GetPointSequence(Size imageSize, Size paddedSize, IImageSwizzle swizzle = null)
        {
            var size = paddedSize == Size.Empty ? imageSize : paddedSize;
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                {
                    var point = new Point(x, y);
                    if (swizzle != null)
                        point = swizzle.Transform(point);

                    yield return point;
                }
        }

        private static IEnumerable<Point> Clamp(this IEnumerable<Point> points, Point min, Point max)
        {
            return points.Select(p => new Point(Math.Clamp(p.X, min.X, max.X), Math.Clamp(p.Y, min.Y, max.Y)));
        }

        private static unsafe Color GetColor(BitmapData bitmapData, int index)
        {
            return Color.FromArgb(((int*)bitmapData.Scan0)[index]);
        }

        private static unsafe void SetColor(BitmapData bitmapData, int index, Color color)
        {
            ((int*)bitmapData.Scan0)[index] = color.ToArgb();
        }
    }
}
