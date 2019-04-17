using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    public interface IColorDitherer
    {
        (IEnumerable<int> indeces, IList<Color> palette) Process(Bitmap image);
    }
}
