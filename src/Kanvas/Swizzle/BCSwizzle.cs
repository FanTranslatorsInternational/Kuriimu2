using System.Drawing;
using Kontract.Kanvas;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used for 4x4 block compressions.
    /// </summary>
    public class BCSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <summary>
        /// Creates a new instance of <see cref="BCSwizzle"/>.
        /// </summary>
        /// <param name="width">The width of the image to swizzle.</param>
        /// <param name="height">The height of the image to swizzle.</param>
        public BCSwizzle(int width, int height)
        {
            Width = (width + 3) & ~3;
            Height = (height + 3) & ~3;

            _swizzle = new MasterSwizzle(Width, new Point(0, 0), new[] { (1, 0), (2, 0), (0, 1), (0, 2) });
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
