using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Kontract.Kanvas;
using Kontract.Kanvas.Quantization;

namespace Kanvas
{
    public static class Composition
    {
        #region ToBitmap

        public static Bitmap ToBitmap(this IEnumerable<int> indices, IList<Color> palette, Size imageSize) =>
            indices.Select(i => palette[i]).ToBitmap(imageSize);

        public static Bitmap ToBitmap(this IEnumerable<int> indices, IList<Color> palette, Size imageSize, IImageSwizzle swizzle) =>
            indices.Select(i => palette[i]).ToBitmap(imageSize, swizzle);

        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize) =>
            colors.ToBitmap(imageSize, null);

        /// <summary>
        /// Compose an image from a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to compose in the image.</param>
        /// <param name="imageSize">The dimensions of the composed image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <returns>The composed image.</returns>
        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize, IImageSwizzle swizzle)
        {
            var image = new Bitmap(imageSize.Width, imageSize.Height);

            var bitmapData = image.LockBits(new Rectangle(Point.Empty, imageSize), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            var colorPoints = Zip(colors, GetPointSequence(imageSize, swizzle));

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

        #endregion

        #region ToColors

        public static IEnumerable<Color> ToColors(this IEnumerable<int> indices, IList<Color> palette) =>
            indices.Select(i => palette[i]);

        public static IEnumerable<Color> ToColors(this Bitmap image) =>
            image.ToColors(Size.Empty, null);

        public static IEnumerable<Color> ToColors(this Bitmap image, Size paddedSize) =>
            image.ToColors(paddedSize, null);

        public static IEnumerable<Color> ToColors(this Bitmap image, IImageSwizzle swizzle) =>
            image.ToColors(Size.Empty, swizzle);

        /// <summary>
        /// Decomposes an image to a collection of colors.
        /// </summary>
        /// <param name="image">The image to decompose.</param>
        /// <param name="paddedSize">The padded dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <returns>The collection of colors.</returns>
        public static IEnumerable<Color> ToColors(this Bitmap image, Size paddedSize, IImageSwizzle swizzle)
        {
            var bitmapData = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var imageSize = paddedSize.IsEmpty ? image.Size : paddedSize;
            var points = GetPointSequence(imageSize, swizzle)
                .Clamp(Point.Empty, new Point(image.Width - 1, image.Height));

            foreach (var point in points)
            {
                var index = point.Y * image.Width + point.X;
                yield return GetColor(bitmapData, index);
            }

            image.UnlockBits(bitmapData);
        }

        #endregion

        #region ToIndices

        public static IEnumerable<int> ToIndices(this Bitmap image, IList<Color> palette) =>
            image.ToColors().ToIndices(palette);

        public static IEnumerable<int> ToIndices(this Bitmap image, IColorCache colorCache) =>
            image.ToColors().ToIndices(colorCache);

        public static IEnumerable<int> ToIndices(this IEnumerable<Color> colors, IList<Color> palette) =>
            colors.Select(palette.IndexOf);

        public static IEnumerable<int> ToIndices(this IEnumerable<Color> colors, IColorCache colorCache) =>
            colors.Select(colorCache.GetPaletteIndex);

        #endregion

        /// <summary>
        /// Create a sequence of <see cref="Point"/>s.
        /// </summary>
        /// <param name="imageSize">The dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the points.</param>
        /// <returns>The sequence of <see cref="Point"/>s.</returns>
        internal static IEnumerable<Point> GetPointSequence(Size imageSize, IImageSwizzle swizzle = null)
        {
            for (var y = 0; y < imageSize.Height; y++)
                for (var x = 0; x < imageSize.Width; x++)
                {
                    var point = new Point(x, y);
                    if (swizzle != null)
                        point = swizzle.Transform(point);

                    yield return point;
                }
        }

        private static IEnumerable<Point> Clamp(this IEnumerable<Point> points, Point min, Point max) => 
            points.Select(p => new Point(Clamp(p.X, min.X, max.X), Clamp(p.Y, min.Y, max.Y)));

        // ReSharper disable once PossibleNullReferenceException
        private static unsafe Color GetColor(BitmapData bitmapData, int index) =>
            Color.FromArgb(((int*)bitmapData.Scan0)[index]);

        // ReSharper disable once PossibleNullReferenceException
        private static unsafe void SetColor(BitmapData bitmapData, int index, Color color)
        {
            ((int*)bitmapData.Scan0)[index] = color.ToArgb();
        }

        // TODO: Remove when targeting only netcoreapp31
        private static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
#if NET_CORE_31
            return first.Zip(second);
#else
            return first.Zip(second, (f, s) => (f, s));
#endif
        }

        // TODO: Remove when targeting only netcoreapp31
        private static int Clamp(int value, int min, int max)
        {
#if NET_CORE_31
            return Math.Clamp(value, min, max);
#else
            return Math.Max(min, Math.Min(value, max));
#endif
        }
    }
}
