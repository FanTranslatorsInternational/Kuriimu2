using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Quantizers;
using Kontract;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class QuantizationOptions : IQuantizationOptions, IQuantizer
    {
        private int _colorCount = 256;
        private int _taskCount = Environment.ProcessorCount;

        private CreateColorCacheDelegate _colorCacheFunc =
            palette => new EuclideanDistanceColorCache(palette);

        private CreateColorQuantizerDelegate _quantizerFunc =
            (colorCount, taskCount) => new WuColorQuantizer(4, 4, colorCount);

        private CreatePaletteDelegate _paletteFunc;

        private CreateColorDithererDelegate _dithererFunc;

        public IQuantizationOptions WithColorCount(int colorCount)
        {
            if (colorCount <= 0)
                throw new InvalidOperationException("Color count has to be greater than 0.");

            _colorCount = colorCount;

            return this;
        }

        public IQuantizationOptions WithColorCache(CreateColorCacheDelegate func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));
            _colorCacheFunc = func;

            return this;
        }

        public IQuantizationOptions WithPalette(CreatePaletteDelegate func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));
            _paletteFunc = func;

            return this;
        }

        public IQuantizationOptions WithColorQuantizer(CreateColorQuantizerDelegate func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));
            _quantizerFunc = func;

            return this;
        }

        public IQuantizationOptions WithColorDitherer(CreateColorDithererDelegate func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));
            _dithererFunc = func;

            return this;
        }

        public IQuantizationOptions WithTaskCount(int taskCount)
        {
            if (taskCount <= 0)
                throw new InvalidOperationException("Task count has to be greater than 0.");

            _taskCount = taskCount;

            return this;
        }

        public (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors, Size imageSize)
        {
            var colorList = colors.ToList();

            var colorCache = GetColorCache(colorList, _taskCount);

            var colorDitherer = _dithererFunc?.Invoke(imageSize, _taskCount);
            var indices = colorDitherer?.Process(colorList, colorCache) ??
                          Composition.ComposeIndices(colorList, colorCache, _taskCount);

            return (indices, colorCache.Palette);
        }

        public Image ProcessImage(Bitmap image)
        {
            var colors = Composition.DecomposeImage(image, Size.Empty);

            var (indices, palette) = Process(colors, image.Size);
            var newColors = indices.Select(i => palette[i]);

            return Composition.ComposeImage(newColors, image.Size);
        }

        private IColorCache GetColorCache(IEnumerable<Color> colors, int taskCount)
        {
            // Create a palette for the input colors
            if (_paletteFunc != null)
            {
                // Retrieve the preset palette
                var palette = _paletteFunc.Invoke();

                return _colorCacheFunc(palette);
            }
            else
            {
                // Create a new palette through quantization
                var quantizer = _quantizerFunc(_colorCount, taskCount);
                var palette = quantizer.CreatePalette(colors);

                return quantizer.IsColorCacheFixed ?
                    quantizer.GetFixedColorCache(palette) :
                    _colorCacheFunc(palette);
            }
        }
    }
}
