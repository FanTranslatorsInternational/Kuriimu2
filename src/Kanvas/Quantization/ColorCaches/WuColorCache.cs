using System.Collections.Generic;
using System.Drawing;
using Kanvas.Quantization.Models.Quantizer.Wu;
using Kontract.Kanvas.Model;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Quantization.ColorCaches
{
    class WuColorCache : IColorCache
    {
        private readonly int _indexBits;
        private readonly int _indexAlphaBits;

        internal byte[] Tag { get; set; }

        public IList<Color> Palette { get; private set; }

        public WuColorCache(int indexBits, int indexAlphaBits)
        {
            _indexBits = indexBits;
            _indexAlphaBits = indexAlphaBits;
        }

        public int GetPaletteIndex(Color color)
        {
            int a = color.A >> (8 - _indexAlphaBits);
            int r = color.R >> (8 - _indexBits);
            int g = color.G >> (8 - _indexBits);
            int b = color.B >> (8 - _indexBits);

            int index = WuCommon.GetIndex(r + 1, g + 1, b + 1, a + 1, _indexBits, _indexAlphaBits);

            return Tag[index];
        }

        internal void SetPalette(IList<Color> palette)
        {
            Palette = palette;
        }
    }
}
