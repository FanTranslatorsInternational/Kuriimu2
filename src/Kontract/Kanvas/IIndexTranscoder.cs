using System.Drawing;
using Kontract.Interfaces.Progress;

namespace Kontract.Kanvas
{
    public interface IIndexTranscoder
    {
        Image Decode(byte[] indexData, byte[] paletteData, Size imageSize, IProgressContext progress = null);
        Image Decode(byte[] indexData, byte[] paletteData, Size imageSize, Size paddedSize, IProgressContext progress = null);

        (byte[] indexData, byte[] paletteData) Encode(Bitmap image, IProgressContext progress = null);
        (byte[] indexData, byte[] paletteData) Encode(Bitmap image, Size paddedSize, IProgressContext progress = null);
    }
}
