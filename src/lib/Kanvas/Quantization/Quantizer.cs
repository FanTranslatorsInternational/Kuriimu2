using Kanvas.Contract.Quantization;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorDitherer;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.DataClasses.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization
{
    internal class Quantizer(QuantizationConfigurationOptions options) : IQuantizer
    {
        public Image<Rgba32> ProcessImage(Image<Rgba32> image)
        {
            var (indices, palette) = Process(image.ToColors(), image.Size);

            return indices.ToColors(palette).ToImage(image.Size);
        }

        public (IEnumerable<int>, IList<Rgba32>) Process(IEnumerable<Rgba32> colors, Size imageSize)
        {
            Rgba32[] colorList = [.. colors];

            IColorCache colorCache = GetColorCache(colorList);
            IColorDitherer? colorDitherer = options.ColorDithererDelegate?.Invoke(imageSize, options.TaskCount);

            IEnumerable<int> indices = colorDitherer == null ?
                colorList.ToIndices(colorCache) :
                colorDitherer.Process(colorList, colorCache);

            return (indices, colorCache.Palette);
        }

        private IColorCache GetColorCache(IEnumerable<Rgba32> colors)
        {
            IList<Rgba32> palette;

            if (options.PaletteDelegate != null)
            {
                // Retrieve and return the preset palette
                palette = options.PaletteDelegate();
                return options.ColorCacheDelegate(palette);
            }

            IColorQuantizer quantizer = options.ColorQuantizerDelegate(options.ColorCount, options.TaskCount, options.ColorChannelBitDepths);

            var fixedColorCount = 0;
            if (options.InitialPaletteDelegate != null)
            {
                // Create a new palette through quantization, primed by an initial set of colors
                IList<Rgba32> initialPalette = options.InitialPaletteDelegate();
                palette = quantizer.CreatePalette(colors, initialPalette);

                fixedColorCount = initialPalette.Count;
            }
            else
            {
                // Create a new palette through quantization
                palette = quantizer.CreatePalette(colors);
            }

            // Order palette colors
            palette = quantizer.ReorderPalette(palette, options.OrderPaletteDelegate, fixedColorCount);

            // Get color cache based on palette
            return quantizer.IsColorCacheFixed ?
                quantizer.GetFixedColorCache(palette) :
                options.ColorCacheDelegate(palette);
        }
    }
}
