﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    /// <summary>
    /// The <see cref="IColorCache"/> to search colors with euclidean distance.
    /// </summary>
    public class EuclideanDistanceColorCache : BaseColorCache
    {
        private readonly ConcurrentDictionary<int, int> _cache;

        public EuclideanDistanceColorCache(IList<Color> palette) :
            base(palette)
        {
            _cache = new ConcurrentDictionary<int, int>();
        }

        /// <inheritdoc />
        public override int GetPaletteIndex(Color color)
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
            return EuclideanHelper.GetSmallestEuclideanDistanceIndex(Palette, color);
        }
    }
}
