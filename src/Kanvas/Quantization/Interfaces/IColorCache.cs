using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to cache and manage a limited amount colors.
    /// </summary>
    public interface IColorCache
    {
        IList<Color> Palette { get; }

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

        /// <summary>
        /// Resets this instance to a new state.
        /// </summary>
        void Reset();
    }
}
