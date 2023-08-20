using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle for the NintendoW Wii, code named Revolution.
    /// </summary>
    public class RevolutionSwizzle : IImageSwizzle
    {
        private readonly IDictionary<int, (int, int)[]> _bitFields = new Dictionary<int, (int, int)[]>
        {
            [04] = new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) },
            [08] = new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2) },
            [16] = new[] { (1, 0), (2, 0), (0, 1), (0, 2) },
            [32] = new[] { (1, 0), (2, 0), (0, 1), (0, 2) }
        };

        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public RevolutionSwizzle(SwizzlePreparationContext context)
        {
            var widthStride = _bitFields[context.EncodingInfo.BitDepth].Sum(x => x.Item1);
            var heightStride = _bitFields[context.EncodingInfo.BitDepth].Sum(x => x.Item2);

            Width = (context.Size.Width + widthStride) & ~widthStride;
            Height = (context.Size.Height + heightStride) & ~heightStride;

            _swizzle = new MasterSwizzle(Width, Point.Empty, _bitFields[context.EncodingInfo.BitDepth]);
        }

        public Point Transform(Point point)
        {
            return _swizzle.Get(point.Y * Width + point.X);
        }
    }
}
