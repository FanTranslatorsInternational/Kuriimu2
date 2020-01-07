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

        IQuantizationConfiguration WithColorCount(int colorCount);

        IQuantizationConfiguration WithColorCache(Func<IList<Color>, IColorCache> func);

        IQuantizationConfiguration WithPalette(Func<IList<Color>> func);

        IQuantizationConfiguration WithColorQuantizer(Func<int, int, IColorQuantizer> func);

        IQuantizationConfiguration WithColorDitherer(Func<Size, int, IColorDitherer> func);

        IQuantizer Build();
    }
}
