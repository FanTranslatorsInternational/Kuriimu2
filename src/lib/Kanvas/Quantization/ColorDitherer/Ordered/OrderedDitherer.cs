using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorDitherer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorDitherer.Ordered
{
    public abstract class OrderedDitherer : IColorDitherer
    {
        private readonly Size _imageSize;
        private readonly int _taskCount;

        private readonly int _matrixWidth;
        private readonly int _matrixHeight;

        protected abstract byte[,] Matrix { get; }

        public OrderedDitherer(Size imageSize, int taskCount)
        {
            _imageSize = imageSize;
            _taskCount = taskCount;

            _matrixWidth = Matrix.GetLength(0);
            _matrixHeight = Matrix.GetLength(1);
        }

        public IEnumerable<int> Process(IEnumerable<Rgba32> colors, IColorCache colorCache)
        {
            return colors.Zip(Composition.GetPointSequence(_imageSize))
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(_taskCount)
                .Select(cp => DitherColor(cp, colorCache));
        }

        private int DitherColor((Rgba32, Point) colorPoint, IColorCache colorCache)
        {
            var threshold = GetThreshold(colorPoint.Item2);

            var red = (byte)Math.Clamp(colorPoint.Item1.R + threshold, 0, 255);
            var green = (byte)Math.Clamp(colorPoint.Item1.G + threshold, 0, 255);
            var blue = (byte)Math.Clamp(colorPoint.Item1.B + threshold, 0, 255);

            return colorCache.GetPaletteIndex(new Rgba32(red, green, blue, colorPoint.Item1.A));
        }

        private int GetThreshold(Point point)
        {
            var x = point.X % _matrixWidth;
            var y = point.Y % _matrixHeight;

            return Convert.ToInt32(Matrix[x, y]);
        }
    }
}
