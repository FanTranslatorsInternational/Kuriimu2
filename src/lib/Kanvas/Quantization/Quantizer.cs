using Kanvas.Contract.Configuration;
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

        private IColorCache GetColorCache(Rgba32[] colors)
        {
            IList<Rgba32> palette;

            if (options.PaletteDelegate != null)
            {
                // Retrieve and return the preset palette
                palette = options.PaletteDelegate();
                return options.ColorCacheDelegate(palette);
            }

            IColorQuantizer quantizer = CreateColorQuantizer(colors);

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

        private IColorQuantizer CreateColorQuantizer(IEnumerable<Rgba32> colors)
        {
            CreateColorQuantizerDelegate quantizerDelegate = options.ColorQuantizerDelegate ??
                                                             ResolveDynamicDefaultColorQuantizerDelegate(colors);

            return quantizerDelegate(options.ColorCount, options.TaskCount, options.ColorChannelBitDepths);
        }

        private static CreateColorQuantizerDelegate ResolveDynamicDefaultColorQuantizerDelegate(IEnumerable<Rgba32> colors)
        {
            // TODO: Dynamic default selection conditions (no switching implemented yet):
            // 1) If source alpha diversity is high (many distinct alpha levels), prefer Distinct Selection.
            // 2) If source contains mostly opaque pixels with low alpha variance, prefer Wu.
            // 3) If target palette encoding has very limited alpha depth (e.g. 1-2 bits), prefer Wu.
            // 4) If target palette encoding stores full alpha (e.g. 8-bit), prefer Distinct Selection.
            // 5) If source appears gradient-heavy in RGB channels, prefer Wu for smoother gradients.
            // 6) If source is mostly flat-color / UI-like art, prefer Distinct Selection.
            // 7) If quantization runtime is constrained and image is very large, prefer Wu.
            // 8) If deterministic color retention for sparse colors is prioritized, prefer Distinct Selection.
            return QuantizationConfigurationOptions.DefaultColorQuantizerDelegate;
        }
    }
}
