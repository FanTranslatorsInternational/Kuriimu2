using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public abstract class BaseOrderedDitherer : IColorDitherer
    {
        private readonly Size _imageSize;
        private readonly int _taskCount;

        private readonly int _matrixWidth;
        private readonly int _matrixHeight;

        protected abstract byte[,] Matrix { get; }

        public BaseOrderedDitherer(Size imageSize, int taskCount)
        {
            _imageSize = imageSize;
            _taskCount = taskCount;

            _matrixWidth = Matrix.GetLength(0);
            _matrixHeight = Matrix.GetLength(1);
        }

        public IEnumerable<int> Process(IEnumerable<Color> colors, IColorCache colorCache)
        {
            return colors.Zip(Composition.GetPointSequence(_imageSize))
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(_taskCount)
                .Select(cp => DitherColor(cp, colorCache));
        }

        private int DitherColor((Color, Point) colorPoint, IColorCache colorCache)
        {
            var threshold = GetThreshold(colorPoint.Item2);

            var red = Math.Clamp(colorPoint.Item1.R + threshold, 0, 255);
            var green = Math.Clamp(colorPoint.Item1.G + threshold, 0, 255);
            var blue = Math.Clamp(colorPoint.Item1.B + threshold, 0, 255);

            return colorCache.GetPaletteIndex(Color.FromArgb(colorPoint.Item1.A, red, green, blue));
        }

        private int GetThreshold(Point point)
        {
            var x = point.X % _matrixWidth;
            var y = point.Y % _matrixHeight;

            return Convert.ToInt32(Matrix[x, y]);
        }
    }
}
