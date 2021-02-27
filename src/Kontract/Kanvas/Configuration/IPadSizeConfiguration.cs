using System.Drawing;

namespace Kontract.Kanvas.Configuration
{
    public delegate Size CreatePaddedSize(Size imageSize);

    public interface IPadSizeConfiguration
    {
        IImageConfiguration With(CreatePaddedSize func);

        IImageConfiguration ToPowerOfTwo();

        IImageConfiguration ToMultiple(int multiple);
    }
}
