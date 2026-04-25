using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Quantization.ColorQuantizer.Wu;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache
{
    class WuColorCache : IColorCache
    {
        private readonly int _indexRedBits;
        private readonly int _indexGreenBits;
        private readonly int _indexBlueBits;
        private readonly int _indexAlphaBits;
        private readonly int _indexRedCount;
        private readonly int _indexGreenCount;
        private readonly int _indexBlueCount;
        private readonly int _indexAlphaCount;

        internal byte[] Tag { get; set; }

        public IList<Rgba32> Palette { get; private set; }

        public WuColorCache(ColorChannelBitDepths bitDepths)
        {
            _indexRedBits = bitDepths.Red;
            _indexGreenBits = bitDepths.Green;
            _indexBlueBits = bitDepths.Blue;
            _indexAlphaBits = bitDepths.Alpha;

            _indexRedCount = (1 << bitDepths.Red) + 1;
            _indexGreenCount = (1 << bitDepths.Green) + 1;
            _indexBlueCount = (1 << bitDepths.Blue) + 1;
            _indexAlphaCount = (1 << bitDepths.Alpha) + 1;
        }

        public int GetPaletteIndex(Rgba32 color)
        {
            int a = color.A >> (8 - _indexAlphaBits);
            int r = color.R >> (8 - _indexRedBits);
            int g = color.G >> (8 - _indexGreenBits);
            int b = color.B >> (8 - _indexBlueBits);

            int index = WuCommon.GetIndex(r + 1, g + 1, b + 1, a + 1, _indexRedCount, _indexGreenCount, _indexBlueCount, _indexAlphaCount);

            return Tag[index];
        }

        internal void SetPalette(IList<Rgba32> palette)
        {
            Palette = palette;
        }
    }
}
