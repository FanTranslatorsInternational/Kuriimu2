using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Models.ColorCache;
using Kontract;
using Kontract.Kanvas.Model;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    public abstract class BaseColorCache : IColorCache
    {
        /// <inheritdoc />
        public IList<Color> Palette { get; private set; }

        public BaseColorCache(IList<Color> palette)
        {
            ContractAssertions.IsNotNull(palette,nameof(palette));

            Palette = palette;
        }

        /// <inheritdoc />
        public int GetPaletteIndex(Color color)
        {
            return OnGetPaletteIndex(color);
        }

        protected abstract int OnGetPaletteIndex(Color color);
    }
}
