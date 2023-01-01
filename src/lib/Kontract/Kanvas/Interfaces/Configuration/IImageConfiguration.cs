using System;
using Kontract.Kanvas.Models;

namespace Kontract.Kanvas.Interfaces.Configuration
{
    public interface IImageConfiguration
    {
        ITranscodeConfiguration Transcode { get; }

        IPadSizeConfiguration PadSize { get; }

        IRemapPixelsConfiguration RemapPixels { get; }

        IImageConfiguration IsAnchoredAt(ImageAnchor anchor);

        IImageConfiguration WithDegreeOfParallelism(int taskCount);

        IImageConfiguration ConfigureQuantization(Action<IQuantizationOptions> configure);

        IImageConfiguration WithoutQuantization();

        IImageTranscoder Build();

        IImageConfiguration Clone();
    }
}
