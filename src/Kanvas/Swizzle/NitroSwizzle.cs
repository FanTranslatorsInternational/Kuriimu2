using System.Drawing;
using Kontract.Kanvas;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo DS.
    /// </summary>
    public class NitroSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <summary>
        /// Creates a new instance of <see cref="NitroSwizzle"/>
        /// </summary>
        /// <param name="width">The width of the image to swizzle.</param>
        /// <param name="height">The height of the image to swizzle.</param>
        public NitroSwizzle(int width, int height)
        {
            Width = width;
            Height = height;

            _swizzle = new MasterSwizzle(Width, new Point(0, 0), new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) });
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
