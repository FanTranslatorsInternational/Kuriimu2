using Kanvas.Contract.Quantization.ColorCache;
using Kanvas.Contract.Quantization.ColorDitherer;
using Kanvas.Contract.Quantization.ColorQuantizer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Contract.Configuration
{
    public delegate IColorCache CreateColorCacheDelegate(IList<Rgba32> palette);
    public delegate IList<Rgba32> CreatePaletteDelegate();
    public delegate IColorQuantizer CreateColorQuantizerDelegate(int colorCount, int taskCount);
    public delegate IColorDitherer CreateColorDithererDelegate(Size imageSize, int taskCount);

    public delegate IQuantizationConfigurationBuilder CreateQuantizationDelegate(IQuantizationConfigurationBuilder options);

    public interface IQuantizationConfigurationBuilder
    {
        IQuantizationConfigurationBuilder WithDegreeOfParallelism(int taskCount);
        IQuantizationConfigurationBuilder WithColorCount(int colorCount);

        IQuantizationConfigurationBuilder WithColorCache(CreateColorCacheDelegate cacheDelegate);

        IQuantizationConfigurationBuilder WithPalette(CreatePaletteDelegate paletteDelegate);

        IQuantizationConfigurationBuilder WithColorQuantizer(CreateColorQuantizerDelegate quantizerDelegate);

        IQuantizationConfigurationBuilder WithColorDitherer(CreateColorDithererDelegate dithererDelegate);

        IQuantizationConfigurationBuilder Clone();
    }
}
