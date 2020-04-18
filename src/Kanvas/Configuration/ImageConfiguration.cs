using System;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    // TODO: PixelShader
    public class ImageConfiguration : IIndexConfiguration
    {
        private int _taskCount = Environment.ProcessorCount;

        private CreatePaddedSize _paddedSizeFunc;

        private CreateColorEncoding _colorFunc;

        private CreateIndexEncoding _indexFunc;
        private CreatePaletteEncoding _paletteFunc;

        private CreatePixelRemapper _swizzleFunc;

        private IQuantizationConfiguration _quantizationConfiguration;

        private bool IsIndexConfiguration => _indexFunc != null;

        public IImageConfiguration WithTaskCount(int taskCount)
        {
            _taskCount = taskCount;

            return this;
        }

        public IImageConfiguration PadSizeWith(CreatePaddedSize func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _paddedSizeFunc = func;
            return this;
        }

        public IImageConfiguration TranscodeWith(CreateColorEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _colorFunc = func;
            _indexFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodeWith(CreateIndexEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _indexFunc = func;
            _colorFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodePaletteWith(CreatePaletteEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _paletteFunc = func;

            return this;
        }

        public IImageConfiguration RemapPixelsWith(CreatePixelRemapper func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _swizzleFunc = func;

            return this;
        }

        public IImageConfiguration ConfigureQuantization(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));

            if (_quantizationConfiguration == null)
                _quantizationConfiguration = new QuantizationConfiguration();

            _quantizationConfiguration.ConfigureOptions(configure);

            return this;
        }

        public IImageConfiguration WithoutQuantization()
        {
            _quantizationConfiguration = null;

            return this;
        }

        public IImageConfiguration Clone()
        {
            var config = new ImageConfiguration();

            if (_taskCount != Environment.ProcessorCount)
                config.WithTaskCount(_taskCount);
            if (_paddedSizeFunc != null)
                config.PadSizeWith(_paddedSizeFunc);
            if (_swizzleFunc != null)
                config.RemapPixelsWith(_swizzleFunc);
            if (_quantizationConfiguration != null)
                config.SetQuantizationConfiguration(_quantizationConfiguration);

            if (_colorFunc != null)
                return config.TranscodeWith(_colorFunc);

            if (_indexFunc == null) 
                return config;

            var indexConfig = config.TranscodeWith(_indexFunc);
            return _paletteFunc != null ? 
                indexConfig.TranscodePaletteWith(_paletteFunc) : 
                config;
        }

        public IImageTranscoder Build()
        {
            return IsIndexConfiguration ? 
                BuildIndexInternal() : 
                BuildColorInternal();
        }

        private IImageTranscoder BuildColorInternal()
        {
            ContractAssertions.IsNotNull(_colorFunc, nameof(_colorFunc));

            // Quantization for normal images is optional
            // If no quantization configuration was done beforehand we assume no quantization to be used here
            var quantizer = _quantizationConfiguration != null ? BuildQuantizer() : null;

            return new ImageTranscoder(_colorFunc, _swizzleFunc, _paddedSizeFunc, quantizer, _taskCount);
        }

        private IImageTranscoder BuildIndexInternal()
        {
            ContractAssertions.IsNotNull(_indexFunc, nameof(_indexFunc));
            ContractAssertions.IsNotNull(_paletteFunc, nameof(_paletteFunc));

            var quantizer = BuildQuantizer();

            return new ImageTranscoder(_indexFunc, _paletteFunc, _swizzleFunc, _paddedSizeFunc, quantizer, _taskCount);
        }

        private IQuantizer BuildQuantizer()
        {
            var configuration = _quantizationConfiguration ?? new QuantizationConfiguration();

            configuration.WithTaskCount(_taskCount);
            return configuration.Build();
        }

        private void SetQuantizationConfiguration(IQuantizationConfiguration quantizationConfiguration)
        {
            _quantizationConfiguration = quantizationConfiguration.Clone();
        }
    }
}
