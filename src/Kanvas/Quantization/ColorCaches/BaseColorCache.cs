using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization.ColorCaches
{
    public abstract class BaseColorCache : IColorCache
    {
        protected abstract int CalculatePaletteIndex(Color color);
        protected abstract void OnPrepare();
        protected abstract void OnCachePalette();

        public IList<Color> Palette { get; private set; }

        public ColorModel ColorModel { get; private set; } = ColorModel.RGB;

        public int AlphaThreshold { get; private set; }

        public void Prepare(ColorModel model, int alphaThreshold = 0)
        {
            ColorModel = model;
            AlphaThreshold = alphaThreshold;
            OnPrepare();
        }

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
