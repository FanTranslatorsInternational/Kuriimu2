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
        Image Decode(byte[] indexData, byte[] paletteData);

        (byte[] indexData, byte[] paletteData) Encode(Bitmap image);
    }
}
