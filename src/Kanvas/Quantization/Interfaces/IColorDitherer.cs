using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to quantize and dither a collection of colors.
    /// </summary>
    public interface IColorDitherer
    {
        /// <summary>
        /// Prepares the ditherer with a quantizer.
        /// </summary>
        /// <param name="quantizer">The quantizer.</param>
        /// <param name="width">Width of the original image.</param>
        /// <param name="height">Height of the original image.</param>
        void Prepare(IColorQuantizer quantizer, int width, int height);

        /// <summary>
        /// Quantizes and dithers a collection of colors.
        /// </summary>
        /// <param name="colors">The collection to quantize and dither.</param>
        /// <returns></returns>
        IEnumerable<int> Process(IEnumerable<Color> colors);
    }
}
