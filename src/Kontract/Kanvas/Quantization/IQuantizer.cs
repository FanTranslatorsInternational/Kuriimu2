using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas.Quantization
{
    public interface IQuantizer
    {
        (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors, Size imageSize);

        Image ProcessImage(Bitmap image);
    }
}
