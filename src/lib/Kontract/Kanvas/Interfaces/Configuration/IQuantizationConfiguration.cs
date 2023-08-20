using System;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kontract.Kanvas.Interfaces.Configuration
{
    public interface IQuantizationConfiguration
    {
        IQuantizationConfiguration ConfigureOptions(Action<IQuantizationOptions> configure);

        IQuantizationConfiguration WithTaskCount(int taskCount);

        IQuantizer Build();

        IQuantizationConfiguration Clone();
    }
}
