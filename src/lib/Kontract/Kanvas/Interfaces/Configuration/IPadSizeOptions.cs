using System.Drawing;

namespace Kontract.Kanvas.Interfaces.Configuration
{
    public interface IPadSizeOptionsBuild : IPadSizeOptions
    {
        Size Build(Size imageSize);
    }

    public interface IPadSizeOptions
    {
        IPadSizeDimensionConfiguration Width { get; }

        IPadSizeDimensionConfiguration Height { get; }

        void To(CreatePaddedSize func);
    }
}
