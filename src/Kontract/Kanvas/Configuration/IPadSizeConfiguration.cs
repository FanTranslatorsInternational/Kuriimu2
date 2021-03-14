using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public delegate Size CreatePaddedSize(Size imageSize);

    public delegate void ConfigurePadSizeOptions(IPadSizeOptions options);

    public interface IPadSizeConfiguration
    {
        IImageConfiguration With(ConfigurePadSizeOptions options);

        IImageConfiguration ToPowerOfTwo();

        IImageConfiguration ToMultiple(int multiple);
    }
}
