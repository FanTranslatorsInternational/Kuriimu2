using System.Drawing;

namespace Kontract.Kanvas
{
    public interface IColorTranscoder
    {
        Image Decode(byte[] data);

        byte[] Encode(Bitmap image);
    }
}
