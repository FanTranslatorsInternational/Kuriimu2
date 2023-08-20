using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Interfaces.Quantization;
using Kontract.Kanvas.Models;

namespace Kanvas
{
    public static class Composition
    {
        #region ToBitmap

        public static Bitmap ToBitmap(this IEnumerable<int> indices, IList<Color> palette, Size imageSize) =>
            indices.Select(i => palette[i]).ToBitmap(imageSize);

        public static Bitmap ToBitmap(this IEnumerable<int> indices, IList<Color> palette, Size imageSize, IImageSwizzle swizzle) =>
            indices.Select(i => palette[i]).ToBitmap(imageSize, imageSize, swizzle, ImageAnchor.TopLeft);

        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize) =>
            colors.ToBitmap(imageSize, imageSize, null, ImageAnchor.TopLeft);

        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize, Size paddedSize) =>
            colors.ToBitmap(imageSize, imageSize, null, ImageAnchor.TopLeft);

        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize, IImageSwizzle swizzle) =>
            colors.ToBitmap(imageSize, imageSize, swizzle, ImageAnchor.TopLeft);

        /// <summary>
        /// Compose an image from a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to compose in the image.</param>
        /// <param name="imageSize">The dimensions of the composed image.</param>
        /// <param name="paddedSize">The padded dimensions of the composed image. Used for the swizzle operation. Is equal to <param name="imageSize">, if it was not further modified by the framework.</param></param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <param name="anchor">Defines where the image with its real size is anchored in the padded size.</param>
        /// <returns>The composed image.</returns>
        public static Bitmap ToBitmap(this IEnumerable<Color> colors, Size imageSize, Size paddedSize, IImageSwizzle swizzle, ImageAnchor anchor)
        {
            var image = new Bitmap(imageSize.Width, imageSize.Height);

            var bitmapData = image.LockBits(new Rectangle(Point.Empty, imageSize), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            // Get point sequence modified by swizzle
            var finalSize = imageSize != paddedSize ? paddedSize : swizzle != null ? new Size(swizzle.Width, swizzle.Height) : imageSize;
            var colorPoints = colors.Zip(GetPointSequence(finalSize, swizzle));

            // Get difference between final padded size and real size
            var widthDiff = finalSize.Width - imageSize.Width;
            var heightDiff = finalSize.Height - imageSize.Height;

            foreach (var (color, p) in colorPoints)
            {
                // If point lies outside the difference based on the anchor, return
                if (anchor == ImageAnchor.BottomLeft || anchor == ImageAnchor.BottomRight)
                    if (p.Y - heightDiff < 0)
                        continue;

                if (anchor == ImageAnchor.TopRight || anchor == ImageAnchor.BottomRight)
                    if (p.X - widthDiff < 0)
                        continue;

                // If point lies inside the difference based on the anchor, modify it
                var point = p;
                if (anchor == ImageAnchor.BottomRight || anchor == ImageAnchor.BottomLeft)
                    point = new Point(point.X, point.Y - heightDiff);
                if (anchor == ImageAnchor.TopRight || anchor == ImageAnchor.BottomRight)
                    point = new Point(point.X - widthDiff, point.Y);

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
            indices.Select(x => palette[x]);

        public static IEnumerable<Color> ToColors(this Bitmap image) =>
            image.ToColors(image.Size, null, ImageAnchor.TopLeft);

        public static IEnumerable<Color> ToColors(this Bitmap image, Size paddedSize) =>
            image.ToColors(paddedSize, null, ImageAnchor.TopLeft);

        public static IEnumerable<Color> ToColors(this Bitmap image, IImageSwizzle swizzle) =>
            image.ToColors(image.Size, swizzle, ImageAnchor.TopLeft);

        public static IEnumerable<Color> ToColors(this Bitmap image, Size paddedSize, ImageAnchor anchor) =>
            image.ToColors(paddedSize, null, anchor);

        /// <summary>
        /// Decomposes an image to a collection of colors.
        /// </summary>
        /// <param name="image">The image to decompose.</param>
        /// <param name="paddedSize">The padded dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <param name="anchor">Defines where the image with its real size is anchored in the padded size.</param>
        /// <returns>The collection of colors.</returns>
        public static IEnumerable<Color> ToColors(this Bitmap image, Size paddedSize, IImageSwizzle swizzle, ImageAnchor anchor)
        {
            var bitmapData = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var finalSize = image.Size != paddedSize ? paddedSize : swizzle != null ? new Size(swizzle.Width, swizzle.Height) : image.Size;

            // Get difference between final padded size and real size
            var widthDiff = finalSize.Width - image.Size.Width;
            var heightDiff = finalSize.Height - image.Size.Height;

            // Get point sequence, modified by swizzle
            var points = GetPointSequence(finalSize, swizzle)
                .Clamp(GetMinPoint(widthDiff, heightDiff, anchor), GetMaxPoint(image.Size, widthDiff, heightDiff, anchor));

            foreach (var p in points)
            {
                // Return default color if swizzled point is out of bounds relative to anchor
                if (anchor == ImageAnchor.BottomLeft || anchor == ImageAnchor.BottomRight)
                    if (p.Y - heightDiff < 0)
                    {
                        yield return Color.Black;
                        continue;
                    }

                if (anchor == ImageAnchor.TopRight || anchor == ImageAnchor.BottomRight)
                    if (p.X - widthDiff < 0)
                    {
                        yield return Color.Black;
                        continue;
                    }

                // If point lies inside the difference based on the anchor, modify it
                var point = p;
                if (anchor == ImageAnchor.BottomRight || anchor == ImageAnchor.BottomLeft)
                    point = new Point(point.X, point.Y - heightDiff);
                if (anchor == ImageAnchor.TopRight || anchor == ImageAnchor.BottomRight)
                    point = new Point(point.X - widthDiff, point.Y);

                // If point is out of bounds of source image, return default color
                if (point.X >= bitmapData.Width || point.Y >= bitmapData.Height)
                {
                    yield return Color.Black;
                    continue;
                }

                // Otherwise return color from source image at the given index
                var index = point.Y * bitmapData.Width + point.X;
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

        public static IEnumerable<int> ToIndices(this IEnumerable<Color> colors, IList<Color> palette)
        {
            var foundColors = new Dictionary<int, int>();

            foreach (var color in colors)
            {
                var colorValue = color.ToArgb();
                if (foundColors.ContainsKey(colorValue))
                {
                    yield return foundColors[colorValue];
                    continue;
                }

                for (var i = 0; i < palette.Count; i++)
                {
                    if (palette[i].ToArgb() == colorValue)
                    {
                        foundColors[palette[i].ToArgb()] = i;
                        yield return i;
                        break;
                    }
                }
            }
        }

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
            points.Select(p => new Point(Math.Clamp(p.X, min.X, max.X), Math.Clamp(p.Y, min.Y, max.Y)));

        // ReSharper disable once PossibleNullReferenceException
        private static unsafe Color GetColor(BitmapData bitmapData, int index) =>
            Color.FromArgb(((int*)bitmapData.Scan0)[index]);

        // ReSharper disable once PossibleNullReferenceException
        private static unsafe void SetColor(BitmapData bitmapData, int index, Color color)
        {
            ((int*)bitmapData.Scan0)[index] = color.ToArgb();
        }

        private static Point GetMinPoint(int widthDiff, int heightDiff, ImageAnchor anchor)
        {
            switch (anchor)
            {
                case ImageAnchor.TopLeft:
                    return Point.Empty;

                case ImageAnchor.TopRight:
                    return new Point(widthDiff, 0);

                case ImageAnchor.BottomLeft:
                    return new Point(0, heightDiff);

                case ImageAnchor.BottomRight:
                    return new Point(widthDiff, heightDiff);

                default:
                    throw new InvalidOperationException($"Unknown image anchor {anchor}.");
            }
        }

        private static Point GetMaxPoint(Size imageSize, int widthDiff, int heightDiff, ImageAnchor anchor)
        {
            switch (anchor)
            {
                case ImageAnchor.TopLeft:
                    return new Point(imageSize.Width - 1, imageSize.Height - 1);

                case ImageAnchor.TopRight:
                    return new Point(imageSize.Width + widthDiff - 1, imageSize.Height - 1);

                case ImageAnchor.BottomLeft:
                    return new Point(imageSize.Width - 1, imageSize.Height + heightDiff - 1);

                case ImageAnchor.BottomRight:
                    return new Point(imageSize.Width + widthDiff - 1, imageSize.Height + heightDiff - 1);

                default:
                    throw new InvalidOperationException($"Unknown image anchor {anchor}.");
            }
        }
    }
}
