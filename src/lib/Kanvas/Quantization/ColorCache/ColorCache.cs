using Kanvas.Contract.Quantization.ColorCache;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache
{
    public abstract class ColorCache(IList<Rgba32> palette) : IColorCache
    {
        /// <inheritdoc />
        public IList<Rgba32> Palette { get; } = palette;

        /// <inheritdoc />
        public abstract int GetPaletteIndex(Rgba32 color);
    }
}
