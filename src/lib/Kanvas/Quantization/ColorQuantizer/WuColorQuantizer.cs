using Kanvas.Contract.Configuration;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.Quantization.ColorCache;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Kanvas.Contract.DataClasses;

namespace Kanvas.Quantization.ColorQuantizer
{
    public class WuColorQuantizer : IColorQuantizer
    {
        private readonly int _colorCount;

        private readonly WuColorCache _colorCache;
        private readonly Wu.Wu3DHistogram _histogram;

        /// <inheritdoc />
        public bool IsColorCacheFixed => true;

        /// <inheritdoc />
        public bool UsesVariableColorCount => true;

        /// <inheritdoc />
        public bool SupportsAlpha => true;

        public WuColorQuantizer(ColorChannelBitDepths bitDepths, int colorCount)
        {
            ColorChannelBitDepths normalizedBitDepths = NormalizeBitDepths(bitDepths);

            _colorCache = new WuColorCache(normalizedBitDepths);
            _histogram = new Wu.Wu3DHistogram(normalizedBitDepths);

            var tableLength = _histogram.IndexRedCount * _histogram.IndexGreenCount * _histogram.IndexBlueCount * _histogram.IndexAlphaCount;
            _colorCache.Tag = new byte[tableLength];

            _colorCount = colorCount;
        }

        private static ColorChannelBitDepths NormalizeBitDepths(ColorChannelBitDepths bitDepths)
        {
            // Histogram indexing is based on 8-bit RGBA samples, so higher bit depths provide no extra precision.
            int redBits = Math.Clamp(bitDepths.Red, 1, 6);
            int greenBits = Math.Clamp(bitDepths.Green, 1, 6);
            int blueBits = Math.Clamp(bitDepths.Blue, 1, 6);
            int alphaBits = Math.Clamp(bitDepths.Alpha, 1, 2);

            return new ColorChannelBitDepths(redBits, greenBits, blueBits, alphaBits);
        }

        /// <inheritdoc />
        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors)
        {
            return CreatePalette(colors, []);
        }

        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var fixedPalette = NormalizeInitialPalette(initialPalette, _colorCount);

            Array.Clear(_colorCache.Tag, 0, _colorCache.Tag.Length);

            int remainingColorCount = _colorCount - fixedPalette.Count;
            if (remainingColorCount <= 0)
            {
                FillFixedOnlyTagTable(fixedPalette);
                return fixedPalette;
            }

            // Step 1: Build a 3-dimensional histogram of all non-fixed colors and calculate moments.
            //         Apply a small synthetic bias around the initial palette to guide cube cuts.
            _histogram.Create([.. colors], fixedPalette);

            // Step 2: Create color cube
            var cube = Wu.WuColorCube.Create(_histogram, remainingColorCount);

            // Step 3: Create palette from color cube
            var dynamicPalette = CreatePalette(cube, fixedPalette.Count).ToArray();
            MarkFixedPaletteBins(fixedPalette, fixedPalette.Count);

            fixedPalette.AddRange(dynamicPalette);

            return fixedPalette;
        }

        /// <inheritdoc />
        public IColorCache GetFixedColorCache(IList<Rgba32> palette)
        {
            _colorCache.SetPalette(palette);
            return _colorCache;
        }

        /// <inheritdoc />
        public IList<Rgba32> ReorderPalette(IList<Rgba32> palette, OrderPaletteDelegate? orderPaletteDelegate, int fixedColorCount)
        {
            if (orderPaletteDelegate is null || palette.Count <= fixedColorCount)
                return palette;

            List<Rgba32> fixedPalette = [.. palette.Take(fixedColorCount)];
            Rgba32[] dynamicPalette = [.. palette.Skip(fixedColorCount)];

            IList<Rgba32> orderedPalette = orderPaletteDelegate(dynamicPalette);

            fixedPalette.AddRange(orderedPalette);
            RemapCacheTagTable(dynamicPalette, orderedPalette, fixedColorCount);

            return fixedPalette;
        }

        private IEnumerable<Rgba32> CreatePalette(Wu.WuColorCube cube, int paletteOffset)
        {
            for (int k = 0; k < cube.ColorCount; k++)
            {
                var box = cube.Boxes[k];
                Mark(box, (byte)(k + paletteOffset));

                var weight = box.GetPartialVolume(5);
                yield return weight == 0 ?
                    Color.Black :
                    new Rgba32(
                        (byte)(box.GetPartialVolume(1) / weight),
                        (byte)(box.GetPartialVolume(2) / weight),
                        (byte)(box.GetPartialVolume(3) / weight),
                        (byte)(box.GetPartialVolume(4) / weight));
            }
        }

        private static List<Rgba32> NormalizeInitialPalette(IList<Rgba32> initialPalette, int maxColorCount)
        {
            if (maxColorCount <= 0 || initialPalette.Count <= 0)
                return [];

            return [.. initialPalette.DistinctBy(color => color.PackedValue).Take(maxColorCount)];
        }

        private void FillFixedOnlyTagTable(List<Rgba32> fixedPalette)
        {
            if (fixedPalette.Count <= 0)
                return;

            int maxRedIndex = _histogram.IndexRedCount - 1;
            int maxGreenIndex = _histogram.IndexGreenCount - 1;
            int maxBlueIndex = _histogram.IndexBlueCount - 1;
            int maxAlphaIndex = _histogram.IndexAlphaCount - 1;
            for (int r = 1; r <= maxRedIndex; r++)
            {
                int red = GetBinCenterValue(r - 1, _histogram.IndexRedBits);

                for (int g = 1; g <= maxGreenIndex; g++)
                {
                    int green = GetBinCenterValue(g - 1, _histogram.IndexGreenBits);

                    for (int b = 1; b <= maxBlueIndex; b++)
                    {
                        int blue = GetBinCenterValue(b - 1, _histogram.IndexBlueBits);

                        for (int a = 1; a <= maxAlphaIndex; a++)
                        {
                            int alpha = GetBinCenterValue(a - 1, _histogram.IndexAlphaBits);
                            int closestIndex = FindClosestPaletteIndex(red, green, blue, alpha, fixedPalette);

                            _colorCache.Tag[Wu.WuCommon.GetIndex(r, g, b, a, _histogram.IndexGreenCount, _histogram.IndexBlueCount, _histogram.IndexAlphaCount)] = (byte)closestIndex;
                        }
                    }
                }
            }
        }

        private void MarkFixedPaletteBins(List<Rgba32> palette, int fixedCount)
        {
            if (fixedCount <= 0)
                return;

            for (int i = 0; i < fixedCount; i++)
            {
                var color = palette[i];

                int r = (color.R >> (8 - _histogram.IndexRedBits)) + 1;
                int g = (color.G >> (8 - _histogram.IndexGreenBits)) + 1;
                int b = (color.B >> (8 - _histogram.IndexBlueBits)) + 1;
                int a = (color.A >> (8 - _histogram.IndexAlphaBits)) + 1;

                var tagIndex = Wu.WuCommon.GetIndex(r, g, b, a, _histogram.IndexGreenCount, _histogram.IndexBlueCount, _histogram.IndexAlphaCount);
                _colorCache.Tag[tagIndex] = (byte)i;
            }
        }

        private static int FindClosestPaletteIndex(int r, int g, int b, int a, List<Rgba32> palette)
        {
            int bestIndex = 0;
            long bestDistance = long.MaxValue;

            for (int i = 0; i < palette.Count; i++)
            {
                var color = palette[i];
                long dr = r - color.R;
                long dg = g - color.G;
                long db = b - color.B;
                long da = a - color.A;
                long distance = dr * dr + dg * dg + db * db + da * da;

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        private static int GetBinCenterValue(int index, int bits)
        {
            int bucketSize = 1 << (8 - bits);
            int value = index * bucketSize + (bucketSize >> 1);
            return Math.Clamp(value, 0, 255);
        }

        private void RemapCacheTagTable(Rgba32[] sourcePalette, IList<Rgba32> orderedPalette, int fixedColorCount)
        {
            if (sourcePalette.Length != orderedPalette.Count)
                throw new InvalidOperationException("Ordered palette size must match source palette size.");

            var sourceIndexByColor = new Dictionary<uint, Queue<int>>(sourcePalette.Length);
            for (int sourceIndex = 0; sourceIndex < sourcePalette.Length; sourceIndex++)
            {
                uint colorKey = sourcePalette[sourceIndex].PackedValue;
                if (!sourceIndexByColor.TryGetValue(colorKey, out Queue<int>? sourceIndices))
                {
                    sourceIndices = new Queue<int>();
                    sourceIndexByColor[colorKey] = sourceIndices;
                }

                sourceIndices.Enqueue(sourceIndex);
            }

            var remapTable = new byte[sourcePalette.Length];
            for (int orderedIndex = 0; orderedIndex < orderedPalette.Count; orderedIndex++)
            {
                uint colorKey = orderedPalette[orderedIndex].PackedValue;
                if (!sourceIndexByColor.TryGetValue(colorKey, out Queue<int>? sourceIndices) || sourceIndices.Count == 0)
                    throw new InvalidOperationException("Ordered palette must contain the same colors as source palette.");

                remapTable[sourceIndices.Dequeue()] = (byte)(fixedColorCount + orderedIndex);
            }

            for (int tagIndex = 0; tagIndex < _colorCache.Tag.Length; tagIndex++)
            {
                var remapIndex = _colorCache.Tag[tagIndex];

                if (remapIndex >= fixedColorCount)
                    _colorCache.Tag[tagIndex] = remapTable[remapIndex - fixedColorCount];
            }
        }

        private void Mark(Wu.WuColorBox box, byte label)
        {
            for (int r = box.R0 + 1; r <= box.R1; r++)
            {
                for (int g = box.G0 + 1; g <= box.G1; g++)
                {
                    for (int b = box.B0 + 1; b <= box.B1; b++)
                    {
                        for (int a = box.A0 + 1; a <= box.A1; a++)
                        {
                            _colorCache.Tag[Wu.WuCommon.GetIndex(r, g, b, a, _histogram.IndexGreenCount, _histogram.IndexBlueCount, _histogram.IndexAlphaCount)] = label;
                        }
                    }
                }
            }
        }
    }
}
