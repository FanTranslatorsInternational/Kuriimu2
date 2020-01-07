using System;
using System.Drawing;
using Kanvas.Quantization.Ditherers.ErrorDiffusion;
using Kanvas.Quantization.Quantizers;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;

namespace Kanvas.Configuration
{
    // TODO: PixelShader
    public class ImageConfiguration : IImageConfiguration, IIndexConfiguration
    {
        private readonly IQuantizationConfiguration _defaultQuantizationConfig =
            new QuantizationConfiguration()
                .WithColorQuantizer((colorCount, taskCount) => new WuColorQuantizer(4, 4, colorCount))
                .WithColorDitherer((imageSize, taskCount) => new FloydSteinbergDitherer(imageSize.Width, imageSize.Height, taskCount));

        private Size _imageSize;
        private Size _paddedSize;

        private Func<Size, IColorEncoding> _colorFunc;
        private Func<Size, IImageSwizzle> _swizzleFunc;
        private Func<Size, IColorIndexEncoding> _indexFunc;
        private Func<IColorEncoding> _paletteFunc;
        private Func<Size, IQuantizationConfiguration> _quantFunc;

        public IImageConfiguration WithImageSize(Size size)
        {
            _imageSize = size;
            return this;
        }

        public IImageConfiguration WithPaddedImageSize(Size size)
        {
            _paddedSize = size;
            return this;
        }

        public IColorConfiguration TranscodeWith(Func<Size, IColorEncoding> func)
        {
            _colorFunc = func;
            _indexFunc = null;
            return this;
        }

        public IIndexConfiguration TranscodeWith(Func<Size, IColorIndexEncoding> func)
        {
            _indexFunc = func;
            _colorFunc = null;
            return this;
        }

        public IImageConfiguration WithSwizzle(Func<Size, IImageSwizzle> func)
        {
            _swizzleFunc = func;
            return this;
        }

        IIndexConfiguration IIndexConfiguration.WithPaletteEncoding(Func<IColorEncoding> func)
        {
            _paletteFunc = func;
            return this;
        }

        IIndexConfiguration IIndexConfiguration.WithQuantization(Func<Size, IQuantizationConfiguration> func)
        {
            _quantFunc = func;
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
            var quantizationConfig = _quantFunc?.Invoke(imageSize) ?? _defaultQuantizationConfig.WithImageSize(imageSize);

            return new Transcoder(_imageSize, _paddedSize, indexEncoding, paletteEncoding, quantizationConfig, swizzle);
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

            return new Transcoder(_imageSize, _paddedSize, colorEncoding, swizzle);
        }
    }
}
