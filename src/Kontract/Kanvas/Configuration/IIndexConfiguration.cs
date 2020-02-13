using System;
using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public interface IIndexConfiguration : IImageConfiguration
    {
        IIndexConfiguration TranscodePaletteWith(Func<IColorEncoding> func);

        IIndexTranscoder Build();
    }
}
