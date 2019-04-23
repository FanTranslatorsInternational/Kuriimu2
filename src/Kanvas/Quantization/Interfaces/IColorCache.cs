using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Models;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to cache and manage a limited amount colors.
    /// </summary>
    public interface IColorCache
    {
        /// <summary>
        /// The cached palette.
        /// </summary>
        IList<Color> Palette { get; }

        /// <summary>
        /// Prepares the cache.
        /// </summary>
        /// <param name="model">The color model to use for index calculations.</param>
        void Prepare(ColorModel model);

        /// <summary>
        /// Caches a list of colors.
        /// </summary>
        /// <param name="palette">The palette to cache.</param>
        void CachePalette(IList<Color> palette);

        /// <summary>
        /// Gets the index of the nearest color in the cache.
        /// </summary>
        /// <param name="color">The color to compare with.</param>
        /// <returns>Index of nearest color in the cache.</returns>
        int GetPaletteIndex(Color color);
    }
}
