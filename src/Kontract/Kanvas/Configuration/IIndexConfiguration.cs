using System;
using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public interface IIndexConfiguration : IColorConfiguration
    {
        IIndexConfiguration WithPaletteEncoding(Func<IColorEncoding> func);

        IIndexConfiguration WithQuantization(Func<Size, IQuantizationConfiguration> func);

        IIndexTranscoder Build();
    }
}
