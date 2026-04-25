using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Contract.Encoding.Descriptor
{
    public interface IPixelDescriptor
    {
        string GetPixelName();

        int GetBitDepth();

        ColorChannelBitDepths GetColorChannelBitDepths();

        Rgba32 GetColor(long value);

        long GetValue(Rgba32 color);
    }
}
