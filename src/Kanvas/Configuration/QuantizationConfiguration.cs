using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    public class QuantizationConfiguration : IQuantizationConfiguration
    {
        private Size _imageSize;
        private int _taskCount = Environment.ProcessorCount;
        private int _colorCount = 256;

        private Func<int, int, IColorQuantizer> _quantizerFunc;
        private Func<Size, int, IColorDitherer> _dithererFunc;

        private Func<IList<Color>, IColorCache> _cacheFunc;
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

        public IQuantizationConfiguration WithColorCount(int colorCount)
        {
            _colorCount = colorCount;
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

        public IQuantizationConfiguration WithColorQuantizer(Func<int, int, IColorQuantizer> func)
        {
            _quantizerFunc = func;
            return this;
        }

        public IQuantizationConfiguration WithColorDitherer(Func<Size, int, IColorDitherer> func)
        {
            _dithererFunc = func;
            return this;
        }

        public IQuantizer Build()
        {
            if (_imageSize == Size.Empty)
                throw new ArgumentException("imageSize");

            var colorQuantizer = _quantizerFunc?.Invoke(_colorCount, _taskCount);
            var colorDitherer = _dithererFunc?.Invoke(_imageSize, _taskCount);

            return new Quantizer(colorQuantizer, colorDitherer, _taskCount, _paletteFunc, _cacheFunc);
        }
    }
}
