using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Extensions;
using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Quantizers;
using Kontract;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas.Interfaces.Configuration;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kanvas.Configuration
{
    class QuantizationOptions : IQuantizationOptions, IQuantizer
    {
        private int _colorCount = 256;
        private int _taskCount = Environment.ProcessorCount;

        private CreateColorCacheDelegate _colorCacheFunc =
            palette => new EuclideanDistanceColorCache(palette);

        private CreateColorQuantizerDelegate _quantizerFunc =
            (colorCount, taskCount) => new WuColorQuantizer(6, 2, colorCount);

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

        public Image ProcessImage(Bitmap image, IProgressContext progress = null)
        {
            var (indices, palette) = Process(image.ToColors(), image.Size, progress);

            return indices.ToColors(palette).ToBitmap(image.Size);
        }

        public (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors, Size imageSize,
            IProgressContext progress = null)
        {
            var colorList = colors.ToList();

            var colorCache = GetColorCache(colorList, _taskCount);

            var setMaxProgress = progress?.SetMaxValue(colorList.Count);

            var colorDitherer = _dithererFunc?.Invoke(imageSize, _taskCount);
            var indices = colorDitherer == null ?
                colorList.ToIndices(colorCache) :
                colorDitherer.Process(colorList, colorCache);

            return (indices.AttachProgress(setMaxProgress, "Encode indices"), colorCache.Palette);
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
