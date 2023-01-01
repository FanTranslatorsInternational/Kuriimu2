using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kontract.Kanvas.Interfaces.Configuration
{
    public delegate IColorCache CreateColorCacheDelegate(IList<Color> palette);
    public delegate IList<Color> CreatePaletteDelegate();
    public delegate IColorQuantizer CreateColorQuantizerDelegate(int colorCount, int taskCount);
    public delegate IColorDitherer CreateColorDithererDelegate(Size imageSize, int taskCount);

    public interface IQuantizationOptions
    {
        IQuantizationOptions WithColorCount(int colorCount);

        IQuantizationOptions WithColorCache(CreateColorCacheDelegate func);

        IQuantizationOptions WithPalette(CreatePaletteDelegate func);

        IQuantizationOptions WithColorQuantizer(CreateColorQuantizerDelegate func);

        IQuantizationOptions WithColorDitherer(CreateColorDithererDelegate func);
    }
}
