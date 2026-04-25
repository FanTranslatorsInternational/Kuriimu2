using Kanvas.Contract.Configuration;
using Kanvas.Contract.DataClasses;
using Kanvas.Quantization.ColorCache;
using Kanvas.Quantization.ColorQuantizer;

namespace Kanvas.DataClasses.Configuration
{
    internal class QuantizationConfigurationOptions
    {
        public int TaskCount { get; set; } = Environment.ProcessorCount;
        public int ColorCount { get; set; } = -1;
        public ColorChannelBitDepths ColorChannelBitDepths { get; set; } = ColorChannelBitDepths.Unknown;
        public CreatePaletteDelegate? PaletteDelegate { get; set; }
        public CreateInitialPaletteDelegate? InitialPaletteDelegate { get; set; }
        public OrderPaletteDelegate? OrderPaletteDelegate { get; set; }
        public CreateColorQuantizerDelegate ColorQuantizerDelegate { get; set; } = (colorCount, _, colorChannelBitDepths) => new WuColorQuantizer(colorChannelBitDepths, colorCount);
        public CreateColorCacheDelegate ColorCacheDelegate { get; set; } = palette => new EuclideanDistanceColorCache(palette);
        public CreateColorDithererDelegate? ColorDithererDelegate { get; set; }
    }
}
