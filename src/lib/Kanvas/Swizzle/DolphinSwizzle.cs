using System.Drawing;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo GameCube.
    /// </summary>
    public class DolphinSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public DolphinSwizzle(SwizzlePreparationContext context)
        {
            _swizzle = new MasterSwizzle(context.Size.Width, new Point(0, 0), new[] { (1, 0), (2, 0), (0, 1), (0, 2) });
            (Width, Height) = ((context.Size.Width + 3) & ~3, (context.Size.Height + 3) & ~3);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
