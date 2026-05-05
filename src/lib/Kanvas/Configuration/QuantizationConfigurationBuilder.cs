using Kanvas.Contract.Configuration;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    internal class QuantizationConfigurationBuilder(QuantizationConfigurationOptions options)
        : IQuantizationConfigurationBuilder
    {
        public IQuantizationConfigurationBuilder WithDegreeOfParallelism(int taskCount)
        {
            options.TaskCount = taskCount;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorCount(int colorCount)
        {
            options.ColorCount = colorCount;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorCache(CreateColorCacheDelegate cacheDelegate)
        {
            options.ColorCacheDelegate = cacheDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithPalette(CreatePaletteDelegate paletteDelegate)
        {
            options.PaletteDelegate = paletteDelegate;
            options.InitialPaletteDelegate = null;
            return this;
        }

        public IQuantizationConfigurationBuilder WithInitialPalette(CreateInitialPaletteDelegate initialPaletteDelegate)
        {
            options.PaletteDelegate = null;
            options.InitialPaletteDelegate = initialPaletteDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder OrderPalette(OrderPaletteDelegate orderPaletteDelegate)
        {
            options.OrderPaletteDelegate = orderPaletteDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorQuantizer(CreateColorQuantizerDelegate quantizerDelegate)
        {
            options.ColorQuantizerDelegate = quantizerDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder WithColorDitherer(CreateColorDithererDelegate dithererDelegate)
        {
            options.ColorDithererDelegate = dithererDelegate;
            return this;
        }

        public IQuantizationConfigurationBuilder Clone()
        {
            var options1 = new QuantizationConfigurationOptions
            {
                TaskCount = options.TaskCount,
                ColorCount = options.ColorCount,
                ColorChannelBitDepths = options.ColorChannelBitDepths,
                PaletteDelegate = options.PaletteDelegate,
                InitialPaletteDelegate = options.InitialPaletteDelegate,
                ColorCacheDelegate = options.ColorCacheDelegate,
                ColorDithererDelegate = options.ColorDithererDelegate,
                ColorQuantizerDelegate = options.ColorQuantizerDelegate
            };

            return new QuantizationConfigurationBuilder(options1);
        }
    }
}
