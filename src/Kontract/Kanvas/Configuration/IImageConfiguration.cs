using System;
using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public interface IImageConfiguration
    {
        IImageConfiguration WithTaskCount(int taskCount);

        IImageConfiguration HasImageSize(Size size);

        IImageConfiguration HasPaddedImageSize(Size size);

        IImageConfiguration RemapPixelsWith(Func<Size, IImageSwizzle> func);

        IImageConfiguration QuantizeWith(Action<IQuantizationOptions> configure);

        IColorConfiguration TranscodeWith(Func<Size, IColorEncoding> func);

        IIndexConfiguration TranscodeWith(Func<Size, IColorIndexEncoding> func);
    }
}
