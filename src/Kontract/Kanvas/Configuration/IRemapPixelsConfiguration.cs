namespace Kontract.Kanvas.Configuration
{
    public delegate IImageSwizzle CreatePixelRemapper();

    public interface IRemapPixelsConfiguration
    {
        IImageConfiguration With(CreatePixelRemapper func);
    }
}
