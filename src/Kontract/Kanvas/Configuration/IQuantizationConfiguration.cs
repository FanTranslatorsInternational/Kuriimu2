using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Quantization;

namespace Kontract.Kanvas.Configuration
{
    public interface IQuantizationConfiguration
    {
        IQuantizationConfiguration WithImageSize(Size size);

        IQuantizationConfiguration WithTaskCount(int taskCount);

        IQuantizationConfiguration WithColorCache(Func<IList<Color>, IColorCache> func);

        IQuantizationConfiguration WithPalette(Func<IList<Color>> func);

        IQuantizationConfiguration WithColorQuantizer(Func<IColorQuantizer> func);

        IQuantizationConfiguration WithColorDitherer(Func<Size, IColorDitherer> func);

        IQuantizer Build();
    }
}
