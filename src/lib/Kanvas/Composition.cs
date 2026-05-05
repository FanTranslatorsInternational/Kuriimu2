using Kanvas.Contract;
using Kanvas.Contract.Enums;
using Kanvas.Contract.Quantization.ColorCache;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas
{
    public static class Composition
    {
        #region ToBitmap

        public static Image<Rgba32> ToImage(this IEnumerable<int> indices, IList<Rgba32> palette, Size imageSize) =>
            indices.Select(i => palette[i]).ToImage(imageSize);

        public static Image<Rgba32> ToImage(this IEnumerable<int> indices, IList<Rgba32> palette, Size imageSize, IImageSwizzle swizzle) =>
            indices.Select(i => palette[i]).ToImage(imageSize, imageSize, swizzle, ImageAnchor.TopLeft);

        public static Image<Rgba32> ToImage(this IEnumerable<Rgba32> colors, Size imageSize) =>
            colors.ToImage(imageSize, imageSize, null, ImageAnchor.TopLeft);

        public static Image<Rgba32> ToImage(this IEnumerable<Rgba32> colors, Size imageSize, Size paddedSize) =>
            colors.ToImage(imageSize, paddedSize, null, ImageAnchor.TopLeft);

        public static Image<Rgba32> ToImage(this IEnumerable<Rgba32> colors, Size imageSize, IImageSwizzle swizzle) =>
            colors.ToImage(imageSize, imageSize, swizzle, ImageAnchor.TopLeft);

        /// <summary>
        /// Compose an image from a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to compose in the image.</param>
        /// <param name="imageSize">The dimensions of the composed image.</param>
        /// <param name="paddedSize">The padded dimensions of the composed image. Used for the swizzle operation. Is equal to <param name="imageSize">, if it was not further modified by the framework.</param></param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <param name="anchor">Defines where the image with its real size is anchored in the padded size.</param>
        /// <returns>The composed image.</returns>
        public static Image<Rgba32> ToImage(this IEnumerable<Rgba32> colors, Size imageSize, Size paddedSize, IImageSwizzle? swizzle, ImageAnchor anchor)
        {
            var image = new Image<Rgba32>(imageSize.Width, imageSize.Height);

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
                
                image[point.X, point.Y] = color;
            }

            return image;
        }

        #endregion

        #region ToColors

        public static IEnumerable<Rgba32> ToColors(this IEnumerable<int> indices, IList<Rgba32> palette) =>
            indices.Select(x => palette[x]);

        public static IEnumerable<Rgba32> ToColors(this Image<Rgba32> image) =>
            image.ToColors(image.Size, null, ImageAnchor.TopLeft);

        public static IEnumerable<Rgba32> ToColors(this Image<Rgba32> image, Size paddedSize) =>
            image.ToColors(paddedSize, null, ImageAnchor.TopLeft);

        public static IEnumerable<Rgba32> ToColors(this Image<Rgba32> image, IImageSwizzle swizzle) =>
            image.ToColors(image.Size, swizzle, ImageAnchor.TopLeft);

        public static IEnumerable<Rgba32> ToColors(this Image<Rgba32> image, Size paddedSize, ImageAnchor anchor) =>
            image.ToColors(paddedSize, null, anchor);

        /// <summary>
        /// Decomposes an image to a collection of colors.
        /// </summary>
        /// <param name="image">The image to decompose.</param>
        /// <param name="paddedSize">The padded dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the colors.</param>
        /// <param name="anchor">Defines where the image with its real size is anchored in the padded size.</param>
        /// <returns>The collection of colors.</returns>
        public static IEnumerable<Rgba32> ToColors(this Image<Rgba32> image, Size paddedSize, IImageSwizzle? swizzle, ImageAnchor anchor)
        {
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
                if (point.X >= image.Width || point.Y >= image.Height)
                {
                    yield return Color.Black;
                    continue;
                }

                // Otherwise return color from source image at the given point
                yield return image[point.X, point.Y];
            }
        }

        #endregion

        #region ToIndices

        public static IEnumerable<int> ToIndices(this Image<Rgba32> image, IList<Rgba32> palette) =>
            image.ToColors().ToIndices(palette);

        public static IEnumerable<int> ToIndices(this Image<Rgba32> image, IColorCache colorCache) =>
            image.ToColors().ToIndices(colorCache);

        public static IEnumerable<int> ToIndices(this IEnumerable<Rgba32> colors, IList<Rgba32> palette)
        {
            var foundColors = new Dictionary<Rgba32, int>();

            foreach (Rgba32 color in colors)
            {
                if (foundColors.TryGetValue(color, out int foundColor))
                {
                    yield return foundColor;
                    continue;
                }

                for (var i = 0; i < palette.Count; i++)
                {
                    if (palette[i] == color)
                    {
                        foundColors[palette[i]] = i;
                        yield return i;
                        break;
                    }
                }
            }
        }

        public static IEnumerable<int> ToIndices(this IEnumerable<Rgba32> colors, IColorCache colorCache) =>
            colors.Select(colorCache.GetPaletteIndex);

        #endregion

        /// <summary>
        /// Create a sequence of <see cref="Point"/>s.
        /// </summary>
        /// <param name="imageSize">The dimensions of the image.</param>
        /// <param name="swizzle">The <see cref="IImageSwizzle"/> to resort the points.</param>
        /// <returns>The sequence of <see cref="Point"/>s.</returns>
        internal static IEnumerable<Point> GetPointSequence(Size imageSize, IImageSwizzle? swizzle = null)
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

        private static Point GetMinPoint(int widthDiff, int heightDiff, ImageAnchor anchor)
        {
            return anchor switch
            {
                ImageAnchor.TopLeft => Point.Empty,
                ImageAnchor.TopRight => new Point(widthDiff, 0),
                ImageAnchor.BottomLeft => new Point(0, heightDiff),
                ImageAnchor.BottomRight => new Point(widthDiff, heightDiff),
                _ => throw new InvalidOperationException($"Unknown image anchor {anchor}.")
            };
        }

        private static Point GetMaxPoint(Size imageSize, int widthDiff, int heightDiff, ImageAnchor anchor)
        {
            return anchor switch
            {
                ImageAnchor.TopLeft => new Point(imageSize.Width - 1, imageSize.Height - 1),
                ImageAnchor.TopRight => new Point(imageSize.Width + widthDiff - 1, imageSize.Height - 1),
                ImageAnchor.BottomLeft => new Point(imageSize.Width - 1, imageSize.Height + heightDiff - 1),
                ImageAnchor.BottomRight => new Point(imageSize.Width + widthDiff - 1,
                    imageSize.Height + heightDiff - 1),
                _ => throw new InvalidOperationException($"Unknown image anchor {anchor}.")
            };
        }
    }
}
