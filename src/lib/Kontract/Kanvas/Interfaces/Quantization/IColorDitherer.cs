﻿using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas.Interfaces.Quantization
{
    /// <summary>
    /// Describes methods to quantize and dither a collection of colors.
    /// </summary>
    public interface IColorDitherer
    {
        /// <summary>
        /// Quantizes and dithers a collection of colors.
        /// </summary>
        /// <param name="colors">The collection to quantize and dither.</param>
        /// <param name="colorCache"></param>
        /// <returns></returns>
        IEnumerable<int> Process(IEnumerable<Color> colors, IColorCache colorCache);
    }
}
