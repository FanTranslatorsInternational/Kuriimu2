using Kontract.Kanvas.Model;

namespace Kontract.Kanvas.Configuration
{
    public delegate IImageSwizzle CreatePixelRemapper(SwizzlePreparationContext context);

    public interface IRemapPixelsConfiguration
    {
        IImageConfiguration With(CreatePixelRemapper func);
    }
}
