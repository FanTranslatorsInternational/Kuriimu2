using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Quantization.ColorCaches;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class QuantizationConfiguration : IQuantizationConfiguration
    {
        private Size _imageSize;
        private int _taskCount = Environment.ProcessorCount;

        private Func<IList<Color>, IColorCache> _cacheFunc;
        private Func<IColorQuantizer> _quantizerFunc;
        private Func<Size, IColorDitherer> _dithererFunc;
        private Func<IList<Color>> _paletteFunc;

        public IQuantizationConfiguration WithImageSize(Size size)
        {
            _imageSize = size;
            return this;
        }

        public IQuantizationConfiguration WithTaskCount(int taskCount)
        {
            _taskCount = taskCount;
            return this;
        }

        public IQuantizationConfiguration WithColorCache(Func<IList<Color>, IColorCache> func)
        {
            _cacheFunc = func;
            return this;
        }

        public IQuantizationConfiguration WithPalette(Func<IList<Color>> func)
        {
            _paletteFunc = func;
            return this;
        }

        public IQuantizationConfiguration WithColorQuantizer(Func<IColorQuantizer> func)
        {
            _quantizerFunc = func;
            return this;
        }

        public IQuantizationConfiguration WithColorDitherer(Func<Size, IColorDitherer> func)
        {
            _dithererFunc = func;
            return this;
        }

        public IQuantizer Build()
        {
            if (_imageSize == Size.Empty)
                throw new ArgumentException("imageSize");

            var palette = _paletteFunc?.Invoke();
            var colorCache = palette != null
                ? _cacheFunc?.Invoke(palette) ?? new EuclideanDistanceColorCache(palette)
                : null;

            var colorQuantizer = _quantizerFunc?.Invoke();
            var colorDitherer = _dithererFunc?.Invoke(_imageSize);

            if (colorQuantizer != null)
                colorQuantizer.TaskCount = _taskCount;
            if (colorDitherer != null)
                colorDitherer.TaskCount = _taskCount;

            return new Quantizer(colorQuantizer, colorDitherer, colorCache);
        }
    }
}
