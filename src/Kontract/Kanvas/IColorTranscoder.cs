using System.Drawing;
using Kontract.Interfaces.Progress;

namespace Kontract.Kanvas
{
    public interface IColorTranscoder
    {
        Bitmap Decode(byte[] data, Size imageSize, IProgressContext progress = null);

        byte[] Encode(Bitmap image, IProgressContext progress = null);
    }
}
