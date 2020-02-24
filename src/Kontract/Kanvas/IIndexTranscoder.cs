using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Kanvas
{
    public interface IIndexTranscoder
    {
        Image Decode(byte[] indexData, byte[] paletteData, Size imageSize);
        Image Decode(byte[] indexData, byte[] paletteData, Size imageSize, Size paddedSize);

        (byte[] indexData, byte[] paletteData) Encode(Bitmap image);
        (byte[] indexData, byte[] paletteData) Encode(Bitmap image, Size paddedSize);
    }
}
