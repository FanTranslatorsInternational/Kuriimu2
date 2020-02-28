using System;
using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public delegate IImageSwizzle CreatePixelRemapper(Size imageSize);
    public delegate IColorEncoding CreateColorEncoding(Size imageSize);
    public delegate IColorIndexEncoding CreateColorIndexEncoding(Size imageSize);

    public interface IImageConfiguration
    {
        IImageConfiguration WithTaskCount(int taskCount);

        IImageConfiguration RemapPixelsWith(CreatePixelRemapper func);

        IImageConfiguration QuantizeWith(Action<IQuantizationOptions> configure);

        IImageConfiguration WithoutQuantization();

        IColorConfiguration TranscodeWith(CreateColorEncoding func);

        IIndexConfiguration TranscodeWith(CreateColorIndexEncoding func);

        IImageConfiguration Clone();
    }
}
