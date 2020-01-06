using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.Models.ColorCache;
using Kontract.Kanvas.Model;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    public abstract class BaseColorCache : IColorCache
    {
        protected abstract int CalculatePaletteIndex(Color color);
        protected abstract void OnCachePalette();

        public IList<Color> Palette { get; private set; }

        public ColorModel ColorModel { get; private set; } = ColorModel.RGB;

        public int AlphaThreshold { get; private set; }

        public void CachePalette(IList<Color> palette)
        {
            Palette = palette;
            OnCachePalette();
        }

        public int GetPaletteIndex(Color color)
        {
            if (Palette == null) throw new ArgumentNullException(nameof(Palette));
            if (!Palette.Any()) throw new InvalidOperationException("Cache is empty.");

            return CalculatePaletteIndex(color);
        }
    }
}
