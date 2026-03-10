using Kanvas.Contract.Quantization;
using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorDitherer;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Kanvas.DataClasses.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization
{
    class Quantizer : IQuantizer
    {
        private readonly QuantizationConfigurationOptions _options;

        public Quantizer(QuantizationConfigurationOptions options)
        {
            _options = options;
        }

        public Image<Rgba32> ProcessImage(Image<Rgba32> image)
        {
            var (indices, palette) = Process(image.ToColors(), image.Size);

            return indices.ToColors(palette).ToImage(image.Size);
        }

        public (IEnumerable<int>, IList<Rgba32>) Process(IEnumerable<Rgba32> colors, Size imageSize)
        {
            Rgba32[] colorList = colors.ToArray();

            IColorCache colorCache = GetColorCache(colorList);
            IColorDitherer? colorDitherer = _options.ColorDithererDelegate?.Invoke(imageSize, _options.TaskCount);

            IEnumerable<int> indices = colorDitherer == null ?
                colorList.ToIndices(colorCache) :
                colorDitherer.Process(colorList, colorCache);

            return (indices, colorCache.Palette);
        }

        private IColorCache GetColorCache(IEnumerable<Rgba32> colors)
        {
            IList<Rgba32> palette;

            if (_options.PaletteDelegate != null)
            {
                // Retrieve and return the preset palette
                palette = _options.PaletteDelegate();
                return _options.ColorCacheDelegate(palette);
            }

            IColorQuantizer quantizer = _options.ColorQuantizerDelegate(_options.ColorCount, _options.TaskCount);

            if (_options.InitialPaletteDelegate != null)
            {
                // Create a new palette through quantization, primed by an initial set of colors
                var initialPalette = _options.InitialPaletteDelegate();
                palette = quantizer.CreatePalette(colors, initialPalette);

                // Order palette colors
                if (_options.OrderPaletteDelegate != null)
                {
                    IList<Rgba32> dynamicPalette = palette.Skip(initialPalette.Count).ToArray();
                    dynamicPalette = _options.OrderPaletteDelegate(dynamicPalette);

                    palette = initialPalette.Concat(dynamicPalette).ToArray();
                }
            }
            else
            {
                // Create a new palette through quantization
                palette = quantizer.CreatePalette(colors);

                // Order palette colors
                if (_options.OrderPaletteDelegate != null)
                    palette = _options.OrderPaletteDelegate(palette);
            }

            return quantizer.IsColorCacheFixed ?
                quantizer.GetFixedColorCache(palette) :
                _options.ColorCacheDelegate(palette);
        }
    }
}
