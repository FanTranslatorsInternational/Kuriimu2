using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Swizzle
{
    public class VitaSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public VitaSwizzle(SwizzlePreparationContext context)
        {
            Width = (context.Size.Width + 3) & ~3;
            Height = (context.Size.Height + 3) & ~3;

            // TODO: To remove with prepend swizzle
            var isBlockEncoding = context.EncodingInfo.ColorsPerValue > 1;

            var bitField = new List<(int, int)>();
            var bitStart = isBlockEncoding ? 4 : 1;

            if (isBlockEncoding)
                bitField.AddRange(new List<(int, int)> { (1, 0), (2, 0), (0, 1), (0, 2) });

            for (var i = bitStart; i < Math.Min(context.Size.Width, context.Size.Height); i *= 2)
                bitField.AddRange(new List<(int, int)> { (0, i), (i, 0) });

            _swizzle = new MasterSwizzle(Width, Point.Empty, bitField.ToArray());
        }

        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
