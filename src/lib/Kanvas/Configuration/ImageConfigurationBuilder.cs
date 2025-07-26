using Kanvas.Contract;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.Enums;
using Kanvas.DataClasses;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    public class ImageConfigurationBuilder : IIndexedImageConfigurationBuilder
    {
        private readonly ImageTranscoderOptions _options;

        private readonly EncodingConfigurationBuilder _encodingConfigurationBuilder;
        private readonly PaletteEncodingConfigurationBuilder _paletteEncodingConfigurationBuilder;
        private readonly ColorShaderConfigurationBuilder _colorShaderConfigurationBuilder;
        private readonly SizePaddingConfigurationBuilder _sizePaddingConfigurationBuilder;
        private readonly PixelRemappingConfigurationBuilder _pixelRemappingConfigurationBuilder;
        private QuantizationConfigurationBuilder? _quantizationConfigurationBuilder;

        public IEncodingConfigurationBuilder Transcode => _encodingConfigurationBuilder;

        public IPaletteEncodingConfigurationBuilder TranscodePalette => _paletteEncodingConfigurationBuilder;

        public IColorShaderConfigurationBuilder ShadeColors => _colorShaderConfigurationBuilder;

        public ISizePaddingConfigurationBuilder PadSize => _sizePaddingConfigurationBuilder;

        public IRemapPixelsConfigurationBuilder RemapPixels => _pixelRemappingConfigurationBuilder;

        public ImageConfigurationBuilder() : this(new ImageTranscoderOptions())
        {
        }

        private ImageConfigurationBuilder(ImageTranscoderOptions options)
        {
            _options = options;

            _encodingConfigurationBuilder = new EncodingConfigurationBuilder(this, options.EncodingOptions);
            _paletteEncodingConfigurationBuilder = new PaletteEncodingConfigurationBuilder(this, options.EncodingOptions);
            _sizePaddingConfigurationBuilder = new SizePaddingConfigurationBuilder(this, options.SizePaddingOptions);
            _pixelRemappingConfigurationBuilder = new PixelRemappingConfigurationBuilder(this, options.PixelRemappingOptions);
            _colorShaderConfigurationBuilder = new ColorShaderConfigurationBuilder(this, options.ColorShaderOptions);
        }

        public IImageConfigurationBuilder IsAnchoredAt(ImageAnchor anchor)
        {
            _options.ImageOptions.Anchor = anchor;
            return this;
        }

        public IImageConfigurationBuilder WithDegreeOfParallelism(int taskCount)
        {
            _options.ImageOptions.TaskCount = taskCount;
            return this;
        }

        public IImageConfigurationBuilder ConfigureQuantization(CreateQuantizationDelegate configure)
        {
            _options.QuantizationOptions ??= new QuantizationConfigurationOptions();
            _quantizationConfigurationBuilder ??= new QuantizationConfigurationBuilder(_options.QuantizationOptions);

            configure(_quantizationConfigurationBuilder);

            return this;
        }

        public IImageConfigurationBuilder WithoutQuantization()
        {
            _options.QuantizationOptions = null;
            _quantizationConfigurationBuilder = null;

            return this;
        }

        public IImageTranscoder Build() => new ImageTranscoder(_options);

        public IImageConfigurationBuilder Clone()
        {
            var options = new ImageTranscoderOptions
            {
                ImageOptions =
                {
                    TaskCount = _options.ImageOptions.TaskCount,
                    Anchor = _options.ImageOptions.Anchor
                },
                EncodingOptions =
                {
                    ColorEncoding = _options.EncodingOptions.ColorEncoding,
                    IndexEncoding = _options.EncodingOptions.IndexEncoding,
                    PaletteEncoding = _options.EncodingOptions.PaletteEncoding
                },
                SizePaddingOptions =
                {
                    WidthDelegate = _options.SizePaddingOptions.WidthDelegate,
                    HeightDelegate = _options.SizePaddingOptions.HeightDelegate
                },
                PixelRemappingOptions =
                {
                    PixelRemappingDelegate=_options.PixelRemappingOptions.PixelRemappingDelegate
                },
                ColorShaderOptions =
                {
                    ColorShaderDelegate = _options.ColorShaderOptions.ColorShaderDelegate
                }
            };

            if (_options.QuantizationOptions != null)
            {
                options.QuantizationOptions = new QuantizationConfigurationOptions
                {
                    TaskCount = _options.QuantizationOptions.TaskCount,
                    ColorCount = _options.QuantizationOptions.ColorCount,
                    PaletteDelegate = _options.QuantizationOptions.PaletteDelegate,
                    ColorCacheDelegate = _options.QuantizationOptions.ColorCacheDelegate,
                    ColorDithererDelegate = _options.QuantizationOptions.ColorDithererDelegate,
                    ColorQuantizerDelegate = _options.QuantizationOptions.ColorQuantizerDelegate
                };
            }

            return new ImageConfigurationBuilder(options);
        }
    }
}
