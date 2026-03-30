using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Configuration;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Contract.Quantization.ColorQuantizer
{
    /// <summary>
    /// Describes methods to quantize a collection of colors.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Determines if the quantizer can only use a fixed color cache.
        /// </summary>
        bool IsColorCacheFixed { get; }

        /// <summary>
        /// Determines if the color count can be changed.
        /// </summary>
        bool UsesVariableColorCount { get; }

        /// <summary>
        /// Determines if alpha is supported for quantization.
        /// </summary>
        bool SupportsAlpha { get; }

        /// <summary>
        /// Creates a palette out of a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to quantize.</param>
        /// <returns>The palette.</returns>
        IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors);

        /// <summary>
        /// Creates a palette out of a collection of colors.
        /// </summary>
        /// <param name="colors">The colors to quantize.</param>
        /// <param name="initialPalette">A pre-determined set of colors guaranteed to be in the palette.</param>
        /// <returns>The palette.</returns>
        IList<Rgba32> CreatePalette(IEnumerable<Rgba32> colors, IList<Rgba32> initialPalette);

        /// <summary>
        /// Gets the fixed color cache for this quantizer.
        /// </summary>
        /// <param name="palette">The palette to store in the fixed color cache.</param>
        /// <returns>The fixed color cache for this quantizer.</returns>
        IColorCache GetFixedColorCache(IList<Rgba32> palette);

        /// <summary>
        /// Reorders a palette while keeping fixed colors in front.
        /// </summary>
        /// <param name="palette">The generated palette.</param>
        /// <param name="orderPaletteDelegate">Delegate used to reorder colors.</param>
        /// <param name="fixedColorCount">The amount of fixed colors at the start of the palette.</param>
        /// <returns>The reordered palette.</returns>
        IList<Rgba32> ReorderPalette(IList<Rgba32> palette, OrderPaletteDelegate? orderPaletteDelegate, int fixedColorCount)
        {
            if (orderPaletteDelegate is null || palette.Count <= fixedColorCount)
                return palette;

            List<Rgba32> fixedPalette = palette.Take(fixedColorCount).ToList();
            Rgba32[] dynamicPalette = palette.Skip(fixedColorCount).ToArray();

            fixedPalette.AddRange(orderPaletteDelegate(dynamicPalette));
            return fixedPalette;
        }
    }
}
