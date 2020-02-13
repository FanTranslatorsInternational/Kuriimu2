using System;
using System.Drawing;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    // TODO: PixelShader
    public class ImageConfiguration : IColorConfiguration, IIndexConfiguration
    {
        private Size _imageSize;
        private Size _paddedSize;

        private int _taskCount = Environment.ProcessorCount;

        private Func<Size, IColorEncoding> _colorFunc;

        private Func<Size, IColorIndexEncoding> _indexFunc;
        private Func<IColorEncoding> _paletteFunc;

        private Func<Size, IImageSwizzle> _swizzleFunc;

        private Action<IQuantizationOptions> _quantizationAction;

        public IImageConfiguration WithTaskCount(int taskCount)
        {
            _taskCount = taskCount;

            return this;
        }

        public IImageConfiguration HasImageSize(Size size)
        {
            if (size == Size.Empty)
                throw new InvalidOperationException("Image size cannot be empty.");

            _imageSize = size;

            return this;
        }

        public IImageConfiguration HasPaddedImageSize(Size size)
        {
            if (size == Size.Empty)
                throw new InvalidOperationException("Padded image size cannot be empty.");

            _paddedSize = size;

            return this;
        }

        public IColorConfiguration TranscodeWith(Func<Size, IColorEncoding> func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _colorFunc = func;
            _indexFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodeWith(Func<Size, IColorIndexEncoding> func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _indexFunc = func;
            _colorFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodePaletteWith(Func<IColorEncoding> func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _paletteFunc = func;

            return this;
        }

        public IImageConfiguration RemapPixelsWith(Func<Size, IImageSwizzle> func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _swizzleFunc = func;

            return this;
        }

        public IImageConfiguration QuantizeWith(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));

            _quantizationAction = configure;

            return this;
        }

        IIndexTranscoder IIndexConfiguration.Build()
        {
            if (_imageSize == Size.Empty)
                throw new ArgumentException("imageSize");

            ContractAssertions.IsNotNull(_indexFunc, "indexFunc");
            ContractAssertions.IsNotNull(_paletteFunc, "paletteFunc");

            var imageSize = _paddedSize == Size.Empty ? _imageSize : _paddedSize;

            var swizzle = _swizzleFunc?.Invoke(imageSize);

            var indexEncoding = _indexFunc(imageSize);
            var paletteEncoding = _paletteFunc();

            var quantizer = BuildQuantizer();

            return new Transcoder(_imageSize, _paddedSize, indexEncoding, paletteEncoding, quantizer, swizzle);
        }

        IColorTranscoder IColorConfiguration.Build()
        {
            if (_imageSize == Size.Empty)
                throw new InvalidOperationException("Image size needs to be set.");

            ContractAssertions.IsNotNull(_colorFunc, "colorFunc");

            var imageSize = _paddedSize == Size.Empty ? _imageSize : _paddedSize;

            var swizzle = _swizzleFunc?.Invoke(imageSize);

            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            var colorEncoding = _colorFunc(_imageSize);

            // Quantization for normal images is optional
            // If no quantization configuration was done beforehand we assume no quantization to be used here
            var quantizer = _quantizationAction == null ? null : BuildQuantizer();

            return new Transcoder(_imageSize, _paddedSize, colorEncoding, quantizer, swizzle);
        }

        IQuantizer BuildQuantizer()
        {
            var quantizationConfiguration = new QuantizationConfiguration();
            if (_quantizationAction != null)
                quantizationConfiguration.WithOptions(_quantizationAction);

            quantizationConfiguration.WithTaskCount(_taskCount);

            return quantizationConfiguration.Build();
        }
    }
}
