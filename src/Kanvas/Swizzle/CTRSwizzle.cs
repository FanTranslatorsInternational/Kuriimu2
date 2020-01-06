using System;
using System.Drawing;
using Kanvas.Swizzle.Models;
using Kontract.Kanvas;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo 3DS.
    /// </summary>
    public class CTRSwizzle : IImageSwizzle
    {
        private readonly CtrTransformation _transform;
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CTRSwizzle"/>.
        /// </summary>
        /// <param name="width">The width of the image to swizzle.</param>
        /// <param name="height">The height of the image to swizzle.</param>
        /// <param name="transform">The transformation mode for this swizzle.</param>
        /// <param name="toPowerOfTwo">Should the dimensions be padded to a power of 2.</param>
        public CTRSwizzle(int width, int height, CtrTransformation transform, bool toPowerOfTwo)
        {
            Width = (toPowerOfTwo) ? 2 << (int)Math.Log(width - 1, 2) : width;
            Height = (toPowerOfTwo) ? 2 << (int)Math.Log(height - 1, 2) : height;

            _transform = transform;
            var stride = transform == CtrTransformation.None || transform == CtrTransformation.YFlip ? Width : Height;
            _swizzle = new MasterSwizzle(stride, new Point(0, 0), new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) });
        }

        /// <inheritdoc />
        public Point Transform(Point point)
        {
            int pointCount = point.Y * Width + point.X;
            var newPoint = _swizzle.Get(pointCount);

            switch (_transform)
            {
                // Transpose
                case CtrTransformation.Transpose: return new Point(newPoint.Y, newPoint.X);
                // Rotate90
                case CtrTransformation.Rotate90: return new Point(newPoint.Y, Height - 1 - newPoint.X);
                // YFlip
                case CtrTransformation.YFlip: return new Point(newPoint.X, Height - 1 - newPoint.Y);
                default: return newPoint;
            }
        }
    }
}
