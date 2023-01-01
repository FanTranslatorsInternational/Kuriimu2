using Kontract.Kanvas.Models;

namespace Kontract.Kanvas.Interfaces.Configuration
{
    public delegate IImageSwizzle CreatePixelRemapper(SwizzlePreparationContext context);

    public interface IRemapPixelsConfiguration
    {
        IImageConfiguration With(CreatePixelRemapper func);
    }
}
