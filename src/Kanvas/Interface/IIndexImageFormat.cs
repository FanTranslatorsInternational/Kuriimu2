using Kanvas.Models;
using Kanvas.Quantization.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Interface
{
    /// <summary>
    /// Describes methods to define an index based image format.
    /// </summary>
    public interface IIndexImageFormat
    {
        /// <summary>
        /// Decodes image data to a list of colors
        /// </summary>
        /// <param name="input">Image data to decode</param>
        /// <param name="paletteData"></param>
        /// <returns>Decoded list of colors</returns>
        (IEnumerable<IndexData> indeces, IList<Color> palette) Load(byte[] input, byte[] paletteData);

        /// <summary>
        /// Composes indeces and a palette to a collection of colors.
        /// </summary>
        /// <param name="indeces"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        IEnumerable<Color> Compose(IEnumerable<IndexData> indeces, IList<Color> palette);

        /// <summary>
        /// Decomposes a collection of colors.
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        (IEnumerable<IndexData> indeces, IList<Color> palette) Decompose(IEnumerable<Color> colors);

        /// <summary>
        /// Quantizes a collection of colors
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        (IEnumerable<IndexData> indeces, IList<Color> palette) Quantize(IEnumerable<Color> colors);

        /// <summary>
        /// Encodes a list of colors
        /// </summary>
        /// <param name="colors">List of colors to encode</param>
        /// <returns>Encoded data</returns>
        (byte[] indexData, byte[] paletteData) Save(IEnumerable<IndexData> indeces, IList<Color> palette);
    }
}
