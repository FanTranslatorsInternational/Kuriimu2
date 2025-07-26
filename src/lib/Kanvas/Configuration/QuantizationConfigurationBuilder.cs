using Kanvas.Contract.Configuration;
using Kanvas.Contract.Quantization;
using Kanvas.DataClasses.Configuration;
using Kanvas.Quantization;

namespace Kanvas.Configuration
{
    internal class QuantizationConfigurationBuilder : IQuantizationConfigurationBuilder
    {
        private readonly QuantizationConfigurationOptions _options;

        public QuantizationConfigurationBuilder(QuantizationConfigurationOptions options)
        {
            _options = options;
        }

        public IQuantizationConfigurationBuilder WithDegreeOfParallelism(int taskCount)
        {
            _options.TaskCount = taskCount;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorCount(int colorCount)
        {
            _options.ColorCount = colorCount;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorCache(CreateColorCacheDelegate cacheDelegate)
        {
            _options.ColorCacheDelegate = cacheDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithPalette(CreatePaletteDelegate paletteDelegate)
        {
            _options.PaletteDelegate = paletteDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorQuantizer(CreateColorQuantizerDelegate quantizerDelegate)
        {
            _options.ColorQuantizerDelegate = quantizerDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorDitherer(CreateColorDithererDelegate dithererDelegate)
        {
            _options.ColorDithererDelegate = dithererDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder Clone()
        {
            var options = new QuantizationConfigurationOptions
            {
                TaskCount = _options.TaskCount,
                ColorCount = _options.ColorCount,
                PaletteDelegate = _options.PaletteDelegate,
                ColorCacheDelegate = _options.ColorCacheDelegate,
                ColorDithererDelegate = _options.ColorDithererDelegate,
                ColorQuantizerDelegate = _options.ColorQuantizerDelegate
            };

            return new QuantizationConfigurationBuilder(options);
        }
    }
}
