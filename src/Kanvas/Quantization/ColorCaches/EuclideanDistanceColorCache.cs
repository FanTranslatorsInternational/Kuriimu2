using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.ColorCaches
{
    /// <inheritdoc cref="IColorCache"/>
    public class EuclideanDistanceColorCache : IColorCache
    {
        private ColorModel _model;

        public IList<Color> Palette { get; private set; }

        /// <summary>
        /// Creates a color cache with euclidean distance comparison.
        /// </summary>
        /// <remarks>Uses color model RGB by default.</remarks>
        public EuclideanDistanceColorCache()
        {
            _model = ColorModel.RGB;
        }

        /// <summary>
        /// Creates a color cache with euclidean distance comparison.
        /// </summary>
        /// <param name="model">The color model to use in the comparison.</param>
        public EuclideanDistanceColorCache(ColorModel model)
        {
            _model = model;
        }

        /// <inheritdoc cref="IColorCache.CachePalette"/>
        public void CachePalette(IList<Color> palette)
        {
            Palette = palette;
        }

        /// <inheritdoc cref="IColorCache.GetPaletteIndex"/>
        public int GetPaletteIndex(Color color)
        {
            if (Palette == null) throw new ArgumentNullException(nameof(Palette));
            if (!Palette.Any()) throw new InvalidOperationException("Cache is empty.");

            long leastDistance = long.MaxValue;
            int result = 0;
            for (int i = 0; i < Palette.Count; i++)
            {
                var distance = ColorModelHelper.GetEuclideanDistance(_model, color, Palette[i]);
                if (distance == 0)
                    return i;

                if (distance < leastDistance)
                {
                    leastDistance = distance;
                    result = i;
                }
            }

            return result;
        }
    }
}
