using System.Drawing;
using Kanvas.Swizzle.Models;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo 3DS.
    /// </summary>
    public class CtrSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;
        private readonly CtrTransformation _transform;

        public int Width { get; }
        public int Height { get; }

        public CtrSwizzle(SwizzlePreparationContext context, CtrTransformation transform = CtrTransformation.None)
        {
            _transform = transform;

            var stride = _transform == CtrTransformation.None || _transform == CtrTransformation.YFlip ? context.Size.Width : context.Size.Height;
            _swizzle = new MasterSwizzle(stride, new Point(0, 0), new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) });

            (Width, Height) = ((context.Size.Width + 7) & ~7, (context.Size.Height + 7) & ~7);
        }

        /// <inheritdoc />
        public Point Transform(Point point)
        {
            int pointCount = point.Y * Width + point.X;
            var newPoint = _swizzle.Get(pointCount);

            switch (_transform)
            {
                // Transpose
                case CtrTransformation.Transpose:
                    return new Point(newPoint.Y, newPoint.X);

                // Rotate90
                case CtrTransformation.Rotate90:
                    return new Point(newPoint.Y, Height - 1 - newPoint.X);

                // YFlip
                case CtrTransformation.YFlip:
                    return new Point(newPoint.X, Height - 1 - newPoint.Y);

                default: return newPoint;
            }
        }
    }
}
