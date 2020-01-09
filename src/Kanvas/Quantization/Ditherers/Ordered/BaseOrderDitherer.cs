using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public abstract class BaseOrderDitherer : IColorDitherer
    {
        private readonly Size _imageSize;
        private readonly int _taskCount;

        private readonly int _matrixWidth;
        private readonly int _matrixHeight;

        protected abstract byte[,] Matrix { get; }

        public BaseOrderDitherer(Size imageSize, int taskCount)
        {
            _imageSize = imageSize;
            _taskCount = taskCount;

            _matrixWidth = Matrix.GetLength(0);
            _matrixHeight = Matrix.GetLength(1);
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors, IColorCache colorCache)
        {
            return Zip(colors, Composition.GetPointSequence(_imageSize, Size.Empty))
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(_taskCount)
                .Select(cp => DitherColor(cp, colorCache));
        }

        private int DitherColor((Color, Point) colorPoint, IColorCache colorCache)
        {
            var threshold = GetThreshold(colorPoint.Item2);

            var red = Clamp(colorPoint.Item1.R + threshold, 0, 255);
            var green = Clamp(colorPoint.Item1.G + threshold, 0, 255);
            var blue = Clamp(colorPoint.Item1.B + threshold, 0, 255);

            return colorCache.GetPaletteIndex(Color.FromArgb(colorPoint.Item1.A, red, green, blue));
        }

        private int GetThreshold(Point point)
        {
            var x = point.X % _matrixWidth;
            var y = point.Y % _matrixHeight;

            return Convert.ToInt32(Matrix[x, y]);
        }

        // TODO: Remove when targeting only netcoreapp31
        private IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
#if NET_CORE_31
            return first.Zip(second);
#else
            return first.Zip(second, (f, s) => (f, s));
#endif
        }

        // TODO: Remove when targeting only netcoreapp31
        private static int Clamp(int value, int min, int max)
        {
#if NET_CORE_31
            return Math.Clamp(value, min, max);
#else
            return Math.Max(min, Math.Min(value, max));
#endif
        }
    }
}
