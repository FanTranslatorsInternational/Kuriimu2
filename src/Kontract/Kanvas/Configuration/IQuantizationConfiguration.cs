using System;
using System.Drawing;
using Kontract.Kanvas.Quantization;

namespace Kontract.Kanvas.Configuration
{
    public interface IQuantizationConfiguration
    {
        IQuantizationConfiguration WithImageSize(Size size);

        IQuantizationConfiguration WithTaskCount(int taskCount);

        IQuantizationConfiguration WithColorCache(Func<IColorCache> func);

        IQuantizationConfiguration WithColorQuantizer(Func<IColorCache, IColorQuantizer> func);

        IQuantizationConfiguration WithColorDitherer(Func<Size, IColorDitherer> func);

        IQuantizer Build();
    }
}
