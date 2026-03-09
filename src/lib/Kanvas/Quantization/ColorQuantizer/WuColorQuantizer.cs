using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.Quantization.ColorCache;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

        public WuColorQuantizer(int indexBits, int indexAlphaBits, int colorCount)
        {
            _colorCache = new WuColorCache(indexBits, indexAlphaBits);
            _histogram = new Wu.Wu3DHistogram(indexBits, indexAlphaBits, (1 << indexBits) + 1, (1 << indexAlphaBits) + 1);

            var tableLength = _histogram.IndexCount * _histogram.IndexCount * _histogram.IndexCount * _histogram.IndexAlphaCount;
            _colorCache.Tag = new byte[tableLength];

            _colorCount = colorCount;
        }

        /// <inheritdoc />
        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors)
        {
            return CreatePalette(colors, []);
        }

        public IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors, IList<Rgba32> initialPalette)
        {
            var fixedPalette = NormalizeInitialPalette(initialPalette, _colorCount);
            
            int remainingColorCount = _colorCount - fixedPalette.Count;
            if (remainingColorCount <= 0)
                return fixedPalette;

            Array.Clear(_colorCache.Tag, 0, _colorCache.Tag.Length);

            // Step 1: Build a 3-dimensional histogram of all colors and calculate their moments
            _histogram.Create(colors.ToList(), new HashSet<uint>(fixedPalette.Select(color => color.PackedValue)));

            // Step 2: Create color cube
            var cube = Wu.WuColorCube.Create(_histogram, remainingColorCount);

            // Step 3: Create palette from color cube
            fixedPalette.AddRange(CreatePalette(cube, fixedPalette.Count));
            return fixedPalette;
        }

        /// <inheritdoc />
        public IColorCache GetFixedColorCache(IList<Rgba32> palette)
        {
            _colorCache.SetPalette(palette);
            return _colorCache;
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

            return initialPalette
                .DistinctBy(color => color.PackedValue)
                .Take(maxColorCount)
                .ToList();
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
                            _colorCache.Tag[Wu.WuCommon.GetIndex(r, g, b, a, _histogram.IndexBits, _histogram.IndexAlphaBits)] = label;
                        }
                    }
                }
            }
        }
    }
}
