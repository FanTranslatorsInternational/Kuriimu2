using System;
using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public delegate Size CreatePaddedSize(Size imageSize);
    public delegate IImageSwizzle CreatePixelRemapper(Size imageSize);
    public delegate IColorEncoding CreateColorEncoding(Size imageSize);
    public delegate IIndexEncoding CreateIndexEncoding(Size imageSize);

    public interface IImageConfiguration
    {
        IImageConfiguration WithTaskCount(int taskCount);

        IImageConfiguration PadSizeWith(CreatePaddedSize func);

        IImageConfiguration RemapPixelsWith(CreatePixelRemapper func);

        IImageConfiguration ConfigureQuantization(Action<IQuantizationOptions> configure);

        IImageConfiguration WithoutQuantization();

        IImageConfiguration TranscodeWith(CreateColorEncoding func);

        IIndexConfiguration TranscodeWith(CreateIndexEncoding func);

        IImageTranscoder Build();

        IImageConfiguration Clone();
    }
}
