using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.ColorCaches
{
    /// <inheritdoc cref="IColorCache"/>
    public class EuclideanDistanceColorCache : BaseColorCache
    {
        private ConcurrentDictionary<int, int> _cache;

        protected override void OnPrepare()
        {
        }

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
            return ColorModelHelper.GetSmallestEuclideanDistanceIndex(_colorModel, color, Palette);
        }
    }
}
