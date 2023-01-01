using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas.Interfaces.Quantization
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
        /// Gets the index of the nearest color in the cache.
        /// </summary>
        /// <param name="color">The color to compare with.</param>
        /// <returns>Index of nearest color in the cache.</returns>
        int GetPaletteIndex(Color color);
    }
}
