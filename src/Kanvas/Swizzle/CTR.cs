using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;

namespace Kanvas.Swizzle
{
    public class CTRSwizzle : IImageSwizzle
    {
        public enum Transformation : byte
        {
            None = 0,
            YFlip = 2,
            Rotate90 = 4,
            Transpose = 8
        }

        Transformation _transform;
        MasterSwizzle _zorder;

        public int Width { get; }
        public int Height { get; }

        public CTRSwizzle(int width, int height, Transformation transform = Transformation.None, bool toPowerOf2 = true)
        {
            Width = (toPowerOf2) ? 2 << (int)Math.Log(width - 1, 2) : width;
            Height = (toPowerOf2) ? 2 << (int)Math.Log(height - 1, 2) : height;

            _transform = transform;
            //"transform == Transformation.None ? Width : Height" before; Based on some changes to G1T in Kuriimu1; may be subject to change later
            _zorder = new MasterSwizzle(transform == Transformation.None ? Width : Width, new Point(0, 0), new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) });
        }

        public Point Get(Point point)
        {
            int pointCount = point.Y * Width + point.X;
            var newPoint = _zorder.Get(pointCount);

            switch (_transform)
            {
                //Transpose
                case Transformation.Transpose: return new Point(newPoint.Y, newPoint.X);
                //Rotate90
                case Transformation.Rotate90: return new Point(newPoint.Y, Height - 1 - newPoint.X);
                //Y Flip (named by Neo :P)
                case Transformation.YFlip: return new Point(newPoint.X, Height - 1 - newPoint.Y);
                default: return newPoint;
            }
        }
    }
}
