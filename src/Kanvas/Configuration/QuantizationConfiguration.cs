using System;
using System.Drawing;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class QuantizationConfiguration : IQuantizationConfiguration
    {
        private Size _imageSize;
        private int _taskCount = Environment.ProcessorCount;

        private Func<IColorCache> _cacheFunc;
        private Func<IColorCache, IColorQuantizer> _quantizerFunc;
        private Func<Size, IColorDitherer> _dithererFunc;

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

        public IQuantizationConfiguration WithColorCache(Func<IColorCache> func)
        {
            _cacheFunc = func;
            return this;
        }

        public IQuantizationConfiguration WithColorQuantizer(Func<IColorCache, IColorQuantizer> func)
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

            var colorCache = _cacheFunc?.Invoke();
            var colorQuantizer = _quantizerFunc?.Invoke(colorCache);
            var colorDitherer = _dithererFunc?.Invoke(_imageSize);

            if (colorQuantizer != null)
                colorQuantizer.TaskCount = _taskCount;
            if (colorDitherer != null)
                colorDitherer.TaskCount = _taskCount;

            return new Quantizer(colorQuantizer, colorDitherer);
        }
    }
}
