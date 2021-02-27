using System;

namespace Kontract.Kanvas.Configuration
{
    public interface IImageConfiguration
    {
        public ITranscodeConfiguration Transcode { get; }

        public IPadSizeConfiguration PadSize { get; }

        public IRemapPixelsConfiguration RemapPixels { get; }

        IImageConfiguration WithDegreeOfParallelism(int taskCount);

        IImageConfiguration ConfigureQuantization(Action<IQuantizationOptions> configure);

        IImageConfiguration WithoutQuantization();

        IImageTranscoder Build();

        IImageConfiguration Clone();
    }
}
