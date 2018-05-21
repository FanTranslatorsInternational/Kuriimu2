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
        byte _orientation;
        MasterSwizzle _zorder;

        public int Width { get; }
        public int Height { get; }

        public CTRSwizzle(int width, int height, byte orientation = 0, bool toPowerOf2 = true)
        {
            Width = (toPowerOf2) ? 2 << (int)Math.Log(width - 1, 2) : width;
            Height = (toPowerOf2) ? 2 << (int)Math.Log(height - 1, 2) : height;

            _orientation = orientation;
            _zorder = new MasterSwizzle(orientation == 0 ? Width : Height, new Point(0, 0), new[] { (1, 0), (0, 1), (2, 0), (0, 2), (4, 0), (0, 4) });
        }

        public Point Get(Point point)
        {
            int pointCount = point.Y * Width + point.X;
            var newPoint = _zorder.Get(pointCount);

            switch (_orientation)
            {
                //Transpose
                case 8: return new Point(newPoint.Y, newPoint.X);
                //Rotate90
                case 4: return new Point(newPoint.Y, Height - 1 - newPoint.X);
                default: return newPoint;
            }
        }
    }
}
