using System.Collections.Generic;
using System.Drawing;
using Kontract;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    public abstract class BaseColorCache : IColorCache
    {
        /// <inheritdoc />
        public IList<Color> Palette { get; }

        public BaseColorCache(IList<Color> palette)
        {
            ContractAssertions.IsNotNull(palette,nameof(palette));

            Palette = palette;
        }

        /// <inheritdoc />
        public abstract int GetPaletteIndex(Color color);
    }
}
