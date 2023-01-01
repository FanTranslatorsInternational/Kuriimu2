using System.Drawing;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo DS.
    /// </summary>
    public class NitroSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public NitroSwizzle(SwizzlePreparationContext context)
        {
            _swizzle = new MasterSwizzle(context.Size.Width, new Point(0, 0), new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) });
            (Width, Height) = ((context.Size.Width + 7) & ~7, (context.Size.Height + 7) & ~7);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
