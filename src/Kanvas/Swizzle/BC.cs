using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;

namespace Kanvas.Swizzle
{
    public class BCSwizzle : IImageSwizzle
    {
        private MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public BCSwizzle(int width, int height)
        {
            Width = (width + 3) & ~3;
            Height = (height + 3) & ~3;

            _swizzle = new MasterSwizzle(Width, new Point(0, 0), new[] { (1, 0), (2, 0), (0, 1), (0, 2) });
        }

        public Point Get(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
