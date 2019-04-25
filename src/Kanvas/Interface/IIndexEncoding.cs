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
    /// Describes methods to define an index based color encoding.
    /// </summary>
    public interface IIndexEncoding
    {
        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <param name="paletteData">Palette data to decode.</param>
        /// <returns>Decoded collection of indices and palette.</returns>
        (IEnumerable<IndexData> indices, IList<Color> palette) Load(byte[] input, byte[] paletteData);

        /// <summary>
        /// Composes indices and a palette to a collection of colors.
        /// </summary>
        /// <param name="indices"><see cref="IndexData"/> to compose.</param>
        /// <param name="palette">Palette of colors to compose.</param>
        /// <returns>Composed collection of colors.</returns>
        IEnumerable<Color> Compose(IEnumerable<IndexData> indices, IList<Color> palette);

        /// <summary>
        /// Decomposes a collection of colors.
        /// </summary>
        /// <param name="colors">Collection of colors.</param>
        /// <returns>Decomposed collection of indices and palette.</returns>
        (IEnumerable<IndexData> indices, IList<Color> palette) Decompose(IEnumerable<Color> colors);

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">Collection of colors.</param>
        /// <returns>Quantized collection of indices and palette.</returns>
        (IEnumerable<IndexData> indices, IList<Color> palette) Quantize(IEnumerable<Color> colors);

        /// <summary>
        /// Encodes a collection of colors.
        /// </summary>
        /// <param name="indices">List of colors to encode.</param>
        /// <param name="palette">Palette of colors to encode.</param>
        /// <returns>Encoded indices and palette data.</returns>
        (byte[] indexData, byte[] paletteData) Save(IEnumerable<IndexData> indices, IList<Color> palette);
    }
}
