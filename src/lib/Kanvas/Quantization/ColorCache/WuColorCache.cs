using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Quantization.ColorQuantizer.Wu;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache
{
    internal class WuColorCache(ColorChannelBitDepths bitDepths) : IColorCache
    {
        private readonly int _indexRedBits = bitDepths.Red;
        private readonly int _indexGreenBits = bitDepths.Green;
        private readonly int _indexBlueBits = bitDepths.Blue;
        private readonly int _indexAlphaBits = bitDepths.Alpha;
        private readonly int _indexGreenCount = (1 << bitDepths.Green) + 1;
        private readonly int _indexBlueCount = (1 << bitDepths.Blue) + 1;
        private readonly int _indexAlphaCount = (1 << bitDepths.Alpha) + 1;

        internal byte[] Tag { get; set; } = [];

        public IList<Rgba32> Palette { get; private set; } = [];

        public int GetPaletteIndex(Rgba32 color)
        {
            int a = color.A >> (8 - _indexAlphaBits);
            int r = color.R >> (8 - _indexRedBits);
            int g = color.G >> (8 - _indexGreenBits);
            int b = color.B >> (8 - _indexBlueBits);

            int index = WuCommon.GetIndex(r + 1, g + 1, b + 1, a + 1, _indexGreenCount, _indexBlueCount, _indexAlphaCount);

            return Tag[index];
        }

        internal void SetPalette(IList<Rgba32> palette)
        {
            Palette = palette;
        }
    }
}
