using System.Drawing;
using Kontract.Interfaces.Progress;

namespace Kontract.Kanvas
{
    public interface IColorTranscoder
    {
        Image Decode(byte[] data, Size imageSize, IProgressContext progress = null);
        Image Decode(byte[] data, Size imageSize, Size paddedSize, IProgressContext progress = null);

        byte[] Encode(Bitmap image, IProgressContext progress = null);
        byte[] Encode(Bitmap image, Size paddedSize, IProgressContext progress = null);
    }
}
