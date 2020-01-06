using Kanvas.Quantization.Helper;
using System.Collections.Concurrent;
using System.Drawing;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    /// <inheritdoc cref="IColorCache"/>
    public class EuclideanDistanceColorCache : BaseColorCache
    {
        private ConcurrentDictionary<int, int> _cache;

        protected override void OnCachePalette()
        {
            _cache = new ConcurrentDictionary<int, int>();
        }

        /// <inheritdoc cref="BaseColorCache.CalculatePaletteIndex"/>
        protected override int CalculatePaletteIndex(Color color)
        {
            return _cache.AddOrUpdate(color.ToArgb(),
                colorKey =>
                {
                    int paletteIndexInside = CalculatePaletteIndexInternal(color);
                    return paletteIndexInside;
                },
                (colorKey, inputIndex) => inputIndex);
        }

        private int CalculatePaletteIndexInternal(Color color)
        {
            return ColorModelHelper.GetSmallestEuclideanDistanceIndex(ColorModel, color, Palette, AlphaThreshold);
        }
    }
}
