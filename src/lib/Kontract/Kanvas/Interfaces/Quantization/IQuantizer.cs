using System.Collections.Generic;
using System.Drawing;
using Kontract.Interfaces.Progress;

namespace Kontract.Kanvas.Interfaces.Quantization
{
    public interface IQuantizer
    {
        Image ProcessImage(Bitmap image, IProgressContext progress = null);

        (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors, Size imageSize, IProgressContext progress = null);
    }
}
