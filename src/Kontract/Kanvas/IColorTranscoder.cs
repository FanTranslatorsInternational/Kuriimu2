using System.Drawing;

namespace Kontract.Kanvas
{
    public interface IColorTranscoder
    {
        Image Decode(byte[] data, Size imageSize);
        Image Decode(byte[] data, Size imageSize, Size paddedSize);

        byte[] Encode(Bitmap image);
        byte[] Encode(Bitmap image, Size paddedSize);
    }
}
