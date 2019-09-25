using System;
using System.Drawing;
using Kanvas.Interface;
using Kanvas.Swizzle;

namespace plugin_kanvas_swizzles
{
    class CustomSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public CustomSwizzle(int width, int height, (int, int)[] bitField)
        {
            Width = width;
            Height = height;
            _swizzle = new MasterSwizzle(Math.Max(width, height), new Point(0, 0), bitField);
        }

        public Point Get(Point point)
        {
            return _swizzle.Get(point.Y * Width + point.X);
        }

        public int Width { get; }
        public int Height { get; }
    }
}
