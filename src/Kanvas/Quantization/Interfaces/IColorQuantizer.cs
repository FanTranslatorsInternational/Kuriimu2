using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to quantize an image.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Quantizes an images.
        /// </summary>
        /// <param name="image">The image to quantize.</param>
        (IEnumerable<int> indeces, IList<Color> palette) Process(Bitmap image);

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">The collection of colors to quantize.</param>
        (IEnumerable<int> indeces, IList<Color> palette) Process(IEnumerable<Color> colors);
    }
}
